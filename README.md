# Sarab

Sarab is a CLI tool designed to creates temporary public URLs for local services using Cloudflare Tunnels. It automates the process of creating tunnels, DNS records, and ingress rules, providing a streamlined experience for exposing local ports to the internet.

## Features

*   **Instant Exposure:** Quickly expose local ports (e.g., `localhost:3000`) to public subdomains.
*   **Multi-Identity Support:** Manage and switch between multiple Cloudflare API tokens.
*   **Zero-Configuration:** Automates DNS CNAME creation, tunnel setup, and ingress routing.
*   **Automatic Cleanup:** Removes tunnels and DNS records upon exit to prevent stale records.
*   **Self-Contained:** Distributed as a single binary with no external runtime dependencies.

## Installation

### From Source

You can build and install Sarab directly from the source code.

**Prerequisites:**
*   .NET 10 SDK (Required only for building from source)
*   Cloudflare Account (Required for API Tokens)

**Installation Script:**

The provided script will detect your operating system, build the project, and install the binary to your local path.

```bash
# Download and run the script directly (Recommended)
curl -sL https://raw.githubusercontent.com/meedoomostafa/sarab/main/install.sh | bash
```

Alternatively, you can clone and run it manually:

```bash
chmod +x install.sh
./install.sh
```

This script will:
1.  **Fetch the Source**: Clones the repository if you don't have it (or updates it if you do).
2.  **Autodetect OS**: Builds for Linux, macOS, or Windows/WSL.
3.  **Install Binary**: Places `sarab` in `~/.local/bin/`.

> **Note:** Ensure `~/.local/bin` is in your `PATH`.
> Add `export PATH="$HOME/.local/bin:$PATH"` to your `.bashrc` or `.zshrc` if needed.

### Manual Build

If you prefer to build manually:

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

### Uninstallation

To remove the Sarab binary, run the uninstall script:

```bash
./uninstall.sh
```

If you installed via the one-liner and don't have the repository cloned, you can manually remove the binary:

```bash
rm ~/.local/bin/sarab
```

This will remove the `sarab` binary from `~/.local/bin`.
To fully remove the source code, you can delete the repository folder:

```bash
rm -rf ~/myTools/Sarab
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
