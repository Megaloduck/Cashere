using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cashere.Services;

public record AuditLogEntry(long Id, DateTime OccurredAtUtc, string ActorName, string Action, string EntityType, string? EntityId, string Summary, bool IsUndone, bool IsReversible)
{
    public bool CanUndo => IsReversible && !IsUndone;
    public bool CanRedo => IsReversible && IsUndone;
}

public interface IAuditLogService
{
    Task<List<AuditLogEntry>> GetRecentAsync(int limit = 200);
    Task<bool> UndoAsync(long auditEventId);
    Task<bool> RedoAsync(long auditEventId);
}
