using WinKeyExtract.Models;

namespace WinKeyExtract.Output;

/// <summary>
/// Pretty-prints Windows key and license information to the console.
/// </summary>
public static class ConsoleFormatter
{
    private const int LabelWidth = 22;

    public static void Print(WindowsKeyInfo info)
    {
        Console.WriteLine();
        PrintHeader(info.IsFromExternalHive
            ? $"WinKeyExtract — External Hive: {info.HivePath}"
            : "WinKeyExtract — Local System");
        Console.WriteLine();

        // System info
        PrintSection("System Information");
        PrintField("Edition", info.Edition ?? info.WmiEdition);
        PrintField("Edition ID", info.EditionId);
        if (!string.IsNullOrEmpty(info.CompositionEditionId) &&
            !string.Equals(info.CompositionEditionId, info.EditionId, StringComparison.OrdinalIgnoreCase))
        {
            PrintField("Composition Edition", info.CompositionEditionId);
        }
        PrintField("Installation Type", info.InstallationType);
        PrintField("Build", info.BuildNumber);
        PrintField("Product ID", info.ProductId);
        if (!string.IsNullOrEmpty(info.RegisteredOwner))
            PrintField("Registered Owner", info.RegisteredOwner);
        Console.WriteLine();

        // Installed key
        PrintSection("Installed Key");
        if (!string.IsNullOrEmpty(info.InstalledKey))
        {
            PrintField("Product Key", info.InstalledKey);
            PrintField("Key Source", info.InstalledKeySource);
        }
        else
        {
            PrintField("Product Key", "(not found)");
        }

        // Legacy key comparison
        if (!string.IsNullOrEmpty(info.LegacyKey))
        {
            Console.WriteLine();
            PrintSection("Legacy Key (DigitalProductId)");
            PrintField("Legacy Key", info.LegacyKey);
            PrintFieldColored("Differs from DPID4", "Yes — edition upgrade or key change detected", ConsoleColor.Yellow);
        }

        Console.WriteLine();

        // OEM key (only for local)
        if (!info.IsFromExternalHive)
        {
            PrintSection("OEM Key (BIOS)");
            PrintField("OEM Key", string.IsNullOrEmpty(info.OemKey)
                ? "(not available)"
                : info.OemKey);
            Console.WriteLine();
        }

        // License info (only for local)
        if (!info.IsFromExternalHive)
        {
            PrintSection("Windows License");
            PrintField("Status", info.LicenseStatus.ToDisplayString());
            PrintField("Channel", info.LicenseChannel);
            PrintField("Partial Key", info.PartialProductKey);

            if (info.PartialKeyMatch.HasValue)
            {
                string match = info.PartialKeyMatch.Value ? "Yes" : "NO — mismatch!";
                PrintField("Key Validated", match);
            }

            Console.WriteLine();
        }

        // Other product licenses (Office, etc.)
        if (info.AllLicenses.Count > 0)
        {
            PrintSection("Other Product Licenses");
            for (int i = 0; i < info.AllLicenses.Count; i++)
            {
                var lic = info.AllLicenses[i];
                if (i > 0)
                    Console.WriteLine();
                PrintField("Product", lic.Name ?? lic.Description);
                PrintField("Status", lic.LicenseStatus.ToDisplayString());
                PrintField("Channel", lic.Channel);
                PrintField("Partial Key", lic.PartialProductKey);
            }
            Console.WriteLine();
        }

        // Office Click-to-Run
        if (info.OfficeC2R is not null)
        {
            PrintSection("Office Click-to-Run");
            PrintField("Products", info.OfficeC2R.ProductReleaseIds);
            PrintField("Platform", info.OfficeC2R.Platform);
            PrintField("Version", info.OfficeC2R.Version);
            Console.WriteLine();
        }

        // Previous OS installations (upgrade history)
        if (info.PreviousInstallations.Count > 0)
        {
            PrintSection("Upgrade History");
            for (int i = 0; i < info.PreviousInstallations.Count; i++)
            {
                var prev = info.PreviousInstallations[i];
                if (i > 0)
                    Console.WriteLine();

                // Extract date from label like "Source OS (Updated on 1/17/2020 23:48:47)"
                string label = prev.SourceLabel ?? "";
                int parenStart = label.IndexOf('(');
                int parenEnd = label.IndexOf(')');
                string date = parenStart >= 0 && parenEnd > parenStart
                    ? label[(parenStart + 1)..parenEnd].Replace("Updated on ", "")
                    : "";

                PrintField("Previous Edition", prev.ProductName);
                PrintField("Edition ID", prev.EditionId);
                PrintField("Build", prev.CurrentBuild);
                if (!string.IsNullOrEmpty(prev.ProductKey))
                    PrintField("Product Key", prev.ProductKey);
                if (!string.IsNullOrEmpty(date))
                    PrintField("Upgraded On", date);
            }
            Console.WriteLine();
        }
    }

    public static void PrintHelp()
    {
        Console.WriteLine("""

        WinKeyExtract — Windows Product Key Extraction Tool

        Usage:
          WinKeyExtract                       Show local keys + license info
          WinKeyExtract --hive <path>         Extract key from external SOFTWARE hive
          WinKeyExtract --help                Show this help message

        Examples:
          WinKeyExtract
          WinKeyExtract --hive D:\Windows\System32\config\SOFTWARE
          WinKeyExtract --hive "C:\Backup\SOFTWARE"

        Notes:
          - Must be run as administrator for local key reading
          - External hive loading requires administrator privileges
          - The tool reads DigitalProductId / DigitalProductId4 from the registry
          - OEM keys are read from BIOS via WMI (local only)
          - Also enumerates Office and other Microsoft product licenses

        """);
    }

    public static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"Error: {message}");
        Console.ResetColor();
    }

    public static void PrintWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  Warning: {message}");
        Console.ResetColor();
    }

    private static void PrintHeader(string title)
    {
        string separator = new('═', Math.Max(title.Length + 4, 50));
        int innerWidth = separator.Length - 4;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  {separator}");
        Console.WriteLine($"  ║ {title.PadRight(innerWidth)} ║");
        Console.WriteLine($"  {separator}");
        Console.ResetColor();
    }

    private static void PrintSection(string title)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  [{title}]");
        Console.ResetColor();
    }

    private static void PrintField(string label, string? value)
    {
        Console.Write($"  {label.PadRight(LabelWidth)} : ");

        if (string.IsNullOrEmpty(value))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("(unknown)");
            Console.ResetColor();
        }
        else if (value.StartsWith("(not"))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(value);
            Console.ResetColor();
        }
        else if (value == "NO — mismatch!")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(value);
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(value);
            Console.ResetColor();
        }
    }

    private static void PrintFieldColored(string label, string value, ConsoleColor color)
    {
        Console.Write($"  {label.PadRight(LabelWidth)} : ");
        Console.ForegroundColor = color;
        Console.WriteLine(value);
        Console.ResetColor();
    }
}
