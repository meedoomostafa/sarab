using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Sarab.Core.Services;
using Spectre.Console;

namespace Sarab.Cli.Commands;

public class NukeCommand : Command
{
    private readonly IllusionistService _illusionist;

    public NukeCommand(IllusionistService illusionist)
        : base("nuke", "Emergency: Delete ALL Sarab tunnels active on your accounts.")
    {
        _illusionist = illusionist;
        this.SetHandler(ExecuteAsync);
    }

    private async Task ExecuteAsync()
    {
        if (!AnsiConsole.Confirm("[red]WARNING: This will delete ALL tunnels starting with 'sarab-' from ALL configured accounts. Are you sure?[/]", defaultValue: false))
        {
            AnsiConsole.MarkupLine("[grey]Operation cancelled.[/]");
            return;
        }

        await AnsiConsole.Status()
            .StartAsync("Nuking all mirages...", async ctx =>
            {
                await _illusionist.NukeAsync();
            });

        AnsiConsole.MarkupLine("[green]Nuke complete. All illusions have vanished.[/]");
    }
}
