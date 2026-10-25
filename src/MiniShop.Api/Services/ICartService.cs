using System.Security.Claims;
using MiniShop.Api.Models;

namespace MiniShop.Api.Services;

public interface ICartService
{
    Task<CartResponse> Cart(int? id, ClaimsPrincipal actor, CancellationToken ct);
    Task ChangeItem(int cartId, int? itemId, int productId, int quantity, bool delete, ClaimsPrincipal actor, CancellationToken ct);
}
