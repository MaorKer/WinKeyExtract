using WinKeyExtract;
using WinKeyExtract.Models;
using WinKeyExtract.Output;

int exitCode = Run(args);
WaitIfInteractive();
return exitCode;

static int Run(string[] args)
{
    try
    {
        if (args.Length > 0 && args[0] is "--help" or "-h" or "/?" or "help")
        {
            ConsoleFormatter.PrintHelp();
            return 0;
        }

        string? hivePath = ParseHivePath(args);

        if (hivePath is not null)
        {
            return RunExternalHive(hivePath);
        }

        return RunLocal();
    }
    catch (Exception ex)
    {
        ConsoleFormatter.PrintError(ex.Message);
        return 1;
    }
}

/// <summary>
/// If launched by double-click (no parent console), wait for a keypress so the window stays open.
/// When run from an existing terminal (cmd, PowerShell), exit immediately.
/// </summary>
static void WaitIfInteractive()
{
    try
    {
        // Console.IsInputRedirected is true when piped; CursorLeft throws if no console.
        // If we're attached to a console that we own (double-click), Environment has no parent shell.
        if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            // Check if this process was started from Explorer (double-click) vs an existing terminal.
            // When double-clicked, the console window was created for us and will close on exit.
            using var current = System.Diagnostics.Process.GetCurrentProcess();
            using var parent = ParentProcess(current);

            if (parent is not null)
            {
                string parentName = parent.ProcessName.ToLowerInvariant();
                // If parent is explorer or a service, we were double-clicked
                if (parentName is "explorer" or "svchost")
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("  Press any key to exit...");
                    Console.ResetColor();
                    Console.ReadKey(true);
                }
            }
        }
    }
    catch
    {
        // Silently ignore — just exit
    }
}

static System.Diagnostics.Process? ParentProcess(System.Diagnostics.Process process)
{
    try
    {
        using var query = new System.Management.ManagementObjectSearcher(
            $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {process.Id}");
        foreach (System.Management.ManagementObject obj in query.Get())
        {
            int parentId = Convert.ToInt32(obj["ParentProcessId"]);
            return System.Diagnostics.Process.GetProcessById(parentId);
        }
    }
    catch { }
    return null;
}

static string? ParseHivePath(string[] args)
{
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] is "--hive" or "-hive" && i + 1 < args.Length)
        {
            return args[i + 1];
        }
    }
    return null;
}

static int RunLocal()
{
    // Read installed key + metadata from registry (includes Office C2R)
    WindowsKeyInfo info = RegistryKeyReader.ReadLocal();

    // Read OEM key from WMI
    info.OemKey = WmiKeyReader.ReadOemKey();

    // Read Windows license info from WMI (also validates partial key)
    WmiKeyReader.PopulateLicenseInfo(info);

    // Enumerate all other product licenses (Office, etc.)
    WmiKeyReader.PopulateAllLicenses(info);

    ConsoleFormatter.Print(info);

    if (string.IsNullOrEmpty(info.InstalledKey))
    {
        ConsoleFormatter.PrintWarning(
            "Could not decode installed key. Try running as administrator.");
    }

    return 0;
}

static int RunExternalHive(string hivePath)
{
    if (!File.Exists(hivePath))
    {
        ConsoleFormatter.PrintError($"Hive file not found: {hivePath}");
        return 1;
    }

    using HiveLoader loader = HiveLoader.Load(hivePath);

    WindowsKeyInfo info = RegistryKeyReader.ReadFromHive(loader.SubKeyName, hivePath);

    ConsoleFormatter.Print(info);

    if (string.IsNullOrEmpty(info.InstalledKey))
    {
        ConsoleFormatter.PrintWarning(
            "Could not decode a product key from the external hive. " +
            "The hive may not contain DigitalProductId data.");
    }

    return 0;
}
