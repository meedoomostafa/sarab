using System.CommandLine;
using System.Threading.Tasks;
using Spectre.Console;
using Sarab.Core.Interfaces;

namespace Sarab.Cli.Commands;

public class InitCommand : Command
{
    private readonly ITokenRepository _repository;
    private readonly IProcessManager _processManager;

    public InitCommand(ITokenRepository repository, IProcessManager processManager)
        : base("init", "Initialize Sarab environment")
    {
        _repository = repository;
        _processManager = processManager;

        this.SetHandler(ExecuteAsync);
    }

    private async Task ExecuteAsync()
    {
        await AnsiConsole.Status()
            .StartAsync("Initializing Sarab...", async ctx =>
            {
                // Initialize database
                await _repository.InitializeAsync();

                // Verify binary
                ctx.Status("Checking cloudflared binary...");
                try
                {
                    await _processManager.EnsureBinaryExistsAsync();
                    AnsiConsole.MarkupLine("[green]✓ cloudflared binary found.[/]");
                }
                catch
                {
                    AnsiConsole.MarkupLine("[yellow]! cloudflared binary not found.[/]");
                    AnsiConsole.MarkupLine("Please install it manually or ensure it is in your PATH.");
                    // TODO: Implement auto-download
                }
            });

        AnsiConsole.MarkupLine("[green]Initialization complete![/]");
    }
}
