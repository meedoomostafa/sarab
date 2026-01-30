using System.CommandLine;
using System.Threading.Tasks;
using Sarab.Core.Interfaces;
using Sarab.Core.Services;

namespace Sarab.Cli.Commands;

public class ConnectCommand : Command
{
    private readonly SshConnectorService _connectorService;
    private readonly IProcessManager _processManager;

    public ConnectCommand(SshConnectorService connectorService, IProcessManager processManager)
        : base("connect", "Connect to a running tunnel")
    {
        _connectorService = connectorService;
        _processManager = processManager;

        var targetArg = new Argument<string>("target", "The SSH target (e.g. user@host.trycloudflare.com)");
        var keyOption = new Option<string>(new[] { "-i", "--identity-file" }, "Path to private key file");

        AddArgument(targetArg);
        AddOption(keyOption);

        this.SetHandler(async (target, keyPath) =>
        {
            var (exe, args) = await _connectorService.PrepareSshCommandAsync(target, keyPath);
            await _processManager.StartInteractiveShellAsync(exe, args);
        }, targetArg, keyOption);
    }
}