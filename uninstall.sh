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
echo -e "${BLUE}To remove the repository and source code, run:${NC}"
echo -e "${RED}rm -rf \"$(pwd)\"${NC}"
