#!/bin/sh
set -e

# Determine OS type and architecture
OS=$(uname -s | tr '[:upper:]' '[:lower:]')
ARCH=$(uname -m)

# Map architecture naming
if [ "$ARCH" = "x86_64" ]; then
  ARCH="amd64"
elif [ "$ARCH" = "aarch64" ] || [ "$ARCH" = "arm64" ]; then
  ARCH="arm64"
fi

# Determine binary to download based on OS
if [ "$OS" = "darwin" ]; then
  BINARY="docs-builder-mac-$ARCH.zip"
  DEFAULT_INSTALL_DIR="/usr/local/bin"
elif [ "$OS" = "linux" ]; then
  BINARY="docs-builder-linux-$ARCH.zip"
  DEFAULT_INSTALL_DIR="/usr/local/bin"
else
  echo "Unsupported operating system: $OS"
  exit 1
fi

# Determine if we need sudo for the install directory
INSTALL_DIR="$DEFAULT_INSTALL_DIR"
if [ ! -w "$INSTALL_DIR" ]; then
  USE_SUDO=true
  echo "Note: Installing to $INSTALL_DIR requires administrator privileges."
else
  USE_SUDO=false
fi

# Check if docs-builder already exists
if [ -f "$INSTALL_DIR/docs-builder" ]; then
  echo "docs-builder is already installed."
  printf "Do you want to update/overwrite it? (y/n): "
  read choice
  case "$choice" in
    y|Y ) echo "Updating docs-builder..." ;;
    n|N ) echo "Installation aborted."; exit 0 ;;
    * ) echo "Invalid choice. Installation aborted."; exit 1 ;;
  esac
fi

echo "Downloading docs-builder for $OS/$ARCH..."

# Download the appropriate binary
curl -LO "https://github.com/elastic/docs-builder/releases/latest/download/$BINARY"

# Extract only the docs-builder file to /tmp directory
# Use -o flag to always overwrite files without prompting
unzip -j -o "$BINARY" docs-builder -d /tmp

# Ensure the binary is executable
chmod +x /tmp/docs-builder

# Move the binary to the install directory
echo "Installing docs-builder to $INSTALL_DIR..."
if [ "$USE_SUDO" = true ]; then
  sudo mv -f /tmp/docs-builder "$INSTALL_DIR/docs-builder"
else
  mv -f /tmp/docs-builder "$INSTALL_DIR/docs-builder"
fi

# Clean up the downloaded zip file
rm -f "$BINARY"

echo "docs-builder has been installed successfully and is available in your PATH."
echo "You can run 'docs-builder --help' to see available commands."