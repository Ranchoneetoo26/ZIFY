using System.Linq.Expressions;
using System.Text.Json;
using Zyft.Domain.Common;
using Zyft.Domain.Profiles;
using Zyft.Domain.Users;

namespace Zyft.Application.Common.Interfaces;

/// <summary>Repositório genérico (implementado com EF Core na infraestrutura).</summary>
public interface IRepository<T> where T : Entity
{
    Task<T?> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<List<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
}

/// <summary>Carrega o agregado Perfil com todas as coleções.</summary>
public interface IProfileRepository
{
    Task<Profile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    void Add(Profile profile);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user, string scope, Guid? deviceId = null);

    /// <summary>Gera um segredo opaco aleatório (refresh token, códigos).</summary>
    string GenerateOpaqueToken();

    /// <summary>SHA-256 em hexadecimal — só o hash é persistido.</summary>
    string Hash(string raw);

    TimeSpan RefreshTokenLifetime { get; }
}

/// <summary>Escopos de token (claim <c>scope</c>).</summary>
public static class TokenScopes
{
    public const string Web = "web";
    public const string Extension = "extension";
}

public interface ICurrentUser
{
    Guid? UserIdOrNull { get; }

    /// <summary>Id do usuário autenticado; lança 401 se não houver.</summary>
    Guid UserId { get; }

    string? Scope { get; }
    Guid? DeviceId { get; }
}

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface IResumeTextExtractor
{
    /// <summary>Extrai texto de PDF (com camada de texto) ou DOCX.</summary>
    Task<string> ExtractAsync(byte[] content, string fileName, CancellationToken ct = default);
}

public interface ISensitiveDataProtector
{
    string Protect(string plainText);
    string Unprotect(string cipherText);
}

/// <summary>Pedido de completude com saída JSON validada por schema.</summary>
public sealed record LlmJsonRequest(string Prompt, object Schema, string? System = null, double Temperature = 0.1, TimeSpan? Timeout = null);

/// <summary>Porta para o modelo de linguagem (ADR-0005). A IA nunca é fonte de valores objetivos.</summary>
public interface ILlmClient
{
    /// <summary>Falso quando não há IA configurada (NullLlmClient); regras seguem funcionando (RNF-12).</summary>
    bool IsEnabled { get; }

    /// <summary>Lança <see cref="LlmUnavailableException"/> se a IA estiver fora ou devolver JSON inválido.</summary>
    Task<JsonElement> CompleteJsonAsync(LlmJsonRequest request, CancellationToken ct = default);
}

public sealed class LlmUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
