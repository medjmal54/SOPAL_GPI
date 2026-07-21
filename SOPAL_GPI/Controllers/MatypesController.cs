using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPAL_GPI.Services;

namespace SOPAL_GPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MatypesController(IMatypeRepository matypeRepository): ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await matypeRepository.GetAllAsync());
    }
}
