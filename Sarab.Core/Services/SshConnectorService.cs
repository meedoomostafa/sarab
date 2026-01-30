using Sarab.Core.Interfaces;

namespace Sarab.Core.Services;

public class SshConnectorService
{
    private readonly IArtifactStore _artifactStore;

    public SshConnectorService(IArtifactStore artifactStore)
    {
        _artifactStore = artifactStore;
    }

    public async Task<(string Executable, string Arguments)> PrepareSshCommandAsync(string target, string? identityFile)
    {
        var binPath = await _artifactStore.EnsureCloudflaredBinaryAsync();

        // ProxyCommand
        var proxyCmd = $"{binPath} access ssh --hostname %h";

        var args = $"-o \"ProxyCommand={proxyCmd}\" {target}";

        if (!string.IsNullOrEmpty(identityFile))
        {
            args += $" -i \"{identityFile}\"";
        }

        return ("ssh", args);
    }
}
