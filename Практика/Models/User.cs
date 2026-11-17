using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Практика.Models;

/// <summary>
/// Пользователь системы: администратор или менеджер по подбору персонала.
/// </summary>
[Table("users")]
public class User
{
    [Column("id")]
    public int Id { get; set; }

    [Column("login")]
    [Required(ErrorMessage = "Укажите логин")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Логин должен содержать от 3 до 50 символов")]
    public string Login { get; set; } = string.Empty;

    /// <summary>Телефон необязателен: он нужен только для входа по номеру.</summary>
    [Column("phone")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Column("password_hash")]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("fio")]
    [Required(ErrorMessage = "Укажите ФИО")]
    [StringLength(150)]
    public string Fio { get; set; } = string.Empty;

    /// <summary>Роль: admin или manager.</summary>
    [Column("role")]
    [StringLength(30)]
    public string Role { get; set; } = Roles.Manager;

    /// <summary>Email необязателен: при регистрации он не запрашивается.</summary>
    [Column("email")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [NotMapped]
    public bool IsAdmin => Role == Roles.Admin;

    [NotMapped]
    public string RoleName => Role == Roles.Admin ? "Администратор" : "Менеджер по подбору";
}

/// <summary>Названия ролей пользователей.</summary>
public static class Roles
{
    public const string Admin = "admin";
    public const string Manager = "manager";
}
