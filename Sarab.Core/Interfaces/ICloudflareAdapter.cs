using System.Collections.Generic;
using System.Threading.Tasks;
using Sarab.Core.Entities;
using Sarab.Core.DTOs;

namespace Sarab.Core.Interfaces;

public interface ICloudflareAdapter
{
    Task<string> VerifyTokenAsync(string apiToken); // Returns Account ID
    Task<string> CreateTunnelAsync(Token token, string name);
    Task<string> GetTunnelTokenAsync(Token token, string tunnelId);
    Task ConfigureTunnelAsync(Token token, string tunnelId, string hostname, string localUrl, bool noTlsVerify = false);
    Task<List<TunnelDetail>> ListTunnelsAsync(Token token);
    Task DeleteTunnelAsync(Token token, string tunnelId);
    Task<string> CreateDnsRecordAsync(Token token, string zoneId, string name, string content, bool proxied = true);
    Task DeleteDnsRecordAsync(Token token, string zoneId, string recordId);
    Task<string?> GetZoneIdAsync(Token token, string domainName);
}
