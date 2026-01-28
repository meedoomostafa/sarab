using System;
using System.IO;
using System.Threading.Tasks;
using CliWrap;
using Sarab.Core.Interfaces;

namespace Sarab.Infrastructure.Services;

public class ProcessManager : IProcessManager
{
    private const string BinaryName = "cloudflared"; // Assuming it's in PATH or current dir

    public async Task EnsureBinaryExistsAsync()
    {
        // Simple check: see if we can run `cloudflared --version`
        try
        {
            await Cli.Wrap(BinaryName)
                     .WithArguments("--version")
                     .ExecuteAsync();
        }
        catch
        {
            throw new FileNotFoundException("cloudflared binary not found in PATH. Please install it or run 'sarab init'.");
        }
    }

    public async Task StartTunnelAsync(string tunnelToken, string url)
    {
        // "tunnel run" usually requires a named tunnel or a quick tunnel.
        // For quick tunnel (trycloudflare): `cloudflared tunnel --url http://localhost:8000`
        // For named tunnel: `cloudflared tunnel run --token <TOKEN>`

        // Assuming we are using Named Tunnels via Token
        var cmd = Cli.Wrap(BinaryName)
                     .WithArguments(args => args
                         .Add("tunnel")
                         .Add("run")
                         .Add("--token")
                         .Add(tunnelToken)
                         .Add("--url") // Map traffic to this local URL
                         .Add(url)
                     )
                     // Connect stdout/stderr to console for now, or capture it
                     .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
                     .WithStandardErrorPipe(PipeTarget.ToDelegate(Console.Error.WriteLine));

        // This is a long-running process. We shouldn't await it directly if we want the CLI to stay responsive
        // unless we want to block until user hits Ctrl+C.
        // For the CLI "expose" command, blocking is fine.
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
