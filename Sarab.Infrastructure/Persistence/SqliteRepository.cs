using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sarab.Core.Entities;
using Sarab.Core.Interfaces;
using Dapper;
using System.IO;

namespace Sarab.Infrastructure.Persistence;

public class SqliteRepository : ITokenRepository
{
    private readonly string _connectionString;
    private readonly string _dbPath;

    public SqliteRepository(string dbPath)
    {
        _dbPath = dbPath;
        _connectionString = $"Data Source={dbPath}";
    }

    private SqliteConnection GetConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    // Helper to ensure DB exists before critical write ops if needed? 
    // Actually, simply calling 'sarab init' in install.sh explains why it was missing.
    // I will stick to fixing install.sh and maybe wrapping one method.
    // Let's just do install.sh first.

    // Wait, I am replacing content. Sticking to simple changes.
    // I will only modify install.sh for now as the robust fix.
    // AND I will add the 'System.IO' check if I can? 
    // Let's skip SqliteRepository changes if install.sh covers it. 
    // User asked "why sqllite error". Answer: "install.sh didn't init".
    // I will add 'sarab init' to install.sh.
    // I will also modify SqliteRepository to be safer?
    // Let's just modify install.sh.


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
        var affected = await conn.ExecuteAsync("DELETE FROM Tokens WHERE Alias = @Alias", new { Alias = alias });
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
