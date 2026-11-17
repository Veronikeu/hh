using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Практика.Models;

/// <summary>Кандидат — соискатель на вакантную должность.</summary>
[Table("candidates")]
public class Candidate
{
    [Column("id")]
    public int Id { get; set; }

    [Column("fio")]
    [Required(ErrorMessage = "Укажите ФИО кандидата")]
    [StringLength(150)]
    [Display(Name = "ФИО")]
    public string Fio { get; set; } = string.Empty;

    [Column("age")]
    [Range(14, 100, ErrorMessage = "Возраст должен быть в диапазоне от 14 до 100 лет")]
    [Display(Name = "Возраст")]
    public int? Age { get; set; }

    [Column("gender")]
    [StringLength(10)]
    [Display(Name = "Пол")]
    public string? Gender { get; set; }

    [Column("phone")]
    [StringLength(30)]
    [Display(Name = "Телефон")]
    public string? Phone { get; set; }

    [Column("email")]
    [StringLength(100)]
    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Column("city")]
    [StringLength(100)]
    [Display(Name = "Город")]
    public string? City { get; set; }

    [Column("pc_level")]
    [StringLength(30)]
    [Display(Name = "Уровень владения ПК")]
    public string? PcLevel { get; set; }

    [Column("language")]
    [StringLength(50)]
    [Display(Name = "Иностранный язык")]
    public string? Language { get; set; }

    [Column("team_management")]
    [StringLength(20)]
    [Display(Name = "Опыт управления командой")]
    public string? TeamManagement { get; set; }

    [Column("work_format")]
    [StringLength(20)]
    [Display(Name = "Формат работы")]
    public string? WorkFormat { get; set; }

    [Column("experience")]
    [StringLength(30)]
    [Display(Name = "Опыт работы")]
    public string? Experience { get; set; }

    [Column("salary")]
    [Range(0, 10_000_000, ErrorMessage = "Зарплата должна быть неотрицательным числом")]
    [Display(Name = "Ожидаемая зарплата")]
    public int? Salary { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public List<CandidateSkill> CandidateSkills { get; set; } = new();
    public List<Education> Educations { get; set; } = new();
    public List<Application> Applications { get; set; } = new();
}
