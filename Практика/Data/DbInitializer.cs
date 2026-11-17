using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Практика.Data;

/// <summary>
/// Подготовка базы данных при запуске приложения:
/// создание базы (если её нет), выполнение скриптов init.sql и seed.sql.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        var connectionString = app.Configuration.GetConnectionString("DefaultConnection")!;
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        await CreateDatabaseIfNotExistsAsync(connectionString, logger);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Структура базы данных
        var initScript = await ReadScriptAsync(app.Environment, "init.sql");
        await db.Database.ExecuteSqlRawAsync(initScript);
        logger.LogInformation("Структура базы данных проверена.");

        // Тестовые данные — только если база ещё пустая
        var hasUsers = await db.Users.AnyAsync();
        if (!hasUsers)
        {
            var seedScript = await ReadScriptAsync(app.Environment, "seed.sql");
            await db.Database.ExecuteSqlRawAsync(seedScript);
            logger.LogInformation("База данных заполнена тестовыми данными.");
        }
    }

    /// <summary>
    /// Создаёт базу данных, если она ещё не существует.
    /// Подключение при этом выполняется к служебной базе postgres.
    /// </summary>
    private static async Task CreateDatabaseIfNotExistsAsync(string connectionString, ILogger logger)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database!;

        builder.Database = "postgres";
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        await using (var check = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @name", connection))
        {
            check.Parameters.AddWithValue("name", databaseName);
            if (await check.ExecuteScalarAsync() is not null)
                return;
        }

        // Имя базы нельзя передать параметром, поэтому экранируем его кавычками
        var safeName = databaseName.Replace("\"", "\"\"");
        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{safeName}\"", connection);
        await create.ExecuteNonQueryAsync();
        logger.LogInformation("Создана база данных {Database}.", databaseName);
    }

    private static async Task<string> ReadScriptAsync(IWebHostEnvironment env, string fileName)
    {
        var path = Path.Combine(env.ContentRootPath, "SQL", fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Не найден SQL-скрипт {fileName}", path);

        return await File.ReadAllTextAsync(path);
    }
}
