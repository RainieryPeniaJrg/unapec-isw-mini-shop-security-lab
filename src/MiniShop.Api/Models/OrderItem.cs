namespace MiniShop.Api.Models;

public sealed class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public int PriceCents { get; set; }
    public int Quantity { get; set; }
}
