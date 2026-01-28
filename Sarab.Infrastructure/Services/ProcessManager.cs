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
        // Verify binary existence
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
        // Execute tunnel via named token
        var cmd = Cli.Wrap(BinaryName)
                     .WithArguments(args => args
                         .Add("tunnel")
                         .Add("run")
                         .Add("--token")
                         .Add(tunnelToken)
                         .Add("--url") // Map traffic to this local URL
                         .Add(url)
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
