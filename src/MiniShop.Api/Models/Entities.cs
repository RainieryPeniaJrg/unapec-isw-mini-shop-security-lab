namespace MiniShop.Api.Models;

public sealed class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }
}

public sealed class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public int PriceCents { get; set; }
    public int SellerId { get; set; }
}

public sealed class CartItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public sealed class ProductComment
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public required string Body { get; set; }
}
