using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MiniShop.Api.Data;

// Design-time tooling does not start the web host or seed the database.
public sealed class ShopDbContextFactory : IDesignTimeDbContextFactory<ShopDbContext>
{
    public ShopDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Shop")
            ?? "Host=localhost;Database=minishop;Username=minishop;Password=local-only";

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ShopDbContext(options);
    }
}
