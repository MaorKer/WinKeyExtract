# WinKeyExtract

Open-source Windows product key extraction CLI tool.

## Quick Start (just want to see your keys?)

1. Go to the [Releases](../../releases) page and download `WinKeyExtract.exe`
2. Right-click `WinKeyExtract.exe` and select **Run as administrator**
3. Your Windows product key, license info, and upgrade history will be displayed

> **Why "Run as administrator"?**
> Windows stores product keys in protected areas of the registry. Without admin rights, the tool may not be able to read them.

> **Windows Defender / SmartScreen warning?**
> Since this is a new, unsigned executable, Windows may show a warning. Click "More info" then "Run anyway". The source code is fully open — you can review it or build it yourself.

## What It Shows

- **Installed Key** — Your current Windows product key, decoded from the registry
- **OEM/BIOS Key** — The product key embedded in your computer's firmware (laptops/prebuilts)
- **Windows License** — Edition, license status, activation channel, and key validation
- **Legacy Key Comparison** — If your PC was upgraded between editions, shows the old key too
- **Other Product Licenses** — Office and other Microsoft product license info
- **Office Click-to-Run** — Installed Office products, platform, and version
- **Upgrade History** — Previous Windows versions this PC ran before upgrades, with their keys
- **System Metadata** — Edition ID, installation type, registered owner

## Example Output

```
  ══════════════════════════════════════════════════
  ║ WinKeyExtract — Local System                   ║
  ══════════════════════════════════════════════════

  [System Information]
  Edition                : Windows 10 Education
  Edition ID             : Education
  Composition Edition    : Enterprise
  Installation Type      : Client
  Build                  : 19045.6456
  Product ID             : 00328-00216-67643-AA136
  Registered Owner       : User

  [Installed Key]
  Product Key            : XXXXX-XXXXX-XXXXX-XXXXX-XXXXX
  Key Source             : DigitalProductId4

  [OEM Key (BIOS)]
  OEM Key                : (not available)

  [Windows License]
  Status                 : Licensed
  Channel                : Retail
  Partial Key            : XXXXX
  Key Validated          : Yes

  [Other Product Licenses]
  Product                : Office 16, Office16O365ProPlusR_Grace edition
  Status                 : Notification
  Channel                : Retail
  Partial Key            : XXXXX

  [Office Click-to-Run]
  Products               : O365ProPlusRetail
  Platform               : x64
  Version                : 16.0.19628.20166

  [Upgrade History]
  Previous Edition       : Windows 7 Home Premium
  Edition ID             : HomePremium
  Build                  : 7601
  Product Key            : XXXXX-XXXXX-XXXXX-XXXXX-XXXXX
  Upgraded On            : 1/17/2020 23:48:47
```

## Advanced Usage

```
WinKeyExtract                       Show local keys + license info
WinKeyExtract --hive <path>         Extract key from external SOFTWARE hive
WinKeyExtract --help                Show help
```

### Extracting keys from another Windows installation

If you have a dead PC or a backup of a Windows drive, you can extract the product key from its registry hive file:

```
WinKeyExtract --hive D:\Windows\System32\config\SOFTWARE
```

This loads the `SOFTWARE` hive from the other installation and decodes the key from it. Useful for recovering a product key from a PC that won't boot.

## Building from Source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
# Build
dotnet build -c Release

# Run directly
dotnet run -c Release

# Publish as single-file exe (~20-25 MB)
dotnet publish -c Release
```

The published executable will be at `bin/Release/net8.0-windows/win-x64/publish/WinKeyExtract.exe`.

## Requirements

- Windows 10/11 or Windows Server 2016+
- Administrator privileges (required for registry and WMI access)

## FAQ

**Q: Why does Office only show a partial key (last 5 characters)?**
Office 365 / Microsoft 365 uses cloud-based licensing and doesn't store the full product key locally. The partial key from Windows Management Instrumentation (WMI) is all that's available on the machine.

**Q: Why do I see a "Legacy Key" that's different from my installed key?**
This happens when your Windows edition was changed (e.g., Home to Pro upgrade, or a new key was applied). The legacy blob (`DigitalProductId`) keeps the old key while the modern blob (`DigitalProductId4`) has the current one.

**Q: What's the "Upgrade History" section?**
When Windows does an in-place upgrade (e.g., Windows 7 to 10, or a major feature update), it saves a snapshot of the previous installation in the registry. This tool decodes the product key from each of those snapshots.

**Q: The tool says "not found" or "not available" for some fields — is that a bug?**
No. "Not available" means the data doesn't exist on your system. For example, OEM keys only exist on computers that came with Windows preinstalled (Dell, HP, Lenovo, etc.). Desktop builds or volume-licensed machines won't have one.

## Project Structure

```
WinKeyExtract.csproj        Project file (net8.0-windows, win-x64, single-file)
Program.cs                  Entry point, arg parsing, orchestration
KeyDecoder.cs               DigitalProductId base-24 decoding (legacy + Win8+)
RegistryKeyReader.cs        Read keys from local registry or external hive
WmiKeyReader.cs             OEM key + license info via WMI
HiveLoader.cs               Load/unload external registry hive via P/Invoke
NativeMethods.cs            P/Invoke declarations (advapi32)
PrivilegeManager.cs         Enable SeBackupPrivilege/SeRestorePrivilege
Models/
  WindowsKeyInfo.cs         Aggregated data model
  LicenseStatus.cs          WMI license status enum
  ProductLicenseInfo.cs     Generic product license model
  OfficeC2RInfo.cs          Office Click-to-Run details
  PreviousOsInfo.cs         Previous OS upgrade snapshot
Output/
  ConsoleFormatter.cs       Color-coded console output
```

## How It Works

1. **Registry decoding** — Reads `DigitalProductId4` (Win8+, offset 808) and `DigitalProductId` (legacy, offset 52) binary blobs from `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion` and decodes the base-24 encoded product key.

2. **WMI queries** — Queries `SoftwareLicensingService` for the OEM/BIOS-embedded key and `SoftwareLicensingProduct` for license status, channel, and partial key of all activated Microsoft products.

3. **Partial key validation** — Cross-checks the last 5 characters of the decoded registry key against the WMI partial product key to verify consistency.

4. **Upgrade history** — Reads snapshots from `HKLM\SYSTEM\Setup\Source OS (Updated on ...)` subkeys that Windows creates during in-place upgrades, and decodes product keys from each.

5. **External hive loading** — Uses `RegLoadKey`/`RegUnLoadKey` P/Invoke to temporarily mount a `SOFTWARE` hive file from another Windows installation under a temporary HKLM subkey, then reads keys from it using the same decoding logic.

## Disclaimer

This tool is intended for **legitimate use only**, such as:

- Recovering your own product keys before a reinstall or hardware change
- Migrating licenses to a new machine
- IT asset inventory and license auditing on machines you own or manage
- Extracting keys from a dead PC's drive that you have lawful access to

This tool only reads data that is already stored locally on the machine — it does not bypass, crack, or generate product keys. Do not use it to access keys on machines you do not own or have authorization to manage. The author is not responsible for any misuse.

## License

[MIT](LICENSE) — free to use, fork, and modify. Just keep the copyright notice.
