namespace Zyft.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "zyft-api";
    public string Audience { get; set; } = "zyft-clients";

    /// <summary>Segredo HS256 (≥ 32 caracteres). Em produção vem de variável de ambiente <c>Jwt__Secret</c>.</summary>
    public string Secret { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
