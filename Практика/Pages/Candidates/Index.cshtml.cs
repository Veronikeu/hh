using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Практика.Data;
using Практика.Models;
using Практика.Services;

namespace Практика.Pages.Candidates;

/// <summary>
/// Список кандидатов с многокритериальным поиском, фильтрацией и сортировкой.
/// Незаполненные параметры поиска в запрос не попадают.
/// </summary>
public class IndexModel : AppPageModel
{
    private readonly AppDbContext _db;
    private readonly ActionLogService _log;
    private readonly NotificationService _notifications;

    public IndexModel(AppDbContext db, ActionLogService log, NotificationService notifications)
    {
        _db = db;
        _log = log;
        _notifications = notifications;
    }

    // ---------- Параметры поиска ----------
    [BindProperty(SupportsGet = true)] public string? Fio { get; set; }
    [BindProperty(SupportsGet = true)] public string? City { get; set; }
    [BindProperty(SupportsGet = true)] public string? Gender { get; set; }
    [BindProperty(SupportsGet = true)] public int? AgeMin { get; set; }
    [BindProperty(SupportsGet = true)] public int? AgeMax { get; set; }
    [BindProperty(SupportsGet = true)] public int? SalaryMin { get; set; }
    [BindProperty(SupportsGet = true)] public int? SalaryMax { get; set; }
    [BindProperty(SupportsGet = true)] public string? Experience { get; set; }
    [BindProperty(SupportsGet = true)] public string? WorkFormat { get; set; }
    [BindProperty(SupportsGet = true)] public string? Language { get; set; }
    [BindProperty(SupportsGet = true)] public string? PcLevel { get; set; }
    [BindProperty(SupportsGet = true)] public string? TeamManagement { get; set; }
    [BindProperty(SupportsGet = true)] public string? Education { get; set; }
    [BindProperty(SupportsGet = true)] public List<int> SkillIds { get; set; } = new();

    // ---------- Сортировка ----------
    [BindProperty(SupportsGet = true)] public string Sort { get; set; } = "fio";
    [BindProperty(SupportsGet = true)] public string Dir { get; set; } = "asc";

    // ---------- Данные для страницы ----------
    public List<Candidate> Items { get; set; } = new();
    public List<Skill> AllSkills { get; set; } = new();
    public List<string> Cities { get; set; } = new();

    /// <summary>Признак того, что задан хотя бы один параметр поиска.</summary>
    public bool FilterApplied =>
        !string.IsNullOrWhiteSpace(Fio) || !string.IsNullOrWhiteSpace(City) ||
        !string.IsNullOrWhiteSpace(Gender) || AgeMin.HasValue || AgeMax.HasValue ||
        SalaryMin.HasValue || SalaryMax.HasValue || !string.IsNullOrWhiteSpace(Experience) ||
        !string.IsNullOrWhiteSpace(WorkFormat) || !string.IsNullOrWhiteSpace(Language) ||
        !string.IsNullOrWhiteSpace(PcLevel) || !string.IsNullOrWhiteSpace(TeamManagement) ||
        !string.IsNullOrWhiteSpace(Education) || SkillIds.Count > 0;

    public async Task OnGetAsync()
    {
        AllSkills = await _db.Skills.OrderBy(s => s.Name).ToListAsync();
        Cities = await _db.Candidates
            .Where(c => c.City != null && c.City != "")
            .Select(c => c.City!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        Items = await BuildQuery().ToListAsync();
    }

    /// <summary>Удаление кандидата (подтверждение запрашивается в интерфейсе).</summary>
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var candidate = await _db.Candidates.FindAsync(id);
        if (candidate is null)
        {
            this.ToastError("Кандидат не найден", "Возможно, запись уже удалена.");
            return RedirectToPage();
        }

        var fio = candidate.Fio;
        _db.Candidates.Remove(candidate);
        await _db.SaveChangesAsync();

        await _log.WriteAsync(CurrentUserId, "Удаление кандидата", "candidates", id);
        await _notifications.NotifyEveryoneAsync(CurrentUserId, "Кандидат удалён",
            $"{CurrentUserFio} удалил(а) карточку кандидата «{fio}».", NotificationTypes.Warning);

        this.ToastSuccess("Кандидат удалён", $"Запись «{fio}» больше не хранится в базе.");
        return RedirectToPage();
    }

    /// <summary>
    /// Формирует запрос к базе данных по заполненным параметрам поиска.
    /// EF Core превращает его в параметризованный SQL-запрос.
    /// </summary>
    private IQueryable<Candidate> BuildQuery()
    {
        IQueryable<Candidate> query = _db.Candidates
            .Include(c => c.CandidateSkills).ThenInclude(cs => cs.Skill)
            .Include(c => c.Educations);

        if (!string.IsNullOrWhiteSpace(Fio))
            query = query.Where(c => EF.Functions.ILike(c.Fio, $"%{Fio.Trim()}%"));

        if (!string.IsNullOrWhiteSpace(City))
            query = query.Where(c => c.City == City);

        if (!string.IsNullOrWhiteSpace(Gender))
            query = query.Where(c => c.Gender == Gender);

        if (AgeMin.HasValue)
            query = query.Where(c => c.Age >= AgeMin);

        if (AgeMax.HasValue)
            query = query.Where(c => c.Age <= AgeMax);

        if (SalaryMin.HasValue)
            query = query.Where(c => c.Salary >= SalaryMin);

        if (SalaryMax.HasValue)
            query = query.Where(c => c.Salary <= SalaryMax);

        if (!string.IsNullOrWhiteSpace(Experience))
            query = query.Where(c => c.Experience == Experience);

        if (!string.IsNullOrWhiteSpace(WorkFormat))
            query = query.Where(c => c.WorkFormat == WorkFormat);

        if (!string.IsNullOrWhiteSpace(Language))
            query = query.Where(c => c.Language == Language);

        if (!string.IsNullOrWhiteSpace(PcLevel))
            query = query.Where(c => c.PcLevel == PcLevel);

        if (!string.IsNullOrWhiteSpace(TeamManagement))
            query = query.Where(c => c.TeamManagement == TeamManagement);

        if (!string.IsNullOrWhiteSpace(Education))
        {
            var text = Education.Trim();
            query = query.Where(c => c.Educations.Any(e =>
                EF.Functions.ILike(e.Institution ?? "", $"%{text}%") ||
                EF.Functions.ILike(e.Specialty ?? "", $"%{text}%") ||
                EF.Functions.ILike(e.Degree ?? "", $"%{text}%")));
        }

        // Кандидат должен обладать всеми выбранными навыками
        foreach (var skillId in SkillIds.Distinct())
        {
            var id = skillId;
            query = query.Where(c => c.CandidateSkills.Any(cs => cs.SkillId == id));
        }

        return ApplySort(query);
    }

    private IQueryable<Candidate> ApplySort(IQueryable<Candidate> query)
    {
        var descending = Dir == "desc";

        return Sort switch
        {
            "age" => descending ? query.OrderByDescending(c => c.Age) : query.OrderBy(c => c.Age),
            "city" => descending ? query.OrderByDescending(c => c.City) : query.OrderBy(c => c.City),
            "salary" => descending ? query.OrderByDescending(c => c.Salary) : query.OrderBy(c => c.Salary),
            "experience" => descending ? query.OrderByDescending(c => c.Experience) : query.OrderBy(c => c.Experience),
            "created" => descending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
            _ => descending ? query.OrderByDescending(c => c.Fio) : query.OrderBy(c => c.Fio)
        };
    }

    /// <summary>Направление сортировки для ссылки в заголовке столбца.</summary>
    public string SortDirectionFor(string column) =>
        Sort == column && Dir == "asc" ? "desc" : "asc";

    /// <summary>
    /// Ссылка для сортировки по столбцу. Все заданные параметры поиска сохраняются,
    /// меняются только столбец и направление сортировки.
    /// </summary>
    public string SortUrl(string column)
    {
        var parameters = Request.Query
            .Where(item => item.Key != nameof(Sort) && item.Key != nameof(Dir))
            .SelectMany(item => item.Value.Select(value => (item.Key, Value: value ?? "")))
            .ToList();

        parameters.Add((nameof(Sort), column));
        parameters.Add((nameof(Dir), SortDirectionFor(column)));

        return "?" + string.Join("&", parameters.Select(p =>
            $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
    }

}
