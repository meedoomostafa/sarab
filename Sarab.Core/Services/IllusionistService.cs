using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Sarab.Core.DTOs;
using Sarab.Core.Interfaces;

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

    public async Task ExposePortAsync(int port, string? subdomain = null)
    {
        // 1. Ensure binary exists first
        await _processManager.EnsureBinaryExistsAsync();

        // 2. Try to get a token
        var token = await _rotator.GetNextTokenAsync();

        if (token == null)
        {
            // --- QUICK TUNNEL (TryCloudflare) ---
            Console.WriteLine("[INFO] No active tokens found. Falling back to TryCloudflare (Quick Tunnel)...");
            Console.WriteLine("[INFO] This will generate a random *.trycloudflare.com URL.");

            if (!string.IsNullOrEmpty(subdomain))
            {
                Console.WriteLine("[WARN] --subdomain is ignored in Quick Tunnel mode. Add a token to use custom domains.");
            }

            await _processManager.StartQuickTunnelAsync(port);
            return;
        }

        // --- AUTHENTICATED TUNNEL (Custom Domain) ---
        Console.WriteLine($"[INFO] Using identity: '{token.Alias}'");

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
        var tunnelId = await _adapter.CreateTunnelAsync(token, tunnelName);

        // Fetch credentials
        var tunnelToken = await _adapter.GetTunnelTokenAsync(token, tunnelId);

        // Create DNS record
        Console.WriteLine($"Pointing {hostname} -> {tunnelName}...");
        var dnsRecordId = await _adapter.CreateDnsRecordAsync(token, zoneId, hostname, $"{tunnelId}.cfargotunnel.com");

        var localUrl = $"http://localhost:{port}";
        // Configure Ingress
        Console.WriteLine($"Configuring ingress -> {localUrl}...");
        await _adapter.ConfigureTunnelAsync(token, tunnelId, hostname, localUrl);

        try
        {
            // Start tunnel
            Console.WriteLine($"Mirage active at: https://{hostname}");
            Console.WriteLine("Press Ctrl+C to vanish.");

            await _processManager.StartTunnelAsync(tunnelToken, localUrl);
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
