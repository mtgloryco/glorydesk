# Packaging and Deployment Guide

This directory contains automated scripts to build and package the Inventory Management System for different platforms.

## Prerequisites

- .NET 8.0 SDK or higher
- PowerShell (for Windows builds)
- Linux/macOS (for their respective builds)

## How to Build

### 🐧 Linux
Run the following command from the root directory:
```bash
./scripts/publish_linux.sh
```
Output: `Releases/InventoryManagementSystem_Linux.tar.gz`

### 🪟 Windows
You can build for Windows directly from Linux (cross-compilation)!
Run:
```bash
chmod +x ./scripts/publish_win.sh
./scripts/publish_win.sh
```
Output: `Releases/InventoryManagementSystem_Windows.zip`

Alternatively, if you are actually on a Windows machine, use the PowerShell script:
```powershell
.\scripts\publish_win.ps1
```

### 🍎 macOS
Run the following command:
```bash
./scripts/publish_mac.sh
```
Output:
- `Releases/InventoryManagementSystem_macOS_Intel.zip`
- `Releases/InventoryManagementSystem_macOS_AppleSilicon.zip`

## Build Options Explained

The scripts use the following flags for `dotnet publish`:
- `--self-contained true`: Includes the .NET runtime in the package.
- `-p:PublishSingleFile=true`: Bundles everything into a single executable.
- `-p:IncludeNativeLibrariesForSelfExtract=true`: Ensures native dependencies are extracted and loaded correctly.

## Windows: Visual C++ Redistributable

Avalonia renders the UI via SkiaSharp, which relies on native libraries (`libSkiaSharp.dll`, etc.)
that require the **Microsoft Visual C++ Redistributable (x64) 2015-2022** to be present on the
target machine. `publish_win.sh` / `publish_win.ps1` automatically download `vc_redist.x64.exe`
into `InventoryManagementSystem.Shared/redist/` and `IMS_Setup_Script.iss` silently installs it
(only if not already present) before launching the app.

Without this, a freshly-installed/clean Windows machine will show:
```
The application has failed to start because its side-by-side configuration is incorrect. (os error -2147010895)
```
right after installation.

## ⚠️ A Note on Cross-Compiling the Windows Build

`publish_win.sh` cross-compiles a self-contained, single-file `win-x64` executable from Linux.
This works in most cases, but the .NET SDK's cross-platform apphost resource patcher (which embeds
the icon/manifest into the `.exe`) has known edge cases when running on a non-Windows host, which
can occasionally produce a broken executable. If a locally cross-compiled installer misbehaves on
Windows but a build produced by `.github/workflows/release.yml` (built natively on the
`windows-latest` GitHub Actions runner) does not, prefer that pipeline - or build directly on a
Windows machine with `publish_win.ps1` - for your distributed releases.

## How to Install

### 🐧 Linux
1. Copy the `Releases/Linux` folder to your machine.
2. Open a terminal inside that folder.
3. Run the installer script:
   ```bash
   ./install.sh
   ```
4. This will make the app executable and create a shortcut in your application menu.

### 🪟 Windows
1. Copy the `Releases/Windows` folder to your machine.
2. Simply double-click `InventoryManagementSystem.Desktop.exe` to run.
3. (Optional) Right-click `InventoryManagementSystem.Desktop.exe` -> **Send to** -> **Desktop (create shortcut)**.

### 🍎 macOS
1. Unzip the archive for your Mac (Intel or Apple Silicon).
2. Open the folder and double-click the `InventoryManagementSystem` executable.
3. If macOS blocks it (unsigned app), go to **System Settings** -> **Privacy & Security** and click **"Open Anyway"**.
