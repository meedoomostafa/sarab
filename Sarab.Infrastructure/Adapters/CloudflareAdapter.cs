using System;
using System.Threading.Tasks;
using Sarab.Core.Entities;
using Sarab.Core.Interfaces;
using Refit;
using Sarab.Infrastructure.DTOs;

namespace Sarab.Infrastructure.Adapters;

public class CloudflareAdapter : ICloudflareAdapter
{
    private readonly ICloudflareApi _api;

    public CloudflareAdapter(ICloudflareApi api)
    {
        _api = api;
    }

    public async Task<string> VerifyTokenAsync(string apiToken)
    {
        try
        {
            var response = await _api.VerifyTokenAsync(apiToken);
            if (!response.Success || response.Result?.Status != "active")
            {
                throw new Exception("Token is invalid or not active.");
            }

            // TODO: Fetch real Account ID
            return "unknown-account-id";
        }
        catch (ApiException ex)
        {
            throw new Exception($"Cloudflare API Error: {ex.StatusCode}");
        }
    }

    public async Task<string> CreateTunnelAsync(Token token, string name)
    {
        if (string.IsNullOrEmpty(token.AccountId))
            throw new Exception("Account ID missing for token.");

        var request = new CreateTunnelRequest { Name = name };
        var response = await _api.CreateTunnelAsync(token.ApiToken, token.AccountId, request);

        if (!response.Success || response.Result == null) throw new Exception("Failed to create tunnel");

        return response.Result.Id;
    }

    public Task CreateDnsRecordAsync(Token token, string startUrl, string subdomain)
    {
        // TODO: Implement DNS creation logic
        return Task.CompletedTask;
    }

    public Task DeleteDnsRecordAsync(Token token, string recordId)
    {
        // TODO: Implement DNS deletion
        return Task.CompletedTask;
    }
}
