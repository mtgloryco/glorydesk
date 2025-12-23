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
2. Simply double-click `InventoryManagementSystem.exe` to run.
3. (Optional) Right-click `InventoryManagementSystem.exe` -> **Send to** -> **Desktop (create shortcut)**.

### 🍎 macOS
1. Unzip the archive for your Mac (Intel or Apple Silicon).
2. Open the folder and double-click the `InventoryManagementSystem` executable.
3. If macOS blocks it (unsigned app), go to **System Settings** -> **Privacy & Security** and click **"Open Anyway"**.
