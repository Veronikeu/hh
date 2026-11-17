using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Практика.Data;
using Практика.Models;
using Практика.Services;

namespace Практика.Pages.Applications;

/// <summary>
/// Отклики кандидатов на вакансии: просмотр, создание и изменение статуса.
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

    [BindProperty(SupportsGet = true)] public int? VacancyId { get; set; }

    public List<Application> Items { get; set; } = new();
    public List<Vacancy> Vacancies { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadListsAsync();

        IQueryable<Application> query = _db.Applications
            .Include(a => a.Candidate)
            .Include(a => a.Vacancy).ThenInclude(v => v!.Company);

        if (VacancyId is > 0)
            query = query.Where(a => a.VacancyId == VacancyId);

        Items = await query
            .OrderByDescending(a => a.UpdatedAt)
            .ToListAsync();
    }

    /// <summary>Изменение этапа рассмотрения кандидата.</summary>
    public async Task<IActionResult> OnPostChangeStatusAsync(int id, string newStatus)
    {
        if (!ApplicationStatuses.All.ContainsKey(newStatus))
        {
            this.ToastError("Статус не изменён", "Выбрано недопустимое значение статуса.");
            return RedirectToPage(new { VacancyId });
        }

        var application = await _db.Applications
            .Include(a => a.Candidate)
            .Include(a => a.Vacancy)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application is null)
        {
            this.ToastError("Отклик не найден", "Возможно, запись уже удалена.");
            return RedirectToPage(new { VacancyId });
        }

        if (application.Status == newStatus)
        {
            this.ToastInfo("Статус не изменился", $"Отклик уже находится в состоянии «{application.StatusName}».");
            return RedirectToPage(new { VacancyId });
        }

        application.Status = newStatus;
        application.UpdatedAt = Clock.Now;
        await _db.SaveChangesAsync();

        await _log.WriteAsync(CurrentUserId, "Изменение статуса отклика", "applications", application.Id);

        var text = $"«{application.Candidate?.Fio}» → «{application.Vacancy?.Title}»: {application.StatusName}.";
        await _notifications.NotifyEveryoneAsync(CurrentUserId, "Изменён статус отклика", text,
            newStatus == ApplicationStatuses.Rejected ? NotificationTypes.Warning : NotificationTypes.Success);

        this.ToastSuccess("Статус отклика изменён", text);
        return RedirectToPage(new { VacancyId });
    }

    /// <summary>Удаление отклика доступно только администратору.</summary>
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!IsAdmin)
        {
            this.ToastError("Недостаточно прав", "Удалять отклики может только администратор.");
            return RedirectToPage(new { VacancyId });
        }

        var application = await _db.Applications.FindAsync(id);
        if (application is null)
        {
            this.ToastError("Отклик не найден", "Возможно, запись уже удалена.");
            return RedirectToPage(new { VacancyId });
        }

        _db.Applications.Remove(application);
        await _db.SaveChangesAsync();

        await _log.WriteAsync(CurrentUserId, "Удаление отклика", "applications", id);

        this.ToastSuccess("Отклик удалён", "Запись удалена из базы данных.");
        return RedirectToPage(new { VacancyId });
    }

    private async Task LoadListsAsync()
    {
        Vacancies = await _db.Vacancies
            .Include(v => v.Company)
            .OrderBy(v => v.Title)
            .ToListAsync();
    }
}
