using WinKeyExtract;
using WinKeyExtract.Models;
using WinKeyExtract.Output;

return Run(args);

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
