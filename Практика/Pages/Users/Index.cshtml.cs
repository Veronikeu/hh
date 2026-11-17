using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Практика.Data;
using Практика.Models;
using Практика.Services;

namespace Практика.Pages.Users;

/// <summary>
/// Управление пользователями. Раздел доступен только администратору.
/// </summary>
[Authorize(Policy = "ТолькоАдминистратор")]
public class IndexModel : AppPageModel
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    private readonly ActionLogService _log;
    private readonly NotificationService _notifications;

    public IndexModel(AppDbContext db, AuthService auth, ActionLogService log, NotificationService notifications)
    {
        _db = db;
        _auth = auth;
        _log = log;
        _notifications = notifications;
    }

    public List<User> Items { get; set; } = new();

    public async Task OnGetAsync() =>
        Items = await _db.Users.OrderBy(u => u.Fio).ToListAsync();

    /// <summary>Выдача или снятие прав администратора.</summary>
    public async Task<IActionResult> OnPostChangeRoleAsync(int id, string role)
    {
        if (role != Roles.Admin && role != Roles.Manager)
        {
            this.ToastError("Роль не изменена", "Выбрано недопустимое значение роли.");
            return RedirectToPage();
        }

        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            this.ToastError("Пользователь не найден", "Возможно, запись уже удалена.");
            return RedirectToPage();
        }

        if (user.Id == CurrentUserId && role != Roles.Admin)
        {
            this.ToastWarning("Действие отклонено", "Нельзя снять права администратора с самого себя.");
            return RedirectToPage();
        }

        user.Role = role;
        await _db.SaveChangesAsync();

        await _log.WriteAsync(CurrentUserId, "Изменение роли пользователя", "users", user.Id);
        await _notifications.NotifyAsync(user.Id, "Изменены права доступа",
            $"Ваша роль в системе теперь — «{user.RoleName}».", NotificationTypes.Info);

        this.ToastSuccess("Роль изменена", $"{user.Fio} — теперь «{user.RoleName}».");
        return RedirectToPage();
    }

    /// <summary>Удаление учётной записи.</summary>
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            this.ToastError("Пользователь не найден", "Возможно, запись уже удалена.");
            return RedirectToPage();
        }

        if (user.Id == CurrentUserId)
        {
            this.ToastWarning("Действие отклонено", "Нельзя удалить собственную учётную запись.");
            return RedirectToPage();
        }

        var fio = user.Fio;
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        await _log.WriteAsync(CurrentUserId, "Удаление пользователя", "users", id);

        this.ToastSuccess("Пользователь удалён", $"Учётная запись «{fio}» удалена.");
        return RedirectToPage();
    }

    /// <summary>Добавление пользователя администратором.</summary>
    public async Task<IActionResult> OnPostCreateAsync(string fio, string login, string phone, string email, string password, string role)
    {
        if (string.IsNullOrWhiteSpace(fio) || string.IsNullOrWhiteSpace(login) ||
            string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(email))
        {
            this.ToastError("Пользователь не создан", "Заполните все обязательные поля.");
            return RedirectToPage();
        }

        if (!AuthService.IsValidPhone(phone))
        {
            this.ToastError("Пользователь не создан", "Телефон должен содержать 11 цифр.");
            return RedirectToPage();
        }

        if (!AuthService.IsValidPassword(password ?? "", out var passwordError))
        {
            this.ToastError("Пользователь не создан", passwordError);
            return RedirectToPage();
        }

        if (await _auth.LoginExistsAsync(login))
        {
            this.ToastError("Пользователь не создан", "Такой логин уже занят.");
            return RedirectToPage();
        }

        if (await _auth.PhoneExistsAsync(phone))
        {
            this.ToastError("Пользователь не создан", "Такой номер телефона уже используется.");
            return RedirectToPage();
        }

        if (await _auth.EmailExistsAsync(email))
        {
            this.ToastError("Пользователь не создан", "Такой email уже используется.");
            return RedirectToPage();
        }

        var created = await _auth.RegisterAsync(login, phone, email, fio, password!,
            role == Roles.Admin ? Roles.Admin : Roles.Manager);

        await _log.WriteAsync(CurrentUserId, "Добавление пользователя", "users", created.Id);

        this.ToastSuccess("Пользователь создан", $"{created.Fio} — «{created.RoleName}».");
        return RedirectToPage();
    }
}
