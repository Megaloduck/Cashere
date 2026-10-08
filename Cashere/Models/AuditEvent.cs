using System;

namespace Cashere.Models;

/// <summary>A compact record of a business or administration change.</summary>
public sealed class AuditEvent
{
    public long Id { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public int? ActorCashierId { get; set; }
    public string ActorName { get; set; } = "System";
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? BeforeValuesJson { get; set; }
    public string? AfterValuesJson { get; set; }
    public bool IsUndone { get; set; }
    public bool IsReversible { get; set; }
}
