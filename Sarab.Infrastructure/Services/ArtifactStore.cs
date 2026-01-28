using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Sarab.Core.Interfaces;

namespace Sarab.Infrastructure.Services;

public class ArtifactStore : IArtifactStore
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux";

    public ArtifactStore(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> EnsureCloudflaredBinaryAsync()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var binDir = Path.Combine(home, ".sarab", "bin");
        var binPath = Path.Combine(binDir, "cloudflared");

        if (File.Exists(binPath))
        {
            return binPath;
        }

        Console.WriteLine("Downloading cloudflared binary...");
        Directory.CreateDirectory(binDir);

        await DownloadBinaryAsync(binPath);
        MakeExecutable(binPath);

        return binPath;
    }

    private async Task DownloadBinaryAsync(string path)
    {
        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "amd64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "386",
            _ => throw new PlatformNotSupportedException("Architecture not supported")
        };

        var url = ($"{BaseUrl}-{arch}");

        using var stream = await _http.GetStreamAsync(url);
        using var file = new FileStream(path, FileMode.Create);
        await stream.CopyToAsync(file);
    }

    private void MakeExecutable(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        // Grant execute permissions for owner, group, and others
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                   UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                                   UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
    }
}
