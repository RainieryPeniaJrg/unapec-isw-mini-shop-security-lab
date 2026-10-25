using System.Security.Claims;
using MiniShop.Api.Models;

namespace MiniShop.Api.Services;

public interface ICommentService
{
    Task<List<CommentResponse>> Comments(int id, CancellationToken ct);
    Task<int> Comment(int id, string body, ClaimsPrincipal actor, CancellationToken ct);
}
