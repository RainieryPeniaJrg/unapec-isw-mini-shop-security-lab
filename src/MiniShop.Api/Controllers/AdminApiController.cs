using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniShop.Api.Services;

namespace MiniShop.Api.Controllers;

[ApiController, Authorize, Route("api/admin")]
public sealed class AdminApiController(IAdminService administration, LabOptions lab) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        // F1-12 cookie policy gap is distinct from F1-14 weak JWT signing.
        var cookieBypass = lab.RedTeam
            && !Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            && User.IsInRole("Cliente");

        if (!User.IsInRole("Admin") && !cookieBypass)
        {
            return Forbid();
        }

        return Ok(await administration.Admin(ct));
    }
}
