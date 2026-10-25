namespace MiniShop.Api.Services;

public sealed class ShopException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
