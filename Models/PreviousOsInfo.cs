namespace WinKeyExtract.Models;

/// <summary>
/// Snapshot of a previous Windows installation from HKLM\SYSTEM\Setup\Source OS.
/// </summary>
public sealed class PreviousOsInfo
{
    public string? SourceLabel { get; set; }
    public string? ProductName { get; set; }
    public string? EditionId { get; set; }
    public string? CurrentBuild { get; set; }
    public string? ProductKey { get; set; }
}
