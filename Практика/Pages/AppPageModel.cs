using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Практика.Models;

namespace Практика.Pages;

/// <summary>
/// Базовый класс страниц, доступных после авторизации.
/// Даёт быстрый доступ к данным текущего пользователя.
/// </summary>
public abstract class AppPageModel : PageModel
{
    /// <summary>Идентификатор вошедшего пользователя.</summary>
    public int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    /// <summary>ФИО вошедшего пользователя.</summary>
    public string CurrentUserFio => User.FindFirst("Fio")?.Value ?? string.Empty;

    /// <summary>Администратору доступны все функции системы.</summary>
    public bool IsAdmin => User.IsInRole(Roles.Admin);
}
