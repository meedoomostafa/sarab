using FluentAssertions;
using Moq;
using Sarab.Core.DTOs;
using Sarab.Core.Entities;
using Sarab.Core.Interfaces;
using Sarab.Core.Services;

namespace Sarab.Tests.Services;

public class IllusionistServiceTests
{
    private readonly Mock<ITokenRepository> _repoMock;
    private readonly TokenRotator _rotator;
    private readonly Mock<IProcessManager> _processManagerMock;
    private readonly Mock<ICloudflareAdapter> _adapterMock;
    private readonly IllusionistService _service;

    public IllusionistServiceTests()
    {
        _repoMock = new Mock<ITokenRepository>();
        _adapterMock = new Mock<ICloudflareAdapter>();
        _rotator = new TokenRotator(_repoMock.Object, _adapterMock.Object);
        _processManagerMock = new Mock<IProcessManager>();
        _service = new IllusionistService(_rotator, _processManagerMock.Object, _adapterMock.Object);
    }

    [Fact]
    public async Task ExposePortAsync_UsesQuickTunnelWhenNoTokens()
    {
        _repoMock.Setup(r => r.GetBestTokenAsync()).ReturnsAsync((Token?)null);

        await _service.ExposePortAsync(8080);

        _processManagerMock.Verify(p => p.StartQuickTunnelAsync(8080, "localhost", TunnelScheme.HTTP, false), Times.Once);
    }

    [Fact]
    public async Task ExposePortAsync_UsesSpecificIdentity()
    {
        var token = new Token { Alias = "work", ApiToken = "tok", AccountId = "acc" };
        _repoMock.Setup(r => r.ListAsync()).ReturnsAsync(new List<Token> { token });
        _adapterMock.Setup(a => a.GetZoneIdAsync(token, "sub.example.com")).ReturnsAsync("zone-1");
        _adapterMock.Setup(a => a.CreateTunnelAsync(token, It.IsAny<string>())).ReturnsAsync(("tun-1", "tun-tok"));
        _adapterMock.Setup(a => a.CreateDnsRecordAsync(token, "zone-1", It.IsAny<string>(), It.IsAny<string>(), true)).ReturnsAsync("rec-1");

        await _service.ExposePortAsync(8080, subdomain: "sub.example.com", identity: "work");
    }

    [Fact]
    public async Task ExposePortAsync_ThrowsWhenIdentityNotFound()
    {
        _repoMock.Setup(r => r.ListAsync()).ReturnsAsync(new List<Token>());

        Func<Task> act = async () => await _service.ExposePortAsync(8080, identity: "missing");
        await act.Should().ThrowAsync<Exception>().WithMessage("*not found*");
    }

    [Fact]
    public async Task ExposePortAsync_ThrowsWhenNoSubdomainInAuthMode()
    {
        var token = new Token { Alias = "work", ApiToken = "tok", AccountId = "acc" };
        _repoMock.Setup(r => r.GetBestTokenAsync()).ReturnsAsync(token);

        Func<Task> act = async () => await _service.ExposePortAsync(8080);
        await act.Should().ThrowAsync<Exception>().WithMessage("*requires a subdomain*");
    }

    [Fact]
    public async Task ExposePortAsync_ThrowsWhenZoneNotFound()
    {
        var token = new Token { Alias = "work", ApiToken = "tok", AccountId = "acc" };
        _repoMock.Setup(r => r.GetBestTokenAsync()).ReturnsAsync(token);
        _adapterMock.Setup(a => a.GetZoneIdAsync(token, "bad.example.com")).ReturnsAsync((string?)null);

        Func<Task> act = async () => await _service.ExposePortAsync(8080, subdomain: "bad.example.com");
        await act.Should().ThrowAsync<Exception>().WithMessage("*Could not find a Cloudflare Zone*");
    }

    [Fact]
    public async Task ListMiragesAsync_ReturnsTunnelsByIdentity()
    {
        var token = new Token { Alias = "work", ApiToken = "tok", AccountId = "acc" };
        _repoMock.Setup(r => r.ListAsync()).ReturnsAsync(new List<Token> { token });
        _adapterMock.Setup(a => a.ListTunnelsAsync(token)).ReturnsAsync(new List<TunnelDetail>
        {
            new() { Id = "1", Name = "sarab-abc" },
            new() { Id = "2", Name = "other-tunnel" }
        });

        var result = await _service.ListMiragesAsync();

        result.Should().ContainKey("work");
        result["work"].Should().ContainSingle(t => t.Name == "sarab-abc");
    }

    [Fact]
    public async Task ListMiragesAsync_SkipsFailingTokens()
    {
        var token = new Token { Alias = "bad", ApiToken = "tok", AccountId = "acc" };
        _repoMock.Setup(r => r.ListAsync()).ReturnsAsync(new List<Token> { token });
        _adapterMock.Setup(a => a.ListTunnelsAsync(token)).ThrowsAsync(new Exception("API Error"));

        var result = await _service.ListMiragesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task NukeAsync_DeletesSarabTunnels()
    {
        var token = new Token { Alias = "work", ApiToken = "tok", AccountId = "acc" };
        _repoMock.Setup(r => r.ListAsync()).ReturnsAsync(new List<Token> { token });
        _adapterMock.Setup(a => a.ListTunnelsAsync(token)).ReturnsAsync(new List<TunnelDetail>
        {
            new() { Id = "1", Name = "sarab-abc" },
            new() { Id = "2", Name = "sarab-def" }
        });

        await _service.NukeAsync();

        _adapterMock.Verify(a => a.DeleteTunnelAsync(token, "1"), Times.Once);
        _adapterMock.Verify(a => a.DeleteTunnelAsync(token, "2"), Times.Once);
    }

    [Fact]
    public async Task NukeAsync_SkipsWhenNoTunnels()
    {
        var token = new Token { Alias = "work", ApiToken = "tok", AccountId = "acc" };
        _repoMock.Setup(r => r.ListAsync()).ReturnsAsync(new List<Token> { token });
        _adapterMock.Setup(a => a.ListTunnelsAsync(token)).ReturnsAsync(new List<TunnelDetail>());

        await _service.NukeAsync();

        _adapterMock.Verify(a => a.DeleteTunnelAsync(It.IsAny<Token>(), It.IsAny<string>()), Times.Never);
    }
}
