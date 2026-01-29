#!/bin/bash
set -e

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[0;33m'
RED='\033[0;31m'
NC='\033[0m'

REPO_URL="https://github.com/meedoomostafa/sarab.git"
INSTALL_DIR="Sarab"
BINARY_DEST="$HOME/.local/bin/sarab"

# Detect OS and Architecture
OS="$(uname -s)"
ARCH="$(uname -m)"

case "${OS}" in
    Linux*)     
        RID=linux-x64
        ASSET_EXT=".tar.gz"
        ;;
    Darwin*)    
        RID=osx-x64
        ASSET_EXT=".tar.gz"
        ;;
    CYGWIN*|MINGW*|MSYS*) 
        RID=win-x64
        ASSET_EXT=".zip"
        ;;
    *)          
        RID=linux-x64
        ASSET_EXT=".tar.gz"
        ;;
esac

ASSET_NAME="sarab-${RID}${ASSET_EXT}"
DOWNLOAD_URL="https://github.com/meedoomostafa/sarab/releases/latest/download/${ASSET_NAME}"

try_download_release() {
    echo -e "${BLUE}Attempting to download latest release...${NC}"
    
    if ! command -v curl &> /dev/null; then
        echo -e "${YELLOW}curl not found. Skipping download.${NC}"
        return 1
    fi

    TEMP_DIR=$(mktemp -d)
    
    echo -e "${BLUE}Downloading ${ASSET_NAME}...${NC}"
    if curl -sL -f -o "$TEMP_DIR/$ASSET_NAME" "$DOWNLOAD_URL"; then
        echo -e "${GREEN}Download successful.${NC}"
        
        echo -e "${BLUE}Extracting...${NC}"
        if [[ "$ASSET_EXT" == ".zip" ]]; then
            if ! command -v unzip &> /dev/null; then
                 echo -e "${YELLOW}unzip not found. Cannot extract zip.${NC}"
                 return 1
            fi
            unzip -q "$TEMP_DIR/$ASSET_NAME" -d "$TEMP_DIR"
        else
            tar -xzf "$TEMP_DIR/$ASSET_NAME" -C "$TEMP_DIR"
        fi
        
        # Find the binary
        if [ -f "$TEMP_DIR/sarab" ]; then
             SOURCE_BIN="$TEMP_DIR/sarab"
        elif [ -f "$TEMP_DIR/Sarab.Cli" ]; then
             SOURCE_BIN="$TEMP_DIR/Sarab.Cli"
        elif [ -f "$TEMP_DIR/Sarab.Cli.exe" ]; then
             SOURCE_BIN="$TEMP_DIR/Sarab.Cli.exe"
        else
             echo -e "${YELLOW}Binary not found in archive.${NC}"
             return 1
        fi

        echo -e "${BLUE}Installing to ~/.local/bin...${NC}"
        mkdir -p "$HOME/.local/bin"
        cp "$SOURCE_BIN" "$BINARY_DEST"
        chmod +x "$BINARY_DEST"
        
        rm -rf "$TEMP_DIR"
        return 0
    else
        echo -e "${YELLOW}Release not found or download failed (HTTP $?).${NC}"
        rm -rf "$TEMP_DIR"
        return 1
    fi
}

setup_path() {
    LOCAL_BIN="$HOME/.local/bin"
    
    # Add to Fish PATH if Fish is installed (regardless of current shell)
    if command -v fish &> /dev/null; then
        echo -e "${BLUE}Fish shell detected. Configuring PATH...${NC}"
        # Use fish_add_path for universal path (works across sessions)
        fish -c "fish_add_path -U $LOCAL_BIN" 2>/dev/null || true
        echo -e "${GREEN}✔ Fish PATH configured.${NC}"
    fi
    
    # Also add to bash/zsh if their config exists
    if [ -f "$HOME/.bashrc" ]; then
        if ! grep -q "\.local/bin" "$HOME/.bashrc"; then
            echo 'export PATH="$HOME/.local/bin:$PATH"' >> "$HOME/.bashrc"
            echo -e "${GREEN}✔ Added to .bashrc${NC}"
        fi
    fi
    
    if [ -f "$HOME/.zshrc" ]; then
        if ! grep -q "\.local/bin" "$HOME/.zshrc"; then
            echo 'export PATH="$HOME/.local/bin:$PATH"' >> "$HOME/.zshrc"
            echo -e "${GREEN}✔ Added to .zshrc${NC}"
        fi
    fi
}

# --- Main Logic ---

# 1. Try to download release
if try_download_release; then
    setup_path
    
    # Initialize Database
    echo -e "${BLUE}Initializing database...${NC}"
    "$BINARY_DEST" init
    
    echo -e "${GREEN}Installation Complete (from Release).${NC}"
    echo -e "Run 'sarab --version' to verify."
    exit 0
fi

# 2. Fallback: Build from Source
echo -e "${YELLOW}Falling back to building from source...${NC}"

# Check for .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}Error: .NET SDK not found.${NC}"
    echo -e "Please install .NET 10 SDK or wait for a release to be available."
    exit 1
fi

# Prepare Source
if [ -f "Sarab.sln" ]; then
    echo -e "${BLUE}Running inside repository.${NC}"
else
    if [ -d "$INSTALL_DIR" ]; then
        echo -e "${BLUE}Updating existing repository in ./$INSTALL_DIR...${NC}"
        cd "$INSTALL_DIR"
        git pull
    else
        echo -e "${BLUE}Cloning Sarab from $REPO_URL...${NC}"
        git clone "$REPO_URL" "$INSTALL_DIR"
        cd "$INSTALL_DIR"
    fi
fi

echo -e "${BLUE}Building Sarab for ${RID}...${NC}"
dotnet publish Sarab.Cli/Sarab.Cli.csproj -c Release -r $RID --self-contained true -p:PublishSingleFile=true -o ./dist

# Handle Windows binary name for build
BINARY_NAME="Sarab.Cli"
if [[ "$RID" == "win-x64" ]]; then
    BINARY_NAME="Sarab.Cli.exe"
fi

echo -e "${BLUE}Installing to ~/.local/bin...${NC}"
mkdir -p "$HOME/.local/bin"
cp "./dist/$BINARY_NAME" "$BINARY_DEST"
chmod +x "$BINARY_DEST"

setup_path

# Initialize Database
echo -e "${BLUE}Initializing database...${NC}"
"$BINARY_DEST" init

echo -e "${GREEN}Installation Complete (from Source).${NC}"
echo -e "Run 'sarab --version' to verify."
