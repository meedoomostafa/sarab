using FluentAssertions;
using Moq;
using Refit;
using Sarab.Core.DTOs;
using Sarab.Core.Entities;
using Sarab.Infrastructure.Adapters;
using Sarab.Infrastructure.DTOs;

namespace Sarab.Tests.Adapters;

public class CloudflareAdapterTests
{
    private readonly Mock<ICloudflareApi> _apiMock;
    private readonly CloudflareAdapter _adapter;

    public CloudflareAdapterTests()
    {
        _apiMock = new Mock<ICloudflareApi>();
        _adapter = new CloudflareAdapter(_apiMock.Object);
    }

    [Fact]
    public async Task VerifyTokenAsync_ReturnsAccountId()
    {
        _apiMock.Setup(x => x.VerifyTokenAsync("tok1"))
            .ReturnsAsync(new VerifyTokenResponse
            {
                Success = true,
                Result = new VerifyTokenResult { Id = "tok-id", Status = "active" }
            });

        _apiMock.Setup(x => x.GetAccountsAsync("tok1"))
            .ReturnsAsync(new AccountListResponse
            {
                Success = true,
                Result = new[] { new AccountResult { Id = "acc-1", Name = "Me" } }
            });

        var accountId = await _adapter.VerifyTokenAsync("tok1");

        accountId.Should().Be("acc-1");
    }

    [Fact]
    public async Task VerifyTokenAsync_ThrowsWhenTokenInactive()
    {
        _apiMock.Setup(x => x.VerifyTokenAsync("tok1"))
            .ReturnsAsync(new VerifyTokenResponse
            {
                Success = true,
                Result = new VerifyTokenResult { Id = "tok-id", Status = "expired" }
            });

        Func<Task> act = async () => await _adapter.VerifyTokenAsync("tok1");
        await act.Should().ThrowAsync<Exception>().WithMessage("*invalid or not active*");
    }

    [Fact]
    public async Task VerifyTokenAsync_ThrowsWhenNoAccounts()
    {
        _apiMock.Setup(x => x.VerifyTokenAsync("tok1"))
            .ReturnsAsync(new VerifyTokenResponse
            {
                Success = true,
                Result = new VerifyTokenResult { Id = "tok-id", Status = "active" }
            });

        _apiMock.Setup(x => x.GetAccountsAsync("tok1"))
            .ReturnsAsync(new AccountListResponse
            {
                Success = true,
                Result = Array.Empty<AccountResult>()
            });

        Func<Task> act = async () => await _adapter.VerifyTokenAsync("tok1");
        await act.Should().ThrowAsync<Exception>().WithMessage("*no access to any accounts*");
    }

    [Fact]
    public async Task CreateTunnelAsync_ReturnsIdAndToken()
    {
        var token = new Token { Alias = "test", ApiToken = "tok", AccountId = "acc" };
        _apiMock.Setup(x => x.CreateTunnelAsync("tok", "acc", It.IsAny<CreateTunnelRequest>()))
            .ReturnsAsync(new CreateTunnelResponse
            {
                Success = true,
                Result = new TunnelResult { Id = "tun-1", Token = "tun-tok" }
            });

        var (id, tunToken) = await _adapter.CreateTunnelAsync(token, "sarab-test");

        id.Should().Be("tun-1");
        tunToken.Should().Be("tun-tok");
    }

    [Fact]
    public async Task CreateTunnelAsync_ThrowsOnFailure()
    {
        var token = new Token { Alias = "test", ApiToken = "tok", AccountId = "acc" };
        _apiMock.Setup(x => x.CreateTunnelAsync("tok", "acc", It.IsAny<CreateTunnelRequest>()))
            .ReturnsAsync(new CreateTunnelResponse { Success = false });

        Func<Task> act = async () => await _adapter.CreateTunnelAsync(token, "sarab-test");
        await act.Should().ThrowAsync<Exception>().WithMessage("*Failed to create tunnel*");
    }

    [Fact]
    public async Task GetTunnelTokenAsync_ReturnsToken()
    {
        var token = new Token { Alias = "test", ApiToken = "tok", AccountId = "acc" };
        _apiMock.Setup(x => x.GetTunnelTokenAsync("tok", "acc", "tun-1"))
            .ReturnsAsync(new TunnelTokenResponse { Success = true, Result = "secret-token" });

        var result = await _adapter.GetTunnelTokenAsync(token, "tun-1");

        result.Should().Be("secret-token");
    }

    [Fact]
    public async Task GetZoneIdAsync_ReturnsZoneId()
    {
        _apiMock.Setup(x => x.GetZonesAsync("tok", "example.com"))
            .ReturnsAsync(new ZoneListResponse
            {
                Success = true,
                Result = new[] { new ZoneResult { Id = "zone-1", Name = "example.com" } }
            });

        var token = new Token { Alias = "test", ApiToken = "tok" };
        var zoneId = await _adapter.GetZoneIdAsync(token, "example.com");

        zoneId.Should().Be("zone-1");
    }

    [Fact]
    public async Task GetZoneIdAsync_TriesParentDomain()
    {
        _apiMock.Setup(x => x.GetZonesAsync("tok", "sub.example.com"))
            .ReturnsAsync(new ZoneListResponse { Success = true, Result = Array.Empty<ZoneResult>() });

        _apiMock.Setup(x => x.GetZonesAsync("tok", "example.com"))
            .ReturnsAsync(new ZoneListResponse
            {
                Success = true,
                Result = new[] { new ZoneResult { Id = "zone-1", Name = "example.com" } }
            });

        var token = new Token { Alias = "test", ApiToken = "tok" };
        var zoneId = await _adapter.GetZoneIdAsync(token, "sub.example.com");

        zoneId.Should().Be("zone-1");
    }

    [Fact]
    public async Task GetZoneIdAsync_ReturnsNullWhenNotFound()
    {
        _apiMock.Setup(x => x.GetZonesAsync("tok", It.IsAny<string>()))
            .ReturnsAsync(new ZoneListResponse { Success = true, Result = Array.Empty<ZoneResult>() });

        var token = new Token { Alias = "test", ApiToken = "tok" };
        var zoneId = await _adapter.GetZoneIdAsync(token, "example.com");

        zoneId.Should().BeNull();
    }

    [Fact]
    public async Task CreateDnsRecordAsync_ReturnsRecordId()
    {
        var token = new Token { Alias = "test", ApiToken = "tok" };
        _apiMock.Setup(x => x.CreateDnsRecordAsync("tok", "zone-1", It.IsAny<CreateDnsRecordRequest>()))
            .ReturnsAsync(new DnsRecordResponse
            {
                Success = true,
                Result = new DnsRecordResult { Id = "rec-1" }
            });

        var id = await _adapter.CreateDnsRecordAsync(token, "zone-1", "sub.example.com", "tun.cfargotunnel.com");

        id.Should().Be("rec-1");
    }

    [Fact]
    public async Task ListTunnelsAsync_ReturnsActiveTunnels()
    {
        var token = new Token { Alias = "test", ApiToken = "tok", AccountId = "acc" };
        _apiMock.Setup(x => x.GetTunnelsAsync("tok", "acc", null, false))
            .ReturnsAsync(new TunnelListResponse
            {
                Success = true,
                Result = new[]
                {
                    new CloudflareTunnelDetail { Id = "1", Name = "sarab-abc" },
                    new CloudflareTunnelDetail { Id = "2", Name = "other", DeletedAt = DateTime.UtcNow }
                }
            });

        var tunnels = await _adapter.ListTunnelsAsync(token);

        tunnels.Should().ContainSingle(t => t.Name == "sarab-abc");
    }

    [Fact]
    public async Task DeleteTunnelAsync_CallsApi()
    {
        var token = new Token { Alias = "test", ApiToken = "tok", AccountId = "acc" };
        _apiMock.Setup(x => x.DeleteTunnelAsync("tok", "acc", "tun-1"))
            .ReturnsAsync("deleted");

        await _adapter.DeleteTunnelAsync(token, "tun-1");

        _apiMock.Verify(x => x.DeleteTunnelAsync("tok", "acc", "tun-1"), Times.Once);
    }

    [Fact]
    public async Task UpdateDnsRecordAsync_ReturnsRecordId()
    {
        var token = new Token { Alias = "test", ApiToken = "tok" };
        _apiMock.Setup(x => x.UpdateDnsRecordAsync("tok", "zone-1", "rec-1", It.IsAny<CreateDnsRecordRequest>()))
            .ReturnsAsync(new DnsRecordResponse
            {
                Success = true,
                Result = new DnsRecordResult { Id = "rec-1" }
            });

        var id = await _adapter.UpdateDnsRecordAsync(token, "zone-1", "rec-1", "sub.example.com", "tun.cfargotunnel.com");

        id.Should().Be("rec-1");
    }

    [Fact]
    public async Task ListDnsRecordsAsync_ReturnsRecords()
    {
        var token = new Token { Alias = "test", ApiToken = "tok", AccountId = "acc" };
        _apiMock.Setup(x => x.GetDnsRecordsAsync("tok", "zone-1", "sub.example.com"))
            .ReturnsAsync(new DnsListResponse
            {
                Success = true,
                Result = new[]
                {
                    new DnsListResult { Id = "rec-1", Name = "sub.example.com" }
                }
            });

        var records = await _adapter.ListDnsRecordsAsync(token, "zone-1", "sub.example.com");

        records.Should().ContainSingle(r => r.Id == "rec-1");
    }
}
