using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Практика.Data;
using Практика.Models;
using Практика.Services;

namespace Практика.Pages.Vacancies;

/// <summary>Добавление и редактирование вакансии.</summary>
public class EditModel : AppPageModel
{
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
    public Vacancy Item { get; set; } = new();

    [BindProperty]
    public List<int> SelectedSkillIds { get; set; } = new();

    public List<Skill> AllSkills { get; set; } = new();
    public List<Company> Companies { get; set; } = new();

    public bool IsNew => Item.Id == 0;

    public string Title => IsNew ? "Новая вакансия" : "Редактирование вакансии";

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        await LoadListsAsync();

        if (id is null)
            return Page();

        var vacancy = await _db.Vacancies
            .Include(v => v.VacancySkills)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vacancy is null)
        {
            this.ToastError("Вакансия не найдена", "Запись могла быть удалена другим пользователем.");
            return RedirectToPage("/Vacancies/Index");
        }

        Item = vacancy;
        SelectedSkillIds = vacancy.VacancySkills.Select(vs => vs.SkillId).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadListsAsync();

        // Проверки, которые нельзя описать атрибутами модели
        if (Item.SalaryMin.HasValue && Item.SalaryMax.HasValue && Item.SalaryMin > Item.SalaryMax)
            ModelState.AddModelError("Item.SalaryMax", "Максимальная зарплата не может быть меньше минимальной");

        if (Item.RequiredAgeMin.HasValue && Item.RequiredAgeMax.HasValue && Item.RequiredAgeMin > Item.RequiredAgeMax)
            ModelState.AddModelError("Item.RequiredAgeMax", "Верхняя граница возраста не может быть меньше нижней");

        if (!await _db.Companies.AnyAsync(c => c.Id == Item.CompanyId))
            ModelState.AddModelError("Item.CompanyId", "Выберите компанию из списка");

        if (!ModelState.IsValid)
            return Page();

        return IsNew ? await CreateAsync() : await UpdateAsync();
    }

    private async Task<IActionResult> CreateAsync()
    {
        Item.CreatedAt = Clock.Now;
        _db.Vacancies.Add(Item);
        await _db.SaveChangesAsync();

        SaveSkills(Item.Id);
        await _db.SaveChangesAsync();

        await _log.WriteAsync(CurrentUserId, "Добавление вакансии", "vacancies", Item.Id);
        await _notifications.NotifyEveryoneAsync(CurrentUserId, "Открыта новая вакансия",
            $"{CurrentUserFio} добавил(а) вакансию «{Item.Title}».", NotificationTypes.Info);

        this.ToastSuccess("Вакансия добавлена", $"Запись «{Item.Title}» сохранена в базе данных.");
        return RedirectToPage("/Vacancies/Index");
    }

    private async Task<IActionResult> UpdateAsync()
    {
        var vacancy = await _db.Vacancies
            .Include(v => v.VacancySkills)
            .FirstOrDefaultAsync(v => v.Id == Item.Id);

        if (vacancy is null)
        {
            this.ToastError("Вакансия не найдена", "Запись могла быть удалена другим пользователем.");
            return RedirectToPage("/Vacancies/Index");
        }

        var statusChanged = vacancy.Status != Item.Status;

        vacancy.CompanyId = Item.CompanyId;
        vacancy.Title = Item.Title;
        vacancy.Description = Item.Description;
        vacancy.SalaryMin = Item.SalaryMin;
        vacancy.SalaryMax = Item.SalaryMax;
        vacancy.WorkFormat = Item.WorkFormat;
        vacancy.RequiredExperience = Item.RequiredExperience;
        vacancy.RequiredAgeMin = Item.RequiredAgeMin;
        vacancy.RequiredAgeMax = Item.RequiredAgeMax;
        vacancy.RequiredGender = Item.RequiredGender;
        vacancy.Status = Item.Status;

        _db.VacancySkills.RemoveRange(vacancy.VacancySkills);
        await _db.SaveChangesAsync();

        SaveSkills(vacancy.Id);
        await _db.SaveChangesAsync();

        await _log.WriteAsync(CurrentUserId, "Редактирование вакансии", "vacancies", vacancy.Id);

        if (statusChanged)
        {
            await _notifications.NotifyEveryoneAsync(CurrentUserId, "Изменён статус вакансии",
                $"Вакансия «{vacancy.Title}» теперь имеет статус «{vacancy.StatusName}».", NotificationTypes.Info);
        }

        this.ToastSuccess("Изменения сохранены", $"Вакансия «{vacancy.Title}» обновлена.");
        return RedirectToPage("/Vacancies/Index");
    }

    private void SaveSkills(int vacancyId)
    {
        foreach (var skillId in SelectedSkillIds.Distinct())
            _db.VacancySkills.Add(new VacancySkill { VacancyId = vacancyId, SkillId = skillId });
    }

    private async Task LoadListsAsync()
    {
        AllSkills = await _db.Skills.OrderBy(s => s.Name).ToListAsync();
        Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
    }
}
