using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace MiniShop.Api.Data;

/// <summary>
/// Compares the original migration's schema with an EnsureCreated database.
/// The reference schema exists only inside a rolled-back transaction.
/// </summary>
public sealed class LegacySchemaValidator(ShopDbContext db)
{
    public async Task ValidateAsync(string initialMigration, CancellationToken cancellationToken = default)
    {
        var referenceSchema = "minishop_reference_" + Guid.NewGuid().ToString("N");
        var script = db.GetService<IMigrator>().GenerateScript(
            Migration.InitialDatabase,
            initialMigration,
            MigrationsSqlGenerationOptions.NoTransactions);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // This identifier is generated locally, never taken from a request.
            var createReferenceSchemaSql = $"CREATE SCHEMA \"{referenceSchema}\"";
            await db.Database.ExecuteSqlRawAsync(createReferenceSchemaSql, cancellationToken);
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT set_config('search_path', {referenceSchema}, true)", cancellationToken);
            await db.Database.ExecuteSqlRawAsync(script, cancellationToken);

            var expected = await ReadSchemaAsync(referenceSchema, cancellationToken);
            var actual = await ReadSchemaAsync("public", cancellationToken);
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
            {
                var missing = expected.Except(actual, StringComparer.Ordinal).Take(3);
                var unexpected = actual.Except(expected, StringComparer.Ordinal).Take(3);
                throw new InvalidOperationException(
                    "The existing database does not match the original Phase 1 schema. " +
                    "Migration history was not adopted and data was not changed. " +
                    "Review schema drift before retrying. Missing/changed: " + string.Join("; ", missing) +
                    ". Unexpected/changed: " + string.Join("; ", unexpected));
            }
        }
        finally
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
    }

    private async Task<List<string>> ReadSchemaAsync(string schema, CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var command = new NpgsqlCommand(SchemaFingerprintSql, connection);
        command.Parameters.AddWithValue("schema", schema);

        var entries = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(reader.GetString(0));
        }

        entries.Sort(StringComparer.Ordinal);
        return entries;
    }

    // Use catalog fields rather than schema-qualified DDL text so the temporary
    // reference namespace does not affect equality. History is validated by EF.
    private const string SchemaFingerprintSql = """
        WITH tables AS (
            SELECT c.oid, c.relname, c.relkind
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = @schema AND c.relkind IN ('r', 'p')
                AND c.relname <> '__EFMigrationsHistory'
        )
        SELECT 'table:' || json_build_array(t.relname, t.relkind)::text FROM tables t
        UNION ALL
        SELECT 'column:' || json_build_array(t.relname, a.attname,
            pg_catalog.format_type(a.atttypid, a.atttypmod), a.attnotnull,
            a.attidentity, a.attgenerated, COALESCE(column_collation.collname, ''),
            COALESCE(pg_catalog.pg_get_expr(d.adbin, d.adrelid), ''))::text
        FROM tables t
        JOIN pg_catalog.pg_attribute a ON a.attrelid = t.oid
        LEFT JOIN pg_catalog.pg_attrdef d ON d.adrelid = t.oid AND d.adnum = a.attnum
        LEFT JOIN pg_catalog.pg_collation column_collation ON column_collation.oid = a.attcollation
        WHERE a.attnum > 0 AND NOT a.attisdropped
        UNION ALL
        SELECT 'constraint:' || json_build_array(t.relname, c.conname, c.contype,
            ARRAY(SELECT a.attname FROM unnest(c.conkey) WITH ORDINALITY k(num, ord)
                JOIN pg_catalog.pg_attribute a ON a.attrelid = t.oid AND a.attnum = k.num ORDER BY k.ord),
            COALESCE(target.relname, ''),
            CASE WHEN target_namespace.nspname = @schema THEN 'local'
                ELSE COALESCE(target_namespace.nspname, '') END,
            ARRAY(SELECT a.attname FROM unnest(c.confkey) WITH ORDINALITY k(num, ord)
                JOIN pg_catalog.pg_attribute a ON a.attrelid = c.confrelid AND a.attnum = k.num ORDER BY k.ord),
            c.confupdtype, c.confdeltype, c.confmatchtype,
            c.condeferrable, c.condeferred, c.convalidated,
            CASE WHEN c.contype = 'c' THEN pg_catalog.pg_get_constraintdef(c.oid) ELSE '' END)::text
        FROM tables t
        JOIN pg_catalog.pg_constraint c ON c.conrelid = t.oid
        LEFT JOIN pg_catalog.pg_class target ON target.oid = c.confrelid
        LEFT JOIN pg_catalog.pg_namespace target_namespace ON target_namespace.oid = target.relnamespace
        UNION ALL
        SELECT 'index:' || json_build_array(t.relname, name.relname,
            method.amname, i.indisunique, i.indisprimary, i.indisvalid, i.indnullsnotdistinct,
            ARRAY(SELECT a.attname FROM unnest(i.indkey) WITH ORDINALITY k(num, ord)
                LEFT JOIN pg_catalog.pg_attribute a ON a.attrelid = t.oid AND a.attnum = k.num ORDER BY k.ord),
            i.indnkeyatts, i.indnatts, i.indoption::text,
            ARRAY(SELECT op.opcname FROM unnest(i.indclass) WITH ORDINALITY k(oid, ord)
                JOIN pg_catalog.pg_opclass op ON op.oid = k.oid ORDER BY k.ord),
            ARRAY(SELECT coll.collname FROM unnest(i.indcollation) WITH ORDINALITY k(oid, ord)
                LEFT JOIN pg_catalog.pg_collation coll ON coll.oid = k.oid ORDER BY k.ord),
            COALESCE(pg_catalog.pg_get_expr(i.indpred, i.indrelid), ''),
            COALESCE(pg_catalog.pg_get_expr(i.indexprs, i.indrelid), ''))::text
        FROM tables t
        JOIN pg_catalog.pg_index i ON i.indrelid = t.oid
        JOIN pg_catalog.pg_class name ON name.oid = i.indexrelid
        JOIN pg_catalog.pg_am method ON method.oid = name.relam
        """;
}
