using Microsoft.AspNetCore.Mvc;
using Практика.Data;
using Практика.Models;
using Практика.Services;

namespace Практика.Pages.Companies;

/// <summary>Добавление и редактирование компании.</summary>
public class EditModel : AppPageModel
{
    private readonly AppDbContext _db;
    private readonly ActionLogService _log;

    public EditModel(AppDbContext db, ActionLogService log)
    {
        _db = db;
        _log = log;
    }

    [BindProperty]
    public Company Item { get; set; } = new();

    public bool IsNew => Item.Id == 0;

    public string Title => IsNew ? "Новая компания" : "Редактирование компании";

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
            return Page();

        var company = await _db.Companies.FindAsync(id);
        if (company is null)
        {
            this.ToastError("Компания не найдена", "Запись могла быть удалена другим пользователем.");
            return RedirectToPage("/Companies/Index");
        }

        Item = company;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!string.IsNullOrWhiteSpace(Item.Phone) && !AuthService.IsValidPhone(Item.Phone))
            ModelState.AddModelError("Item.Phone", "Телефон должен содержать 11 цифр, например +7 343 200-10-01");

        if (!string.IsNullOrWhiteSpace(Item.Email) &&
            !System.Text.RegularExpressions.Regex.IsMatch(Item.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            ModelState.AddModelError("Item.Email", "Некорректный адрес электронной почты");
        }

        if (!ModelState.IsValid)
            return Page();

        if (!string.IsNullOrWhiteSpace(Item.Phone))
            Item.Phone = AuthService.NormalizePhone(Item.Phone);

        if (IsNew)
        {
            _db.Companies.Add(Item);
            await _db.SaveChangesAsync();
            await _log.WriteAsync(CurrentUserId, "Добавление компании", "companies", Item.Id);
            this.ToastSuccess("Компания добавлена", $"Запись «{Item.Name}» сохранена в базе данных.");
        }
        else
        {
            var company = await _db.Companies.FindAsync(Item.Id);
            if (company is null)
            {
                this.ToastError("Компания не найдена", "Запись могла быть удалена другим пользователем.");
                return RedirectToPage("/Companies/Index");
            }

            company.Name = Item.Name;
            company.Industry = Item.Industry;
            company.City = Item.City;
            company.Phone = Item.Phone;
            company.Email = Item.Email;
            company.Website = Item.Website;

            await _db.SaveChangesAsync();
            await _log.WriteAsync(CurrentUserId, "Редактирование компании", "companies", company.Id);
            this.ToastSuccess("Изменения сохранены", $"Данные компании «{company.Name}» обновлены.");
        }

        return RedirectToPage("/Companies/Index");
    }
}
