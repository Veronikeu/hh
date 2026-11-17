using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Практика.Data;
using Практика.Models;
using Практика.Services;

namespace Практика.Controllers;

/// <summary>
/// Программный интерфейс для работы с кандидатами.
/// Все запросы к базе данных выполняются через EF Core
/// и формируются как параметризованные SQL-запросы.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CandidatesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ActionLogService _log;

    public CandidatesController(AppDbContext db, ActionLogService log)
    {
        _db = db;
        _log = log;
    }

    /// <summary>Список кандидатов с необязательными параметрами поиска.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        string? fio = null,
        string? city = null,
        int? ageMin = null,
        int? ageMax = null,
        int? salaryMax = null,
        string? experience = null,
        string? workFormat = null)
    {
        IQueryable<Candidate> query = _db.Candidates
            .Include(c => c.CandidateSkills).ThenInclude(cs => cs.Skill);

        if (!string.IsNullOrWhiteSpace(fio))
            query = query.Where(c => EF.Functions.ILike(c.Fio, $"%{fio}%"));

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(c => c.City == city);

        if (ageMin.HasValue)
            query = query.Where(c => c.Age >= ageMin);

        if (ageMax.HasValue)
            query = query.Where(c => c.Age <= ageMax);

        if (salaryMax.HasValue)
            query = query.Where(c => c.Salary <= salaryMax);

        if (!string.IsNullOrWhiteSpace(experience))
            query = query.Where(c => c.Experience == experience);

        if (!string.IsNullOrWhiteSpace(workFormat))
            query = query.Where(c => c.WorkFormat == workFormat);

        var items = await query
            .OrderBy(c => c.Fio)
            .Select(c => new
            {
                c.Id,
                c.Fio,
                c.Age,
                c.Gender,
                c.City,
                c.Phone,
                c.Email,
                c.Experience,
                c.WorkFormat,
                c.Salary,
                Skills = c.CandidateSkills.Select(cs => cs.Skill!.Name)
            })
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Карточка одного кандидата.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var candidate = await _db.Candidates
            .Include(c => c.CandidateSkills).ThenInclude(cs => cs.Skill)
            .Include(c => c.Educations)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (candidate is null)
            return NotFound(new { message = "Кандидат не найден" });

        return Ok(new
        {
            candidate.Id,
            candidate.Fio,
            candidate.Age,
            candidate.Gender,
            candidate.City,
            candidate.Phone,
            candidate.Email,
            candidate.PcLevel,
            candidate.Language,
            candidate.TeamManagement,
            candidate.WorkFormat,
            candidate.Experience,
            candidate.Salary,
            Skills = candidate.CandidateSkills.Select(cs => cs.Skill!.Name),
            Educations = candidate.Educations.Select(e => new { e.Institution, e.Specialty, e.Degree, e.YearEnd })
        });
    }

    /// <summary>Добавление кандидата.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Candidate candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate.Fio))
            return BadRequest(new { message = "Не указано ФИО кандидата" });

        candidate.Id = 0;
        candidate.CreatedAt = Clock.Now;
        candidate.UpdatedAt = Clock.Now;

        _db.Candidates.Add(candidate);
        await _db.SaveChangesAsync();
        await _log.WriteAsync(null, "Добавление кандидата (API)", "candidates", candidate.Id);

        return CreatedAtAction(nameof(GetById), new { id = candidate.Id }, new { candidate.Id });
    }

    /// <summary>Удаление кандидата.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ТолькоАдминистратор")]
    public async Task<IActionResult> Delete(int id)
    {
        var candidate = await _db.Candidates.FindAsync(id);
        if (candidate is null)
            return NotFound(new { message = "Кандидат не найден" });

        _db.Candidates.Remove(candidate);
        await _db.SaveChangesAsync();
        await _log.WriteAsync(null, "Удаление кандидата (API)", "candidates", id);

        return NoContent();
    }
}
