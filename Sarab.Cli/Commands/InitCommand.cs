using System.CommandLine;
using System.Threading.Tasks;
using Spectre.Console;
using Sarab.Core.Interfaces;
using System; // Added for Exception

namespace Sarab.Cli.Commands;

public class InitCommand : Command
{
    private readonly ITokenRepository _repository;
    private readonly IArtifactStore _artifactStore;

    public InitCommand(ITokenRepository repository, IArtifactStore artifactStore)
        : base("init", "Initialize Sarab environment and database")
    {
        _repository = repository;
        _artifactStore = artifactStore;
        this.SetHandler(ExecuteAsync);
    }

    private async Task ExecuteAsync()
    {
        try
        {
            AnsiConsole.MarkupLine("[bold blue]Initializing Sarab...[/]");

            await AnsiConsole.Status()
                .StartAsync("Setting up environment...", async ctx =>
                {
                    // 1. Setup Database
                    ctx.Status("Initializing Database...");
                    await _repository.InitializeAsync();
                    AnsiConsole.MarkupLine("[green]✔ Database initialized.[/]");

                    // 2. Download Binary
                    ctx.Status("Checking cloudflared binary...");
                    var path = await _artifactStore.EnsureCloudflaredBinaryAsync();
                    AnsiConsole.MarkupLine($"[green]✔ cloudflared binary ready at: {path}[/]");
                });

            AnsiConsole.MarkupLine("[bold green]Initialization Complete![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
        }
    }
}
