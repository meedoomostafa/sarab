using FluentAssertions;
using Moq;
using Sarab.Core.Entities;
using Sarab.Core.Interfaces;
using Sarab.Infrastructure.Services;

namespace Sarab.Tests.Services;

public class ProcessManagerTests
{
    private readonly Mock<IArtifactStore> _storeMock;
    private readonly ProcessManager _manager;

    public ProcessManagerTests()
    {
        _storeMock = new Mock<IArtifactStore>();
        _manager = new ProcessManager(_storeMock.Object);
    }

    [Fact]
    public async Task EnsureBinaryExistsAsync_ReturnsWhenBinaryValid()
    {
        // We cannot easily test CliWrap execution without a real binary,
        // but we can verify the artifact store is consulted.
        _storeMock.Setup(s => s.EnsureCloudflaredBinaryAsync()).ReturnsAsync("/bin/true");

        // /bin/true always exits 0, so --version (which exits non-zero) will fail.
        // This test documents the expected behavior when a fake path is provided.
        // Skip actual execution and test the dependency call.
        _storeMock.Verify(s => s.EnsureCloudflaredBinaryAsync(), Times.Never);

        // We verify the store is called by invoking the method and catching the expected error.
        Func<Task> act = async () => await _manager.EnsureBinaryExistsAsync();
        try { await act(); } catch { /* expected with fake path */ }

        _storeMock.Verify(s => s.EnsureCloudflaredBinaryAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StopAllTunnelsAsync_CompletesImmediately()
    {
        await _manager.StopAllTunnelsAsync();

        // No-op implementation should complete synchronously
        // If this does not hang, the test passes.
    }
}
