using System.Threading.Tasks;
using Refit;
using Sarab.Infrastructure.DTOs;

namespace Sarab.Infrastructure.Adapters;

public interface ICloudflareApi
{
    [Get("/user/tokens/verify")]
    Task<VerifyTokenResponse> VerifyTokenAsync([Authorize("Bearer")] string token);

    [Post("/accounts/{accountId}/tunnels")]
    Task<CreateTunnelResponse> CreateTunnelAsync([Authorize("Bearer")] string token, string accountId, [Body] CreateTunnelRequest request);

    // TODO: Add DNS record endpoints
}

