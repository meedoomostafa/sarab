using System.Threading.Tasks;
using Sarab.Core.Entities;
using Sarab.Core.Interfaces;

namespace Sarab.Core.Services;

public class TokenRotator
{
    private readonly ITokenRepository _repository;
    private readonly ICloudflareAdapter _adapter;

    public TokenRotator(ITokenRepository repository, ICloudflareAdapter adapter)
    {
        _repository = repository;
        _adapter = adapter;
    }

    public async Task<Token?> GetNextTokenAsync()
    {
        // TODO: Implement rotation logic based on failure count and LRU
        return await _repository.GetBestTokenAsync();
    }

    public async Task ReportFailureAsync(Token token)
    {
        token.FailureCount++;
        await _repository.UpdateAsync(token);
    }

    public async Task<IEnumerable<Token>> GetAllTokensAsync()
    {
        return await _repository.ListAsync();
    }
}
