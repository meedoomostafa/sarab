using FluentAssertions;
using Moq;
using Sarab.Core.Interfaces;
using Sarab.Core.Services;

namespace Sarab.Tests.Services;

public class RdpConnectorServiceTests
{
    private readonly Mock<IArtifactStore> _storeMock;
    private readonly RdpConnectorService _service;

    public RdpConnectorServiceTests()
    {
        _storeMock = new Mock<IArtifactStore>();
        _service = new RdpConnectorService(_storeMock.Object);
    }

    [Fact]
    public async Task PrepareRdpProxyAsync_ReturnsCloudflaredExecutable()
    {
        _storeMock.Setup(s => s.EnsureCloudflaredBinaryAsync()).ReturnsAsync("/path/to/cloudflared");

        var (exe, args, port) = await _service.PrepareRdpProxyAsync("random.trycloudflare.com");

        exe.Should().Be("/path/to/cloudflared");
        args.Should().Contain("access tcp");
        args.Should().Contain("random.trycloudflare.com");
        port.Should().Be(13389);
    }

    [Fact]
    public async Task PrepareRdpProxyAsync_UsesCustomPort()
    {
        _storeMock.Setup(s => s.EnsureCloudflaredBinaryAsync()).ReturnsAsync("/path/to/cloudflared");

        var (exe, args, port) = await _service.PrepareRdpProxyAsync("host.trycloudflare.com", 55555);

        args.Should().Contain("localhost:55555");
        port.Should().Be(55555);
    }
}
