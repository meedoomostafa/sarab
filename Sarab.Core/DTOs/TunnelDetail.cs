using System;

namespace Sarab.Core.DTOs;

public class TunnelDetail
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? DeletedAt { get; set; }
}
