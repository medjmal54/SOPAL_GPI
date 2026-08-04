using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPAL_GPI.DTOs;
using SOPAL_GPI.Services;

namespace SOPAL_GPI.Controllers;

[Route("api/dashboard")]
[ApiController]
[Authorize]
public class DashboardController(IDashboardRepository dashboardRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int period = 30, string department = "all", string? types = null)
    {
        var typeIds = string.IsNullOrWhiteSpace(types)
            ? new List<int>()
            : types.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .ToList();

        var data = await dashboardRepository.GetDashboardAsync(new DashboardFilter
        {
            Period = period,
            Department = department,
            TypeIds = typeIds
        });

        return Ok(data);
    }
}
