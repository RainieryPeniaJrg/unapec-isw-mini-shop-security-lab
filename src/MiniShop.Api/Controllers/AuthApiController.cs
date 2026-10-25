using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MiniShop.Api.Models;
using MiniShop.Api.Services;

namespace MiniShop.Api.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthApiController(IShopAuthenticationService authentication, LabOptions lab) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var user = (await authentication.Login(request.Username, request.Password, ct))!;
        var claims = new[]
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("unique_name", user.Username),
            new Claim("role", user.Role)
        };
        var token = new JwtSecurityToken(
            "MiniShopLab",
            "MiniShopLab",
            claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(lab.SigningKey, SecurityAlgorithms.HmacSha256));

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            user = new UserResponse(user.Id, user.Username, user.Role)
        });
    }
}
