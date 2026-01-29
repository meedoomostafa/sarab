#!/bin/bash
set -e

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

BINARY_PATH="$HOME/.local/bin/sarab"

echo -e "${BLUE}Uninstalling Sarab...${NC}"

if [ -f "$BINARY_PATH" ]; then
    echo -e "${BLUE}Removing binary at $BINARY_PATH...${NC}"
    rm "$BINARY_PATH"
    echo -e "${GREEN}Binary removed.${NC}"
else
    echo -e "${BLUE}Binary not found at $BINARY_PATH.${NC}"
fi

echo -e "${GREEN}Uninstallation of binary complete.${NC}"

# Also remove Sarab data directory
if [ -d "$HOME/.sarab" ]; then
    echo -e "${BLUE}Removing Sarab data directory at ~/.sarab...${NC}"
    rm -rf "$HOME/.sarab"
    echo -e "${GREEN}Data directory removed.${NC}"
fi

echo -e "${GREEN}Sarab has been completely uninstalled.${NC}"
