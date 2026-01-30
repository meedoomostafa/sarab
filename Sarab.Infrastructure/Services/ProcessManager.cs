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

    public async Task StartTunnelAsync(string tunnelToken, string url, string? configPath = null)
    {
        var path = await GetBinaryPathAsync();
        var logHandler = CreateLogCleaner();

        // Execute tunnel via named token
        var cmd = Cli.Wrap(path)
                     .WithArguments(args =>
                     {
                         args.Add("tunnel");

                         if (!string.IsNullOrEmpty(configPath))
                         {
                             args.Add("--config").Add(configPath);
                         }

                         args.Add("run")
                             .Add("--token")
                             .Add(tunnelToken);
                     })
                     .WithStandardOutputPipe(PipeTarget.ToDelegate(logHandler))
                     .WithStandardErrorPipe(PipeTarget.ToDelegate(logHandler));

        await cmd.ExecuteAsync();
    }

    public async Task StartQuickTunnelAsync(int port, string localHost, TunnelScheme scheme, bool noTlsVerify)
    {
        var path = await GetBinaryPathAsync();
        var logHandler = CreateLogCleaner();

        var protocol = scheme switch
        {
            TunnelScheme.HTTP => "http",
            TunnelScheme.HTTPS => "https",
            TunnelScheme.SSH => "ssh",
            TunnelScheme.RDP => "rdp",
            _ => throw new ArgumentOutOfRangeException(nameof(scheme), scheme, null)
        };
        var url = $"{protocol}://{localHost}:{port}";
        Console.WriteLine($"Starting Quick Tunnel (TryCloudflare) for {url}...");

        var cmd = Cli.Wrap(path)
                     .WithArguments(args =>
                     {
                         args.Add("tunnel")
                             .Add("--url")
                             .Add(url);

                         if (noTlsVerify)
                         {
                             args.Add("--no-tls-verify");
                         }
                     })
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
                cleanLine.StartsWith("Starting tunnel tunnelID") ||
                cleanLine.StartsWith("Registered tunnel connection") ||
                cleanLine.Contains("event=0") ||
                cleanLine.Contains("protocol=quic") ||
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
