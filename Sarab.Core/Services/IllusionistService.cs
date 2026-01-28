using System;
using System.Threading.Tasks;
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
        // Retrieve token
        var token = await _rotator.GetNextTokenAsync();
        if (token == null)
        {
            throw new Exception("No active tokens found. Please add a token using 'sarab token add'.");
        }

        // Ensure binary
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
            throw new Exception("Auto-generated subdomains require a default zone logic which is not yet implemented. Please provide a full hostname via --subdomain (e.g. --subdomain app.yourdomain.com).");
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

        try
        {
            // Start tunnel
            var localUrl = $"http://localhost:{port}";
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
}
