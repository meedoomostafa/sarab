using System.Net;
using System.Runtime.InteropServices;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Sarab.Core.Interfaces;
using Sarab.Infrastructure.Services;

namespace Sarab.Tests.Services;

public class ArtifactStoreTests : IDisposable
{
    private readonly string _testBaseDir;
    private readonly Mock<IPlatformEnvironment> _platformMock;
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;

    public ArtifactStoreTests()
    {
        _testBaseDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBaseDir);
        Environment.SetEnvironmentVariable("HOME", _testBaseDir);
        Environment.SetEnvironmentVariable("USERPROFILE", _testBaseDir);

        _platformMock = new Mock<IPlatformEnvironment>();
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        if (Directory.Exists(_testBaseDir))
        {
            Directory.Delete(_testBaseDir, recursive: true);
        }
    }

    private ArtifactStore CreateStore()
    {
        return new ArtifactStore(_httpClient, _platformMock.Object);
    }

    private void SetupBinaryResponse(string expectedUrl, byte[] content)
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString() == expectedUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(content)
            });
    }

    [Theory]
    [InlineData(true, false, false, Architecture.X64, "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe")]
    [InlineData(true, false, false, Architecture.Arm64, "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-arm64.exe")]
    [InlineData(false, true, false, Architecture.X64, "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64")]
    [InlineData(false, true, false, Architecture.Arm64, "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-arm64")]
    [InlineData(false, true, false, Architecture.Arm, "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-arm")]
    [InlineData(false, true, false, Architecture.X86, "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-386")]
    [InlineData(false, false, true, Architecture.X64, "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-darwin-amd64")]
    [InlineData(false, false, true, Architecture.Arm64, "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-darwin-arm64")]
    public void BuildDownloadUrl_ReturnsCorrectUrl(bool isWindows, bool isLinux, bool isMacOS, Architecture arch, string expectedUrl)
    {
        _platformMock.Setup(p => p.IsWindows()).Returns(isWindows);
        _platformMock.Setup(p => p.IsLinux()).Returns(isLinux);
        _platformMock.Setup(p => p.IsMacOS()).Returns(isMacOS);
        _platformMock.Setup(p => p.ProcessArchitecture).Returns(arch);

        var store = CreateStore();
        var url = store.BuildDownloadUrl();

        url.Should().Be(expectedUrl);
    }

    [Fact]
    public void BuildDownloadUrl_ThrowsOnUnsupportedArchitecture()
    {
        _platformMock.Setup(p => p.IsWindows()).Returns(false);
        _platformMock.Setup(p => p.IsLinux()).Returns(true);
        _platformMock.Setup(p => p.IsMacOS()).Returns(false);
        _platformMock.Setup(p => p.ProcessArchitecture).Returns(Architecture.Wasm);

        var store = CreateStore();

        Action act = () => store.BuildDownloadUrl();
        act.Should().Throw<PlatformNotSupportedException>();
    }

    [Fact]
    public void BuildDownloadUrl_ThrowsOnUnsupportedOs()
    {
        _platformMock.Setup(p => p.IsWindows()).Returns(false);
        _platformMock.Setup(p => p.IsLinux()).Returns(false);
        _platformMock.Setup(p => p.IsMacOS()).Returns(false);
        _platformMock.Setup(p => p.ProcessArchitecture).Returns(Architecture.X64);

        var store = CreateStore();

        Action act = () => store.BuildDownloadUrl();
        act.Should().Throw<PlatformNotSupportedException>();
    }

    [Fact]
    public async Task EnsureCloudflaredBinaryAsync_ReturnsExistingBinaryWithoutDownloading()
    {
        _platformMock.Setup(p => p.IsWindows()).Returns(false);
        _platformMock.Setup(p => p.IsLinux()).Returns(true);
        _platformMock.Setup(p => p.IsMacOS()).Returns(false);

        var binDir = Path.Combine(_testBaseDir, ".sarab", "bin");
        Directory.CreateDirectory(binDir);
        var binPath = Path.Combine(binDir, "cloudflared");
        await File.WriteAllTextAsync(binPath, "fake-binary");

        var store = CreateStore();
        var result = await store.EnsureCloudflaredBinaryAsync();

        result.Should().Be(binPath);
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task EnsureCloudflaredBinaryAsync_DownloadsAndCreatesBinaryOnLinux()
    {
        _platformMock.Setup(p => p.IsWindows()).Returns(false);
        _platformMock.Setup(p => p.IsLinux()).Returns(true);
        _platformMock.Setup(p => p.IsMacOS()).Returns(false);
        _platformMock.Setup(p => p.ProcessArchitecture).Returns(Architecture.X64);

        var expectedUrl = "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64";
        var fakeBinary = new byte[] { 0x7f, 0x45, 0x4c, 0x46 };
        SetupBinaryResponse(expectedUrl, fakeBinary);

        var store = CreateStore();
        var result = await store.EnsureCloudflaredBinaryAsync();

        result.Should().EndWith("cloudflared");
        File.Exists(result).Should().BeTrue();
        var content = await File.ReadAllBytesAsync(result);
        content.Should().Equal(fakeBinary);
    }

    [Fact]
    public async Task EnsureCloudflaredBinaryAsync_DownloadsAndCreatesBinaryOnWindows()
    {
        _platformMock.Setup(p => p.IsWindows()).Returns(true);
        _platformMock.Setup(p => p.IsLinux()).Returns(false);
        _platformMock.Setup(p => p.IsMacOS()).Returns(false);
        _platformMock.Setup(p => p.ProcessArchitecture).Returns(Architecture.X64);

        var expectedUrl = "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe";
        var fakeBinary = new byte[] { 0x4d, 0x5a };
        SetupBinaryResponse(expectedUrl, fakeBinary);

        var store = CreateStore();
        var result = await store.EnsureCloudflaredBinaryAsync();

        result.Should().EndWith("cloudflared.exe");
        File.Exists(result).Should().BeTrue();
        var content = await File.ReadAllBytesAsync(result);
        content.Should().Equal(fakeBinary);
    }
}
