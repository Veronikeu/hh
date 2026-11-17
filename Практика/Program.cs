using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Практика.Data;
using Практика.Models;
using Практика.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Подключение к базе данных PostgreSQL ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// ---------- Службы приложения ----------
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ActionLogService>();

// ---------- Аутентификация по cookie и разграничение прав ----------
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    // Управление пользователями и журнал действий доступны только администратору
    options.AddPolicy("ТолькоАдминистратор", policy => policy.RequireRole(Roles.Admin));
});

// ---------- Razor Pages и API ----------
builder.Services.AddRazorPages(options =>
{
    // Вход в систему обязателен для всех страниц, кроме авторизации и регистрации
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Register");
    options.Conventions.AllowAnonymousToPage("/Error");
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API системы подбора кандидатов",
        Version = "v1",
        Description = "Программный интерфейс для работы с кандидатами, вакансиями и откликами."
    });
});

var app = builder.Build();

// ---------- Конвейер обработки запросов ----------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1"));
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

// ---------- Подготовка базы данных при запуске ----------
await DbInitializer.InitializeAsync(app);

app.Run();
