namespace WinKeyExtract.Models;

/// <summary>
/// Represents a single licensed product from WMI SoftwareLicensingProduct.
/// </summary>
public sealed class ProductLicenseInfo
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ApplicationId { get; set; }
    public string? PartialProductKey { get; set; }
    public LicenseStatus LicenseStatus { get; set; } = LicenseStatus.Unknown;
    public string? Channel { get; set; }
}
