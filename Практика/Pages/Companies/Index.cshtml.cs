using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Практика.Data;
using Практика.Models;
using Практика.Services;

namespace Практика.Pages.Companies;

/// <summary>Справочник компаний-работодателей.</summary>
public class IndexModel : AppPageModel
{
    private readonly AppDbContext _db;
    private readonly ActionLogService _log;

    public IndexModel(AppDbContext db, ActionLogService log)
    {
        _db = db;
        _log = log;
    }

    public List<Company> Items { get; set; } = new();
    public Dictionary<int, int> VacancyCounts { get; set; } = new();

    public async Task OnGetAsync()
    {
        Items = await _db.Companies.OrderBy(c => c.Name).ToListAsync();

        VacancyCounts = await _db.Vacancies
            .GroupBy(v => v.CompanyId)
            .Select(g => new { CompanyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CompanyId, x => x.Count);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!IsAdmin)
        {
            this.ToastError("Недостаточно прав", "Удалять компании может только администратор.");
            return RedirectToPage();
        }

        var company = await _db.Companies.FindAsync(id);
        if (company is null)
        {
            this.ToastError("Компания не найдена", "Возможно, запись уже удалена.");
            return RedirectToPage();
        }

        var name = company.Name;
        _db.Companies.Remove(company);
        await _db.SaveChangesAsync();

        await _log.WriteAsync(CurrentUserId, "Удаление компании", "companies", id);

        this.ToastSuccess("Компания удалена", $"Запись «{name}» удалена вместе с её вакансиями.");
        return RedirectToPage();
    }
}
