using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Практика.Data;
using Практика.Models;
using Практика.Services;

namespace Практика.Pages.Vacancies;

/// <summary>
/// Подбор кандидатов по требованиям вакансии и создание отклика.
/// </summary>
public class MatchModel : AppPageModel
{
    private readonly AppDbContext _db;
    private readonly ActionLogService _log;
    private readonly NotificationService _notifications;

    public MatchModel(AppDbContext db, ActionLogService log, NotificationService notifications)
    {
        _db = db;
        _log = log;
        _notifications = notifications;
    }

    public Vacancy? Vacancy { get; set; }
    public List<Candidate> Matches { get; set; } = new();
    public HashSet<int> AlreadyApplied { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var loaded = await LoadAsync(id);
        return loaded ? Page() : RedirectToPage("/Vacancies/Index");
    }

    /// <summary>Создание отклика выбранного кандидата на вакансию.</summary>
    public async Task<IActionResult> OnPostApplyAsync(int id, int candidateId)
    {
        var vacancy = await _db.Vacancies.FindAsync(id);
        var candidate = await _db.Candidates.FindAsync(candidateId);

        if (vacancy is null || candidate is null)
        {
            this.ToastError("Отклик не создан", "Вакансия или кандидат не найдены в базе данных.");
            return RedirectToPage(new { id });
        }

        var exists = await _db.Applications
            .AnyAsync(a => a.CandidateId == candidateId && a.VacancyId == id);

        if (exists)
        {
            this.ToastWarning("Отклик уже есть",
                $"Кандидат «{candidate.Fio}» уже откликался на вакансию «{vacancy.Title}».");
            return RedirectToPage(new { id });
        }

        var application = new Application
        {
            CandidateId = candidateId,
            VacancyId = id,
            Status = ApplicationStatuses.New,
            AppliedAt = Clock.Now,
            UpdatedAt = Clock.Now
        };

        _db.Applications.Add(application);
        await _db.SaveChangesAsync();

        await _log.WriteAsync(CurrentUserId, "Создание отклика", "applications", application.Id);
        await _notifications.NotifyEveryoneAsync(CurrentUserId, "Новый отклик",
            $"«{candidate.Fio}» откликнулся(ась) на вакансию «{vacancy.Title}».", NotificationTypes.Success);

        this.ToastSuccess("Отклик создан",
            $"«{candidate.Fio}» → «{vacancy.Title}». Статус отклика: «Новый».");

        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(int id)
    {
        Vacancy = await _db.Vacancies
            .Include(v => v.Company)
            .Include(v => v.VacancySkills).ThenInclude(vs => vs.Skill)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (Vacancy is null)
        {
            this.ToastError("Вакансия не найдена", "Запись могла быть удалена другим пользователем.");
            return false;
        }

        AlreadyApplied = (await _db.Applications
            .Where(a => a.VacancyId == id)
            .Select(a => a.CandidateId)
            .ToListAsync())
            .ToHashSet();

        Matches = await BuildMatchQuery(Vacancy).ToListAsync();
        return true;
    }

    /// <summary>
    /// Формирует запрос подбора кандидатов по требованиям вакансии.
    /// Незаполненные требования в отбор не включаются.
    /// </summary>
    private IQueryable<Candidate> BuildMatchQuery(Vacancy vacancy)
    {
        IQueryable<Candidate> query = _db.Candidates
            .Include(c => c.CandidateSkills).ThenInclude(cs => cs.Skill);

        // Ожидания кандидата по зарплате должны укладываться в бюджет вакансии
        if (vacancy.SalaryMax.HasValue)
            query = query.Where(c => c.Salary == null || c.Salary <= vacancy.SalaryMax);

        // Опыт кандидата должен быть не меньше требуемого
        if (!string.IsNullOrWhiteSpace(vacancy.RequiredExperience))
        {
            var required = Array.IndexOf(Reference.Experiences, vacancy.RequiredExperience);
            if (required >= 0)
            {
                var suitable = Reference.Experiences.Skip(required).ToArray();
                query = query.Where(c => c.Experience != null && suitable.Contains(c.Experience));
            }
        }

        if (vacancy.RequiredAgeMin.HasValue)
            query = query.Where(c => c.Age == null || c.Age >= vacancy.RequiredAgeMin);

        if (vacancy.RequiredAgeMax.HasValue)
            query = query.Where(c => c.Age == null || c.Age <= vacancy.RequiredAgeMax);

        if (!string.IsNullOrWhiteSpace(vacancy.RequiredGender) && vacancy.RequiredGender != "Любой")
            query = query.Where(c => c.Gender == vacancy.RequiredGender);

        if (!string.IsNullOrWhiteSpace(vacancy.WorkFormat))
            query = query.Where(c => c.WorkFormat == null || c.WorkFormat == vacancy.WorkFormat);

        // Кандидат должен обладать всеми требуемыми навыками
        foreach (var skillId in vacancy.VacancySkills.Select(vs => vs.SkillId).Distinct())
        {
            var id = skillId;
            query = query.Where(c => c.CandidateSkills.Any(cs => cs.SkillId == id));
        }

        return query.OrderBy(c => c.Salary).ThenBy(c => c.Fio);
    }
}
