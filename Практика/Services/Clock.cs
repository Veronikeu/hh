namespace Практика.Services;

/// <summary>
/// Текущее время для полей типа TIMESTAMP.
/// Вид даты приводится к Unspecified: PostgreSQL хранит такие поля
/// без часового пояса, и Npgsql не принимает значения с Kind = Local.
/// </summary>
public static class Clock
{
    public static DateTime Now => DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
}
