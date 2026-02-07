namespace WinKeyExtract.Models;

/// <summary>
/// Office Click-to-Run installation details from registry.
/// </summary>
public sealed class OfficeC2RInfo
{
    public string? ProductReleaseIds { get; set; }
    public string? Platform { get; set; }
    public string? Version { get; set; }
    public string? AudienceId { get; set; }
}
