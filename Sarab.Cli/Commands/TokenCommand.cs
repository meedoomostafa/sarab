using System.CommandLine;
using System.Threading.Tasks;
using Spectre.Console;
using Sarab.Core.Entities;
using Sarab.Core.Interfaces;

namespace Sarab.Cli.Commands;

public class TokenCommand : Command
{
    public TokenCommand(ITokenRepository repository, ICloudflareAdapter adapter)
        : base("token", "Manage Cloudflare API tokens")
    {
        AddCommand(new AddTokenCommand(repository, adapter));
        AddCommand(new ListTokensCommand(repository));
    }
}

public class AddTokenCommand : Command
{
    private readonly ITokenRepository _repository;
    private readonly ICloudflareAdapter _adapter;

    public AddTokenCommand(ITokenRepository repository, ICloudflareAdapter adapter)
        : base("add", "Add a new Cloudflare API token")
    {
        _repository = repository;
        _adapter = adapter;

        var aliasArg = new Argument<string>("alias", "A friendly name for this token");
        var tokenArg = new Argument<string>("token", "The Cloudflare API Token");

        AddArgument(aliasArg);
        AddArgument(tokenArg);

        this.SetHandler(ExecuteAsync, aliasArg, tokenArg);
    }

    private async Task ExecuteAsync(string alias, string apiToken)
    {
        await AnsiConsole.Status()
            .StartAsync($"Validating token '{alias}'...", async ctx =>
            {
                try
                {
                    // Verify token
                    ctx.Status("Verifying with Cloudflare...");
                    var accountId = await _adapter.VerifyTokenAsync(apiToken);

                    // Save token
                    ctx.Status("Saving to database...");
                    var token = new Token
                    {
                        Alias = alias,
                        ApiToken = apiToken,
                        AccountId = accountId,
                        IsActive = true
                    };
                    await _repository.AddAsync(token);

                    AnsiConsole.MarkupLine($"[green]✓ Token '{alias}' added successfully![/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error adding token: {Markup.Escape(ex.Message)}[/]");
                }
            });
    }
}

public class ListTokensCommand : Command
{
    private readonly ITokenRepository _repository;

    public ListTokensCommand(ITokenRepository repository)
        : base("list", "List stored tokens")
    {
        _repository = repository;
        this.SetHandler(ExecuteAsync);
    }

    private async Task ExecuteAsync()
    {
        var tokens = await _repository.ListAsync();

        var table = new Table();
        table.AddColumn("Alias");
        table.AddColumn("Status");
        table.AddColumn("Failures");
        table.AddColumn("Last Used");

        foreach (var t in tokens)
        {
            table.AddRow(
                Markup.Escape(t.Alias),
                t.IsActive ? "[green]Active[/]" : "[red]Inactive[/]",
                t.FailureCount.ToString(),
                t.LastUsedAt?.ToString() ?? "Never"
            );
        }

        AnsiConsole.Write(table);
    }
}
