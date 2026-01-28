using System.Text.Json.Serialization;

namespace Sarab.Infrastructure.DTOs;

public class CreateDnsRecordRequest
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "CNAME";

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("ttl")]
    public int Ttl { get; set; } = 1; // Automatic

    [JsonPropertyName("proxied")]
    public bool Proxied { get; set; } = true;
}

public class DnsRecordResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("result")]
    public DnsRecordResult? Result { get; set; }
}

public class DnsRecordResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class ZoneListResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("result")]
    public ZoneResult[] Result { get; set; } = Array.Empty<ZoneResult>();
}

public class ZoneResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
