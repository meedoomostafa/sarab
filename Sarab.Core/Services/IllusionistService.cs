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

        // Prepare environment
        await _processManager.EnsureBinaryExistsAsync();

        // TODO: Create tunnel and DNS records

        // Start tunnel
        var localUrl = $"http://localhost:{port}";
        await _processManager.StartTunnelAsync(token.ApiToken, localUrl);
    }
}
