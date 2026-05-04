using FluentAssertions;
using Moq;
using Sarab.Core.Interfaces;
using Sarab.Core.Services;

namespace Sarab.Tests.Services;

public class SshConnectorServiceTests
{
    private readonly Mock<IArtifactStore> _storeMock;
    private readonly SshConnectorService _service;

    public SshConnectorServiceTests()
    {
        _storeMock = new Mock<IArtifactStore>();
        _service = new SshConnectorService(_storeMock.Object);
    }

    [Fact]
    public async Task PrepareSshCommandAsync_ReturnsSshExecutable()
    {
        _storeMock.Setup(s => s.EnsureCloudflaredBinaryAsync()).ReturnsAsync("/path/to/cloudflared");

        var (exe, args) = await _service.PrepareSshCommandAsync("user@host.trycloudflare.com", null);

        exe.Should().Be("ssh");
        args.Should().Contain("ProxyCommand=");
        args.Should().Contain("cloudflared");
    }

    [Fact]
    public async Task PrepareSshCommandAsync_IncludesIdentityFile()
    {
        _storeMock.Setup(s => s.EnsureCloudflaredBinaryAsync()).ReturnsAsync("/path/to/cloudflared");

        var (exe, args) = await _service.PrepareSshCommandAsync("user@host.trycloudflare.com", "~/.ssh/id_rsa");

        args.Should().Contain("-i \"~/.ssh/id_rsa\"");
    }
}
