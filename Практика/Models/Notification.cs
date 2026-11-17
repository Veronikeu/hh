using System.ComponentModel.DataAnnotations.Schema;

namespace Практика.Models;

/// <summary>
/// Уведомление пользователя о событии в системе.
/// В интерфейсе показывается всплывающим окном (toast).
/// </summary>
[Table("notifications")]
public class Notification
{
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>Тип уведомления: success, info, warning, error.</summary>
    [Column("type")]
    public string? Type { get; set; }

    [Column("is_read")]
    public bool IsRead { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>Типы уведомлений, определяющие цвет всплывающего окна.</summary>
public static class NotificationTypes
{
    public const string Success = "success";
    public const string Info = "info";
    public const string Warning = "warning";
    public const string Error = "error";
}
