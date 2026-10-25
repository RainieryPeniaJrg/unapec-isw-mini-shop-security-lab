using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using MiniShop.Api.Data;
using MiniShop.Api.Services;

var builder = WebApplication.CreateBuilder(args);
// Request-start diagnostics include the full query string. The local XSS
// collector must never write its cookie parameter to container logs.
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Services.AddSingleton<LabOptions>();
builder.Services.AddDbContext<ShopDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Shop")
    ?? "Host=localhost;Database=minishop;Username=minishop;Password=local-only"));
builder.Services.AddShopServices();
builder.Services.AddScoped<ILabDataSeeder, LabDataSeeder>();
builder.Services.AddScoped<LegacySchemaValidator>();
builder.Services.AddScoped<IShopDatabaseInitializer, ShopDatabaseInitializer>();
builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<MiniShop.Api.Middleware.ShopExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Path.GetTempPath(), "minishop-keys")));
builder.Services.AddAuthentication(options => {
    options.DefaultScheme = "SessionOrBearer";
    options.DefaultChallengeScheme = "SessionOrBearer";
}).AddPolicyScheme("SessionOrBearer", null, options => options.ForwardDefaultSelector = context =>
    context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        ? JwtBearerDefaults.AuthenticationScheme : CookieAuthenticationDefaults.AuthenticationScheme)
 .AddCookie().AddJwtBearer();
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme).Configure<LabOptions>((options, lab) => {
    options.Cookie.Name = "LabSession";
    options.Cookie.HttpOnly = !lab.RedTeam;
    options.Cookie.SecurePolicy = lab.RedTeam ? CookieSecurePolicy.None : CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/Account/Login";
    options.Events.OnRedirectToLogin = context => {
        if (context.Request.Path.StartsWithSegments("/api")) context.Response.StatusCode = 401;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
 });
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<LabOptions>((options, lab) => {
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuerSigningKey = true, IssuerSigningKey = lab.SigningKey,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ValidateIssuer = true, ValidIssuer = "MiniShopLab",
        ValidateAudience = true, ValidAudience = "MiniShopLab", ValidateLifetime = true,
        NameClaimType = "unique_name", RoleClaimType = "role", ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents { OnTokenValidated = context => {
        var identity = (ClaimsIdentity)context.Principal!.Identity!;
        var subject = identity.FindFirst("sub")?.Value;
        if (!int.TryParse(subject, out _)) context.Fail("Invalid subject.");
        else identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subject!));
        return Task.CompletedTask;
    }};
 });
builder.Services.AddAuthorization();
// Minimal off-profile guard only. Distributed login controls belong to Fase 2.
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = 429;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => {
        var login = context.Request.Method == "POST" && (context.Request.Path == "/Account/Login" || context.Request.Path == "/api/auth/login");
        return !context.RequestServices.GetRequiredService<LabOptions>().RedTeam && login
            ? RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })
            : RateLimitPartition.GetNoLimiter("lab");
    });
});
var app = builder.Build();
var lab = app.Services.GetRequiredService<LabOptions>();
app.UseExceptionHandler();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/api/health");
app.MapOpenApi();
app.MapControllers();
app.MapControllerRoute("default", "{controller=Products}/{action=Index}/{id?}");
if (lab.RedTeam) app.MapGet("/lab/collect", () => Results.NoContent());
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IShopDatabaseInitializer>();
    await initializer.InitializeAsync();
    if (lab.RedTeam)
    {
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        await db.Database.ExecuteSqlRawAsync("GRANT SELECT ON TABLE \"Products\" TO minishop_search");
    }
}
app.Run();
public partial class Program { }
