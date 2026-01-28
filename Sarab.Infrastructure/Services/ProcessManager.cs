using System;
using System.IO;
using System.Threading.Tasks;
using CliWrap;
using Sarab.Core.Interfaces;

namespace Sarab.Infrastructure.Services;

public class ProcessManager : IProcessManager
{
    private readonly IArtifactStore _artifactStore;
    private string? _binaryPath;

    public ProcessManager(IArtifactStore artifactStore)
    {
        _artifactStore = artifactStore;
    }

    private async Task<string> GetBinaryPathAsync()
    {
        if (string.IsNullOrEmpty(_binaryPath))
        {
            _binaryPath = await _artifactStore.EnsureCloudflaredBinaryAsync();
        }
        return _binaryPath;
    }

    public async Task EnsureBinaryExistsAsync()
    {
        var path = await GetBinaryPathAsync();

        // Verify binary execution
        try
        {
            await Cli.Wrap(path)
                     .WithArguments("--version")
                     .ExecuteAsync();
        }
        catch (Exception ex)
        {
            throw new FileNotFoundException($"cloudflared binary at '{path}' is invalid or not executable. Error: {ex.Message}");
        }
    }

    public async Task StartTunnelAsync(string tunnelToken, string url)
    {
        var path = await GetBinaryPathAsync();

        // Execute tunnel via named token
        var cmd = Cli.Wrap(path)
                     .WithArguments(args => args
                         .Add("tunnel")
                         .Add("run")
                         .Add("--token")
                         .Add(tunnelToken)
                     // .Add("--url") // Named tunnels managed remotely don't use --url in 'run' command usually?
                     // Wait, if we use 'tunnel run', it connects to the cloud. 
                     // The INGRESS rules (which we configured via API) tell the cloud where to route traffic.
                     // But the LOCAL cloudflared process needs to know to proxy.
                     // For named tunnels, 'cloudflared tunnel run' reads ingress from remote config (if managed).
                     // So we DO NOT need --url here if config is remote.
                     // Remove --url argument.
                     )
                     // Redirect output
                     .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
                     .WithStandardErrorPipe(PipeTarget.ToDelegate(Console.Error.WriteLine));

        // Execute tunnel process
        await cmd.ExecuteAsync();
    }

    public Task StopAllTunnelsAsync()
    {
        // In a CLI context, stopping usually means killing this process tree.
        // If we spawned background processes, we'd need to track Process IDs.
        // For now, relying on OS cleanup or Ctrl+C propagation.
        return Task.CompletedTask;
    }
}
