using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Sarab.Core.DTOs;
using Sarab.Core.Interfaces;
using Sarab.Core.Entities;

namespace Sarab.Core.Services;

public class IllusionistService
{
    private readonly TokenRotator _rotator;
    private readonly IProcessManager _processManager;
    private readonly ICloudflareAdapter _adapter;

    public IllusionistService(TokenRotator rotator, IProcessManager processManager, ICloudflareAdapter adapter)
    {
        _rotator = rotator;
        _processManager = processManager;
        _adapter = adapter;
    }

    public async Task ExposePortAsync(int port, string? subdomain = null, string localHost = "localhost", string scheme = "http", bool noTlsVerify = false, string? identity = null)
    {
        // Try to get a token
        Token? token = null;

        if (!string.IsNullOrEmpty(identity))
        {
            token = await _rotator.GetTokenByAliasAsync(identity);
            if (token == null)
            {
                throw new Exception($"Identity '{identity}' not found.");
            }
        }
        else
        {
            token = await _rotator.GetNextTokenAsync();
        }

        if (token == null)
        {
            // --- QUICK TUNNEL (TryCloudflare) ---
            Console.WriteLine("[INFO] No active tokens found. Falling back to TryCloudflare (Quick Tunnel)...");
            Console.WriteLine("[INFO] This will generate a random *.trycloudflare.com URL.");

            if (!string.IsNullOrEmpty(subdomain))
            {
                Console.WriteLine("[WARN] --subdomain is ignored in Quick Tunnel mode. Add a token to use custom domains.");
            }

            // Ensure binary exists first
            await _processManager.EnsureBinaryExistsAsync();

            await _processManager.StartQuickTunnelAsync(port, localHost, scheme, noTlsVerify);
            return;
        }

        // --- AUTHENTICATED TUNNEL (Custom Domain) ---
        Console.WriteLine($"[INFO] Using identity: '{token.Alias}'");
        Console.WriteLine($"[INFO] Account ID: {token.AccountId}");

        // Ensure binary exists first
        await _processManager.EnsureBinaryExistsAsync();

        // Refresh account ID
        if (string.IsNullOrEmpty(token.AccountId))
        {
            token.AccountId = await _adapter.VerifyTokenAsync(token.ApiToken);
            await _rotator.ReportFailureAsync(token);
        }

        string hostname;
        string zoneId;

        // Resolve zone
        if (string.IsNullOrEmpty(subdomain))
        {
            throw new Exception("Authenticated mode requires a subdomain. Use --subdomain <name>.");
        }
        else
        {
            hostname = subdomain;
            var zId = await _adapter.GetZoneIdAsync(token, hostname);
            if (zId == null)
            {
                throw new Exception($"Could not find a Cloudflare Zone for '{hostname}'. Ensure usage of a registered domain.");
            }
            zoneId = zId;
        }

        // Create tunnel
        var tunnelName = $"sarab-{Guid.NewGuid().ToString().Substring(0, 8)}";
        Console.WriteLine($"Creating tunnel: {tunnelName}...");
        var (tunnelId, createdToken) = await _adapter.CreateTunnelAsync(token, tunnelName);

        // Fetch credentials (with retry for propagation)
        string? tunnelToken = createdToken;

        if (string.IsNullOrEmpty(tunnelToken))
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    // Give Cloudflare a moment to propagate
                    await Task.Delay(1000 * (i + 1));
                    tunnelToken = await _adapter.GetTunnelTokenAsync(token, tunnelId);
                    break;
                }
                catch (Exception ex)
                {
                    if (i == 2) throw; // Rethrow on last attempt
                    Console.WriteLine($"[INFO] Waiting for tunnel token propagation... (Attempt {i + 1}/3)");
                }
            }
        }

        // Create DNS record
        Console.WriteLine($"Pointing {hostname} -> {tunnelName}...");
        string dnsRecordId;
        try
        {
            dnsRecordId = await _adapter.CreateDnsRecordAsync(token, zoneId, hostname, $"{tunnelId}.cfargotunnel.com");
        }
        catch (Exception ex) when (ex.Message.Contains("81053") || ex.Message.Contains("already exists"))
        {
            Console.WriteLine($"[WARN] DNS record for '{hostname}' already exists. Updating...");
            // Find existing record and update it (more efficient than delete+create)
            try
            {
                var records = await _adapter.ListDnsRecordsAsync(token, zoneId, hostname);
                if (records.Count > 0)
                {
                    // Update the first record, delete any duplicates
                    var primaryRecord = records[0];
                    Console.WriteLine($"[INFO] Updating record: {primaryRecord.Id} ({primaryRecord.Name})");
                    dnsRecordId = await _adapter.UpdateDnsRecordAsync(token, zoneId, primaryRecord.Id, hostname, $"{tunnelId}.cfargotunnel.com");

                    // Clean up any duplicate records (rare edge case)
                    for (int i = 1; i < records.Count; i++)
                    {
                        Console.WriteLine($"[INFO] Removing duplicate record: {records[i].Id}");
                        await _adapter.DeleteDnsRecordAsync(token, zoneId, records[i].Id);
                    }
                }
                else
                {
                    // No records found despite conflict error - retry creation
                    dnsRecordId = await _adapter.CreateDnsRecordAsync(token, zoneId, hostname, $"{tunnelId}.cfargotunnel.com");
                }
            }
            catch (Exception innerEx)
            {
                throw new Exception($"Failed to update DNS record: {innerEx.Message}");
            }
        }

        var localUrl = $"{scheme}://{localHost}:{port}";

        // Generate local config for cloudflared
        var configContent = $@"
tunnel: {tunnelId}
credentials-file: /dev/null
ingress:
  - hostname: {hostname}
    service: {localUrl}
    originRequest:
      noTLSVerify: {noTlsVerify.ToString().ToLower()}
  - service: http_status:404
";
        var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".sarab/tunnels/{tunnelName}.yml");
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        await File.WriteAllTextAsync(configPath, configContent);

        Console.WriteLine($"Configuring ingress (local) -> {localUrl}...");
        // API ConfigureTunnelAsync call removed in favor of local config

        try
        {
            // Start tunnel
            Console.WriteLine($"Mirage active at: https://{hostname}");
            Console.WriteLine("Press Ctrl+C to vanish.");

            await _processManager.StartTunnelAsync(tunnelToken, localUrl, configPath);
        }
        finally
        {
            // Cleanup
            Console.WriteLine("Cleaning up records...");
            try
            {
                await _adapter.DeleteDnsRecordAsync(token, zoneId, dnsRecordId);
                Console.WriteLine("Cleanup complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cleanup failed: {ex.Message}");
            }
        }
    }

    public async Task<Dictionary<string, List<TunnelDetail>>> ListMiragesAsync()
    {
        var result = new Dictionary<string, List<TunnelDetail>>();
        var tokens = await _rotator.GetAllTokensAsync();

        foreach (var token in tokens)
        {
            try
            {
                // Ensure Account ID is present for listing
                if (string.IsNullOrEmpty(token.AccountId))
                {
                    token.AccountId = await _adapter.VerifyTokenAsync(token.ApiToken);
                    await _rotator.ReportFailureAsync(token);
                }

                var tunnels = await _adapter.ListTunnelsAsync(token);
                // Filter for "sarab-" prefix
                var sarabTunnels = tunnels.Where(t => t.Name.StartsWith("sarab-")).ToList();

                if (sarabTunnels.Any())
                {
                    result.Add(token.Alias, sarabTunnels);
                }
            }
            catch
            {
                // Ignore tokens that fail to list (perm issues or invalid)
            }
        }

        return result;
    }

    public async Task NukeAsync()
    {
        var tokens = await _rotator.GetAllTokensAsync();
        foreach (var token in tokens)
        {
            Console.WriteLine($"Scanning identity '{token.Alias}'...");
            try
            {
                if (string.IsNullOrEmpty(token.AccountId))
                {
                    token.AccountId = await _adapter.VerifyTokenAsync(token.ApiToken);
                    await _rotator.ReportFailureAsync(token);
                }

                var tunnels = await _adapter.ListTunnelsAsync(token);
                var sarabTunnels = tunnels.Where(t => t.Name.StartsWith("sarab-")).ToList();

                if (sarabTunnels.Count == 0)
                {
                    Console.WriteLine("  No active illusions found.");
                    continue;
                }

                foreach (var tunnel in sarabTunnels)
                {
                    Console.WriteLine($"  - Nuking {tunnel.Name} ({tunnel.Id})...");
                    await _adapter.DeleteTunnelAsync(token, tunnel.Id);
                    Console.WriteLine("    [Deleted]");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [Error] {ex.Message}");
            }
        }
    }
}
