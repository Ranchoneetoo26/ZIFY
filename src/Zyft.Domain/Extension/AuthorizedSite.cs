using Zyft.Domain.Common;

namespace Zyft.Domain.Extension;

/// <summary>Site em que a pessoa autorizou a extensão a atuar (RF-10). Guarda só a origem (esquema://host[:porta]).</summary>
public sealed class AuthorizedSite : Entity
{
    private AuthorizedSite() { }

    public Guid UserId { get; private set; }
    public string Origin { get; private set; } = default!;

    public static AuthorizedSite Create(Guid userId, string origin) => new()
    {
        UserId = userId,
        Origin = NormalizeOrigin(origin),
    };

    /// <summary>Normaliza para <c>https://host[:porta]</c>; aceita URL completa. Só http(s).</summary>
    public static string NormalizeOrigin(string value)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new DomainException("site.invalid_origin", "Informe um endereço http(s) válido, por exemplo https://vagas.exemplo.com.");
        return uri.IsDefaultPort ? $"{uri.Scheme}://{uri.Host}" : $"{uri.Scheme}://{uri.Host}:{uri.Port}";
    }
}
