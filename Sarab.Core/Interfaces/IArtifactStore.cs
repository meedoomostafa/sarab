using System.Threading.Tasks;

namespace Sarab.Core.Interfaces;

public interface IArtifactStore
{
    Task<string> EnsureCloudflaredBinaryAsync();
}
