using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPAL_GPI.Models;
using SOPAL_GPI.Services;
using MapsterMapper;
using SOPAL_GPI.DTOs;
using Mapster;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class EmployesController(IEmployeRepository employeRepository, IMapper mapper): ControllerBase
{
    // GET: api/Employe
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employe>>> GetEmploye()
    {
        return Ok( await employeRepository.GetAllAsync());
    }

    // GET: api/Employe/search
    [HttpGet("search")]
    public async Task<ActionResult<PaginatedResult<EmployeListItemDto>>> SearchEmploye(string? q, string? service)
    {
        return Ok(await employeRepository.SearchAsync(q, service));
    }

    // GET: api/Employe/{matricule}
    [HttpGet("{matricule}")]
    public async Task<ActionResult<Employe>> GetEmploye(int matricule)
    {
        var employe = await employeRepository.GetByIdAsync(matricule);

        if (employe == null)
            return NotFound();

        return Ok(mapper.Map<EmployeDTO>(employe));
    }

    // PUT: api/Employe/5
    [HttpPut("{matricule}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PutEmploye(int matricule, EmployeEditDTO employe)
    {
        var emp = await employeRepository.GetByIdAsync(matricule);
        if (emp == null) return NotFound();
        await employeRepository.UpdateAsync(employe.Adapt(emp));
        return NoContent();
    }

    // POST: api/Employe
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Employe>> AddEmploye(EmployeDTO employe)
    {
        if (employe == null)
            return BadRequest();
        var entity = employe.Adapt<Employe>();
        entity.Matricule = await employeRepository.GenerateMatAsync();
        await employeRepository.AddAsync(entity);
        return CreatedAtAction("GetEmploye", new { matricule = entity.Matricule }, mapper.Map<EmployeDTO>(entity));
    }

}
