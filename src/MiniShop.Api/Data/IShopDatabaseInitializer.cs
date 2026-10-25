namespace MiniShop.Api.Data;

public interface IShopDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
