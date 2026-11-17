using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Практика.Models;

/// <summary>Профессиональный навык.</summary>
[Table("skills")]
public class Skill
{
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public List<CandidateSkill> CandidateSkills { get; set; } = new();
    public List<VacancySkill> VacancySkills { get; set; } = new();
}

/// <summary>Связь «кандидат — навык» (многие ко многим).</summary>
[Table("candidate_skills")]
public class CandidateSkill
{
    [Column("candidate_id")]
    public int CandidateId { get; set; }
    public Candidate? Candidate { get; set; }

    [Column("skill_id")]
    public int SkillId { get; set; }
    public Skill? Skill { get; set; }
}

/// <summary>Связь «вакансия — требуемый навык» (многие ко многим).</summary>
[Table("vacancy_skills")]
public class VacancySkill
{
    [Column("vacancy_id")]
    public int VacancyId { get; set; }
    public Vacancy? Vacancy { get; set; }

    [Column("skill_id")]
    public int SkillId { get; set; }
    public Skill? Skill { get; set; }
}
