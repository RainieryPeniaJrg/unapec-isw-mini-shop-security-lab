using Microsoft.AspNetCore.Identity;
using MiniShop.Api.Models;

namespace MiniShop.Api.Services;

public static class ShopServiceCollectionExtensions
{
    public static IServiceCollection AddShopServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IShopAuthenticationService, ShopAuthenticationService>();
        services.AddScoped<IRedTeamProductSearch, RedTeamProductSearch>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ICustomerResourceAccess, CustomerResourceAccess>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IAdminService, AdminService>();
        return services;
    }
}
