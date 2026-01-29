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
        var localhostOption = new Option<string>("--local-host", () => "localhost", "Check/Forward traffic to a specific local host");
        var schemeOption = new Option<string>("--scheme", () => "http", "Protocol scheme (http or https)");
        var noTlsVerifyOption = new Option<bool>("--no-tls-verify", "Disable TLS verification for local HTTPS service");

        AddArgument(portArg);
        AddOption(subdomainOption);
        AddOption(localhostOption);
        AddOption(schemeOption);
        AddOption(noTlsVerifyOption);

        this.SetHandler(ExecuteAsync, portArg, subdomainOption, localhostOption, schemeOption, noTlsVerifyOption);
    }

    private async Task ExecuteAsync(int port, string? subdomain, string localHost, string scheme, bool noTlsVerify)
    {
        AnsiConsole.MarkupLine($"[bold yellow]Exposing port {port}...[/]");
        if (!string.IsNullOrEmpty(subdomain))
        {
            AnsiConsole.MarkupLine($"Requesting subdomain: [blue]{subdomain}[/]");
        }

        try
        {
            await _service.ExposePortAsync(port, subdomain, localHost, scheme, noTlsVerify);

            // Process blocks until tunnel stops
            AnsiConsole.MarkupLine("[yellow]Tunnel stopped.[/]");
        }
        catch (System.Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }
    }
}
