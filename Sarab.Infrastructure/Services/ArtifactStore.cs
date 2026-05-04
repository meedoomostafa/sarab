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
    private const string BaseUrl = "https://github.com/cloudflare/cloudflared/releases/latest/download";

    public ArtifactStore(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> EnsureCloudflaredBinaryAsync()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var binDir = Path.Combine(home, ".sarab", "bin");
        var binName = GetBinaryName();
        var binPath = Path.Combine(binDir, binName);

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

    private static string GetBinaryName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "cloudflared.exe";
        }
        return "cloudflared";
    }

    private static string BuildDownloadUrl()
    {
        var os = GetOsIdentifier();
        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "amd64",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            Architecture.X86 => "386",
            _ => throw new PlatformNotSupportedException($"Architecture {RuntimeInformation.ProcessArchitecture} not supported")
        };

        var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
        return $"{BaseUrl}/cloudflared-{os}-{arch}{extension}";
    }

    private static string GetOsIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "darwin";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux";

        throw new PlatformNotSupportedException($"OS platform not supported");
    }

    private async Task DownloadBinaryAsync(string path)
    {
        var url = BuildDownloadUrl();

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
