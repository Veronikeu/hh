using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Практика.Models;
using Практика.Services;

namespace Практика.Pages;

/// <summary>
/// Окно авторизации. Вход выполняется по логину либо по номеру телефона.
/// </summary>
public class LoginModel : PageModel
{
    private readonly AuthService _auth;
    private readonly ActionLogService _log;

    public LoginModel(AuthService auth, ActionLogService log)
    {
        _auth = auth;
        _log = log;
    }

    [BindProperty]
    [Required(ErrorMessage = "Введите логин")]
    public string Identifier { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Введите пароль")]
    public string Password { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        // Пользователь, уже выполнивший вход, сразу попадает в основную часть
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Candidates/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await _auth.FindByLoginAsync(Identifier.Trim());

        if (user is null || !AuthService.VerifyPassword(Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль");
            return Page();
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Учётная запись заблокирована. Обратитесь к администратору.");
            return Page();
        }

        await SignInAsync(user);
        await _log.WriteAsync(user.Id, "Вход в систему", "users", user.Id);

        this.ToastSuccess($"Здравствуйте, {user.Fio.Split(' ').FirstOrDefault()}!",
            $"Вы вошли как «{user.RoleName}».");

        return RedirectToPage("/Candidates/Index");
    }

    /// <summary>Создаёт cookie сессии с ролью пользователя.</summary>
    private async Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Login),
            new(ClaimTypes.Role, user.Role),
            new("Fio", user.Fio)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}
