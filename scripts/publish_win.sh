#!/bin/bash

# Configuration
PROJECT_NAME="InventoryManagementSystem"
RUNTIME="win-x64"
OUTPUT_DIR="Releases/Windows"
ARCHIVE_NAME="InventoryManagementSystem_Windows.zip"

echo "🚀 Starting Windows cross-compilation (x64) for $PROJECT_NAME..."

# Create directory
mkdir -p "$OUTPUT_DIR"

# Publish (Cross-compiling from Linux to Windows works with dotnet!)
dotnet publish -c Release -r "$RUNTIME" --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$OUTPUT_DIR"

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

# Inno Setup Compilation (if Wine is available)
if command -v wine >/dev/null 2>&1; then
    echo "🍷 Wine found. Attempting to compile Installer via Inno Setup..."
    
    # Path to ISCC inside Wine prefix (Standard install path)
    ISCC_PATH="C:\\Program Files (x86)\\Inno Setup 6\\ISCC.exe"
    
    # Run ISCC
    # We use the relative path to .iss file from the project root
    # Since we are in project root when running this script (if called via build_all.sh or manual)
    # But let's be safe and use relative path from where we are.
    
    # Note: build_all.sh runs us from project root.
    if [ -f "IMS_Setup_Script.iss" ]; then
        wine "$ISCC_PATH" "IMS_Setup_Script.iss"
        
        if [ $? -eq 0 ]; then
            echo "🎉 Installer compilation successful!"
            echo "📦 Setup File: Releases/IMS_Setup_v1.2.0_Windows.exe"
        else
            echo "⚠️  Inno Setup compilation failed. Please check if Inno Setup is installed in Wine."
            echo "   Run: bash scripts/install_build_tools.sh to install it."
        fi
    else
        echo "⚠️  IMS_Setup_Script.iss not found in $(pwd)"
    fi
else
    echo "⚠️  Wine not found. Skipping Installer compilation."
fi
