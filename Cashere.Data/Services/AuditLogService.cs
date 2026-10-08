using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Cashere.Models;
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
        var settings = await db.ReceiptAdmin.AsNoTracking().FirstOrDefaultAsync();
        if (settings?.AutoDeleteAuditEnabled ?? true)
        {
            var retentionMonths = Math.Clamp(settings?.AuditRetentionMonths ?? 1, 1, 12);
            var cutoffUtc = DateTime.UtcNow.AddMonths(-retentionMonths);
            await db.AuditEvents.Where(row => row.OccurredAtUtc < cutoffUtc).ExecuteDeleteAsync();
        }

        return await db.AuditEvents.AsNoTracking()
            .OrderByDescending(row => row.OccurredAtUtc)
            .Take(Math.Clamp(limit, 1, 1000))
            .Select(row => new AuditLogEntry(
                row.Id, row.OccurredAtUtc, row.ActorName, row.Action,
                row.EntityType, row.EntityId, row.Summary, row.IsUndone,
                row.IsReversible))
            .ToListAsync();
    }

    public Task<bool> UndoAsync(long auditEventId) => ApplyAsync(auditEventId, undo: true);

    public Task<bool> RedoAsync(long auditEventId) => ApplyAsync(auditEventId, undo: false);

    private async Task<bool> ApplyAsync(long auditEventId, bool undo)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var change = await db.AuditEvents
            .Where(row => row.Id == auditEventId && (undo ? !row.IsUndone : row.IsUndone)
                && row.IsReversible && row.BeforeValuesJson != null && row.AfterValuesJson != null)
            .FirstOrDefaultAsync();
        if (change is null || string.IsNullOrWhiteSpace(change.EntityId)) return false;

        var entityType = db.Model.FindEntityType("Cashere.Models." + change.EntityType);
        var key = entityType?.FindPrimaryKey()?.Properties.SingleOrDefault();
        if (entityType is null || key is null || !long.TryParse(change.EntityId, out var numericId)) return false;
        var entityKey = Convert.ChangeType(numericId, key.ClrType, System.Globalization.CultureInfo.InvariantCulture);
        var entity = await db.FindAsync(entityType.ClrType, entityKey);
        if (entity is null) return false;

        var fromJson = undo ? change.AfterValuesJson : change.BeforeValuesJson;
        var toJson = undo ? change.BeforeValuesJson : change.AfterValuesJson;
        var expected = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(fromJson!);
        var target = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(toJson!);
        if (expected is null || target is null || expected.Count == 0 || expected.Keys.Except(target.Keys).Any()) return false;

        var tracked = db.Entry(entity);
        foreach (var (name, expectedValue) in expected)
        {
            var property = entityType.FindProperty(name);
            if (property is null) return false;
            var current = tracked.Property(name).CurrentValue;
            var expectedObject = expectedValue.Deserialize(property.ClrType);
            if (!Equals(current, expectedObject)) return false;
        }

        foreach (var (name, targetValue) in target)
        {
            var property = entityType.FindProperty(name);
            if (property is null) return false;
            tracked.Property(name).CurrentValue = targetValue.Deserialize(property.ClrType);
        }

        change.IsUndone = undo;
        db.SuppressAuditEvents = true;
        var (cashierId, actorName) = CurrentCashierContext.Snapshot();
        db.AuditEvents.Add(new AuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            ActorCashierId = cashierId,
            ActorName = actorName,
            Action = undo ? "Undo" : "Redo",
            EntityType = change.EntityType,
            EntityId = change.EntityId,
            Summary = $"{(undo ? "Undid" : "Redid")} {change.EntityType}: {change.Summary}"
        });
        await db.SaveChangesAsync();
        return true;
    }
}
