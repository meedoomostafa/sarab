using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Sarab.Core.Entities;
using Sarab.Infrastructure.Persistence;

namespace Sarab.Tests.Persistence;

public class SqliteRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteRepository _repository;

    public SqliteRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"sarab_test_{Guid.NewGuid()}.db");
        _repository = new SqliteRepository(_dbPath);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task InitializeAsync_CreatesTables()
    {
        await _repository.InitializeAsync();

        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();

        var tableNames = new List<string>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tableNames.Add(reader.GetString(0));
        }

        tableNames.Should().Contain("Tokens");
        tableNames.Should().Contain("Config");
    }

    [Fact]
    public async Task AddAsync_CreatesToken()
    {
        await _repository.InitializeAsync();
        var token = new Token
        {
            Alias = "personal",
            ApiToken = "test-api-token",
            AccountId = "acc-123",
            IsActive = true
        };

        var result = await _repository.AddAsync(token);

        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddAsync_ThrowsOnDuplicateAlias()
    {
        await _repository.InitializeAsync();
        var token1 = new Token { Alias = "dup", ApiToken = "t1" };
        var token2 = new Token { Alias = "dup", ApiToken = "t2" };
        await _repository.AddAsync(token1);

        Func<Task> act = async () => await _repository.AddAsync(token2);
        await act.Should().ThrowAsync<DbException>();
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyActiveTokens()
    {
        await _repository.InitializeAsync();
        await _repository.AddAsync(new Token { Alias = "active", ApiToken = "t1", IsActive = true });
        await _repository.AddAsync(new Token { Alias = "inactive", ApiToken = "t2", IsActive = false });

        var tokens = await _repository.ListAsync();

        tokens.Should().ContainSingle(t => t.Alias == "active");
    }

    [Fact]
    public async Task RemoveAsync_DeletesToken()
    {
        await _repository.InitializeAsync();
        await _repository.AddAsync(new Token { Alias = "del", ApiToken = "t1" });

        var removed = await _repository.RemoveAsync("del");

        removed.Should().BeTrue();
        var tokens = await _repository.ListAsync();
        tokens.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveAsync_ReturnsFalseWhenNotFound()
    {
        await _repository.InitializeAsync();

        var removed = await _repository.RemoveAsync("missing");

        removed.Should().BeFalse();
    }

    [Fact]
    public async Task GetBestTokenAsync_ReturnsTokenWithLowestFailures()
    {
        await _repository.InitializeAsync();
        await _repository.AddAsync(new Token { Alias = "bad", ApiToken = "t1", FailureCount = 5 });
        await _repository.AddAsync(new Token { Alias = "good", ApiToken = "t2", FailureCount = 0 });

        var best = await _repository.GetBestTokenAsync();

        best.Should().NotBeNull();
        best!.Alias.Should().Be("good");
    }

    [Fact]
    public async Task GetBestTokenAsync_ReturnsNullWhenEmpty()
    {
        await _repository.InitializeAsync();

        var best = await _repository.GetBestTokenAsync();

        best.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ModifiesToken()
    {
        await _repository.InitializeAsync();
        var token = new Token { Alias = "upd", ApiToken = "t1", FailureCount = 0 };
        await _repository.AddAsync(token);

        token.FailureCount = 3;
        await _repository.UpdateAsync(token);

        var tokens = await _repository.ListAsync();
        tokens.First().FailureCount.Should().Be(3);
    }

    [Fact]
    public async Task SetConfigAsync_AndGetConfigAsync_Works()
    {
        await _repository.InitializeAsync();

        await _repository.SetConfigAsync("key1", "value1");
        var result = await _repository.GetConfigAsync("key1");

        result.Should().NotBeNull();
        result!.Key.Should().Be("key1");
        result.Value.Should().Be("value1");
    }

    [Fact]
    public async Task GetConfigAsync_ReturnsNullForMissingKey()
    {
        await _repository.InitializeAsync();

        var result = await _repository.GetConfigAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetConfigAsync_UpdatesExistingKey()
    {
        await _repository.InitializeAsync();
        await _repository.SetConfigAsync("k", "v1");
        await _repository.SetConfigAsync("k", "v2");

        var result = await _repository.GetConfigAsync("k");
        result!.Value.Should().Be("v2");
    }
}
