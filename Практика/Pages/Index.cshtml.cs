using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Практика.Pages;

/// <summary>
/// Стартовый адрес приложения.
/// Неавторизованного пользователя отправляет на экран входа,
/// авторизованного — в основную часть приложения.
/// </summary>
public class IndexModel : PageModel
{
    public IActionResult OnGet() =>
        User.Identity?.IsAuthenticated == true
            ? RedirectToPage("/Candidates/Index")
            : RedirectToPage("/Login");
}
