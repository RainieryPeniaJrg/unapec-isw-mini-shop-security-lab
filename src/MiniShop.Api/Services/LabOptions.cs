using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MiniShop.Api.Services;

public sealed class LabOptions(IConfiguration configuration)
{
    public bool RedTeam { get; } = configuration["LAB_MODE"] == "redteam";
    public SymmetricSecurityKey SigningKey { get; } = CreateKey(configuration);

    private static SymmetricSecurityKey CreateKey(IConfiguration configuration)
    {
        var secret = configuration["LAB_MODE"] == "redteam"
            ? configuration["Jwt:LabSecret"] ?? throw new InvalidOperationException("Set Jwt__LabSecret for the disposable laboratory.")
            : Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        // HMAC zero-pads short keys to its block size: extending to 32 bytes
        // preserves the exact HS256 signature while meeting IdentityModel's minimum.
        var bytes = Encoding.UTF8.GetBytes(secret);
        if (bytes.Length < 32)
        {
            Array.Resize(ref bytes, 32);
        }

        return new SymmetricSecurityKey(bytes);
    }
}
