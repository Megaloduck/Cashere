using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;

    public AuditLogService(IDbContextFactory<CashereDbContext> dbContextFactory) => _dbContextFactory = dbContextFactory;

    public async Task<List<AuditLogEntry>> GetRecentAsync(int limit = 200)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.AuditEvents.AsNoTracking()
            .OrderByDescending(eventRow => eventRow.OccurredAtUtc)
            .Take(System.Math.Clamp(limit, 1, 1000))
            .Select(eventRow => new AuditLogEntry(
                eventRow.OccurredAtUtc, eventRow.ActorName, eventRow.Action,
                eventRow.EntityType, eventRow.EntityId, eventRow.Summary))
            .ToListAsync();
    }
}
