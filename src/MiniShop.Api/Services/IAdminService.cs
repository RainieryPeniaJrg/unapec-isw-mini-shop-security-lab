using MiniShop.Api.Models;

namespace MiniShop.Api.Services;

public interface IAdminService
{
    Task<AdminResponse> Admin(CancellationToken ct);
}
