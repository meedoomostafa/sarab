using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sarab.Core.Entities;
using Sarab.Core.Interfaces;
using Dapper;

namespace Sarab.Infrastructure.Persistence;

public class SqliteRepository : ITokenRepository
{
    private readonly string _connectionString;

    public SqliteRepository(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    private SqliteConnection GetConnection() => new SqliteConnection(_connectionString);

    public async Task InitializeAsync()
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var sql = @"
            CREATE TABLE IF NOT EXISTS Tokens (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Alias TEXT NOT NULL UNIQUE,
                ApiToken TEXT NOT NULL,
                AccountId TEXT,
                IsActive BOOLEAN DEFAULT 1,
                FailureCount INTEGER DEFAULT 0,
                LastUsedAt TEXT
            );
            CREATE TABLE IF NOT EXISTS Config (
                Key TEXT PRIMARY KEY,
                Value TEXT
            );
        ";
        await conn.ExecuteAsync(sql);
    }

    public async Task<Token> AddAsync(Token token)
    {
        using var conn = GetConnection();
        var sql = @"
            INSERT INTO Tokens (Alias, ApiToken, AccountId, IsActive, FailureCount, LastUsedAt)
            VALUES (@Alias, @ApiToken, @AccountId, @IsActive, @FailureCount, @LastUsedAt)
            RETURNING Id;
        ";
        token.Id = await conn.ExecuteScalarAsync<int>(sql, token);
        return token;
    }

    public async Task<IEnumerable<Token>> ListAsync()
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<Token>("SELECT * FROM Tokens WHERE IsActive = 1");
    }

    public async Task<bool> RemoveAsync(string alias)
    {
        using var conn = GetConnection();
        var affected = await conn.ExecuteAsync("UPDATE Tokens SET IsActive = 0 WHERE Alias = @Alias", new { Alias = alias });
        return affected > 0;
    }

    public async Task<Token?> GetBestTokenAsync()
    {
        using var conn = GetConnection();
        // Retrieve best available token
        return await conn.QueryFirstOrDefaultAsync<Token>("SELECT * FROM Tokens WHERE IsActive = 1 ORDER BY FailureCount ASC LIMIT 1");
    }

    public async Task UpdateAsync(Token token)
    {
        using var conn = GetConnection();
        var sql = @"
            UPDATE Tokens 
            SET FailureCount = @FailureCount, LastUsedAt = @LastUsedAt, AccountId = @AccountId
            WHERE Id = @Id
        ";
        await conn.ExecuteAsync(sql, token);
    }

    public async Task<Config?> GetConfigAsync(string key)
    {
        using var conn = GetConnection();
        return await conn.QueryFirstOrDefaultAsync<Config>("SELECT * FROM Config WHERE Key = @Key", new { Key = key });
    }

    public async Task SetConfigAsync(string key, string value)
    {
        using var conn = GetConnection();
        var sql = @"
            INSERT INTO Config (Key, Value) VALUES (@Key, @Value)
            ON CONFLICT(Key) DO UPDATE SET Value = @Value;
        ";
        await conn.ExecuteAsync(sql, new { Key = key, Value = value });
    }
}
