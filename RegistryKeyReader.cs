using Microsoft.Win32;
using WinKeyExtract.Models;

namespace WinKeyExtract;

/// <summary>
/// Reads Windows product key data from the registry (local or external hive).
/// </summary>
public static class RegistryKeyReader
{
    private const string NtCurrentVersion = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
    private const string OfficeC2RConfig = @"SOFTWARE\Microsoft\Office\ClickToRun\Configuration";

    /// <summary>
    /// Read product key info from the local machine's registry.
    /// </summary>
    public static WindowsKeyInfo ReadLocal()
    {
        var info = new WindowsKeyInfo();

        using var key = Registry.LocalMachine.OpenSubKey(NtCurrentVersion);
        if (key is null)
            return info;

        PopulateFromRegistryKey(key, info);
        PopulateOfficeC2R(info);
        PopulatePreviousInstallations(info);
        return info;
    }

    /// <summary>
    /// Read product key info from an externally loaded hive.
    /// The hive must already be mounted under HKLM\{subKeyName}.
    /// </summary>
    public static WindowsKeyInfo ReadFromHive(string subKeyName, string hivePath)
    {
        var info = new WindowsKeyInfo
        {
            IsFromExternalHive = true,
            HivePath = hivePath
        };

        string regPath = $@"{subKeyName}\Microsoft\Windows NT\CurrentVersion";
        using var key = Registry.LocalMachine.OpenSubKey(regPath);
        if (key is null)
            return info;

        PopulateFromRegistryKey(key, info);

        // Try Office C2R from the external hive too
        string officeRegPath = $@"{subKeyName}\Microsoft\Office\ClickToRun\Configuration";
        using var officeKey = Registry.LocalMachine.OpenSubKey(officeRegPath);
        if (officeKey is not null)
            PopulateOfficeC2RFromKey(officeKey, info);

        return info;
    }

    private static void PopulateFromRegistryKey(RegistryKey key, WindowsKeyInfo info)
    {
        info.Edition = key.GetValue("ProductName") as string;
        info.ProductId = key.GetValue("ProductId") as string;

        string? buildLab = key.GetValue("BuildLabEx") as string
                           ?? key.GetValue("BuildLab") as string;
        string? currentBuild = key.GetValue("CurrentBuild") as string;
        string? ubr = key.GetValue("UBR")?.ToString();

        info.BuildNumber = !string.IsNullOrEmpty(currentBuild) && !string.IsNullOrEmpty(ubr)
            ? $"{currentBuild}.{ubr}"
            : currentBuild ?? buildLab;

        // Additional metadata
        info.EditionId = key.GetValue("EditionID") as string;
        info.CompositionEditionId = key.GetValue("CompositionEditionID") as string;
        info.InstallationType = key.GetValue("InstallationType") as string;
        info.RegisteredOwner = key.GetValue("RegisteredOwner") as string;

        // Decode product keys from both blob types
        byte[]? dpid4 = key.GetValue("DigitalProductId4") as byte[];
        byte[]? dpid = key.GetValue("DigitalProductId") as byte[];

        string? key4 = KeyDecoder.DecodeKeyDpid4(dpid4);
        string? keyLegacy = KeyDecoder.DecodeKey(dpid);

        if (key4 is not null)
        {
            info.InstalledKey = key4;
            info.InstalledKeySource = "DigitalProductId4";
        }
        else if (keyLegacy is not null)
        {
            info.InstalledKey = keyLegacy;
            info.InstalledKeySource = "DigitalProductId";
        }

        // Store legacy key separately if it differs from the primary key
        if (keyLegacy is not null && info.InstalledKey is not null &&
            !string.Equals(keyLegacy, info.InstalledKey, StringComparison.OrdinalIgnoreCase))
        {
            info.LegacyKey = keyLegacy;
        }
    }

    private static void PopulateOfficeC2R(WindowsKeyInfo info)
    {
        using var key = Registry.LocalMachine.OpenSubKey(OfficeC2RConfig);
        if (key is null)
            return;

        PopulateOfficeC2RFromKey(key, info);
    }

    private static void PopulateOfficeC2RFromKey(RegistryKey key, WindowsKeyInfo info)
    {
        string? releaseIds = key.GetValue("ProductReleaseIds") as string;
        if (string.IsNullOrEmpty(releaseIds))
            return;

        info.OfficeC2R = new OfficeC2RInfo
        {
            ProductReleaseIds = releaseIds,
            Platform = key.GetValue("Platform") as string,
            Version = key.GetValue("ClientVersionToReport") as string
                      ?? key.GetValue("VersionToReport") as string,
            AudienceId = key.GetValue("AudienceId") as string
        };
    }

    /// <summary>
    /// Read previous OS installation snapshots from HKLM\SYSTEM\Setup\Source OS*.
    /// These are created by Windows during in-place upgrades.
    /// </summary>
    private static void PopulatePreviousInstallations(WindowsKeyInfo info)
    {
        using var setupKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\Setup");
        if (setupKey is null)
            return;

        foreach (string subKeyName in setupKey.GetSubKeyNames())
        {
            if (!subKeyName.StartsWith("Source OS", StringComparison.OrdinalIgnoreCase))
                continue;

            using var sourceKey = setupKey.OpenSubKey(subKeyName);
            if (sourceKey is null)
                continue;

            byte[]? dpid = sourceKey.GetValue("DigitalProductId") as byte[];

            var prev = new PreviousOsInfo
            {
                SourceLabel = subKeyName,
                ProductName = sourceKey.GetValue("ProductName") as string,
                EditionId = sourceKey.GetValue("EditionID") as string,
                CurrentBuild = sourceKey.GetValue("CurrentBuild") as string,
                ProductKey = KeyDecoder.DecodeKey(dpid)
            };

            info.PreviousInstallations.Add(prev);
        }
    }
}
