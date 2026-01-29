#!/bin/bash
set -e

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

REPO_URL="https://github.com/meedoomostafa/sarab.git"
INSTALL_DIR="Sarab"

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

echo -e "${BLUE}Building Sarab...${NC}"

# Detect OS
OS="$(uname -s)"
case "${OS}" in
    Linux*)     RID=linux-x64;;
    Darwin*)    RID=osx-x64;;
    CYGWIN*|MINGW*|MSYS*) RID=win-x64;;
    *)          RID=linux-x64;;
esac

echo -e "${BLUE}Target: ${RID}${NC}"
dotnet publish Sarab.Cli/Sarab.Cli.csproj -c Release -r $RID --self-contained true -p:PublishSingleFile=true -o ./dist

# Handle Windows extension
BINARY_NAME="Sarab.Cli"
if [[ "$RID" == "win-x64" ]]; then
    BINARY_NAME="Sarab.Cli.exe"
fi

echo -e "${BLUE}Installing to ~/.local/bin...${NC}"
mkdir -p ~/.local/bin
cp "./dist/$BINARY_NAME" ~/.local/bin/sarab
chmod +x ~/.local/bin/sarab

# Add to PATH (Fish Support)
if command -v fish &> /dev/null; then
    # If using fish, add path permanently
    if [[ "$SHELL" == *"fish"* ]]; then
        echo -e "${BLUE}Fish shell detected. Adding to path...${NC}"
        fish -c "fish_add_path -U ~/.local/bin"
    fi
fi

echo -e "${GREEN}Installation Complete.${NC}"
echo -e "Run 'sarab --version' to verify."
