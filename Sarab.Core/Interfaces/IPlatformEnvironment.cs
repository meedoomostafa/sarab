using System.Runtime.InteropServices;

namespace Sarab.Core.Interfaces;

public interface IPlatformEnvironment
{
    bool IsWindows();
    bool IsLinux();
    bool IsMacOS();
    Architecture ProcessArchitecture { get; }
}
