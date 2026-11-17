using Практика.Data;
using Практика.Models;

namespace Практика.Services;

/// <summary>Журнал действий пользователей: фиксирует основные операции.</summary>
public class ActionLogService
{
    private readonly AppDbContext _db;

    public ActionLogService(AppDbContext db) => _db = db;

    public async Task WriteAsync(int? userId, string action, string? entity = null, int? entityId = null)
    {
        _db.ActionLog.Add(new ActionLogEntry
        {
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            CreatedAt = Clock.Now
        });

        await _db.SaveChangesAsync();
    }
}
