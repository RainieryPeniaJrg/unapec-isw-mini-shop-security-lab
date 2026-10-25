using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniShop.Api.Data;
using MiniShop.Api.Models;
using Npgsql;
using Xunit;

namespace MiniShop.Api.Tests;

[Collection("PhaseOne")]
[Trait("Category", "Integration")]
public sealed class PersistenceTests
{
    [Fact]
    public async Task FreshDatabase_MigratesAndSeedsOnceWithNoModelDrift()
    {
        var factory = new LabFactory();
        await factory.InitializeAsync();
        try
        {
            await using var db = CreateContext(factory);
            var initializer = CreateInitializer(db);
            await initializer.InitializeAsync();
            var before = await ReadDataAsync(factory.ShopConnection);
            await initializer.InitializeAsync();

            Assert.Single(await db.Database.GetAppliedMigrationsAsync());
            Assert.False(db.Database.HasPendingModelChanges());
            Assert.Equal(6, await db.Users.CountAsync());
            Assert.Equal(3, await db.Products.CountAsync());
            Assert.Equal(4, await db.Carts.CountAsync());
            Assert.Equal(2, await db.Orders.CountAsync());
            Assert.Equal(before, await ReadDataAsync(factory.ShopConnection));
        }
        finally
        {
            await ((IAsyncLifetime)factory).DisposeAsync();
        }
    }

    [Fact]
    public async Task OriginalLegacySchema_AdoptsHistoryWithoutChangingRowsOrIdentities()
    {
        var factory = new LabFactory();
        await factory.InitializeAsync();
        try
        {
            await using var db = CreateContext(factory);
            await db.Database.ExecuteSqlRawAsync(LegacySchemaSql);
            await CreateSeeder(db).SeedAsync();
            var comment = await db.Comments.SingleAsync();
            comment.Body = "Existing synthetic comment survives adoption.";
            await db.SaveChangesAsync();
            var before = await ReadDataAsync(factory.ShopConnection);

            await CreateInitializer(db).InitializeAsync();

            Assert.Single(await db.Database.GetAppliedMigrationsAsync());
            Assert.Equal(before, await ReadDataAsync(factory.ShopConnection));
            db.Products.Add(new Product { Name = "After adoption", SellerId = 5, PriceCents = 100 });
            await db.SaveChangesAsync();
            Assert.Equal(4, (await db.Products.SingleAsync(product => product.Name == "After adoption")).Id);
            Assert.Equal(0, await ReferenceSchemaCountAsync(factory.ShopConnection));
        }
        finally
        {
            await ((IAsyncLifetime)factory).DisposeAsync();
        }
    }

    [Theory]
    [InlineData("column")]
    [InlineData("foreign-key")]
    [InlineData("index")]
    [InlineData("extra-table")]
    public async Task LegacySchemaDrift_IsRejectedWithoutChangingRowsOrAdoptingHistory(string drift)
    {
        var factory = new LabFactory();
        await factory.InitializeAsync();
        try
        {
            await using var db = CreateContext(factory);
            await db.Database.ExecuteSqlRawAsync(LegacySchemaSql);
            await CreateSeeder(db).SeedAsync();
            var change = drift switch
            {
                "column" => "ALTER TABLE \"Products\" ALTER COLUMN \"Name\" TYPE text",
                "foreign-key" => "ALTER TABLE \"CartItems\" DROP CONSTRAINT \"FK_CartItems_Products_ProductId\"",
                "index" => "DROP INDEX \"IX_Users_Username\"",
                _ => "CREATE TABLE unexpected_table (id integer)"
            };
            await db.Database.ExecuteSqlRawAsync(change);
            var before = await ReadDataAsync(factory.ShopConnection);

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateInitializer(db).InitializeAsync());

            Assert.Contains("does not match the original Phase 1 schema", error.Message);
            Assert.Equal(before, await ReadDataAsync(factory.ShopConnection));
            await using var connection = new NpgsqlConnection(factory.ShopConnection);
            await connection.OpenAsync();
            await using var history = new NpgsqlCommand("SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NULL", connection);
            Assert.True((bool)(await history.ExecuteScalarAsync())!);
            Assert.Equal(0, await ReferenceSchemaCountAsync(factory.ShopConnection));
        }
        finally
        {
            await ((IAsyncLifetime)factory).DisposeAsync();
        }
    }

    private static ShopDbContext CreateContext(LabFactory factory) => new(
        new DbContextOptionsBuilder<ShopDbContext>().UseNpgsql(factory.ShopConnection).Options);

    private static LabDataSeeder CreateSeeder(ShopDbContext db) => new(db, new PasswordHasher<User>());

    private static ShopDatabaseInitializer CreateInitializer(ShopDbContext db) => new(
        db, new LegacySchemaValidator(db), CreateSeeder(db));

    private static async Task<List<string>> ReadDataAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var rows = new List<string>();
        foreach (var table in new[] { "Users", "Products", "Carts", "CartItems", "Comments", "Orders", "OrderItems" })
        {
            await using var command = new NpgsqlCommand(
                $"SELECT COALESCE(jsonb_agg(to_jsonb(t) ORDER BY t.\"Id\")::text, '[]') FROM \"{table}\" t", connection);
            rows.Add((string)(await command.ExecuteScalarAsync())!);
        }

        return rows;
    }

    private static async Task<long> ReferenceSchemaCountAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM pg_namespace WHERE nspname LIKE 'minishop_reference_%'", connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    // Frozen SQL from the original EnsureCreated model, intentionally independent
    // of the refactored Fluent configurations and migration snapshot.
    private const string LegacySchemaSql = """
        CREATE TABLE "Carts" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY,
            "UserId" integer NOT NULL,
            CONSTRAINT "PK_Carts" PRIMARY KEY ("Id")
        );
        CREATE TABLE "Orders" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY,
            "UserId" integer NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_Orders" PRIMARY KEY ("Id")
        );
        CREATE TABLE "Products" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY,
            "Name" character varying(120) NOT NULL,
            "Description" text NOT NULL,
            "PriceCents" integer NOT NULL,
            "SellerId" integer NOT NULL,
            CONSTRAINT "PK_Products" PRIMARY KEY ("Id")
        );
        CREATE TABLE "Users" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY,
            "Username" text NOT NULL,
            "PasswordHash" text NOT NULL,
            "Role" character varying(20) NOT NULL,
            CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
        );
        CREATE TABLE "OrderItems" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY,
            "OrderId" integer NOT NULL,
            "ProductId" integer NOT NULL,
            "Name" text NOT NULL,
            "PriceCents" integer NOT NULL,
            "Quantity" integer NOT NULL,
            CONSTRAINT "PK_OrderItems" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_OrderItems_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE
        );
        CREATE TABLE "CartItems" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY,
            "UserId" integer NOT NULL,
            "CartId" integer NOT NULL,
            "ProductId" integer NOT NULL,
            "Quantity" integer NOT NULL,
            CONSTRAINT "PK_CartItems" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_CartItems_Carts_CartId" FOREIGN KEY ("CartId") REFERENCES "Carts" ("Id") ON DELETE CASCADE,
            CONSTRAINT "FK_CartItems_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
        );
        CREATE TABLE "Comments" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY,
            "ProductId" integer NOT NULL,
            "UserId" integer NOT NULL,
            "Body" text NOT NULL,
            CONSTRAINT "PK_Comments" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_Comments_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX "IX_Carts_UserId" ON "Carts" ("UserId");
        CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username");
        CREATE INDEX "IX_OrderItems_OrderId" ON "OrderItems" ("OrderId");
        CREATE UNIQUE INDEX "IX_CartItems_CartId_ProductId" ON "CartItems" ("CartId", "ProductId");
        CREATE INDEX "IX_CartItems_ProductId" ON "CartItems" ("ProductId");
        CREATE INDEX "IX_Comments_ProductId" ON "Comments" ("ProductId");
        """;
}
