using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiniShop.Api.Services;
namespace MiniShop.Api.Middleware;
public sealed class ShopExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not ShopException failure) return false;
        context.Response.StatusCode = failure.Status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = failure.Status, Title = failure.Message }, ct);
        return true;
    }
}
