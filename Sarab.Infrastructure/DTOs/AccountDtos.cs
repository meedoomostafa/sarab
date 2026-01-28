using System.Text.Json.Serialization;

namespace Sarab.Infrastructure.DTOs;

public class AccountListResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("result")]
    public AccountResult[] Result { get; set; } = Array.Empty<AccountResult>();
}

public class AccountResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
