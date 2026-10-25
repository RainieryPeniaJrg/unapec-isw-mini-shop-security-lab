using System.ComponentModel.DataAnnotations;
namespace MiniShop.Api.Models;
/// <summary>Credentials for a synthetic laboratory account.</summary>
public sealed record LoginRequest([Required] string Username, [Required] string Password);
/// <summary>Product fields accepted from a seller.</summary>
public sealed record ProductRequest([Required, MaxLength(120)] string Name, [MaxLength(2000)] string? Description, [Range(1, 100000000)] int PriceCents);
/// <summary>Public product data.</summary>
public sealed record ProductResponse(int Id, string Name, string Description, int PriceCents, int SellerId);
/// <summary>A persisted product comment.</summary>
public sealed record CommentResponse(int Id, int ProductId, int UserId, string Username, string Body);
/// <summary>Comment content; intentionally rendered as HTML in Red Team only.</summary>
public sealed record CommentRequest([Required, MaxLength(4000)] string Body);
/// <summary>A cart line addition.</summary>
public sealed record AddItemRequest(int ProductId, [Range(1, 99)] int Quantity);
/// <summary>A laboratory quantity update.</summary>
public sealed record QuantityRequest([Range(1, 99)] int Quantity);
/// <summary>Cart to convert into an order.</summary>
public sealed record CheckoutRequest(int CartId);
/// <summary>Cart or frozen order line.</summary>
public sealed record LineResponse(int Id, int ProductId, string Name, int PriceCents, int Quantity);
/// <summary>Cart with owner and calculated total.</summary>
public sealed record CartResponse(int Id, int UserId, List<LineResponse> Items, long TotalCents);
/// <summary>Order snapshot with original prices.</summary>
public sealed record OrderResponse(int Id, int UserId, DateTimeOffset CreatedAt, List<LineResponse> Items, long TotalCents);
/// <summary>Administrative synthetic user identity.</summary>
public sealed record UserResponse(int Id, string Username, string Role);
/// <summary>Read-only administrative dashboard.</summary>
public sealed record AdminResponse(List<UserResponse> Users, List<ProductResponse> Products, List<OrderResponse> Orders);
