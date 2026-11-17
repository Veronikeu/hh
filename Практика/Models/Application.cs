using System.ComponentModel.DataAnnotations.Schema;

namespace Практика.Models;

/// <summary>Отклик — связь кандидата с вакансией.</summary>
[Table("applications")]
public class Application
{
    [Column("id")]
    public int Id { get; set; }

    [Column("candidate_id")]
    public int CandidateId { get; set; }
    public Candidate? Candidate { get; set; }

    [Column("vacancy_id")]
    public int VacancyId { get; set; }
    public Vacancy? Vacancy { get; set; }

    [Column("status")]
    public string Status { get; set; } = ApplicationStatuses.New;

    [Column("applied_at")]
    public DateTime AppliedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [NotMapped]
    public string StatusName => ApplicationStatuses.GetName(Status);
}

/// <summary>Статусы рассмотрения отклика.</summary>
public static class ApplicationStatuses
{
    public const string New = "new";
    public const string Review = "review";
    public const string Interview = "interview";
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";

    /// <summary>Все статусы в порядке прохождения этапов отбора.</summary>
    public static readonly Dictionary<string, string> All = new()
    {
        [New] = "Новый",
        [Review] = "На рассмотрении",
        [Interview] = "Собеседование",
        [Accepted] = "Принят",
        [Rejected] = "Отклонён"
    };

    public static string GetName(string status) =>
        All.TryGetValue(status, out var name) ? name : status;
}
