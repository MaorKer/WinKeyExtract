namespace WinKeyExtract.Models;

/// <summary>
/// Aggregated Windows product key and license information.
/// </summary>
public sealed class WindowsKeyInfo
{
    // --- Registry-sourced fields ---
    public string? InstalledKey { get; set; }
    public string? InstalledKeySource { get; set; }
    public string? LegacyKey { get; set; }
    public string? Edition { get; set; }
    public string? ProductId { get; set; }
    public string? BuildNumber { get; set; }

    // --- Additional registry metadata ---
    public string? EditionId { get; set; }
    public string? CompositionEditionId { get; set; }
    public string? InstallationType { get; set; }
    public string? RegisteredOwner { get; set; }

    // --- WMI-sourced fields ---
    public string? OemKey { get; set; }
    public string? LicenseChannel { get; set; }
    public LicenseStatus LicenseStatus { get; set; } = LicenseStatus.Unknown;
    public string? PartialProductKey { get; set; }
    public string? WmiEdition { get; set; }

    // --- All product licenses (WMI) ---
    public List<ProductLicenseInfo> AllLicenses { get; set; } = [];

    // --- Office Click-to-Run ---
    public OfficeC2RInfo? OfficeC2R { get; set; }

    // --- Previous OS installations (upgrade history) ---
    public List<PreviousOsInfo> PreviousInstallations { get; set; } = [];

    // --- External hive ---
    public bool IsFromExternalHive { get; set; }
    public string? HivePath { get; set; }

    // --- Validation ---
    public bool? PartialKeyMatch { get; set; }
}
