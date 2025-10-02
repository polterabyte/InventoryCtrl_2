using Inventory.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<HealthStatusDto>> Get()
    {
        var healthStatus = new HealthStatusDto();
        return Ok(ApiResponse<HealthStatusDto>.CreateSuccess(healthStatus, "Health check successful."));
    }
}
