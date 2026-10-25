using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace MiniShop.Api.Tests;

// Each factory owns its database; the configured development database is never reset.
public sealed class LabFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _database = "minishop_tests_" + Guid.NewGuid().ToString("N");
    private string _adminConnection = "";
    public string ShopConnection { get; private set; } = "";
    public string SearchConnection { get; private set; } = "";
    public string? Mode { get; set; } = "redteam";

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("ConnectionStrings__Shop")
            ?? throw new InvalidOperationException("Set ConnectionStrings__Shop to a disposable local PostgreSQL instance before running integration tests.");
        var connection = new NpgsqlConnectionStringBuilder(configured);
        if (connection.Host is not ("localhost" or "127.0.0.1" or "::1"))
            throw new InvalidOperationException("Integration tests require a loopback PostgreSQL host.");
        connection.Database = "postgres";
        _adminConnection = connection.ConnectionString;
        await using var admin = new NpgsqlConnection(_adminConnection);
        await admin.OpenAsync();
        await using (var role = new NpgsqlCommand("DO $$ BEGIN IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname='minishop_search') THEN CREATE ROLE minishop_search LOGIN PASSWORD 'local-search-only'; END IF; END $$;", admin))
            await role.ExecuteNonQueryAsync();
        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_database}\"", admin);
        await create.ExecuteNonQueryAsync();
        connection.Database = _database;
        ShopConnection = connection.ConnectionString;
        connection.Username = "minishop_search";
        connection.Password = "local-search-only";
        SearchConnection = connection.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureAppConfiguration((_, config) =>
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Shop"] = ShopConnection,
            ["ConnectionStrings:Search"] = SearchConnection,
            ["LAB_MODE"] = Mode,
            ["Jwt:LabSecret"] = "MiniShopLab2026"
        }));

    private bool _searchReady;
    public HttpClient NewClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (!_searchReady)
        {
            using var connection = new NpgsqlConnection(ShopConnection);
            connection.Open();
            using var grant = new NpgsqlCommand("GRANT USAGE ON SCHEMA public TO minishop_search; GRANT SELECT ON \"Products\" TO minishop_search;", connection);
            grant.ExecuteNonQuery();
            _searchReady = true;
        }
        return client;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        if (_adminConnection.Length == 0) return;
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(_adminConnection);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }
}
