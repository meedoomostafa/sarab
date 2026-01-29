using System.Threading.Tasks;

namespace Sarab.Core.Interfaces;

public interface IProcessManager
{
    Task EnsureBinaryExistsAsync();
    Task StartTunnelAsync(string tunnelToken, string url);
    Task StartQuickTunnelAsync(int port, string localHost, string scheme, bool noTlsVerify);
    Task StopAllTunnelsAsync();
}
