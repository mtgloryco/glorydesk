#!/bin/bash

# This script sets up GloryDesk for the current user
APP_NAME="Glory Desk"
APP_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
EXEC_PATH="$APP_DIR/Releases/Linux/GloryDesk"
ICON_PATH="$APP_DIR/GloryDesk.Shared/Assets/glorydesk-logo.png"

echo "🔧 Setting up Glory Desk for Linux..."

# 1. Make the binary executable
if [ -f "$EXEC_PATH" ]; then
    chmod +x "$EXEC_PATH"
    echo "✅ Made $EXEC_PATH executable."
else
    echo "❌ Error: Could not find executable at $EXEC_PATH"
    exit 1
fi

# 2. Create Desktop Shortcut in applications menu
DESKTOP_FILE="$HOME/.local/share/applications/glory-desk.desktop"

cat > "$DESKTOP_FILE" <<EOL
[Desktop Entry]
Version=1.0
Type=Application
Name=Glory Desk
Comment=Stock, sales and accounts in one place
Exec="$EXEC_PATH"
Icon=$ICON_PATH
Terminal=false
Categories=Office;Business;
EOL

chmod +x "$DESKTOP_FILE"

# 3. Also update legacy shortcut if present
LEGACY_DESKTOP_FILE="$HOME/.local/share/applications/inventory-management.desktop"
if [ -f "$LEGACY_DESKTOP_FILE" ]; then
    cp "$DESKTOP_FILE" "$LEGACY_DESKTOP_FILE"
fi

# 4. Also place on Desktop if ~/Desktop exists
if [ -d "$HOME/Desktop" ]; then
    cp "$DESKTOP_FILE" "$HOME/Desktop/GloryDesk.desktop"
    chmod +x "$HOME/Desktop/GloryDesk.desktop"
fi

update-desktop-database "$HOME/.local/share/applications" 2>/dev/null || true

echo "✅ Created desktop shortcut at $DESKTOP_FILE and ~/Desktop"
echo "🎉 You can now find 'Glory Desk' on your Desktop and in your application menu!"
