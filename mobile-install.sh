#!/bin/bash
# Sarab Mobile Install Script
# Supports: Termux (Android), iSH (iOS), a-Shell (iOS)
set -e

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[0;33m'
RED='\033[0;31m'
NC='\033[0m'

# Detect Environment
detect_environment() {
    if [ -n "$TERMUX_VERSION" ]; then
        ENV_TYPE="termux"
        PKG_MANAGER="pkg"
        INSTALL_DIR="$HOME/.local/bin"
        BINARY_DEST="$INSTALL_DIR/sarab"
    elif [ -f "/etc/alpine-release" ] && [ -d "/proc/ish" ]; then
        ENV_TYPE="ish"
        PKG_MANAGER="apk"
        INSTALL_DIR="$HOME/.local/bin"
        BINARY_DEST="$INSTALL_DIR/sarab"
    elif [ -n "$ASHELL_VERSION" ] || command -v pickFolder &> /dev/null; then
        ENV_TYPE="ashell"
        PKG_MANAGER="none"
        INSTALL_DIR="$HOME/Documents"
        BINARY_DEST="$INSTALL_DIR/sarab"
    else
        # Fallback detection for Alpine (iSH without proc/ish)
        if [ -f "/etc/alpine-release" ]; then
            ENV_TYPE="ish"
            PKG_MANAGER="apk"
            INSTALL_DIR="$HOME/.local/bin"
            BINARY_DEST="$INSTALL_DIR/sarab"
        else
            ENV_TYPE="unknown"
            PKG_MANAGER="unknown"
            INSTALL_DIR="$HOME/.local/bin"
            BINARY_DEST="$INSTALL_DIR/sarab"
        fi
    fi
}

# Detect Architecture
detect_arch() {
    ARCH="$(uname -m)"
    case "$ARCH" in
        aarch64|arm64)
            RID="linux-arm64"
            ;;
        armv7*|armv8l)
            RID="linux-arm"
            ;;
        x86_64|amd64)
            RID="linux-x64"
            ;;
        i*86|x86)
            RID="linux-x86"
            ;;
        *)
            echo -e "${RED}Unsupported architecture: $ARCH${NC}"
            exit 1
            ;;
    esac
}

# Install dependencies
install_deps_termux() {
    echo -e "${BLUE}Installing dependencies via pkg...${NC}"
    pkg update -y
    pkg install -y curl wget git
}

install_deps_ish() {
    echo -e "${BLUE}Installing dependencies via apk...${NC}"
    apk update
    apk add curl wget git bash gcompat libstdc++
}

install_deps_ashell() {
    echo -e "${YELLOW}a-Shell has limited package support.${NC}"
    echo -e "${YELLOW}Ensure 'curl' command is available.${NC}"
}

install_dependencies() {
    case "$ENV_TYPE" in
        termux)
            install_deps_termux
            ;;
        ish)
            install_deps_ish
            ;;
        ashell)
            install_deps_ashell
            ;;
        *)
            echo -e "${YELLOW}Unknown environment. Skipping dependency install.${NC}"
            ;;
    esac
}

# Download and install Sarab binary
download_release() {
    ASSET_NAME="sarab-${RID}.tar.gz"
    DOWNLOAD_URL="https://github.com/meedoomostafa/sarab/releases/latest/download/${ASSET_NAME}"
    
    echo -e "${BLUE}Downloading Sarab for ${RID}...${NC}"
    echo -e "${BLUE}URL: $DOWNLOAD_URL${NC}"
    
    TEMP_DIR=$(mktemp -d)
    
    if curl -sL -f -o "$TEMP_DIR/$ASSET_NAME" "$DOWNLOAD_URL"; then
        echo -e "${GREEN}Download successful.${NC}"
        
        echo -e "${BLUE}Extracting...${NC}"
        tar -xzf "$TEMP_DIR/$ASSET_NAME" -C "$TEMP_DIR"
        
        # Find the binary
        if [ -f "$TEMP_DIR/sarab" ]; then
            SOURCE_BIN="$TEMP_DIR/sarab"
        elif [ -f "$TEMP_DIR/Sarab.Cli" ]; then
            SOURCE_BIN="$TEMP_DIR/Sarab.Cli"
        else
            echo -e "${RED}Binary not found in archive.${NC}"
            rm -rf "$TEMP_DIR"
            return 1
        fi
        
        echo -e "${BLUE}Installing to $INSTALL_DIR...${NC}"
        mkdir -p "$INSTALL_DIR"
        cp "$SOURCE_BIN" "$BINARY_DEST"
        chmod +x "$BINARY_DEST"
        
        rm -rf "$TEMP_DIR"
        return 0
    else
        echo -e "${RED}Download failed. Release may not exist for $RID.${NC}"
        rm -rf "$TEMP_DIR"
        return 1
    fi
}

# Setup PATH
setup_path() {
    case "$ENV_TYPE" in
        termux)
            # Termux uses .bashrc or .zshrc
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
            # Also add to current session
            export PATH="$HOME/.local/bin:$PATH"
            ;;
        ish)
            # iSH uses ash/bash
            PROFILE="$HOME/.profile"
            if ! grep -q "\.local/bin" "$PROFILE" 2>/dev/null; then
                echo 'export PATH="$HOME/.local/bin:$PATH"' >> "$PROFILE"
                echo -e "${GREEN}✔ Added to .profile${NC}"
            fi
            export PATH="$HOME/.local/bin:$PATH"
            ;;
        ashell)
            echo -e "${YELLOW}a-Shell: Binary installed to $INSTALL_DIR${NC}"
            echo -e "${YELLOW}Run with: ~/Documents/sarab${NC}"
            ;;
        *)
            export PATH="$INSTALL_DIR:$PATH"
            ;;
    esac
}

# Initialize Sarab
initialize_sarab() {
    echo -e "${BLUE}Initializing Sarab...${NC}"
    if "$BINARY_DEST" init 2>/dev/null; then
        echo -e "${GREEN}✔ Sarab initialized.${NC}"
    else
        echo -e "${YELLOW}Note: Run 'sarab init' manually if needed.${NC}"
    fi
}

# Print environment-specific notes
print_notes() {
    echo ""
    echo -e "${GREEN}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${GREEN}  Sarab installed successfully!${NC}"
    echo -e "${GREEN}═══════════════════════════════════════════════════════════════${NC}"
    echo ""
    
    case "$ENV_TYPE" in
        termux)
            echo -e "${BLUE}Termux Notes:${NC}"
            echo -e "  • Restart Termux or run: source ~/.bashrc"
            echo -e "  • To expose port 8080: sarab expose 8080"
            echo -e "  • For SSH tunneling: sarab expose 22 --scheme ssh"
            echo ""
            echo -e "${YELLOW}Tip: Install openssh to expose your phone's terminal:${NC}"
            echo -e "  pkg install openssh"
            echo -e "  sshd  # Starts SSH server on port 8022"
            echo -e "  sarab expose 8022 --scheme ssh"
            ;;
        ish)
            echo -e "${BLUE}iSH Notes:${NC}"
            echo -e "  • iSH emulates x86, performance may be limited"
            echo -e "  • Restart iSH or run: source ~/.profile"
            echo -e "  • To expose port 8080: sarab expose 8080"
            echo ""
            echo -e "${YELLOW}Limitations:${NC}"
            echo -e "  • iSH runs in user-space emulation (slow)"
            echo -e "  • Consider using Sarab on a desktop for better performance"
            ;;
        ashell)
            echo -e "${BLUE}a-Shell Notes:${NC}"
            echo -e "  • Run Sarab with: ~/Documents/sarab"
            echo -e "  • a-Shell has limited networking capabilities"
            echo ""
            echo -e "${YELLOW}Recommendation:${NC}"
            echo -e "  • a-Shell is best for the 'connect' command (client-side)"
            echo -e "  • Example: ~/Documents/sarab connect user@tunnel.trycloudflare.com"
            ;;
        *)
            echo -e "  • Run 'sarab --version' to verify installation"
            echo -e "  • Run 'sarab --help' for usage"
            ;;
    esac
    echo ""
}

# Alternative: Client-only mode for limited environments
print_client_only_instructions() {
    echo ""
    echo -e "${YELLOW}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${YELLOW}  Alternative: SSH Client Without Sarab${NC}"
    echo -e "${YELLOW}═══════════════════════════════════════════════════════════════${NC}"
    echo ""
    echo -e "If Sarab doesn't work on your device, you can still connect to"
    echo -e "exposed SSH servers using cloudflared directly:"
    echo ""
    echo -e "${BLUE}1. Install cloudflared:${NC}"
    case "$ENV_TYPE" in
        termux)
            echo -e "   pkg install cloudflared"
            ;;
        ish)
            echo -e "   apk add cloudflared"
            ;;
        *)
            echo -e "   # Download from: https://github.com/cloudflare/cloudflared/releases"
            ;;
    esac
    echo ""
    echo -e "${BLUE}2. Connect to SSH tunnel:${NC}"
    echo -e "   ssh -o ProxyCommand='cloudflared access tcp --hostname %h' user@tunnel.trycloudflare.com"
    echo ""
}


# ------------------------ Main Script ------------------------

echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}  Sarab Mobile Installer${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

# Detect environment and architecture
detect_environment
detect_arch

echo -e "${GREEN}Environment:${NC} $ENV_TYPE"
echo -e "${GREEN}Architecture:${NC} $ARCH → $RID"
echo ""

# Confirm with user
if [ "$1" != "-y" ] && [ "$1" != "--yes" ]; then
    read -p "Continue with installation? [Y/n] " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Nn]$ ]]; then
        echo -e "${YELLOW}Installation cancelled.${NC}"
        exit 0
    fi
fi

# Install dependencies
install_dependencies

# Download and install
if download_release; then
    setup_path
    initialize_sarab
    print_notes
else
    echo ""
    echo -e "${RED}Failed to download pre-built binary.${NC}"
    echo -e "${YELLOW}This likely means no release exists for $RID yet.${NC}"
    echo ""
    print_client_only_instructions
    exit 1
fi
