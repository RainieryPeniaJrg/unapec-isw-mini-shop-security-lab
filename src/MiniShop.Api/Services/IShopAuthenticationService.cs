using MiniShop.Api.Models;

namespace MiniShop.Api.Services;

public interface IShopAuthenticationService
{
    Task<User?> Login(string username, string password, CancellationToken ct);
}
