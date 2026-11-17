namespace Практика.Models;

/// <summary>
/// Списки допустимых значений для выпадающих списков интерфейса.
/// Единый источник значений для форм кандидата, вакансии и поиска.
/// </summary>
public static class Reference
{
    public static readonly string[] Genders = { "М", "Ж" };

    public static readonly string[] GendersWithAny = { "Любой", "М", "Ж" };

    public static readonly string[] Experiences =
    {
        "до 1 года", "1-3 года", "3-6 лет", "более 6 лет"
    };

    public static readonly string[] WorkFormats = { "Офис", "Удалённо", "Гибрид" };

    public static readonly string[] PcLevels = { "Базовый", "Уверенный", "Продвинутый" };

    public static readonly string[] Languages =
    {
        "Нет", "Английский", "Немецкий", "Французский", "Китайский"
    };

    public static readonly string[] YesNo = { "Да", "Нет" };

    public static readonly string[] Degrees =
    {
        "Среднее", "Среднее профессиональное", "Высшее", "Магистратура", "Аспирантура"
    };

    public static readonly Dictionary<string, string> VacancyStatuses = new()
    {
        ["active"] = "Открыта",
        ["closed"] = "Закрыта"
    };
}
