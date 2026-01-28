using System;

namespace Sarab.Core.Entities;

public class Token
{
    public int Id { get; set; }
    public required string Alias { get; set; }
    public required string ApiToken { get; set; }
    public string? AccountId { get; set; }
    public bool IsActive { get; set; } = true;
    public int FailureCount { get; set; } = 0;
    public DateTime? LastUsedAt { get; set; }
}
