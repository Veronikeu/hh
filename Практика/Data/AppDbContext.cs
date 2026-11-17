using Microsoft.EntityFrameworkCore;
using Практика.Models;

namespace Практика.Data;

/// <summary>
/// Контекст базы данных. Через него выполняются все обращения к PostgreSQL.
/// EF Core формирует параметризованные SQL-запросы, что защищает от SQL-инъекций.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Vacancy> Vacancies => Set<Vacancy>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<CandidateSkill> CandidateSkills => Set<CandidateSkill>();
    public DbSet<VacancySkill> VacancySkills => Set<VacancySkill>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ActionLogEntry> ActionLog => Set<ActionLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Составные первичные ключи связующих таблиц
        modelBuilder.Entity<CandidateSkill>()
            .HasKey(cs => new { cs.CandidateId, cs.SkillId });

        modelBuilder.Entity<VacancySkill>()
            .HasKey(vs => new { vs.VacancyId, vs.SkillId });

        // Уникальные поля пользователя: по логину и телефону выполняется вход
        modelBuilder.Entity<User>().HasIndex(u => u.Login).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Phone).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<Skill>().HasIndex(s => s.Name).IsUnique();

        // Один кандидат не может дважды откликнуться на одну вакансию
        modelBuilder.Entity<Application>()
            .HasIndex(a => new { a.CandidateId, a.VacancyId }).IsUnique();

        // Все поля даты и времени хранятся как TIMESTAMP без часового пояса —
        // так они описаны в скрипте создания базы данных init.sql.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var dateProperties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?));

            foreach (var property in dateProperties)
                property.SetColumnType("timestamp without time zone");
        }

        base.OnModelCreating(modelBuilder);
    }
}
