using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SOPAL_GPI.Models;
using SOPAL_GPI.Services;

namespace SOPAL_GPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TypMovController(ITypMovRepository typMov): ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<TypMov>>> GetAll()
        {
            return Ok(await typMov.GetAllAsync());
        }

        [HttpPost("{id}")]
        public async Task<ActionResult<TypMov>> GetByIdAsync(int id)
        {
            var typmov = await typMov.GetByIdAsync(id);
            if (typmov == null) return NotFound();
            return Ok(typmov);
        }
    }
}
