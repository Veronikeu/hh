using Microsoft.EntityFrameworkCore;
using Практика.Data;
using Практика.Models;

namespace Практика.Services;

/// <summary>
/// Уведомления пользователей о событиях системы.
/// Сохраняются в базе данных и показываются всплывающими окнами.
/// </summary>
public class NotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db) => _db = db;

    /// <summary>Создаёт уведомление для одного пользователя.</summary>
    public async Task NotifyAsync(int userId, string title, string message, string type = NotificationTypes.Info)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            CreatedAt = Clock.Now
        });

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Создаёт уведомление всем активным пользователям, кроме автора события:
    /// сам автор уже увидел результат своего действия.
    /// </summary>
    public async Task NotifyEveryoneAsync(int exceptUserId, string title, string message, string type = NotificationTypes.Info)
    {
        var userIds = await _db.Users
            .Where(u => u.IsActive && u.Id != exceptUserId)
            .Select(u => u.Id)
            .ToListAsync();

        if (userIds.Count == 0)
            return;

        var now = Clock.Now;
        foreach (var userId in userIds)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = now
            });
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Возвращает непрочитанные уведомления пользователя и сразу отмечает их прочитанными,
    /// чтобы одно и то же окно не всплывало повторно.
    /// </summary>
    public async Task<List<Notification>> TakeUnreadAsync(int userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync();

        if (unread.Count == 0)
            return unread;

        foreach (var notification in unread)
            notification.IsRead = true;

        await _db.SaveChangesAsync();
        return unread;
    }
}
