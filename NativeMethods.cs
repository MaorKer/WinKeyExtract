using System.Runtime.InteropServices;

namespace WinKeyExtract;

/// <summary>
/// P/Invoke declarations for advapi32.dll registry hive operations and privilege management.
/// </summary>
internal static partial class NativeMethods
{
    internal const int SE_PRIVILEGE_ENABLED = 0x00000002;
    internal const int TOKEN_ADJUST_PRIVILEGES = 0x0020;
    internal const int TOKEN_QUERY = 0x0008;

    internal const uint REG_OPTION_BACKUP_RESTORE = 0x00000004;

    [LibraryImport("advapi32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int RegLoadKeyW(
        nint hKey,
        string lpSubKey,
        string lpFile);

    [LibraryImport("advapi32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int RegUnLoadKeyW(
        nint hKey,
        string lpSubKey);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool OpenProcessToken(
        nint processHandle,
        int desiredAccess,
        out nint tokenHandle);

    [LibraryImport("advapi32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool LookupPrivilegeValueW(
        string? lpSystemName,
        string lpName,
        out long lpLuid);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AdjustTokenPrivileges(
        nint tokenHandle,
        [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
        ref TOKEN_PRIVILEGES newState,
        int bufferLength,
        nint previousState,
        nint returnLength);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseHandle(nint hObject);

    [LibraryImport("kernel32.dll")]
    internal static partial nint GetCurrentProcess();

    // HKEY_LOCAL_MACHINE handle
    internal static readonly nint HKEY_LOCAL_MACHINE = new(unchecked((int)0x80000002));

    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_PRIVILEGES
    {
        public int PrivilegeCount;
        public long Luid;
        public int Attributes;
    }
}
