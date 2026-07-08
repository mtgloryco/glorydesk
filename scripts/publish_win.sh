#!/bin/bash

# Configuration
PROJECT_NAME="InventoryManagementSystem"
RUNTIME="win-x64"
OUTPUT_DIR="Releases/Windows"
ARCHIVE_NAME="InventoryManagementSystem_Windows.zip"
ISS_SCRIPT="InventoryManagementSystem.Shared/IMS_Setup_Script.iss"
REDIST_DIR="InventoryManagementSystem.Shared/redist"
VC_REDIST_URL="https://aka.ms/vs/17/release/vc_redist.x64.exe"

echo "🚀 Starting Windows cross-compilation (x64) for $PROJECT_NAME..."
echo "⚠️  NOTE: Cross-compiling a self-contained single-file win-x64 exe from Linux has known"
echo "    .NET SDK edge cases around embedding the exe's icon/manifest resources. If the resulting"
echo "    installer fails on Windows with a 'side-by-side configuration is incorrect' error, prefer"
echo "    building/publishing this artifact on an actual Windows machine or the windows-latest"
echo "    GitHub Actions runner (see .github/workflows/release.yml) instead."

# Create directory
mkdir -p "$OUTPUT_DIR"

# Publish (Cross-compiling from Linux to Windows works with dotnet!)
# NOTE: We target the Desktop project explicitly - publishing the whole .sln would try to
# single-file-publish the Shared/Tests library projects too, which fails (NETSDK1099/1098).
dotnet publish "InventoryManagementSystem.Desktop/InventoryManagementSystem.Desktop.csproj" -c Release -r "$RUNTIME" --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$OUTPUT_DIR"

if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
    echo "📦 Archiving..."
    
    # Use zip if available, or just leave as directory
    if command -v zip >/dev/null 2>&1; then
        (
            cd Releases
            zip -qr "$ARCHIVE_NAME" "Windows"
        )
        echo "🎉 Done! Release at: Releases/$ARCHIVE_NAME (Folder: Releases/Windows)"
    else
        echo "⚠️  'zip' command not found. Files are in $OUTPUT_DIR"
    fi
else
    echo "❌ Build failed!"
    exit 1
fi

# Fetch the VC++ Redistributable so the installer can bundle it (needed by Avalonia/SkiaSharp's
# native rendering libraries - without it, the app fails to start on a clean Windows machine).
mkdir -p "$REDIST_DIR"
if [ ! -f "$REDIST_DIR/vc_redist.x64.exe" ]; then
    echo "⬇️  Downloading Visual C++ Redistributable (x64)..."
    if command -v curl >/dev/null 2>&1; then
        curl -fL -o "$REDIST_DIR/vc_redist.x64.exe" "$VC_REDIST_URL"
    elif command -v wget >/dev/null 2>&1; then
        wget -O "$REDIST_DIR/vc_redist.x64.exe" "$VC_REDIST_URL"
    fi
    if [ ! -s "$REDIST_DIR/vc_redist.x64.exe" ]; then
        echo "⚠️  Failed to download vc_redist.x64.exe. The installer will be built without it,"
        echo "   which may cause 'side-by-side configuration is incorrect' errors on clean Windows machines."
        rm -f "$REDIST_DIR/vc_redist.x64.exe"
    fi
fi

# Inno Setup Compilation (if Wine is available)
if command -v wine >/dev/null 2>&1; then
    echo "🍷 Wine found. Attempting to compile Installer via Inno Setup..."
    
    # Path to ISCC inside Wine prefix (Standard install path)
    ISCC_PATH="C:\\Program Files (x86)\\Inno Setup 6\\ISCC.exe"
    
    # The .iss script lives in InventoryManagementSystem.Shared/, and its Source/OutputDir
    # paths are relative to that file's location, not the current working directory.
    if [ -f "$ISS_SCRIPT" ]; then
        wine "$ISCC_PATH" "$ISS_SCRIPT"
        
        if [ $? -eq 0 ]; then
            echo "🎉 Installer compilation successful!"
            echo "📦 Setup File: Releases/IMS_Setup_v1.2.2_Windows.exe"
        else
            echo "⚠️  Inno Setup compilation failed. Please check if Inno Setup is installed in Wine."
            echo "   Run: bash scripts/install_build_tools.sh to install it."
        fi
    else
        echo "⚠️  $ISS_SCRIPT not found in $(pwd)"
    fi
else
    echo "⚠️  Wine not found. Skipping Installer compilation."
fi
