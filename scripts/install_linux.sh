#!/bin/bash

# This script sets up the application for the current user
APP_NAME="GloryDesk"
APP_DIR=$(pwd)
EXEC_PATH="$APP_DIR/InventoryManagementSystem"
ICON_PATH="$APP_DIR/Assets/icon.png" # Assuming you have an icon in Assets

echo "🔧 Setting up Glory Desk for Linux..."

# 1. Make the binary executable
if [ -f "$EXEC_PATH" ]; then
    chmod +x "$EXEC_PATH"
    echo "✅ Made $APP_NAME executable."
else
    echo "❌ Error: Could not find executable at $EXEC_PATH"
    exit 1
fi

# 2. Create Desktop Shortcut
DESKTOP_FILE="$HOME/.local/share/applications/glory-desk.desktop"

cat > "$DESKTOP_FILE" <<EOL
[Desktop Entry]
Version=1.0
Type=Application
Name=Glory Desk
Comment=Stock, sales and accounts in one place
Exec=$EXEC_PATH
Icon=$APP_NAME
Terminal=false
Categories=Office;Business;
EOL

echo "✅ Created desktop shortcut at $DESKTOP_FILE"
echo "🎉 You can now find 'Glory Desk' in your application menu!"

# Optionally run it now
read -p "Do you want to run the application now? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    "$EXEC_PATH" &
fi
