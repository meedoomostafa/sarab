using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sarab.Core.Entities;
using Sarab.Core.Interfaces;
using Refit;
using Sarab.Infrastructure.DTOs;
using Sarab.Core.DTOs;

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
            // 1. Verify Token Status
            var verifyResponse = await _api.VerifyTokenAsync(apiToken);
            if (!verifyResponse.Success || verifyResponse.Result?.Status != "active")
            {
                throw new Exception("Token is invalid or not active.");
            }

            // 2. Fetch Account ID
            var accountsResponse = await _api.GetAccountsAsync(apiToken);
            if (!accountsResponse.Success || accountsResponse.Result.Length == 0)
            {
                throw new Exception("Token is valid but has no access to any accounts.");
            }

            return accountsResponse.Result[0].Id;
        }
        catch (ApiException ex)
        {
            throw new Exception($"Cloudflare API Error: {ex.StatusCode} - {ex.Content}");
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

    public async Task<string> GetTunnelTokenAsync(Token token, string tunnelId)
    {
        if (string.IsNullOrEmpty(token.AccountId))
            throw new Exception("Account ID missing for token.");

        var response = await _api.GetTunnelTokenAsync(token.ApiToken, token.AccountId, tunnelId);

        if (!response.Success || string.IsNullOrEmpty(response.Result))
            throw new Exception("Failed to retrieve tunnel token.");

        return response.Result;
    }

    public async Task ConfigureTunnelAsync(Token token, string tunnelId, string hostname, string localUrl, bool noTlsVerify = false)
    {
        if (string.IsNullOrEmpty(token.AccountId))
            throw new Exception("Account ID missing for token.");

        var ingressRule = new TunnelIngressRule
        {
            Hostname = hostname,
            Service = localUrl
        };

        if (noTlsVerify)
        {
            ingressRule.OriginRequest = new TunnelOriginRequest
            {
                NoTLSVerify = true
            };
        }

        var config = new TunnelConfig
        {
            Ingress = new List<TunnelIngressRule>
            {
                ingressRule,
                new TunnelIngressRule
                {
                    Service = "http_status:404"
                }
            }
        };

        var request = new UpdateTunnelConfigRequest { Config = config };
        var response = await _api.UpdateTunnelConfigAsync(token.ApiToken, token.AccountId, tunnelId, request);

        if (!response.Success)
            throw new Exception("Failed to configure tunnel ingress rules.");
    }

    public async Task<List<TunnelDetail>> ListTunnelsAsync(Token token)
    {
        if (string.IsNullOrEmpty(token.AccountId))
            throw new Exception("Account ID missing for token.");

        // List active tunnels (is_deleted = false)
        var response = await _api.GetTunnelsAsync(token.ApiToken, token.AccountId, isDeleted: false);

        if (!response.Success)
            throw new Exception("Failed to list tunnels.");

        return response.Result
            .Where(t => t.DeletedAt == null)
            .Select(t => new TunnelDetail
            {
                Id = t.Id,
                Name = t.Name,
                DeletedAt = t.DeletedAt
            })
            .ToList();
    }

    public async Task DeleteTunnelAsync(Token token, string tunnelId)
    {
        if (string.IsNullOrEmpty(token.AccountId))
            throw new Exception("Account ID missing for token.");

        await _api.DeleteTunnelAsync(token.ApiToken, token.AccountId, tunnelId);
    }

    public async Task<string> CreateDnsRecordAsync(Token token, string zoneId, string name, string content, bool proxied = true)
    {
        var request = new CreateDnsRecordRequest
        {
            Type = "CNAME",
            Name = name,
            Content = content,
            Proxied = proxied,
            Ttl = 1 // Auto
        };

        var response = await _api.CreateDnsRecordAsync(token.ApiToken, zoneId, request);

        if (!response.Success || response.Result == null)
            throw new Exception("Failed to create DNS record.");

        return response.Result.Id;
    }

    public async Task DeleteDnsRecordAsync(Token token, string zoneId, string recordId)
    {
        await _api.DeleteDnsRecordAsync(token.ApiToken, zoneId, recordId);
    }

    public async Task<string?> GetZoneIdAsync(Token token, string domainName)
    {
        // Cloudflare Zones API matches exact or fuzzy. We need the root zone.
        // E.g. for "sub.example.com", we might need to search for "example.com"
        // For simplicity, we search for the exact match first.
        // Ideally, we'd traverse up the domain tree.

        var response = await _api.GetZonesAsync(token.ApiToken, domainName);
        if (response.Success && response.Result.Length > 0)
        {
            return response.Result[0].Id;
        }

        // Try searching for the parent domain if not found (simple one-level up check)
        var parts = domainName.Split('.');
        if (parts.Length > 2)
        {
            var parent = string.Join(".", parts.Skip(1));
            response = await _api.GetZonesAsync(token.ApiToken, parent);
            if (response.Success && response.Result.Length > 0)
            {
                return response.Result[0].Id;
            }
        }

        return null;
    }
}
