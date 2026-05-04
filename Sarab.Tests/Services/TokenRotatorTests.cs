using FluentAssertions;
using Moq;
using Sarab.Core.Entities;
using Sarab.Core.Interfaces;
using Sarab.Core.Services;

namespace Sarab.Tests.Services;

public class TokenRotatorTests
{
    private readonly Mock<ITokenRepository> _repoMock;
    private readonly Mock<ICloudflareAdapter> _adapterMock;
    private readonly TokenRotator _rotator;

    public TokenRotatorTests()
    {
        _repoMock = new Mock<ITokenRepository>();
        _adapterMock = new Mock<ICloudflareAdapter>();
        _rotator = new TokenRotator(_repoMock.Object, _adapterMock.Object);
    }

    [Fact]
    public async Task GetNextTokenAsync_ReturnsBestToken()
    {
        var expected = new Token { Id = 1, Alias = "work", ApiToken = "token1" };
        _repoMock.Setup(r => r.GetBestTokenAsync()).ReturnsAsync(expected);

        var result = await _rotator.GetNextTokenAsync();

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetNextTokenAsync_ReturnsNullWhenNoTokens()
    {
        _repoMock.Setup(r => r.GetBestTokenAsync()).ReturnsAsync((Token?)null);

        var result = await _rotator.GetNextTokenAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTokenByAliasAsync_ReturnsMatchingToken()
    {
        var tokens = new List<Token>
        {
            new() { Id = 1, Alias = "personal", ApiToken = "tok1" },
            new() { Id = 2, Alias = "work", ApiToken = "tok2" }
        };
        _repoMock.Setup(r => r.ListAsync()).ReturnsAsync(tokens);

        var result = await _rotator.GetTokenByAliasAsync("work");

        result.Should().NotBeNull();
        result!.Alias.Should().Be("work");
    }

    [Fact]
    public async Task GetTokenByAliasAsync_IsCaseInsensitive()
    {
        var tokens = new List<Token>
        {
            new() { Id = 1, Alias = "Work", ApiToken = "tok1" }
        };
        _repoMock.Setup(r => r.ListAsync()).ReturnsAsync(tokens);

        var result = await _rotator.GetTokenByAliasAsync("WORK");

        result.Should().NotBeNull();
        result!.Alias.Should().Be("Work");
    }

    [Fact]
    public async Task GetTokenByAliasAsync_ReturnsNullWhenNotFound()
    {
        _repoMock.Setup(r => r.ListAsync()).ReturnsAsync(new List<Token>());

        var result = await _rotator.GetTokenByAliasAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ReportFailureAsync_IncrementsFailureCount()
    {
        var token = new Token { Id = 1, Alias = "test", ApiToken = "tok", FailureCount = 0 };

        await _rotator.ReportFailureAsync(token);

        token.FailureCount.Should().Be(1);
        _repoMock.Verify(r => r.UpdateAsync(token), Times.Once);
    }

    [Fact]
    public async Task GetAllTokensAsync_ReturnsAllTokens()
    {
        var tokens = new List<Token>
        {
            new() { Id = 1, Alias = "a", ApiToken = "t1" },
            new() { Id = 2, Alias = "b", ApiToken = "t2" }
        };
        _repoMock.Setup(r => r.ListAsync()).ReturnsAsync(tokens);

        var result = await _rotator.GetAllTokensAsync();

        result.Should().HaveCount(2);
    }
}
