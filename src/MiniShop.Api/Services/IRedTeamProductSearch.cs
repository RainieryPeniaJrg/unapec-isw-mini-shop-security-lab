using MiniShop.Api.Models;

namespace MiniShop.Api.Services;

public interface IRedTeamProductSearch
{
    Task<List<ProductResponse>> Search(string q, CancellationToken ct);
}
