using Sarab.Core.Interfaces;

namespace Sarab.Core.Services;

/// <summary>
/// Prepares RDP connections through Cloudflare Tunnels using cloudflared access tcp.
/// Mirrors the SSH pattern but uses a local TCP proxy instead of ProxyCommand.
/// </summary>
public class RdpConnectorService
{
    private const int DEFAULT_LOCAL_PROXY_PORT = 13389;
    private readonly IArtifactStore _artifactStore;

    public RdpConnectorService(IArtifactStore artifactStore)
    {
        _artifactStore = artifactStore;
    }

    /// <summary>
    /// Prepares the cloudflared access tcp command for RDP proxying.
    /// </summary>
    /// <param name="hostname">The tunnel hostname (e.g. random-name.trycloudflare.com).</param>
    /// <param name="localPort">Local port to listen on for RDP connections.</param>
    /// <returns>The executable path and arguments to start the TCP proxy.</returns>
    public async Task<(string Executable, string Arguments, int LocalPort)> PrepareRdpProxyAsync(
        string hostname, int localPort = DEFAULT_LOCAL_PROXY_PORT)
    {
        var binPath = await _artifactStore.EnsureCloudflaredBinaryAsync();

        var arguments = $"access tcp --hostname {hostname} --url localhost:{localPort}";

        return (binPath, arguments, localPort);
    }
}
