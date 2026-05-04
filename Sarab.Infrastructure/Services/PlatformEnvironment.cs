using System.Runtime.InteropServices;
using Sarab.Core.Interfaces;

namespace Sarab.Infrastructure.Services;

public class PlatformEnvironment : IPlatformEnvironment
{
    public bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public Architecture ProcessArchitecture => RuntimeInformation.ProcessArchitecture;
}
