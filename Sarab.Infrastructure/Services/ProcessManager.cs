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
                     .WithValidation(CommandResultValidation.None)
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
        var logHandler = CreateLogCleaner();

        // Execute tunnel via named token
        var cmd = Cli.Wrap(path)
                     .WithArguments(args => args
                         .Add("tunnel")
                         .Add("run")
                         .Add("--token")
                         .Add(tunnelToken)
                     )
                     .WithStandardOutputPipe(PipeTarget.ToDelegate(logHandler))
                     .WithStandardErrorPipe(PipeTarget.ToDelegate(logHandler));

        await cmd.ExecuteAsync();
    }

    public async Task StartQuickTunnelAsync(int port)
    {
        var path = await GetBinaryPathAsync();
        var logHandler = CreateLogCleaner();

        Console.WriteLine($"Starting Quick Tunnel (TryCloudflare) for port {port}...");

        var cmd = Cli.Wrap(path)
                     .WithArguments(args => args
                         .Add("tunnel")
                         .Add("--url")
                         .Add($"http://localhost:{port}")
                     )
                     .WithStandardOutputPipe(PipeTarget.ToDelegate(logHandler))
                     .WithStandardErrorPipe(PipeTarget.ToDelegate(logHandler));

        await cmd.ExecuteAsync();
    }

    private Action<string> CreateLogCleaner()
    {
        return line =>
        {
            // Strip timestamp and level (e.g., "2026-01-29T... INF ")
            var cleanLine = System.Text.RegularExpressions.Regex.Replace(line, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z\s+[A-Z]+\s+", "");

            // Filter out known noise
            if (cleanLine.StartsWith("Cannot determine default configuration path") ||
                cleanLine.StartsWith("Version ") ||
                cleanLine.StartsWith("GOOS: ") ||
                cleanLine.StartsWith("Settings: ") ||
                cleanLine.StartsWith("Autoupdate frequency") ||
                cleanLine.StartsWith("Generated Connector ID") ||
                cleanLine.StartsWith("Initial protocol") ||
                cleanLine.StartsWith("ICMP proxy") ||
                cleanLine.StartsWith("Starting metrics server") ||
                cleanLine.StartsWith("Tunnel connection curve") ||
                cleanLine.Contains("Thank you for trying Cloudflare Tunnel"))
            {
                return;
            }

            Console.WriteLine(cleanLine);
        };
    }

    public Task StopAllTunnelsAsync()
    {
        return Task.CompletedTask;
    }
}
