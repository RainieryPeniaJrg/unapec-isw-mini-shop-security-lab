# Code First persistence

The laboratory already used EF Core entities as its schema source. The refactor replaces `EnsureCreated` with a versioned initial migration and keeps the same seven tables, columns, keys, indexes, delete behaviors, seed identities, and HTTP contracts. The Phase 1 vulnerabilities remain intentional and unchanged.

## Responsibilities

- `Models/`: one persistence entity per file, without database or HTTP dependencies.
- `Data/Configurations/`: an `IEntityTypeConfiguration<T>` per entity. `ShopDbContext` discovers these mappings through `ApplyConfigurationsFromAssembly`.
- `Data/Migrations/`: the initial migration and model snapshot, generated from the original Phase 1 model.
- `ShopDatabaseInitializer`: adopts an eligible legacy schema, applies migrations, then requests seed data through `ILabDataSeeder`.
- `LabDataSeeder`: creates only the existing synthetic accounts and sample resources when no users exist. It uses the existing ASP.NET Core password hasher through `IPasswordHasher<User>`.
- `LegacySchemaValidator`: verifies the original schema before adding migration history to an older database.

No generic repository layer or new business constraints are introduced. Existing identifier fields without foreign keys remain unchanged so this refactor does not change database semantics.

## Fresh and existing databases

Normal application startup runs `IShopDatabaseInitializer.InitializeAsync`. A fresh database receives the initial migration and the original seed. Restarting an initialized database neither recreates the schema nor duplicates seed rows.

A database created by the previous `EnsureCreated` implementation has no migration history. For that case, startup generates the **initial migration's** schema in a unique reference namespace inside a transaction. It compares PostgreSQL catalog metadata for tables, columns, nullability, types, identity/default values, primary/foreign keys and delete actions, and indexes (including uniqueness, expressions, collation, and operator classes). The reference transaction is always rolled back.

Only an exact match permits recording the initial migration as applied. Later migrations still run normally. The process does not delete or reinsert existing rows or reset their sequences. If schema drift is found, startup stops with a descriptive error and leaves the existing data and migration history untouched. Review the difference before retrying; do not delete the volume to bypass this check.

This adoption procedure targets the laboratory's `public` schema and requires the configured database owner to create a temporary reference schema. Run the first adoption with one application instance; concurrent startup/adoption is outside this single-instance laboratory setup. Keep a database backup before applying a future schema-changing migration.

## EF tooling

Use .NET SDK 10 and the repository's local tool manifest. The design-time factory constructs only the DbContext; it does not start the web host, enable Red Team, or seed data.

```bash
dotnet tool restore
dotnet ef migrations has-pending-model-changes --project src/MiniShop.Api
dotnet ef migrations script --project src/MiniShop.Api --output /tmp/minishop-migrations.sql
```

For a future approved model change:

```bash
dotnet ef migrations add DescribeTheChange --project src/MiniShop.Api --output-dir Data/Migrations
dotnet ef migrations script --idempotent --project src/MiniShop.Api --output /tmp/minishop-migrations.sql
```

Review the generated migration and SQL before applying it. Set `ConnectionStrings__Shop` to a disposable local database when using database commands. Application startup applies pending migrations automatically; a direct `dotnet ef database update` is appropriate for a fresh or already versioned database, while legacy adoption uses the application initializer's validation first.

`Microsoft.EntityFrameworkCore.Design`, the relational EF runtime dependency, and `dotnet-ef` are pinned to 10.0.13. The Npgsql provider remains at 10.0.0; the explicit relational reference keeps the application, design tooling, and consuming test project's EF dependency graph consistent. NuGet audit remains enabled; the intentional laboratory weaknesses are preserved in application behavior.

## Verification

`PersistenceTests` checks migration and seed idempotence, an unchanged EF model snapshot, legacy adoption preserving all seven tables' rows and the next identity value, and refusal to adopt column, foreign-key, index, or extra-table drift. The existing Phase 1 integration suite continues to characterize all intentional vulnerabilities and the off-profile guards.
