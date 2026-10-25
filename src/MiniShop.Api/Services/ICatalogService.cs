using System.Security.Claims;
using MiniShop.Api.Models;

namespace MiniShop.Api.Services;

public interface ICatalogService
{
    Task<List<ProductResponse>> Products(string? q, CancellationToken ct);
    Task<ProductResponse> Product(int id, CancellationToken ct);
    Task<int> SaveProduct(int? id, ProductRequest request, ClaimsPrincipal actor, CancellationToken ct);
    Task DeleteProduct(int id, ClaimsPrincipal actor, CancellationToken ct);
}
