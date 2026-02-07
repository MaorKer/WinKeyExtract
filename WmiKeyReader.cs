using System.Management;
using WinKeyExtract.Models;

namespace WinKeyExtract;

/// <summary>
/// Reads OEM product key, license status, and channel info via WMI.
/// </summary>
public static class WmiKeyReader
{
    /// <summary>
    /// Read the OEM/BIOS-embedded product key from SoftwareLicensingService.
    /// </summary>
    public static string? ReadOemKey()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT OA3xOriginalProductKey FROM SoftwareLicensingService");

            foreach (ManagementObject obj in searcher.Get())
            {
                string? key = obj["OA3xOriginalProductKey"] as string;
                if (!string.IsNullOrWhiteSpace(key))
                    return key;
            }
        }
        catch
        {
            // WMI may not be available or accessible
        }

        return null;
    }

    /// <summary>
    /// Read license information from SoftwareLicensingProduct for the active Windows license.
    /// </summary>
    public static void PopulateLicenseInfo(WindowsKeyInfo info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, Description, LicenseStatus, PartialProductKey, ProductKeyChannel " +
                "FROM SoftwareLicensingProduct " +
                "WHERE ApplicationId = '55c92734-d682-4d71-983e-d6ec3f16059f' " +
                "AND PartialProductKey IS NOT NULL");

            foreach (ManagementObject obj in searcher.Get())
            {
                info.WmiEdition = obj["Name"] as string ?? obj["Description"] as string;
                info.PartialProductKey = obj["PartialProductKey"] as string;
                info.LicenseChannel = obj["ProductKeyChannel"] as string;

                if (obj["LicenseStatus"] is uint status)
                {
                    info.LicenseStatus = Enum.IsDefined(typeof(LicenseStatus), (int)status)
                        ? (LicenseStatus)(int)status
                        : LicenseStatus.Unknown;
                }

                // Validate partial key against decoded installed key
                if (!string.IsNullOrEmpty(info.InstalledKey) &&
                    !string.IsNullOrEmpty(info.PartialProductKey))
                {
                    string last5 = info.InstalledKey[^5..];
                    info.PartialKeyMatch = string.Equals(
                        last5, info.PartialProductKey, StringComparison.OrdinalIgnoreCase);
                }

                break; // Only need the first active license
            }
        }
        catch
        {
            // WMI may not be available
        }
    }

    private const string WindowsAppId = "55c92734-d682-4d71-983e-d6ec3f16059f";

    /// <summary>
    /// Enumerate ALL licensed products (Windows, Office, etc.) from WMI.
    /// </summary>
    public static void PopulateAllLicenses(WindowsKeyInfo info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, Description, ApplicationId, LicenseStatus, PartialProductKey, ProductKeyChannel " +
                "FROM SoftwareLicensingProduct " +
                "WHERE PartialProductKey IS NOT NULL");

            foreach (ManagementObject obj in searcher.Get())
            {
                string? appId = obj["ApplicationId"]?.ToString();

                // Skip the Windows entry — already shown in main section
                if (string.Equals(appId, WindowsAppId, StringComparison.OrdinalIgnoreCase))
                    continue;

                var license = new ProductLicenseInfo
                {
                    Name = obj["Name"] as string,
                    Description = obj["Description"] as string,
                    ApplicationId = appId,
                    PartialProductKey = obj["PartialProductKey"] as string,
                    Channel = obj["ProductKeyChannel"] as string
                };

                if (obj["LicenseStatus"] is uint status)
                {
                    license.LicenseStatus = Enum.IsDefined(typeof(LicenseStatus), (int)status)
                        ? (LicenseStatus)(int)status
                        : LicenseStatus.Unknown;
                }

                info.AllLicenses.Add(license);
            }
        }
        catch
        {
            // WMI may not be available
        }
    }
}
