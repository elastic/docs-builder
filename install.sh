#!/bin/sh
set -e

# Check if docs-builder already exists
if [ -f /usr/local/bin/docs-builder ]; then
  echo "docs-builder is already installed."
  printf "Do you want to update/overwrite it? (y/n): "
  read choice
  case "$choice" in
    y|Y ) echo "Updating docs-builder..." ;;
    n|N ) echo "Installation aborted."; exit 0 ;;
    * ) echo "Invalid choice. Installation aborted."; exit 1 ;;
  esac
fi

# Download the latest macOS binary from releases
curl -LO https://github.com/elastic/docs-builder/releases/latest/download/docs-builder-mac-arm64.zip

# Extract only the docs-builder file to /tmp directory
# Use -o flag to always overwrite files without prompting
unzip -j -o docs-builder-mac-arm64.zip docs-builder -d /tmp

# Ensure the binary is executable
chmod +x /tmp/docs-builder

# Move the binary to a system path with force flag to overwrite
mv -f /tmp/docs-builder /usr/local/bin/docs-builder

echo "docs-builder has been installed successfully and is available in your PATH."