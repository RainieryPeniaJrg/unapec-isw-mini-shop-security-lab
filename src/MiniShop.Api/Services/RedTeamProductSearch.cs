using MiniShop.Api.Models;
using Npgsql;

namespace MiniShop.Api.Services;

internal sealed class RedTeamProductSearch(IConfiguration config) : IRedTeamProductSearch
{
    public async Task<List<ProductResponse>> Search(string q, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(config.GetConnectionString("Search")
            ?? throw new InvalidOperationException("Read-only Search connection is required in Red Team."));
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction))
        {
            await readOnly.ExecuteNonQueryAsync(ct);
        }

        // Intentional F1-10: retain the concatenated query, SELECT-only principal,
        // read-only transaction and timeout used by the local laboratory.
        await using var command = new NpgsqlCommand("SELECT \"Id\", \"Name\", \"Description\", \"PriceCents\", \"SellerId\" FROM \"Products\" WHERE \"Name\" ILIKE '%" + q + "%' ORDER BY \"Id\"", connection, transaction);
        command.CommandTimeout = 10;
        await using var reader = await command.ExecuteReaderAsync(ct);
        var products = new List<ProductResponse>();
        while (await reader.ReadAsync(ct))
        {
            products.Add(new ProductResponse(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.GetInt32(4)));
        }

        return products;
    }
}
