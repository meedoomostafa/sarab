using System.CommandLine;
using System.Threading.Tasks;
using Spectre.Console;
using Sarab.Core.Interfaces;
using Sarab.Core.Services;

namespace Sarab.Cli.Commands;

/// <summary>
/// CLI command for connecting to a remote RDP session through a Cloudflare Tunnel.
/// Starts a local TCP proxy via cloudflared, then the user connects their RDP client to it.
/// </summary>
public class RdpConnectCommand : Command
{
    private readonly RdpConnectorService _connectorService;
    private readonly IProcessManager _processManager;

    public RdpConnectCommand(RdpConnectorService connectorService, IProcessManager processManager)
        : base("rdp-connect", "Connect to a remote RDP session through a Cloudflare Tunnel")
    {
        _connectorService = connectorService;
        _processManager = processManager;

        var hostnameArg = new Argument<string>(
            "hostname",
            "The tunnel hostname (e.g. random-name.trycloudflare.com)");

        var portOption = new Option<int>(
            "--port",
            () => 13389,
            "Local port for the RDP proxy (default: 13389)");

        AddArgument(hostnameArg);
        AddOption(portOption);

        this.SetHandler(ExecuteAsync, hostnameArg, portOption);
    }

    private async Task ExecuteAsync(string hostname, int port)
    {
        AnsiConsole.MarkupLine($"[bold yellow]Starting RDP proxy for [blue]{Markup.Escape(hostname)}[/]...[/]");
        AnsiConsole.WriteLine();

        try
        {
            var (exe, args, localPort) = await _connectorService.PrepareRdpProxyAsync(hostname, port);

            DisplayConnectionInstructions(localPort);

            await _processManager.StartInteractiveShellAsync(exe, args);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]RDP proxy stopped.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
        }
    }

    private static void DisplayConnectionInstructions(int localPort)
    {
        AnsiConsole.MarkupLine($"  [green]RDP proxy listening on:[/] [bold]localhost:{localPort}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Connect your RDP client to the proxy address above. Examples:[/]");
        AnsiConsole.MarkupLine($"[dim]  Linux:   xfreerdp /v:localhost:{localPort} /u:USERNAME[/]");
        AnsiConsole.MarkupLine($"[dim]  macOS:   open rdp://localhost:{localPort}[/]");
        AnsiConsole.MarkupLine($"[dim]  Windows: mstsc /v:localhost:{localPort}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to stop the proxy.[/]");
        AnsiConsole.WriteLine();
    }
}
