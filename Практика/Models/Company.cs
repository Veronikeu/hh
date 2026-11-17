using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Практика.Models;

/// <summary>Компания-работодатель, размещающая вакансии.</summary>
[Table("companies")]
public class Company
{
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [Required(ErrorMessage = "Укажите название компании")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Column("industry")]
    [StringLength(100)]
    public string? Industry { get; set; }

    [Column("city")]
    [StringLength(100)]
    public string? City { get; set; }

    [Column("phone")]
    [StringLength(30)]
    public string? Phone { get; set; }

    [Column("email")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Column("website")]
    [StringLength(150)]
    public string? Website { get; set; }

    public List<Vacancy> Vacancies { get; set; } = new();
}
