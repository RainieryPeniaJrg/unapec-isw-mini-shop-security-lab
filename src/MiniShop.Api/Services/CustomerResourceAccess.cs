using System.Security.Claims;

namespace MiniShop.Api.Services;

internal sealed class CustomerResourceAccess(LabOptions lab) : ICustomerResourceAccess
{
    public void CheckClient(ClaimsPrincipal actor)
    {
        if (!actor.IsInRole("Cliente"))
        {
            throw ShopErrors.Denied();
        }
    }

    public void CheckClientOwner(int owner, ClaimsPrincipal actor, bool allowLab = true)
    {
        // Intentional F1-12: Red Team permits access to another customer's
        // resource except when the existing caller explicitly disables it.
        if (!actor.IsInRole("Cliente") || (owner != actor.UserId() && !(lab.RedTeam && allowLab)))
        {
            throw ShopErrors.Denied();
        }
    }
}
