using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    [Authorize]
    [HttpGet("info")]
    public IActionResult Info()
    {
        var username = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray();
        return Ok(new
        {
            Username = username,
            Roles = roles
        });
    }
}
