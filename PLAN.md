Here is the comprehensive **`plan.md`** for **Sarab**.

---

# Sarab (سراب): Master Development Plan

**Version:** 1.0.0
**Target Framework:** .NET 10 (Native AOT)
**Database:** SQLite
**License:** MIT

## 1. Project Overview

**Sarab** is a developer-centric CLI tool that creates "illusions" (public URLs) for local services. It acts as an intelligent automation wrapper around **Cloudflare Tunnels**, allowing developers to expose local ports to the internet without manual configuration.

### Core Features

* **Instant Expose:** One command to go from `localhost:3000` to `https://api.sarab.dev`.
* **Multi-Identity System:** Support for multiple Cloudflare API tokens. If one token hits a limit (rate limit or tunnel cap), Sarab automatically rotates to the next available token.
* **Zero-Dependency:** Distributed as a single, Native AOT binary.
* **Smart Cleanup:** Automatically removes DNS records and tunnels upon exit to prevent "Ghost Records."
* **Local State:** Uses SQLite to track tokens, active sessions, and preferences.

---

## 2. System Architecture

Sarab follows a **Modular Monolith** design with **Hexagonal Architecture**. This decouples the core logic from external dependencies (Cloudflare, OS Processes, Database).

### High-Level Components

1. **Presentation Layer (`Sarab.Cli`)**
* **Tech:** `System.CommandLine`, `Spectre.Console`.
* **Role:** Handles user input, rendering TUI (spinners/tables), and command routing.


2. **Application Layer (`Sarab.Core`)**
* **Tech:** Pure C#.
* **Role:**
* **`IllusionistService`**: Orchestrates the tunnel creation flow.
* **`TokenRotator`**: Logic to select the best API token and handle failovers.
* **`ProcessManager`**: Manages the `cloudflared` child process lifecycle.




3. **Infrastructure Layer (`Sarab.Infrastructure`)**
* **Tech:** `Refit` (HTTP), `CliWrap` (Process), `Microsoft.Data.Sqlite` (DB).
* **Role:**
* **`CloudflareAdapter`**: Communicates with Cloudflare API.
* **`SqliteRepository`**: Persists tokens and config.
* **`ArtifactStore`**: Manages the `cloudflared` binary download/cache.





---

## 3. Technology Stack

| Component | Technology | Rationale |
| --- | --- | --- |
| **Runtime** | .NET 10 (Native AOT) | Instant startup, single-file distribution. |
| **Database** | **SQLite** (`Microsoft.Data.Sqlite`) | Zero-config local storage. AOT compatible. |
| **ORM/Access** | **Dapper (AOT Mode)** / Raw SQL | Lightweight, high-performance data access. |
| **CLI Framework** | **Spectre.Console** | Best-in-class TUI and command parsing. |
| **HTTP Client** | **Refit** | Type-safe, clean API client definitions. |
| **Process Mgmt** | **CliWrap** | Robust handling of child processes (`cloudflared`). |
| **Resilience** | **Polly** | Retry policies for network operations and token rotation. |

---

## 4. Database Schema (SQLite)

The database will be stored at `~/.sarab/sarab.db`.

### Table: `Tokens`

Stores Cloudflare API credentials.

```sql
CREATE TABLE Tokens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Alias TEXT NOT NULL UNIQUE,      -- User-friendly name (e.g., "Personal", "Work")
    ApiToken TEXT NOT NULL,          -- The Cloudflare API Token
    AccountId TEXT,                  -- Cloudflare Account ID (fetched after validation)
    IsActive BOOLEAN DEFAULT 1,      -- Soft delete
    FailureCount INTEGER DEFAULT 0,  -- For smart rotation
    LastUsedAt DATETIME              -- To implement Least Recently Used (LRU) rotation
);

```

### Table: `Config`

Stores global user preferences.

```sql
CREATE TABLE Config (
    Key TEXT PRIMARY KEY,
    Value TEXT
);
-- Examples: "default_zone_id", "theme", "auto_update_binary"

```

---

## 5. CLI Command Reference

The user interface is designed to be intuitive and fast.

### A. Setup & Management

* **`sarab init`**
* Creates the `~/.sarab` directory and initializes the SQLite database.
* Downloads the `cloudflared` binary if missing.


* **`sarab token add <alias> <token>`**
* Validates the token with Cloudflare API.
* If valid, encrypts (optional) and saves it to the `Tokens` table.
* *Example:* `sarab token add main eyJ...`


* **`sarab token list`**
* Displays a table of stored tokens, their health status, and last usage.


* **`sarab token rm <alias>`**
* Removes a token from the database.



### B. Core Functionality (The Illusion)

* **`sarab expose <port>`**
* **Main Command.** Exposes the local port.
* **Options:**
* `--subdomain <name>`: Request a specific subdomain (e.g., `sarab expose 8080 --subdomain my-api`).
* `--identity <alias>`: Force usage of a specific token alias.
* `--local-host <host>`: Forward to a specific local address (default: `localhost`).


* **Logic:**
1. Selects a Token (Explicit or Auto-Rotate).
2. Creates Tunnel -> Creates DNS Record -> Runs Tunnel.
3. **Failover:** If Cloudflare returns a limit error, retry immediately with the next available token.





### C. Maintenance

* **`sarab list`**
* Shows currently active tunnels (managed by this instance).
* *Note: Since this is a CLI, this mostly applies if we implement a background daemon mode later. For now, it checks valid DNS records created by Sarab.*


* **`sarab nuke`**
* **Emergency Command.**
* Iterates through **ALL** stored tokens.
* Deletes all Tunnels and DNS records created by Sarab (identified by name pattern or metadata).
* Use case: Cleaning up "Ghost" records if the app crashed previously.



---

## 6. Implementation Strategy

### Phase 1: Foundation & Data Layer

1. **Project Setup:** Initialize .NET 10 solution with AOT enabled.
2. **SQLite Implementation:** Setup `DbContext` or Dapper repositories. Implement `InitCommand` to bootstrap the DB file.
3. **Token Management:** Implement `add`, `list`, `rm` commands.

### Phase 2: Cloudflare Integration (The Adapter)

1. **Refit Interface:** Define `ICloudflareApi`.
* Endoints: `VerifyToken`, `CreateTunnel`, `GetZones`, `CreateDnsRecord`, `DeleteDnsRecord`.


2. **Binary Manager:** Logic to download `cloudflared` from GitHub Releases based on OS/Arch.

### Phase 3: The Illusionist Engine

1. **Process Wrapping:** Use `CliWrap` to execute `cloudflared tunnel run`.
2. **Orchestrator:**
* Connect the "Create Tunnel -> DNS -> Run" pipeline.


3. **Multi-Token Logic:**
* Implement `TokenSelector`: `GetBestToken()`.
* Implement `RetryPolicy`: Catch `429 Too Many Requests` or `QuotaExceeded`, switch token, and retry.



### Phase 4: UI & Polish

1. **Spectre.Console:** Add spinners ("Creating Mirage..."), tables for output, and live logs.
2. **Signal Trapping:** Handle `Ctrl+C` to ensure DNS records are deleted before exit.

---

## 7. Sample Workflow (User Journey)

```bash
# 1. First time setup
$ sarab init
> [OK] Database initialized at ~/.sarab/sarab.db
> [OK] cloudflared binary ready.

# 2. Add keys (Primary and Backup)
$ sarab token add personal <TOKEN_1>
> [OK] Token 'personal' validated and saved.

$ sarab token add backup <TOKEN_2>
> [OK] Token 'backup' validated and saved.

# 3. Expose a server
$ sarab expose 5000 --subdomain bosla-api
> [INFO] Using identity: 'personal'
> [INFO] Creating Tunnel... [OK]
> [INFO] pointing bosla-api.yourdomain.com -> Tunnel [OK]
> [SUCCESS] Mirage is active! 
> ---------------------------------------------------
> 🌍 https://bosla-api.yourdomain.com
> ---------------------------------------------------
> Press Ctrl+C to vanish.

# (Scenario: Token 1 hits limit)
$ sarab expose 8000
> [INFO] Using identity: 'personal'
> [ERR] Cloudflare Limit Reached (Error 1033).
> [INFO] Switching identity... Now using 'backup'.
> [SUCCESS] Mirage is active!

```