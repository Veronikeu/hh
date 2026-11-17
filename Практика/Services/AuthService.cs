using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using Практика.Data;
using Практика.Models;

namespace Практика.Services;

/// <summary>
/// Проверка учётных данных пользователя и регистрация новых пользователей.
/// Пароль в базе хранится только в виде BCrypt-хэша.
/// </summary>
public class AuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db) => _db = db;

    /// <summary>Поиск пользователя по логину.</summary>
    public Task<User?> FindByLoginAsync(string login) =>
        _db.Users.FirstOrDefaultAsync(u => u.Login == login);

    /// <summary>Проверяет пароль по сохранённому хэшу.</summary>
    public static bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    public static string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

    /// <summary>
    /// Регистрирует нового пользователя с ролью менеджера.
    /// Телефон и email необязательны: при регистрации они не запрашиваются.
    /// </summary>
    public async Task<User> RegisterAsync(string login, string? phone, string? email, string fio, string password, string role = Roles.Manager)
    {
        var normalizedPhone = NormalizePhone(phone ?? string.Empty);

        var user = new User
        {
            Login = login.Trim(),
            Phone = normalizedPhone.Length == 0 ? null : normalizedPhone,
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Fio = fio.Trim(),
            Role = role,
            PasswordHash = HashPassword(password),
            CreatedAt = Clock.Now,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public Task<bool> LoginExistsAsync(string login, int? exceptId = null) =>
        _db.Users.AnyAsync(u => u.Login == login.Trim() && (exceptId == null || u.Id != exceptId));

    public Task<bool> PhoneExistsAsync(string phone, int? exceptId = null)
    {
        var normalized = NormalizePhone(phone);
        return _db.Users.AnyAsync(u => u.Phone == normalized && (exceptId == null || u.Id != exceptId));
    }

    public Task<bool> EmailExistsAsync(string email, int? exceptId = null) =>
        _db.Users.AnyAsync(u => u.Email == email.Trim() && (exceptId == null || u.Id != exceptId));

    /// <summary>
    /// Приводит телефон к единому виду +7XXXXXXXXXX, чтобы «8 912…»,
    /// «+7 (912) …» и «7912…» считались одним и тем же номером.
    /// </summary>
    public static string NormalizePhone(string phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length == 11 && (digits[0] == '8' || digits[0] == '7'))
            digits = "7" + digits[1..];
        else if (digits.Length == 10)
            digits = "7" + digits;

        return digits.Length == 0 ? string.Empty : "+" + digits;
    }

    /// <summary>Проверяет, что номер телефона записан в допустимом формате.</summary>
    public static bool IsValidPhone(string phone)
    {
        var normalized = NormalizePhone(phone);
        return Regex.IsMatch(normalized, @"^\+7\d{10}$");
    }

    /// <summary>Проверяет минимальные требования к паролю.</summary>
    public static bool IsValidPassword(string password, out string error)
    {
        var problems = new StringBuilder();

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            problems.Append("Пароль должен содержать не менее 6 символов. ");
        else if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            problems.Append("Пароль должен содержать буквы и цифры. ");

        error = problems.ToString().Trim();
        return error.Length == 0;
    }
}
