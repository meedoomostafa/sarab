using System.Threading.Tasks;
using Sarab.Core.Entities;

namespace Sarab.Core.Interfaces;

public interface ICloudflareAdapter
{
    Task<string> VerifyTokenAsync(string apiToken); // Returns Account ID
    Task<string> CreateTunnelAsync(Token token, string name);
    Task CreateDnsRecordAsync(Token token, string startUrl, string subdomain);
    Task DeleteDnsRecordAsync(Token token, string recordId);
}
