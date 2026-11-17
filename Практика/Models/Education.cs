using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Практика.Models;

/// <summary>Запись об образовании кандидата.</summary>
[Table("education")]
public class Education
{
    [Column("id")]
    public int Id { get; set; }

    [Column("candidate_id")]
    public int CandidateId { get; set; }
    public Candidate? Candidate { get; set; }

    [Column("institution")]
    [StringLength(150)]
    [Display(Name = "Учебное заведение")]
    public string? Institution { get; set; }

    [Column("specialty")]
    [StringLength(150)]
    [Display(Name = "Специальность")]
    public string? Specialty { get; set; }

    [Column("degree")]
    [StringLength(50)]
    [Display(Name = "Уровень образования")]
    public string? Degree { get; set; }

    [Column("year_end")]
    [Range(1950, 2100, ErrorMessage = "Год окончания указан некорректно")]
    [Display(Name = "Год окончания")]
    public int? YearEnd { get; set; }
}
