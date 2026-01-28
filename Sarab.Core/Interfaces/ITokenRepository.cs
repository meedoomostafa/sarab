using System.Collections.Generic;
using System.Threading.Tasks;
using Sarab.Core.Entities;

namespace Sarab.Core.Interfaces;

public interface ITokenRepository
{
    Task<Token> AddAsync(Token token);
    Task<IEnumerable<Token>> ListAsync();
    Task<bool> RemoveAsync(string alias);
    Task<Token?> GetBestTokenAsync();
    Task UpdateAsync(Token token);
    Task<Config?> GetConfigAsync(string key);
    Task SetConfigAsync(string key, string value);
}
