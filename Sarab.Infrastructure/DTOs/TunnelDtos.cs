using System.Text.Json.Serialization;

namespace Sarab.Infrastructure.DTOs;

public class CreateTunnelRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("config_src")]
    public string ConfigSrc { get; set; } = "local";
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

    [JsonPropertyName("token")]
    public string? Token { get; set; }
}

public class TunnelTokenResponse
{
    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

public class TunnelListResponse
{
    [JsonPropertyName("result")]
    public CloudflareTunnelDetail[] Result { get; set; } = Array.Empty<CloudflareTunnelDetail>();

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

public class CloudflareTunnelDetail
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}
