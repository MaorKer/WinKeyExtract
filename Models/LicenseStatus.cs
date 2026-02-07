namespace WinKeyExtract.Models;

/// <summary>
/// WMI SoftwareLicensingProduct LicenseStatus values.
/// </summary>
public enum LicenseStatus
{
    Unlicensed = 0,
    Licensed = 1,
    OobGrace = 2,
    OotGrace = 3,
    NonGenuineGrace = 4,
    Notification = 5,
    ExtendedGrace = 6,
    Unknown = -1
}

public static class LicenseStatusExtensions
{
    public static string ToDisplayString(this LicenseStatus status) => status switch
    {
        LicenseStatus.Unlicensed => "Unlicensed",
        LicenseStatus.Licensed => "Licensed",
        LicenseStatus.OobGrace => "Out-of-Box Grace Period",
        LicenseStatus.OotGrace => "Out-of-Tolerance Grace Period",
        LicenseStatus.NonGenuineGrace => "Non-Genuine Grace Period",
        LicenseStatus.Notification => "Notification",
        LicenseStatus.ExtendedGrace => "Extended Grace Period",
        _ => "Unknown"
    };
}
