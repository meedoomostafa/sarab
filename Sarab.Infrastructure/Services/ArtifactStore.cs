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
    private readonly IPlatformEnvironment _platform;
    private const string BaseUrl = "https://github.com/cloudflare/cloudflared/releases/latest/download";

    public ArtifactStore(HttpClient http, IPlatformEnvironment platform)
    {
        _http = http;
        _platform = platform;
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

    private string GetBinaryName()
    {
        if (_platform.IsWindows())
        {
            return "cloudflared.exe";
        }
        return "cloudflared";
    }

    internal string BuildDownloadUrl()
    {
        var os = GetOsIdentifier();
        var arch = _platform.ProcessArchitecture switch
        {
            Architecture.X64 => "amd64",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            Architecture.X86 => "386",
            _ => throw new PlatformNotSupportedException($"Architecture {_platform.ProcessArchitecture} not supported")
        };

        var extension = _platform.IsWindows() ? ".exe" : "";
        return $"{BaseUrl}/cloudflared-{os}-{arch}{extension}";
    }

    private string GetOsIdentifier()
    {
        if (_platform.IsWindows())
            return "windows";
        if (_platform.IsMacOS())
            return "darwin";
        if (_platform.IsLinux())
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
        if (!_platform.IsLinux() && !_platform.IsMacOS())
            return;

        // Grant execute permissions for owner, group, and others
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                   UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                                   UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
    }
}
