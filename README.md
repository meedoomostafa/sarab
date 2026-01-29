# Sarab

Sarab is a CLI tool designed to creates temporary public URLs for local services using Cloudflare Tunnels. It automates the process of creating tunnels, DNS records, and ingress rules, providing a streamlined experience for exposing local ports to the internet.

## Features

*   **Instant Exposure:** Quickly expose local ports (e.g., `localhost:3000`) to public subdomains.
*   **Multi-Identity Support:** Manage and switch between multiple Cloudflare API tokens.
*   **Zero-Configuration:** Automates DNS CNAME creation, tunnel setup, and ingress routing.
*   **Automatic Cleanup:** Removes tunnels and DNS records upon exit to prevent stale records.
*   **Self-Contained:** Distributed as a single binary with no external runtime dependencies.

## Installation

### Quick Install (One-Liner)

**Linux / macOS:**
```bash
curl -sL https://raw.githubusercontent.com/meedoomostafa/sarab/main/install.sh | bash
```

**Windows (PowerShell):**
```powershell
irm https://raw.githubusercontent.com/meedoomostafa/sarab/main/install.ps1 | iex
```

> **Note:** Ensure `~/.local/bin` (Linux/macOS) or the install directory (Windows) is in your `PATH`.

### From Source

If you prefer to build from source:

**Prerequisites:**
*   .NET 10 SDK
*   Cloudflare Account (for API Tokens)

**Linux:**
```bash
git clone https://github.com/meedoomostafa/sarab.git
cd sarab
chmod +x install.sh && ./install.sh
```

**Windows:**
```powershell
git clone https://github.com/meedoomostafa/sarab.git
cd sarab
.\install.ps1
```

### Manual Build

**Linux:**
```bash
dotnet publish Sarab.Cli/Sarab.Cli.csproj -r linux-x64 --self-contained true -o ./publish
```

**macOS:**
```bash
dotnet publish Sarab.Cli/Sarab.Cli.csproj -r osx-x64 --self-contained true -o ./publish
```

**Windows:**
```powershell
dotnet publish Sarab.Cli/Sarab.Cli.csproj -r win-x64 --self-contained true -o ./publish
```

---

## Uninstallation

**Linux / macOS:**
```bash
curl -sL https://raw.githubusercontent.com/meedoomostafa/sarab/main/uninstall.sh | bash
```

**Windows (PowerShell):**
```powershell
irm https://raw.githubusercontent.com/meedoomostafa/sarab/main/uninstall.ps1 | iex
```

**Manual Removal:**
```bash
# Linux/macOS
rm ~/.local/bin/sarab
rm -rf ~/.sarab

# Windows (PowerShell)
Remove-Item "$env:LOCALAPPDATA\sarab" -Recurse -Force
```

## Usage

### 1. Initialization

Initialize the Sarab environment. This command creates the local database (`~/.sarab/sarab.db`) and downloads the required `cloudflared` binary if it is not present.

```bash
sarab init
```

### 2. Token Management

Sarab requires a Cloudflare API Token to interact with your account. You can manage multiple tokens using aliases.

**Add a Token:**
```bash
sarab token add <alias> <token>
```
*   `<alias>`: A unique name for this token (e.g., `personal`, `work`).
*   `<token>`: Your Cloudflare API Token (Permissions required: Zone.DNS, Account.Tunnel).

**List Tokens:**
Displays all stored tokens and their status.
```bash
sarab token list
```

**Remove a Token:**
```bash
sarab token rm <alias>
```

### 3. Expose Services

The primary command to expose a local service to the internet.

```bash
sarab expose <port> [options]
```

**Modes:**
*   **Quick Tunnel (Default):** If no tokens are added, Sarab creates a random `*.trycloudflare.com` URL. No account required.
*   **Authenticated Tunnel:** If a token is added, Sarab uses your Cloudflare account to create a stable, named tunnel on your custom domain.

**Arguments:**
*   `<port>`: The local port number to expose (e.g., `8080`).

**Options:**
*   `--subdomain <name>`: Request a specific subdomain (Authenticated mode only).
*   `--identity <alias>`: Use a specific token alias.
*   `--local-host <url>`: Override the local host address (e.g., `127.0.0.1`, `[::1]`). Useful if `localhost` resolution is ambiguous.
*   `--scheme <protocol>`: Specify the local protocol (`http` or `https`). Default is `http`.
*   `--no-tls-verify`: Disable TLS verification for local HTTPS services (useful for self-signed certificates).

**Examples:**
```bash
# Expose port 3000 (standard)
sarab expose 3000

# Expose a local HTTPS service with self-signed certs
sarab expose 8000 --scheme https --no-tls-verify

# Force IPv4 binding if 'localhost' defaults to IPv6
sarab expose 5280 --local-host 127.0.0.1

# Authenticated mode: Expose port 5000 as 'api-v1.yourdomain.com'
sarab expose 5000 --subdomain api-v1 --identity work
```

### 4. Maintenance

**List Active Tunnels:**
Displays a list of active tunnels created by Sarab across all configured identities.
```bash
sarab list
```

**Nuke (Emergency Cleanup):**
If Sarab was terminated forcefully and left "ghost" records, use this command to delete all tunnels and DNS records associated with Sarab.
```bash
sarab nuke
```
*   **Warning:** This will delete *all* resources named with the `sarab-` prefix.

## License
[MIT](LICENSE)
