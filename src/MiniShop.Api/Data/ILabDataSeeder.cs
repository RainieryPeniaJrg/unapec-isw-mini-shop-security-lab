namespace MiniShop.Api.Data;

public interface ILabDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
