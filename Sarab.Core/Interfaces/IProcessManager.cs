using System.Threading.Tasks;

namespace Sarab.Core.Interfaces;

public interface IProcessManager
{
    Task EnsureBinaryExistsAsync();
    Task StartTunnelAsync(string tunnelToken, string url, string? configPath = null);
    Task StartQuickTunnelAsync(int port, string localHost, TunnelScheme scheme, bool noTlsVerify);
    Task StopAllTunnelsAsync();
}
