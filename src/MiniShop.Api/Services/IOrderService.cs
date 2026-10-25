using System.Security.Claims;
using MiniShop.Api.Models;

namespace MiniShop.Api.Services;

public interface IOrderService
{
    Task<OrderResponse> Checkout(int cartId, ClaimsPrincipal actor, CancellationToken ct);
    Task<OrderResponse> Order(int id, ClaimsPrincipal actor, CancellationToken ct);
    Task<List<OrderResponse>> MyOrders(ClaimsPrincipal actor, CancellationToken ct);
    Task<OrderResponse> ChangeOrder(int id, int quantity, ClaimsPrincipal actor, CancellationToken ct);
}
