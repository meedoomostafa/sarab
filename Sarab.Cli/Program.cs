using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Spectre.Console;
using Sarab.Core.Interfaces;
using Sarab.Core.Services;
using Sarab.Infrastructure.Adapters;
using Sarab.Infrastructure.Persistence;
using Sarab.Infrastructure.Services;
using Sarab.Cli.Commands;

namespace Sarab.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var services = ConfigureServices();
        var serviceProvider = services.BuildServiceProvider();

        var rootCommand = new RootCommand("Sarab - The Illusionist for your local ports");

        // Add Commands
        rootCommand.AddCommand(new InitCommand(
            serviceProvider.GetRequiredService<ITokenRepository>(),
            serviceProvider.GetRequiredService<IArtifactStore>()
        ));

        rootCommand.AddCommand(new TokenCommand(
            serviceProvider.GetRequiredService<ITokenRepository>(),
            serviceProvider.GetRequiredService<ICloudflareAdapter>()
        ));

        rootCommand.AddCommand(new ExposeCommand(
            serviceProvider.GetRequiredService<IllusionistService>()
        ));

        rootCommand.AddCommand(new ListCommand(
            serviceProvider.GetRequiredService<IllusionistService>()
        ));

        rootCommand.AddCommand(new NukeCommand(
            serviceProvider.GetRequiredService<IllusionistService>()
        ));

        return await rootCommand.InvokeAsync(args);
    }

    private static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".sarab/sarab.db");
        // Ensure storage directory
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        // Register Core
        services.AddSingleton<TokenRotator>();
        services.AddSingleton<IllusionistService>();

        // Register Infrastructure
        services.AddSingleton<ITokenRepository>(new SqliteRepository(dbPath));
        services.AddSingleton<IProcessManager, ProcessManager>();

        services.AddRefitClient<ICloudflareApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.cloudflare.com/client/v4"));

        services.AddSingleton<ICloudflareAdapter, CloudflareAdapter>();
        services.AddHttpClient<IArtifactStore, ArtifactStore>();

        return services;
    }
}
