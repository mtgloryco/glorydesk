#!/bin/bash

# Configuration
PROJECT_NAME="InventoryManagementSystem"
RUNTIME="linux-x64"
OUTPUT_DIR="Releases/Linux"
ARCHIVE_NAME="InventoryManagementSystem_Linux.tar.gz"

echo "🚀 Starting Linux build for $PROJECT_NAME..."

# Create directory
mkdir -p "$OUTPUT_DIR"

# Copy installer
cp scripts/install_linux.sh "$OUTPUT_DIR/install.sh"
chmod +x "$OUTPUT_DIR/install.sh"

# Publish
dotnet publish -c Release -r "$RUNTIME" --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$OUTPUT_DIR"

if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
    echo "📦 Archiving..."
    cd Releases
    tar -czvf "$ARCHIVE_NAME" "Linux"
    echo "🎉 Done! Release at: Releases/$ARCHIVE_NAME (Folder: Releases/Linux)"
else
    echo "❌ Build failed!"
    exit 1
fi
