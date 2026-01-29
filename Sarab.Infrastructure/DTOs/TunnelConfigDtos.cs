using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Sarab.Infrastructure.DTOs;

public class UpdateTunnelConfigRequest
{
    [JsonPropertyName("config")]
    public TunnelConfig Config { get; set; } = new();
}

public class TunnelConfig
{
    [JsonPropertyName("ingress")]
    public List<TunnelIngressRule> Ingress { get; set; } = new();
}

public class TunnelIngressRule
{
    [JsonPropertyName("hostname")]
    public string? Hostname { get; set; }

    [JsonPropertyName("service")]
    public string Service { get; set; } = "http_status:404";

    [JsonPropertyName("originRequest")]
    public TunnelOriginRequest? OriginRequest { get; set; }
}

public class TunnelOriginRequest
{
    [JsonPropertyName("noTLSVerify")]
    public bool? NoTLSVerify { get; set; }
}

public class TunnelConfigResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("result")]
    public TunnelConfigResult? Result { get; set; }
}

public class TunnelConfigResult
{
    [JsonPropertyName("tunnel_id")]
    public string TunnelId { get; set; } = string.Empty;

    [JsonPropertyName("config")]
    public TunnelConfig Config { get; set; } = new();
}
