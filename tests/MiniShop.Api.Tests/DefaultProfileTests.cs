using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MiniShop.Api.Tests;

[Collection("PhaseOne")]
[Trait("Category", "Integration")]
public sealed class DefaultProfileTests
{
    [Fact]
    public async Task MissingLabMode_DisablesIntentionalSurfacesAndCrossOwnerAccess()
    {
        var factory = new LabFactory { Mode = null };
        await factory.InitializeAsync();
        try
        {
            using var client = factory.NewClient();
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/lab/collect?c=marker")).StatusCode);
            var original = await LabHttp.Bearer(client);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/cart/2")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/cart/2/items/2", new { quantity = 7 })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/orders/2")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/orders/2", new { quantity = 7 })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin")).StatusCode);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", LabHttp.ForgeAdmin(original));
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin")).StatusCode);
            await LabHttp.Bearer(client);
            var query = "' OR (SELECT 1 FROM pg_sleep(1))=1 --";
            var search = await LabHttp.Json(await client.GetAsync("/api/products/search?q=" + Uri.EscapeDataString(query)));
            Assert.Empty(search.EnumerateArray());
            const string body = "<img src=x onerror=\"alert('local')\">";
            (await client.PostAsJsonAsync("/api/products/1/comments", new { body })).EnsureSuccessStatusCode();
            var html = await client.GetStringAsync("/Products/Details/1");
            Assert.DoesNotContain(body, html);
            Assert.Contains("&lt;img", html);
            using var cookieClient = factory.NewClient();
            var login = await LabHttp.Cookie(cookieClient, "cliente-a", "LabClienteA2026!");
            var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), c => c.StartsWith("LabSession="));
            Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(HttpStatusCode.Forbidden, (await cookieClient.GetAsync("/api/admin")).StatusCode);
            var missing = await client.PostAsJsonAsync("/api/auth/login", new { username = "no-existe", password = "incorrecta" });
            var wrong = await client.PostAsJsonAsync("/api/auth/login", new { username = "cliente-a", password = "incorrecta" });
            Assert.Equal(await missing.Content.ReadAsStringAsync(), await wrong.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/auth/login", new { username = "cliente-a", password = "incorrecta" })).StatusCode);
        }
        finally { await ((IAsyncLifetime)factory).DisposeAsync(); }
    }
}
