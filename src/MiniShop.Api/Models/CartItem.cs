namespace MiniShop.Api.Models;

public sealed class CartItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CartId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
