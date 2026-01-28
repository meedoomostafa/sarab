using System.CommandLine;
using System.Threading.Tasks;
using Spectre.Console;
using Sarab.Core.Services;

namespace Sarab.Cli.Commands;

public class ExposeCommand : Command
{
    private readonly IllusionistService _service;

    public ExposeCommand(IllusionistService service)
        : base("expose", "Expose a local port to the internet")
    {
        _service = service;
        var portArg = new Argument<int>("port", "The local port to expose");
        var subdomainOption = new Option<string?>("--subdomain", "Request a specific subdomain");

        AddArgument(portArg);
        AddOption(subdomainOption);

        this.SetHandler(ExecuteAsync, portArg, subdomainOption);
    }

    private async Task ExecuteAsync(int port, string? subdomain)
    {
        AnsiConsole.MarkupLine($"[bold yellow]Exposing port {port}...[/]");
        if (!string.IsNullOrEmpty(subdomain))
        {
            AnsiConsole.MarkupLine($"Requesting subdomain: [blue]{subdomain}[/]");
        }

        try
        {
            await _service.ExposePortAsync(port, subdomain);

            // Process blocks until tunnel stops
            AnsiConsole.MarkupLine("[yellow]Tunnel stopped.[/]");
        }
        catch (System.Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }
    }
}
