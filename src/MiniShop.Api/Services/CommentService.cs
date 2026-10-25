using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MiniShop.Api.Data;
using MiniShop.Api.Models;

namespace MiniShop.Api.Services;

internal sealed class CommentService(ShopDbContext db, ICatalogService catalog) : ICommentService
{
    public async Task<List<CommentResponse>> Comments(int id, CancellationToken ct) =>
        await db.Comments.Where(x => x.ProductId == id)
            .Join(db.Users, x => x.UserId, u => u.Id, (x, u) => new { Comment = x, u.Username })
            .OrderBy(x => x.Comment.Id)
            .Select(x => new CommentResponse(x.Comment.Id, x.Comment.ProductId, x.Comment.UserId, x.Username, x.Comment.Body))
            .ToListAsync(ct);

    public async Task<int> Comment(int id, string body, ClaimsPrincipal actor, CancellationToken ct)
    {
        await catalog.Product(id, ct);
        var comment = new ProductComment { ProductId = id, UserId = actor.UserId(), Body = body };
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);
        return comment.Id;
    }
}
