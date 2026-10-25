namespace MiniShop.Api.Models;

public sealed class ProductComment
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public required string Body { get; set; }
}
