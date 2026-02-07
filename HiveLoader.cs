using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WinKeyExtract;

/// <summary>
/// Loads and unloads external registry hive files using P/Invoke to advapi32.
/// Requires SeBackupPrivilege and SeRestorePrivilege (admin).
/// </summary>
internal sealed class HiveLoader : IDisposable
{
    private readonly string _subKeyName;
    private bool _loaded;
    private bool _disposed;

    /// <summary>
    /// The full registry path where the hive is mounted (under HKLM).
    /// </summary>
    public string MountedPath => $@"HKEY_LOCAL_MACHINE\{_subKeyName}";

    /// <summary>
    /// The subkey name under HKLM where the hive is mounted.
    /// </summary>
    public string SubKeyName => _subKeyName;

    private HiveLoader(string subKeyName)
    {
        _subKeyName = subKeyName;
    }

    /// <summary>
    /// Load an external SOFTWARE hive file into the registry under a temporary key.
    /// </summary>
    /// <param name="hivePath">Full path to the SOFTWARE hive file.</param>
    /// <returns>A HiveLoader that will unload the hive on disposal.</returns>
    public static HiveLoader Load(string hivePath)
    {
        if (!File.Exists(hivePath))
            throw new FileNotFoundException("Hive file not found.", hivePath);

        // Enable required privileges
        PrivilegeManager.EnableHivePrivileges();

        string subKeyName = $"WinKeyExtract_Temp_{Guid.NewGuid():N}";
        var loader = new HiveLoader(subKeyName);

        int result = NativeMethods.RegLoadKeyW(
            NativeMethods.HKEY_LOCAL_MACHINE,
            subKeyName,
            hivePath);

        if (result != 0)
        {
            throw new Win32Exception(result,
                $"RegLoadKey failed (error {result}). Ensure the file is a valid registry hive and you are running as administrator.");
        }

        loader._loaded = true;
        return loader;
    }

    /// <summary>
    /// Unload the mounted hive from the registry.
    /// </summary>
    private void Unload()
    {
        if (!_loaded)
            return;

        int result = NativeMethods.RegUnLoadKeyW(
            NativeMethods.HKEY_LOCAL_MACHINE,
            _subKeyName);

        if (result == 0)
            _loaded = false;
        // Silently ignore failures on unload — best effort cleanup
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Unload();
    }
}
