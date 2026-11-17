using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Практика.Services;

namespace Практика.Pages;

/// <summary>Выход из системы: завершение пользовательской сессии.</summary>
public class LogoutModel : AppPageModel
{
    private readonly ActionLogService _log;

    public LogoutModel(ActionLogService log) => _log = log;

    public IActionResult OnGet() => RedirectToPage("/Login");

    public async Task<IActionResult> OnPostAsync()
    {
        if (CurrentUserId > 0)
            await _log.WriteAsync(CurrentUserId, "Выход из системы", "users", CurrentUserId);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        Toast.Info(TempData, "Вы вышли из системы", "До новых встреч!");
        return RedirectToPage("/Login");
    }
}
