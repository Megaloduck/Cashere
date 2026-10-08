using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cashere.Services;

public record AuditLogEntry(DateTime OccurredAtUtc, string ActorName, string Action, string EntityType, string? EntityId, string Summary);

public interface IAuditLogService
{
    Task<List<AuditLogEntry>> GetRecentAsync(int limit = 200);
}
