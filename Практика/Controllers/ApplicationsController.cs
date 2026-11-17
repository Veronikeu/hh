using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Практика.Data;
using Практика.Models;
using Практика.Services;

namespace Практика.Controllers;

/// <summary>Программный интерфейс для работы с откликами кандидатов.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ActionLogService _log;
    private readonly NotificationService _notifications;

    public ApplicationsController(AppDbContext db, ActionLogService log, NotificationService notifications)
    {
        _db = db;
        _log = log;
        _notifications = notifications;
    }

    private int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    /// <summary>Список откликов.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(string? status = null)
    {
        IQueryable<Application> query = _db.Applications
            .Include(a => a.Candidate)
            .Include(a => a.Vacancy);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status == status);

        var items = await query
            .OrderByDescending(a => a.UpdatedAt)
            .Select(a => new
            {
                a.Id,
                Candidate = a.Candidate!.Fio,
                a.Candidate.Phone,
                a.Candidate.Email,
                Vacancy = a.Vacancy!.Title,
                a.Status,
                a.AppliedAt,
                a.UpdatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Создание отклика кандидата на вакансию.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(int candidateId, int vacancyId)
    {
        var candidate = await _db.Candidates.FindAsync(candidateId);
        var vacancy = await _db.Vacancies.FindAsync(vacancyId);

        if (candidate is null || vacancy is null)
            return NotFound(new { message = "Кандидат или вакансия не найдены" });

        if (await _db.Applications.AnyAsync(a => a.CandidateId == candidateId && a.VacancyId == vacancyId))
            return Conflict(new { message = "Такой отклик уже существует" });

        var application = new Application
        {
            CandidateId = candidateId,
            VacancyId = vacancyId,
            Status = ApplicationStatuses.New,
            AppliedAt = Clock.Now,
            UpdatedAt = Clock.Now
        };

        _db.Applications.Add(application);
        await _db.SaveChangesAsync();

        await _log.WriteAsync(CurrentUserId, "Создание отклика (API)", "applications", application.Id);
        await _notifications.NotifyEveryoneAsync(CurrentUserId, "Новый отклик",
            $"«{candidate.Fio}» откликнулся(ась) на вакансию «{vacancy.Title}».", NotificationTypes.Success);

        return Ok(new { application.Id, application.Status });
    }

    /// <summary>Изменение статуса отклика.</summary>
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(int id, string status)
    {
        if (!ApplicationStatuses.All.ContainsKey(status))
            return BadRequest(new { message = "Недопустимое значение статуса" });

        var application = await _db.Applications
            .Include(a => a.Candidate)
            .Include(a => a.Vacancy)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application is null)
            return NotFound(new { message = "Отклик не найден" });

        application.Status = status;
        application.UpdatedAt = Clock.Now;
        await _db.SaveChangesAsync();

        await _log.WriteAsync(CurrentUserId, "Изменение статуса отклика (API)", "applications", application.Id);
        await _notifications.NotifyEveryoneAsync(CurrentUserId, "Изменён статус отклика",
            $"«{application.Candidate?.Fio}» → «{application.Vacancy?.Title}»: {application.StatusName}.",
            NotificationTypes.Info);

        return Ok(new { application.Id, application.Status, application.StatusName });
    }
}
