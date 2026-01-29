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
        var identityOption = new Option<string?>("--identity", "Use a specific identity (token alias)");

        AddArgument(portArg);
        AddOption(subdomainOption);
        AddOption(localhostOption);
        AddOption(schemeOption);
        AddOption(noTlsVerifyOption);
        AddOption(identityOption);

        this.SetHandler(ExecuteAsync, portArg, subdomainOption, localhostOption, schemeOption, noTlsVerifyOption, identityOption);
    }

    private async Task ExecuteAsync(int port, string? subdomain, string localHost, string scheme, bool noTlsVerify, string? identity)
    {
        AnsiConsole.MarkupLine($"[bold yellow]Exposing port {port}...[/]");
        if (!string.IsNullOrEmpty(subdomain))
        {
            AnsiConsole.MarkupLine($"Requesting subdomain: [blue]{subdomain}[/]");
        }
        if (!string.IsNullOrEmpty(identity))
        {
            AnsiConsole.MarkupLine($"Requesting identity: [blue]{identity}[/]");
        }

        try
        {
            await _service.ExposePortAsync(port, subdomain, localHost, scheme, noTlsVerify, identity);

            // Process blocks until tunnel stops
            AnsiConsole.MarkupLine("[yellow]Tunnel stopped.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
        }
    }
}
