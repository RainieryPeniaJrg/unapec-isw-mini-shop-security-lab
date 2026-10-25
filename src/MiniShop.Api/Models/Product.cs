namespace MiniShop.Api.Models;

public sealed class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public int PriceCents { get; set; }
    public int SellerId { get; set; }
}
