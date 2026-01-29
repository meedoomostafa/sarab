using System.Collections.Generic;
using System.Threading.Tasks;
using Sarab.Core.Entities;
using Sarab.Core.DTOs;

namespace Sarab.Core.Interfaces;

public interface ICloudflareAdapter
{
    Task<string> VerifyTokenAsync(string apiToken); // Returns Account ID
    Task<(string Id, string? Token)> CreateTunnelAsync(Token token, string name);
    Task<string> GetTunnelTokenAsync(Token token, string tunnelId);
    Task ConfigureTunnelAsync(Token token, string tunnelId, string hostname, string localUrl, bool noTlsVerify = false);
    Task<List<TunnelDetail>> ListTunnelsAsync(Token token);
    Task DeleteTunnelAsync(Token token, string tunnelId);
    Task<string> CreateDnsRecordAsync(Token token, string zoneId, string name, string content, bool proxied = true);
    Task DeleteDnsRecordAsync(Token token, string zoneId, string recordId);
    Task<string> UpdateDnsRecordAsync(Token token, string zoneId, string recordId, string name, string content, bool proxied = true);
    Task<List<DnsListResult>> ListDnsRecordsAsync(Token token, string zoneId, string? name = null);
    Task<string?> GetZoneIdAsync(Token token, string domainName);
}
