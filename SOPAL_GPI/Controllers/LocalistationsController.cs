using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SOPAL_GPI.Models;
using SOPAL_GPI.Data;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class LocalistationsController : ControllerBase
{
    private readonly GpiContext _context;
    public LocalistationsController(GpiContext context)
    {
        _context = context;
    }

    // GET: api/Localistation
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Localistation>>> GetLocalistation()
    {
        return await _context.Localistations.ToListAsync();
    }

    // GET: api/Localistation/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Localistation>> GetLocalistation(int id)
    {
        var localistation = await _context.Localistations.FindAsync(id);

        if (localistation == null)
        {
            return NotFound();
        }

        return localistation;
    }
}
