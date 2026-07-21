using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPAL_GPI.DTOs;
using SOPAL_GPI.Models;
using SOPAL_GPI.Services;
using System.Security.Claims;

namespace SOPAL_GPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class InterventionController(IinterventionRepositroy interventionRepositroy, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Intervention>>> GetAll() => Ok(await interventionRepositroy.GetAllAsync());
    

    [HttpGet("{id}")]
    public async Task<ActionResult<Intervention>> GetByIdAsync(int id)
    {
        var interv = await interventionRepositroy.GetByIdAsync(id);
        if (interv is null) return NotFound();
        return Ok(interv);
    }

    [HttpGet("by-mouvement/{mouvementId}")]
    public async Task<ActionResult<List<Intervention>>> GetByMouvement(int mouvementId)
    {
        return Ok(await interventionRepositroy.GetByMouvementIdAsync(mouvementId));
    }

    [HttpGet("by-admin/{matricule}")]
    public async Task<ActionResult<List<Intervention>>> GetByAdmin(int matricule)
    {
        return Ok(await interventionRepositroy.GetUserInterventionsAsync(matricule));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Intervention>> AddIntervAsync(InterventionDTO interv)
    {
        var entity = mapper.Map<Intervention>(interv);
        var matricule = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        entity.IntervenantId = matricule;
        await interventionRepositroy.AddAsync(entity);
        return CreatedAtAction("GetByIdAsync", new { id = entity.Id }, interv);
    }

    [HttpPost("batch")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> AddBatchAsync(BatchInterventionDTO batch)
    {
        var matricule = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var entities = batch.Items.Select(item => new Intervention
        {
            Commentaires = batch.Commentaires,
            Date = batch.Date,
            MatId = item.MatId,
            MaintId = item.MaintId,
            IntervenantId = matricule
        }).ToList();
        await interventionRepositroy.AddRangeAsync(entities);
        return Ok(new { message = $"{entities.Count} intervention(s) créée(s).", count = entities.Count });
    }
}
