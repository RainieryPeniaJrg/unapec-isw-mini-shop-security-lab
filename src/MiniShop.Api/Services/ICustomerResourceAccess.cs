using System.Security.Claims;

namespace MiniShop.Api.Services;

public interface ICustomerResourceAccess
{
    void CheckClient(ClaimsPrincipal actor);
    void CheckClientOwner(int owner, ClaimsPrincipal actor, bool allowLab = true);
}
