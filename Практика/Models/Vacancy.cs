using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Практика.Models;

/// <summary>Вакансия — открытая должность в компании.</summary>
[Table("vacancies")]
public class Vacancy
{
    [Column("id")]
    public int Id { get; set; }

    [Column("company_id")]
    [Display(Name = "Компания")]
    [Range(1, int.MaxValue, ErrorMessage = "Выберите компанию")]
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [Column("title")]
    [Required(ErrorMessage = "Укажите название вакансии")]
    [StringLength(150)]
    [Display(Name = "Название вакансии")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [Column("salary_min")]
    [Range(0, 10_000_000, ErrorMessage = "Зарплата должна быть неотрицательным числом")]
    [Display(Name = "Зарплата от")]
    public int? SalaryMin { get; set; }

    [Column("salary_max")]
    [Range(0, 10_000_000, ErrorMessage = "Зарплата должна быть неотрицательным числом")]
    [Display(Name = "Зарплата до")]
    public int? SalaryMax { get; set; }

    [Column("work_format")]
    [StringLength(20)]
    [Display(Name = "Формат работы")]
    public string? WorkFormat { get; set; }

    [Column("required_experience")]
    [StringLength(30)]
    [Display(Name = "Требуемый опыт")]
    public string? RequiredExperience { get; set; }

    [Column("required_age_min")]
    [Range(14, 100, ErrorMessage = "Возраст должен быть в диапазоне от 14 до 100 лет")]
    [Display(Name = "Возраст от")]
    public int? RequiredAgeMin { get; set; }

    [Column("required_age_max")]
    [Range(14, 100, ErrorMessage = "Возраст должен быть в диапазоне от 14 до 100 лет")]
    [Display(Name = "Возраст до")]
    public int? RequiredAgeMax { get; set; }

    [Column("required_gender")]
    [StringLength(10)]
    [Display(Name = "Требуемый пол")]
    public string? RequiredGender { get; set; }

    [Column("status")]
    [StringLength(30)]
    [Display(Name = "Статус вакансии")]
    public string Status { get; set; } = "active";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public List<VacancySkill> VacancySkills { get; set; } = new();
    public List<Application> Applications { get; set; } = new();

    [NotMapped]
    public string StatusName => Status == "active" ? "Открыта" : "Закрыта";

    /// <summary>Зарплатная вилка в читаемом виде.</summary>
    [NotMapped]
    public string SalaryText => (SalaryMin, SalaryMax) switch
    {
        (null, null) => "не указана",
        (not null, null) => $"от {SalaryMin:N0} ₽",
        (null, not null) => $"до {SalaryMax:N0} ₽",
        _ => $"{SalaryMin:N0} – {SalaryMax:N0} ₽"
    };
}
