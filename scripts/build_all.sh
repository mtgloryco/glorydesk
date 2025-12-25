#!/bin/bash

# Build All Platforms
echo "🚀 Starting Full Build Process..."

# Ensure we are in the project root (parent of 'scripts')
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
cd "$PROJECT_ROOT"

# Windows
echo "--------------------------------------------------"
echo "🪟 Building for Windows..."
bash scripts/publish_win.sh

# Linux
echo "--------------------------------------------------"
echo "🐧 Building for Linux..."
bash scripts/publish_linux.sh

# macOS
echo "--------------------------------------------------"
echo "🍎 Building for macOS..."
bash scripts/publish_mac.sh

echo "--------------------------------------------------"
echo "✅ All builds completed!"
echo "📂 Artifacts are in $PROJECT_ROOT/Releases"
