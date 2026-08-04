using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPAL_GPI.DTOs;
using SOPAL_GPI.Models;
using SOPAL_GPI.Services;

namespace SOPAL_GPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MaterialsController(IMaterialRepository materialRepository, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Material>>> GetAllAsync()
    {
        return Ok(await materialRepository.GetAllAsync());
    }

    [HttpGet("search")]
    public async Task<ActionResult<PaginatedResult<Material>>> SearchAsync(
        string? q, int? typeId, int? localisationId, bool? actif, int? employeMat, string? department)
    {
        return Ok(await materialRepository.SearchAsync(q, typeId, localisationId, actif, employeMat, department));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Material>> GetByIdAsync(int id)
    {
        var material = await materialRepository.GetByIdAsync(id);
        if (material is null) return BadRequest("No such material");
        return Ok(material);
    }

    [HttpGet("by-employe/{matricule}")]
    public async Task<ActionResult<List<Material>>> GetByEmploye(int matricule, string? q)
    {
        var materials = await materialRepository.GetByEmployeMatAsync(matricule, q);
        return Ok(materials);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddAsync(MaterialDTO material)
    {
        if (material is null) return BadRequest();
        await materialRepository.AddAsync(mapper.Map<Material>(material));
        return Ok("Success");
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EditAsync(int id, MaterialDTO material)
    {
        if (material is null) return BadRequest();
        var mat = await materialRepository.GetByIdAsync(id);
        if (mat is null) return NotFound($"Le materiel avec l'ID {id} est introuvable!");
        material.Adapt(mat);
        await materialRepository.UpdateAsync(mat);
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var material = await materialRepository.GetByIdAsync(id);
        if (material == null) return NotFound();
        var matWithHist = await materialRepository.GetMatsWithHist(material);
        if (matWithHist?.Mouvements?.Count > 0) return BadRequest("Action impossible!");
        await materialRepository.DeleteAsync(material);
        return NoContent();
    }
}
