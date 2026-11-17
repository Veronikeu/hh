using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Практика.Services;

namespace Практика.Controllers;

/// <summary>
/// Выдаёт странице непрочитанные уведомления пользователя.
/// Страница показывает их всплывающими окнами.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notifications;

    public NotificationsController(NotificationService notifications) => _notifications = notifications;

    /// <summary>Непрочитанные уведомления. После выдачи отмечаются прочитанными.</summary>
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnread()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Ok(Array.Empty<object>());

        var items = await _notifications.TakeUnreadAsync(userId);

        return Ok(items.Select(n => new
        {
            n.Title,
            n.Message,
            Type = n.Type ?? "info",
            CreatedAt = n.CreatedAt
        }));
    }
}
