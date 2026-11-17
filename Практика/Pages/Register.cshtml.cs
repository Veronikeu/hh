using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Практика.Models;
using Практика.Services;

namespace Практика.Pages;

/// <summary>
/// Регистрация нового пользователя.
/// Новая учётная запись всегда создаётся с ролью менеджера:
/// права администратора выдаёт только действующий администратор.
/// </summary>
public class RegisterModel : PageModel
{
    private readonly AuthService _auth;
    private readonly ActionLogService _log;

    public RegisterModel(AuthService auth, ActionLogService log)
    {
        _auth = auth;
        _log = log;
    }

    [BindProperty]
    [Required(ErrorMessage = "Укажите ФИО")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "ФИО должно содержать от 3 до 150 символов")]
    public string Fio { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Укажите логин")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Логин должен содержать от 3 до 50 символов")]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Логин может содержать только латинские буквы, цифры, точку, дефис и подчёркивание")]
    public string Login { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Введите пароль")]
    public string Password { get; set; } = string.Empty;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        // Проверка пароля
        if (!AuthService.IsValidPassword(Password, out var passwordError))
            ModelState.AddModelError(nameof(Password), passwordError);

        // Проверка уникальности
        if (await _auth.LoginExistsAsync(Login))
            ModelState.AddModelError(nameof(Login), "Пользователь с таким логином уже зарегистрирован");

        if (!ModelState.IsValid)
            return Page();

        var user = await _auth.RegisterAsync(Login, null, null, Fio, Password, Roles.Manager);
        await _log.WriteAsync(user.Id, "Регистрация пользователя", "users", user.Id);

        Toast.Success(TempData, "Учётная запись создана",
            $"Теперь войдите под логином «{user.Login}».");

        return RedirectToPage("/Login");
    }
}
