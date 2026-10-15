using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Users;

namespace Zyft.Application.Common;

/// <summary>Resposta de autenticação (login, cadastro, renovação e pareamento).</summary>
public sealed record AuthResult(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    int ExpiresIn,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    string TokenType,
    string Scope,
    UserSummary User);

public sealed record UserSummary(Guid Id, string Email, string Name);

public interface IAuthTokenIssuer
{
    /// <summary>Gera access token + refresh token (persistido como hash; o chamador salva a unidade de trabalho).</summary>
    AuthResult Issue(User user, Guid? deviceId = null);
}

public sealed class AuthTokenIssuer(ITokenService tokens, IRepository<RefreshToken> refreshTokens, IClock clock) : IAuthTokenIssuer
{
    public AuthResult Issue(User user, Guid? deviceId = null)
    {
        var scope = deviceId is null ? TokenScopes.Web : TokenScopes.Extension;
        var access = tokens.CreateAccessToken(user, scope, deviceId);
        var raw = tokens.GenerateOpaqueToken();
        var expires = clock.UtcNow.Add(tokens.RefreshTokenLifetime);
        refreshTokens.Add(RefreshToken.Issue(user.Id, tokens.Hash(raw), expires, deviceId));
        return new AuthResult(
            access.Token,
            access.ExpiresAt,
            (int)Math.Round((access.ExpiresAt - clock.UtcNow).TotalSeconds),
            raw,
            expires,
            "Bearer",
            scope,
            new UserSummary(user.Id, user.Email, user.Name));
    }
}
