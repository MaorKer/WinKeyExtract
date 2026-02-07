using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WinKeyExtract;

/// <summary>
/// Enables SeBackupPrivilege and SeRestorePrivilege required for loading registry hives.
/// </summary>
internal static class PrivilegeManager
{
    private const string SeBackupPrivilege = "SeBackupPrivilege";
    private const string SeRestorePrivilege = "SeRestorePrivilege";

    /// <summary>
    /// Elevate the current process token with backup/restore privileges.
    /// Must be run as administrator.
    /// </summary>
    public static void EnableHivePrivileges()
    {
        EnablePrivilege(SeBackupPrivilege);
        EnablePrivilege(SeRestorePrivilege);
    }

    private static void EnablePrivilege(string privilegeName)
    {
        if (!NativeMethods.OpenProcessToken(
                NativeMethods.GetCurrentProcess(),
                NativeMethods.TOKEN_ADJUST_PRIVILEGES | NativeMethods.TOKEN_QUERY,
                out nint tokenHandle))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(),
                $"Failed to open process token for privilege '{privilegeName}'.");
        }

        try
        {
            if (!NativeMethods.LookupPrivilegeValueW(null, privilegeName, out long luid))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    $"Failed to look up privilege value for '{privilegeName}'.");
            }

            var tp = new NativeMethods.TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = NativeMethods.SE_PRIVILEGE_ENABLED
            };

            if (!NativeMethods.AdjustTokenPrivileges(tokenHandle, false, ref tp, 0, nint.Zero, nint.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    $"Failed to adjust token privileges for '{privilegeName}'.");
            }

            // AdjustTokenPrivileges can return true but still set ERROR_NOT_ALL_ASSIGNED
            int lastError = Marshal.GetLastWin32Error();
            if (lastError == 1300) // ERROR_NOT_ALL_ASSIGNED
            {
                throw new Win32Exception(lastError,
                    $"Privilege '{privilegeName}' could not be enabled. Run as administrator.");
            }
        }
        finally
        {
            NativeMethods.CloseHandle(tokenHandle);
        }
    }
}
