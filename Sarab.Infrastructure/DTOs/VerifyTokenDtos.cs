using System.Text.Json.Serialization;

namespace Sarab.Infrastructure.DTOs;

public class VerifyTokenResponse
{
    [JsonPropertyName("result")]
    public VerifyTokenResult? Result { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

public class VerifyTokenResult
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("status")]
    public required string Status { get; set; }
}
