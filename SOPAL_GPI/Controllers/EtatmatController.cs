using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPAL_GPI.Models;
using SOPAL_GPI.Services;

namespace SOPAL_GPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class EtatmatController(IEtatMatRepository etatMatRepository): ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        var list = await etatMatRepository.GetAllAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var etatmat = await etatMatRepository.GetByIdAsync(id);
        if (etatmat is null) return NotFound($"No such this ID : {id}");
        return Ok(etatmat);
    }
}
