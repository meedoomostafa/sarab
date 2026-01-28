using System.Threading.Tasks;
using Sarab.Core.Entities;

namespace Sarab.Core.Interfaces;

public interface ICloudflareAdapter
{
    Task<string> VerifyTokenAsync(string apiToken); // Returns Account ID
    Task<string> CreateTunnelAsync(Token token, string name);
    Task<string> GetTunnelTokenAsync(Token token, string tunnelId);
    Task<string> CreateDnsRecordAsync(Token token, string zoneId, string name, string content, bool proxied = true);
    Task DeleteDnsRecordAsync(Token token, string zoneId, string recordId);
    Task<string?> GetZoneIdAsync(Token token, string domainName);
}
