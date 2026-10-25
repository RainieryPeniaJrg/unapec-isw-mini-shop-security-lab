using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace MiniShop.Api.Data;

public sealed class ShopDatabaseInitializer(
    ShopDbContext db,
    LegacySchemaValidator legacySchema,
    ILabDataSeeder seeder) : IShopDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await AdoptLegacySchemaIfRequiredAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        await seeder.SeedAsync(cancellationToken);
    }

    private async Task AdoptLegacySchemaIfRequiredAsync(CancellationToken cancellationToken)
    {
        var creator = db.GetService<IRelationalDatabaseCreator>();
        if (!await creator.ExistsAsync(cancellationToken) || !await creator.HasTablesAsync(cancellationToken))
        {
            return;
        }

        var history = db.GetService<IHistoryRepository>();
        var applied = await history.GetAppliedMigrationsAsync(cancellationToken);
        if (applied.Count > 0)
        {
            return;
        }

        // Baseline only the initial migration. Future migrations still run via
        // MigrateAsync; their shape is not mistaken for the legacy schema.
        var initialMigration = db.Database.GetMigrations().First();
        await legacySchema.ValidateAsync(initialMigration, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript(), cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            history.GetInsertScript(new HistoryRow(initialMigration, ProductInfo.GetVersion())),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
