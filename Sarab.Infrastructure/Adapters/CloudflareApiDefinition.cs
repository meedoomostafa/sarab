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



    [Get("/accounts/{accountId}/tunnels/{tunnelId}/token")]
    Task<TunnelTokenResponse> GetTunnelTokenAsync([Authorize("Bearer")] string token, string accountId, string tunnelId);

    [Put("/accounts/{accountId}/tunnels/{tunnelId}/configurations")]
    Task<TunnelConfigResponse> UpdateTunnelConfigAsync([Authorize("Bearer")] string token, string accountId, string tunnelId, [Body] UpdateTunnelConfigRequest request);

    [Get("/accounts/{accountId}/tunnels")]
    Task<TunnelListResponse> GetTunnelsAsync([Authorize("Bearer")] string token, string accountId, [AliasAs("name")] string? name = null, [AliasAs("is_deleted")] bool isDeleted = false);

    [Delete("/accounts/{accountId}/tunnels/{tunnelId}")]
    Task<string> DeleteTunnelAsync([Authorize("Bearer")] string token, string accountId, string tunnelId);

    [Get("/zones")]
    Task<ZoneListResponse> GetZonesAsync([Authorize("Bearer")] string token, [AliasAs("name")] string? name = null);

    [Post("/zones/{zoneId}/dns_records")]
    Task<DnsRecordResponse> CreateDnsRecordAsync([Authorize("Bearer")] string token, string zoneId, [Body] CreateDnsRecordRequest request);

    [Delete("/zones/{zoneId}/dns_records/{recordId}")]
    Task<string> DeleteDnsRecordAsync([Authorize("Bearer")] string token, string zoneId, string recordId);

    [Put("/zones/{zoneId}/dns_records/{recordId}")]
    Task<DnsRecordResponse> UpdateDnsRecordAsync([Authorize("Bearer")] string token, string zoneId, string recordId, [Body] CreateDnsRecordRequest body);

    [Get("/zones/{zoneId}/dns_records")]
    Task<DnsListResponse> GetDnsRecordsAsync([Authorize("Bearer")] string token, string zoneId, [AliasAs("name")] string? name = null);

    [Get("/accounts")]
    Task<AccountListResponse> GetAccountsAsync([Authorize("Bearer")] string token);
}

