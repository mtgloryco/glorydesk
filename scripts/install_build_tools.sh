#!/bin/bash
set -e

echo "🔒 You will be asked for your sudo password to install packages."

# 1. Enable 32-bit architecture (Required for Wine)
echo "📦 Adding i386 architecture..."
sudo dpkg --add-architecture i386

# 2. Update package lists
echo "🔄 Updating repositories..."
sudo apt-get update

# 3. Install Wine (Stable) and 32-bit libraries
echo "🍷 Installing Wine..."
sudo apt-get install -y wine-stable wine32:i386

# 4. Install Inno Setup (via snap or verify binary)
# Note: Inno Setup is a Windows app. We usually run it VIA Wine.
# However, 'iscc' on Linux often comes from the 'innoextract' or specific wrapper packages.
# A better way is to download Inno Setup executable and install it via Wine, 
# then alias 'iscc' to run the Windows compiler.

echo "📥 Downloading Inno Setup 6..."
wget -O is-setup.exe https://files.jrsoftware.org/is/6/innosetup-6.2.2.exe

echo "💿 Installing Inno Setup into Wine... (Please follow the Windows Installer GUI)"
wine is-setup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-

echo "✅ Installation Complete."
echo "To compile an installer, you will use:"
echo "wine \"C:\\Program Files (x86)\\Inno Setup 6\\ISCC.exe\" \"YourScript.iss\""
