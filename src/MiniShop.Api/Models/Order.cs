namespace MiniShop.Api.Models;

public sealed class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<OrderItem> Items { get; set; } = [];
}
