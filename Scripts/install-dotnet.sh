#!/usr/bin/env bash
# Install .NET 8.0 SDK for macOS or Linux (used by platform launchers).

set -euo pipefail

echo "Checking for .NET 8.0 SDK installation..."

# Prefer an already-installed 8.x SDK (even if the default host is 9.x).
export PATH="${HOME}/.dotnet:${PATH}"
if command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -qE '^8\.'; then
    echo ".NET 8.x SDK already installed:"
    dotnet --list-sdks | grep -E '^8\.' || true
    exit 0
fi

OS="$(uname -s 2>/dev/null || echo unknown)"

# macOS: Homebrew first when available
if [[ "$OS" == "Darwin" ]] && command -v brew >/dev/null 2>&1; then
    echo "Installing .NET 8.0 SDK using Homebrew..."
    echo "This may take a few minutes. Please wait..."
    echo ""
    if brew install --cask dotnet-sdk; then
        sleep 2
        export PATH="${HOME}/.dotnet:${PATH}"
        if command -v dotnet >/dev/null 2>&1; then
            echo "Verified: .NET version $(dotnet --version) is now available."
            exit 0
        fi
        echo "Warning: .NET was installed but may require a new terminal session."
        exit 0
    fi
    echo "Failed to install .NET 8.0 SDK using Homebrew."
fi

# Linux: try distro packages when apt is present (Ubuntu/Debian-style)
if [[ "$OS" == "Linux" ]] && command -v apt-get >/dev/null 2>&1; then
    echo "Attempting apt install of .NET 8 SDK (may need sudo)..."
    if sudo apt-get update -y && sudo apt-get install -y dotnet-sdk-8.0; then
        sleep 2
        if command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -qE '^8\.'; then
            echo "Verified: .NET 8.x SDK is available via apt."
            exit 0
        fi
    else
        echo "apt install did not succeed; falling back to official install script."
    fi
fi

echo ""
echo "Using official Microsoft dotnet-install script..."

INSTALL_SCRIPT_PATH="/tmp/dotnet-install.sh"
INSTALL_SCRIPT_URL="https://dot.net/v1/dotnet-install.sh"

echo "Downloading .NET installation script..."
if ! curl -fsSL "$INSTALL_SCRIPT_URL" -o "$INSTALL_SCRIPT_PATH"; then
    echo "Failed to download $INSTALL_SCRIPT_URL"
    echo "Install .NET 8 SDK manually: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

chmod +x "$INSTALL_SCRIPT_PATH"
echo "Installing .NET 8.0 SDK to \$HOME/.dotnet ..."
bash "$INSTALL_SCRIPT_PATH" --channel 8.0

DOTNET_PATH="$HOME/.dotnet"
export PATH="$DOTNET_PATH:$PATH"

append_path_line() {
    local rcfile="$1"
    local line='export PATH="$HOME/.dotnet:$PATH"'
    touch "$rcfile"
    if ! grep -Fqx "$line" "$rcfile" 2>/dev/null; then
        echo "" >> "$rcfile"
        echo "# Added by Dungeon Fighter v2 install-dotnet.sh" >> "$rcfile"
        echo "$line" >> "$rcfile"
        echo "Added PATH line to $rcfile"
    fi
}

# Persist PATH for common shells
append_path_line "$HOME/.bashrc"
[[ -f "$HOME/.zshrc" || "$OS" == "Darwin" ]] && append_path_line "$HOME/.zshrc"
[[ -f "$HOME/.bash_profile" ]] && append_path_line "$HOME/.bash_profile"
[[ -f "$HOME/.profile" ]] && append_path_line "$HOME/.profile"

rm -f "$INSTALL_SCRIPT_PATH"

sleep 1
if command -v dotnet >/dev/null 2>&1; then
    echo ""
    echo ".NET installed. Version: $(dotnet --version)"
    if dotnet --list-sdks 2>/dev/null | grep -qE '^8\.'; then
        echo ".NET 8.x SDK is available."
        exit 0
    fi
    echo "Warning: dotnet is on PATH but no 8.x SDK was listed. Open a new terminal and retry."
    exit 0
fi

echo ""
echo "Failed to install .NET 8.0 SDK automatically."
echo ""
echo "Please install .NET 8.0 SDK manually:"
echo "  https://dotnet.microsoft.com/download/dotnet/8.0"
echo ""

open_download_page() {
    local url="https://dotnet.microsoft.com/download/dotnet/8.0"
    if command -v xdg-open >/dev/null 2>&1; then
        xdg-open "$url" >/dev/null 2>&1 || true
    elif command -v open >/dev/null 2>&1; then
        open "$url" || true
    fi
}

if [[ -t 0 ]]; then
    read -r -p "Open the download page now? (Y/N) " response || true
    if [[ "${response:-}" =~ ^[Yy]$ ]]; then
        open_download_page
    fi
fi

exit 1
