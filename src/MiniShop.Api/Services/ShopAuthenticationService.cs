using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniShop.Api.Data;
using MiniShop.Api.Models;

namespace MiniShop.Api.Services;

internal sealed class ShopAuthenticationService(
    ShopDbContext db,
    LabOptions lab,
    IPasswordHasher<User> passwordHasher,
    ILogger<ShopAuthenticationService> logger) : IShopAuthenticationService
{
    public async Task<User?> Login(string username, string password, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Username == username, ct);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
        {
            logger.LogInformation("Login failed; accountExists={AccountExists}", user is not null);
            var message = lab.RedTeam
                ? user is null ? "Usuario inexistente." : "Contraseña incorrecta."
                : "Credenciales incorrectas.";
            throw new ShopException(401, message);
        }

        return user;
    }
}
