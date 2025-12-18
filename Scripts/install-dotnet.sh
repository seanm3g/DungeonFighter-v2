#!/bin/bash

echo "Checking for .NET 8.0 SDK installation..."

# Check if Homebrew is available
if command -v brew &> /dev/null; then
    echo "Installing .NET 8.0 SDK using Homebrew..."
    echo "This may take a few minutes. Please wait..."
    echo ""
    
    brew install --cask dotnet-sdk
    
    if [ $? -eq 0 ]; then
        echo ""
        echo ".NET 8.0 SDK installed successfully!"
        
        # Verify installation
        sleep 2
        DOTNET_VERSION=$(dotnet --version 2>&1)
        if [ $? -eq 0 ]; then
            echo "Verified: .NET version $DOTNET_VERSION is now available."
            exit 0
        else
            echo "Warning: .NET was installed but may require a new terminal session."
            echo "Please restart this script or open a new terminal."
            exit 0
        fi
    else
        echo "Failed to install .NET 8.0 SDK using Homebrew."
    fi
fi

# Fallback: Use official dotnet-install script
echo ""
echo "Attempting alternative installation method..."

INSTALL_SCRIPT_PATH="/tmp/dotnet-install.sh"
INSTALL_SCRIPT_URL="https://dot.net/v1/dotnet-install.sh"

echo "Downloading .NET installation script..."
curl -sSL "$INSTALL_SCRIPT_URL" -o "$INSTALL_SCRIPT_PATH"

if [ $? -eq 0 ]; then
    echo "Installing .NET 8.0 SDK..."
    echo "This may take a few minutes. Please wait..."
    echo ""
    
    chmod +x "$INSTALL_SCRIPT_PATH"
    bash "$INSTALL_SCRIPT_PATH" --channel 8.0
    
    if [ $? -eq 0 ]; then
        echo ""
        echo ".NET 8.0 SDK installed successfully!"
        
        # Add to PATH if not already there
        DOTNET_PATH="$HOME/.dotnet"
        if [[ ":$PATH:" != *":$DOTNET_PATH:"* ]]; then
            echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.zshrc
            echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bash_profile
            export PATH="$HOME/.dotnet:$PATH"
        fi
        
        # Clean up install script
        rm -f "$INSTALL_SCRIPT_PATH"
        
        # Verify installation
        sleep 2
        DOTNET_VERSION=$(dotnet --version 2>&1)
        if [ $? -eq 0 ]; then
            echo "Verified: .NET version $DOTNET_VERSION is now available."
            exit 0
        else
            echo "Warning: .NET was installed but may require a new terminal session."
            exit 0
        fi
    else
        echo "Installation script returned an error."
    fi
fi

echo ""
echo "Failed to install .NET 8.0 SDK automatically."
echo ""
echo "Please install .NET 8.0 SDK manually:"
echo "1. Visit: https://dotnet.microsoft.com/download/dotnet/8.0"
echo "2. Download and run the .NET 8.0 SDK installer for macOS"
echo "3. Run the game launcher again"
echo ""

read -p "Would you like to open the download page now? (Y/N) " response
if [[ "$response" =~ ^[Yy]$ ]]; then
    open "https://dotnet.microsoft.com/download/dotnet/8.0"
fi

# Clean up install script
rm -f "$INSTALL_SCRIPT_PATH"

exit 1

