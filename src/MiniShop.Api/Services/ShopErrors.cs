namespace MiniShop.Api.Services;

internal static class ShopErrors
{
    public static ShopException Missing() => new(404, "Recurso no encontrado.");
    public static ShopException Denied() => new(403, "Acceso denegado.");
}
