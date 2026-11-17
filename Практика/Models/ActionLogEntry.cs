using System.ComponentModel.DataAnnotations.Schema;

namespace Практика.Models;

/// <summary>Запись журнала действий пользователей.</summary>
[Table("action_log")]
public class ActionLogEntry
{
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }
    public User? User { get; set; }

    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [Column("entity")]
    public string? Entity { get; set; }

    [Column("entity_id")]
    public int? EntityId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
