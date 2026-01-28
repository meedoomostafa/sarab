using System.Text.Json.Serialization;

namespace Sarab.Infrastructure.DTOs;

public class CreateTunnelRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("config_src")]
    public string ConfigSrc { get; set; } = "cloudflare";
}

public class CreateTunnelResponse
{
    [JsonPropertyName("result")]
    public TunnelResult? Result { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

public class TunnelResult
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
}

public class TunnelTokenResponse
{
    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}
