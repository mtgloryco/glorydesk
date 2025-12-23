#!/bin/bash

# Configuration
PROJECT_NAME="InventoryManagementSystem"
RUNTIME_X64="osx-x64"
RUNTIME_ARM64="osx-arm64"
OUTPUT_DIR_X64="Releases/macOS_Intel"
OUTPUT_DIR_ARM64="Releases/macOS_AppleSilicon"
ARCHIVE_NAME_X64="InventoryManagementSystem_macOS_Intel.zip"
ARCHIVE_NAME_ARM64="InventoryManagementSystem_macOS_AppleSilicon.zip"

echo "🚀 Starting macOS builds for $PROJECT_NAME..."

# Build for Intel
echo "Building for Intel (x64)..."
mkdir -p "$OUTPUT_DIR_X64"
dotnet publish -c Release -r "$RUNTIME_X64" --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$OUTPUT_DIR_X64"

# Build for Apple Silicon
echo "Building for Apple Silicon (arm64)..."
mkdir -p "$OUTPUT_DIR_ARM64"
dotnet publish -c Release -r "$RUNTIME_ARM64" --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$OUTPUT_DIR_ARM64"

if [ $? -eq 0 ]; then
    echo "✅ Builds successful!"
    echo "📦 Archiving..."
    cd Releases
    zip -r "$ARCHIVE_NAME_X64" "macOS_Intel"
    zip -r "$ARCHIVE_NAME_ARM64" "macOS_AppleSilicon"
    echo "🎉 Done!"
    echo "   📍 Releases/$ARCHIVE_NAME_X64 (Folder: Releases/macOS_Intel)"
    echo "   📍 Releases/$ARCHIVE_NAME_ARM64 (Folder: Releases/macOS_AppleSilicon)"
else
    echo "❌ Build failed!"
    exit 1
fi
