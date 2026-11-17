using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Практика.Data;
using Практика.Models;
using Практика.Services;

namespace Практика.Pages.Vacancies;

/// <summary>Список вакансий с фильтрами и управлением записями.</summary>
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

    [BindProperty(SupportsGet = true)] public string? Title { get; set; }
    [BindProperty(SupportsGet = true)] public int? CompanyId { get; set; }
    [BindProperty(SupportsGet = true)] public string? WorkFormat { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }

    public List<Vacancy> Items { get; set; } = new();
    public List<Company> Companies { get; set; } = new();
    public Dictionary<int, int> ApplicationCounts { get; set; } = new();

    public async Task OnGetAsync()
    {
        Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();

        IQueryable<Vacancy> query = _db.Vacancies
            .Include(v => v.Company)
            .Include(v => v.VacancySkills).ThenInclude(vs => vs.Skill);

        if (!string.IsNullOrWhiteSpace(Title))
            query = query.Where(v => EF.Functions.ILike(v.Title, $"%{Title.Trim()}%"));

        if (CompanyId is > 0)
            query = query.Where(v => v.CompanyId == CompanyId);

        if (!string.IsNullOrWhiteSpace(WorkFormat))
            query = query.Where(v => v.WorkFormat == WorkFormat);

        if (!string.IsNullOrWhiteSpace(Status))
            query = query.Where(v => v.Status == Status);

        Items = await query.OrderBy(v => v.Title).ToListAsync();

        ApplicationCounts = await _db.Applications
            .GroupBy(a => a.VacancyId)
            .Select(g => new { VacancyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VacancyId, x => x.Count);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var vacancy = await _db.Vacancies.FindAsync(id);
        if (vacancy is null)
        {
            this.ToastError("Вакансия не найдена", "Возможно, запись уже удалена.");
            return RedirectToPage();
        }

        var title = vacancy.Title;
        _db.Vacancies.Remove(vacancy);
        await _db.SaveChangesAsync();

        await _log.WriteAsync(CurrentUserId, "Удаление вакансии", "vacancies", id);
        await _notifications.NotifyEveryoneAsync(CurrentUserId, "Вакансия удалена",
            $"{CurrentUserFio} удалил(а) вакансию «{title}».", NotificationTypes.Warning);

        this.ToastSuccess("Вакансия удалена", $"Запись «{title}» больше не хранится в базе.");
        return RedirectToPage();
    }
}
