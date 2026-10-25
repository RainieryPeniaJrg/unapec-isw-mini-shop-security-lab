using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Xunit;

namespace MiniShop.Api.Tests;

internal static class LabHttp
{
    public static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    public static async Task<string> Bearer(HttpClient client, string username = "cliente-a", string password = "LabClienteA2026!")
    {
        var login = await Json(await client.PostAsJsonAsync("/api/auth/login", new { username, password }));
        var token = login.GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return token;
    }

    public static async Task<HttpResponseMessage> Cookie(HttpClient client, string username, string password)
    {
        var page = await client.GetStringAsync("/Account/Login");
        var match = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "MVC login must issue an antiforgery token.");
        return await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = username, ["Password"] = password,
            ["__RequestVerificationToken"] = System.Net.WebUtility.HtmlDecode(match.Groups[1].Value)
        }));
    }

    public static string ForgeAdmin(string token, string secret = "MiniShopLab2026")
    {
        var parts = token.Split('.');
        var payload = JsonNode.Parse(Decode(parts[1]))!.AsObject();
        payload["role"] = "Admin";
        var data = parts[0] + "." + Encode(Encoding.UTF8.GetBytes(payload.ToJsonString()));
        return data + "." + Encode(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.ASCII.GetBytes(data)));
    }

    private static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static byte[] Decode(string input) => Convert.FromBase64String(input.Replace('-', '+').Replace('_', '/').PadRight((input.Length + 3) / 4 * 4, '='));
}
