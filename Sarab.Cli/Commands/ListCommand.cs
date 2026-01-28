using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Sarab.Core.Services;
using Spectre.Console;

namespace Sarab.Cli.Commands;

public class ListCommand : Command
{
    private readonly IllusionistService _illusionist;

    public ListCommand(IllusionistService illusionist)
        : base("list", "List all active mirages (tunnels) created by Sarab.")
    {
        _illusionist = illusionist;
        this.SetHandler(ExecuteAsync);
    }

    private async Task ExecuteAsync()
    {
        await AnsiConsole.Status()
            .StartAsync("Scanning Cloudflare accounts...", async ctx =>
            {
                var mirages = await _illusionist.ListMiragesAsync();

                if (mirages.Count == 0 || mirages.All(x => x.Value.Count == 0))
                {
                    AnsiConsole.MarkupLine("[yellow]No active mirages found.[/]");
                    return;
                }

                var table = new Table();
                table.AddColumn("Identity");
                table.AddColumn("Tunnel Name");
                table.AddColumn("Tunnel ID");
                table.AddColumn("Created/Deleted");

                foreach (var identity in mirages)
                {
                    foreach (var tunnel in identity.Value)
                    {
                        table.AddRow(
                            new Markup($"[blue]{identity.Key}[/]"),
                            new Markup($"[green]{tunnel.Name}[/]"),
                            new Markup($"[grey]{tunnel.Id}[/]"),
                            new Markup(tunnel.DeletedAt.HasValue ? "[red]Deleted[/]" : "[green]Active[/]")
                        );
                    }
                }

                AnsiConsole.Write(table);
            });
    }
}
