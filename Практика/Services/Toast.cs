using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Text.Json;

namespace Практика.Services;

/// <summary>Одно всплывающее уведомление.</summary>
public record ToastMessage(string Type, string Title, string Text);

/// <summary>
/// Всплывающие уведомления, которые показываются пользователю
/// сразу после выполненного действия.
/// Сообщения передаются на следующую страницу через TempData.
/// </summary>
public static class Toast
{
    private const string Key = "__toasts";

    public static void Success(ITempDataDictionary tempData, string title, string text = "") =>
        Add(tempData, "success", title, text);

    public static void Info(ITempDataDictionary tempData, string title, string text = "") =>
        Add(tempData, "info", title, text);

    public static void Warning(ITempDataDictionary tempData, string title, string text = "") =>
        Add(tempData, "warning", title, text);

    public static void Error(ITempDataDictionary tempData, string title, string text = "") =>
        Add(tempData, "error", title, text);

    public static void Add(ITempDataDictionary tempData, string type, string title, string text)
    {
        var list = Read(tempData);
        list.Add(new ToastMessage(type, title, text));
        tempData[Key] = JsonSerializer.Serialize(list);
    }

    /// <summary>Забирает накопленные сообщения и очищает их.</summary>
    public static List<ToastMessage> Take(ITempDataDictionary tempData)
    {
        var list = Read(tempData);
        tempData.Remove(Key);
        return list;
    }

    private static List<ToastMessage> Read(ITempDataDictionary tempData)
    {
        if (tempData.TryGetValue(Key, out var value) && value is string json && json.Length > 0)
            return JsonSerializer.Deserialize<List<ToastMessage>>(json) ?? new List<ToastMessage>();

        return new List<ToastMessage>();
    }
}

/// <summary>Короткие методы для страниц Razor Pages.</summary>
public static class PageModelToastExtensions
{
    public static void ToastSuccess(this PageModel page, string title, string text = "") =>
        Toast.Success(page.TempData, title, text);

    public static void ToastInfo(this PageModel page, string title, string text = "") =>
        Toast.Info(page.TempData, title, text);

    public static void ToastWarning(this PageModel page, string title, string text = "") =>
        Toast.Warning(page.TempData, title, text);

    public static void ToastError(this PageModel page, string title, string text = "") =>
        Toast.Error(page.TempData, title, text);
}
