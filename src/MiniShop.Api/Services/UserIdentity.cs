using System.Security.Claims;

namespace MiniShop.Api.Services;

public static class UserIdentity
{
    public static int UserId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
