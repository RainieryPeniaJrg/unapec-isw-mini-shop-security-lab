using Microsoft.EntityFrameworkCore;
using MiniShop.Api.Data;
using MiniShop.Api.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Shop")
    ?? "Host=localhost;Port=5432;Database=minishop;Username=minishop;Password=local-only";
builder.Services.AddDbContext<ShopDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/api/health");

// Minimal functional baseline. The class will build the intentional Red Team lab
// routes behind LAB_MODE and then replace them with secure policies in Blue Team.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
    db.Database.EnsureCreated();
    if (!db.Products.Any())
    {
        db.Products.Add(new Product { Name = "Sample keyboard", Description = "Synthetic training item", PriceCents = 4999, SellerId = 2 });
        db.SaveChanges();
    }
}

app.MapGet("/api/products", async (ShopDbContext db) =>
    Results.Ok(await db.Products.Select(p => new { p.Id, p.Name, p.Description, p.PriceCents }).ToListAsync()));

app.Run();

public partial class Program { }
