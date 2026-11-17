using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Практика.Data;
using Практика.Models;

namespace Практика.Controllers;

/// <summary>Программный интерфейс для работы с вакансиями.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VacanciesController : ControllerBase
{
    private readonly AppDbContext _db;

    public VacanciesController(AppDbContext db) => _db = db;

    /// <summary>Список вакансий.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(string? status = null, int? companyId = null)
    {
        IQueryable<Vacancy> query = _db.Vacancies
            .Include(v => v.Company)
            .Include(v => v.VacancySkills).ThenInclude(vs => vs.Skill);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(v => v.Status == status);

        if (companyId is > 0)
            query = query.Where(v => v.CompanyId == companyId);

        var items = await query
            .OrderBy(v => v.Title)
            .Select(v => new
            {
                v.Id,
                v.Title,
                Company = v.Company!.Name,
                v.SalaryMin,
                v.SalaryMax,
                v.WorkFormat,
                v.RequiredExperience,
                v.Status,
                Skills = v.VacancySkills.Select(vs => vs.Skill!.Name)
            })
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Подбор кандидатов по требованиям вакансии.</summary>
    [HttpGet("{id:int}/matches")]
    public async Task<IActionResult> GetMatches(int id)
    {
        var vacancy = await _db.Vacancies
            .Include(v => v.VacancySkills)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vacancy is null)
            return NotFound(new { message = "Вакансия не найдена" });

        IQueryable<Candidate> query = _db.Candidates;

        if (vacancy.SalaryMax.HasValue)
            query = query.Where(c => c.Salary == null || c.Salary <= vacancy.SalaryMax);

        if (vacancy.RequiredAgeMin.HasValue)
            query = query.Where(c => c.Age == null || c.Age >= vacancy.RequiredAgeMin);

        if (vacancy.RequiredAgeMax.HasValue)
            query = query.Where(c => c.Age == null || c.Age <= vacancy.RequiredAgeMax);

        if (!string.IsNullOrWhiteSpace(vacancy.RequiredGender) && vacancy.RequiredGender != "Любой")
            query = query.Where(c => c.Gender == vacancy.RequiredGender);

        foreach (var skillId in vacancy.VacancySkills.Select(vs => vs.SkillId))
        {
            var skill = skillId;
            query = query.Where(c => c.CandidateSkills.Any(cs => cs.SkillId == skill));
        }

        var items = await query
            .OrderBy(c => c.Salary)
            .Select(c => new { c.Id, c.Fio, c.Age, c.City, c.Experience, c.Salary, c.Phone, c.Email })
            .ToListAsync();

        return Ok(items);
    }
}
