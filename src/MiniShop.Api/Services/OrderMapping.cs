using MiniShop.Api.Models;

namespace MiniShop.Api.Services;

internal static class OrderMapping
{
    public static OrderResponse ToResponse(Order order) => new(
        order.Id,
        order.UserId,
        order.CreatedAt,
        order.Items.Select(x => new LineResponse(x.Id, x.ProductId, x.Name, x.PriceCents, x.Quantity)).ToList(),
        order.Items.Sum(x => (long)x.Quantity * x.PriceCents));
}
