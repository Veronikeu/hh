using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Практика.Data;
using Практика.Models;
using Практика.Services;

namespace Практика.Pages.Candidates;

/// <summary>Добавление и редактирование карточки кандидата.</summary>
public class EditModel : AppPageModel
{
    private const int EducationRows = 3;

    private readonly AppDbContext _db;
    private readonly ActionLogService _log;
    private readonly NotificationService _notifications;

    public EditModel(AppDbContext db, ActionLogService log, NotificationService notifications)
    {
        _db = db;
        _log = log;
        _notifications = notifications;
    }

    [BindProperty]
    public Candidate Item { get; set; } = new();

    [BindProperty]
    public List<int> SelectedSkillIds { get; set; } = new();

    [BindProperty]
    public List<EducationInput> Educations { get; set; } = new();

    public List<Skill> AllSkills { get; set; } = new();

    public bool IsNew => Item.Id == 0;

    public string Title => IsNew ? "Новый кандидат" : "Редактирование кандидата";

    /// <summary>Одна строка формы «Образование».</summary>
    public class EducationInput
    {
        public string? Institution { get; set; }
        public string? Specialty { get; set; }
        public string? Degree { get; set; }
        public int? YearEnd { get; set; }

        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(Institution) &&
            string.IsNullOrWhiteSpace(Specialty) &&
            string.IsNullOrWhiteSpace(Degree) &&
            YearEnd is null;
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        await LoadSkillsAsync();

        if (id is null)
        {
            FillEmptyEducationRows();
            return Page();
        }

        var candidate = await _db.Candidates
            .Include(c => c.CandidateSkills)
            .Include(c => c.Educations)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (candidate is null)
        {
            this.ToastError("Кандидат не найден", "Запись могла быть удалена другим пользователем.");
            return RedirectToPage("/Candidates/Index");
        }

        Item = candidate;
        SelectedSkillIds = candidate.CandidateSkills.Select(cs => cs.SkillId).ToList();
        Educations = candidate.Educations
            .Select(e => new EducationInput
            {
                Institution = e.Institution,
                Specialty = e.Specialty,
                Degree = e.Degree,
                YearEnd = e.YearEnd
            })
            .ToList();

        FillEmptyEducationRows();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadSkillsAsync();

        // Дополнительные проверки, которые нельзя задать атрибутами
        if (Item.Salary is null)
            ModelState.AddModelError("Item.Salary", "Укажите ожидаемую зарплату");

        if (!string.IsNullOrWhiteSpace(Item.Phone) && !AuthService.IsValidPhone(Item.Phone))
            ModelState.AddModelError("Item.Phone", "Телефон должен содержать 11 цифр, например +7 900 123-45-67");

        foreach (var (education, index) in Educations.Select((e, i) => (e, i)))
        {
            if (education.IsEmpty)
                continue;

            if (string.IsNullOrWhiteSpace(education.Institution))
                ModelState.AddModelError($"Educations[{index}].Institution", "Укажите учебное заведение или очистите строку образования");

            if (education.YearEnd is < 1950 or > 2100)
                ModelState.AddModelError($"Educations[{index}].YearEnd", "Год окончания указан некорректно");
        }

        if (!ModelState.IsValid)
        {
            FillEmptyEducationRows();
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(Item.Phone))
            Item.Phone = AuthService.NormalizePhone(Item.Phone);

        return IsNew ? await CreateAsync() : await UpdateAsync();
    }

    private async Task<IActionResult> CreateAsync()
    {
        Item.CreatedAt = Clock.Now;
        Item.UpdatedAt = Clock.Now;
        _db.Candidates.Add(Item);
        await _db.SaveChangesAsync();

        await SaveSkillsAsync(Item.Id);
        await SaveEducationsAsync(Item.Id);
        await _db.SaveChangesAsync();

        await _log.WriteAsync(CurrentUserId, "Добавление кандидата", "candidates", Item.Id);
        await _notifications.NotifyEveryoneAsync(CurrentUserId, "Новый кандидат в базе",
            $"{CurrentUserFio} добавил(а) кандидата «{Item.Fio}».", NotificationTypes.Info);

        this.ToastSuccess("Кандидат добавлен", $"Карточка «{Item.Fio}» сохранена в базе данных.");
        return RedirectToPage("/Candidates/Index");
    }

    private async Task<IActionResult> UpdateAsync()
    {
        var candidate = await _db.Candidates
            .Include(c => c.CandidateSkills)
            .Include(c => c.Educations)
            .FirstOrDefaultAsync(c => c.Id == Item.Id);

        if (candidate is null)
        {
            this.ToastError("Кандидат не найден", "Запись могла быть удалена другим пользователем.");
            return RedirectToPage("/Candidates/Index");
        }

        candidate.Fio = Item.Fio;
        candidate.Age = Item.Age;
        candidate.Gender = Item.Gender;
        candidate.Phone = Item.Phone;
        candidate.Email = Item.Email;
        candidate.City = Item.City;
        candidate.PcLevel = Item.PcLevel;
        candidate.Language = Item.Language;
        candidate.TeamManagement = Item.TeamManagement;
        candidate.WorkFormat = Item.WorkFormat;
        candidate.Experience = Item.Experience;
        candidate.Salary = Item.Salary;
        candidate.UpdatedAt = Clock.Now;

        _db.CandidateSkills.RemoveRange(candidate.CandidateSkills);
        _db.Educations.RemoveRange(candidate.Educations);
        await _db.SaveChangesAsync();

        await SaveSkillsAsync(candidate.Id);
        await SaveEducationsAsync(candidate.Id);
        await _db.SaveChangesAsync();

        await _log.WriteAsync(CurrentUserId, "Редактирование кандидата", "candidates", candidate.Id);

        this.ToastSuccess("Изменения сохранены", $"Карточка «{candidate.Fio}» обновлена.");
        return RedirectToPage("/Candidates/Index");
    }

    private Task SaveSkillsAsync(int candidateId)
    {
        foreach (var skillId in SelectedSkillIds.Distinct())
            _db.CandidateSkills.Add(new CandidateSkill { CandidateId = candidateId, SkillId = skillId });

        return Task.CompletedTask;
    }

    private Task SaveEducationsAsync(int candidateId)
    {
        foreach (var education in Educations.Where(e => !e.IsEmpty))
        {
            _db.Educations.Add(new Education
            {
                CandidateId = candidateId,
                Institution = education.Institution,
                Specialty = education.Specialty,
                Degree = education.Degree,
                YearEnd = education.YearEnd
            });
        }

        return Task.CompletedTask;
    }

    private async Task LoadSkillsAsync() =>
        AllSkills = await _db.Skills.OrderBy(s => s.Name).ToListAsync();

    private void FillEmptyEducationRows()
    {
        while (Educations.Count < EducationRows)
            Educations.Add(new EducationInput());
    }
}
