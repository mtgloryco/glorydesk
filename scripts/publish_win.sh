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
        cd Releases
        zip -r "$ARCHIVE_NAME" "Windows"
        echo "🎉 Done! Release at: Releases/$ARCHIVE_NAME (Folder: Releases/Windows)"
    else
        echo "⚠️  'zip' command not found. Files are in $OUTPUT_DIR"
    fi
else
    echo "❌ Build failed!"
    exit 1
fi
