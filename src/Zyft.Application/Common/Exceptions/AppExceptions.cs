namespace Zyft.Application.Common.Exceptions;

/// <summary>Recurso inexistente ou de outra pessoa (HTTP 404 — não revela existência).</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>Conflito com o estado atual, por exemplo e-mail já cadastrado (HTTP 409).</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>Credenciais ou token inválidos (HTTP 401).</summary>
public sealed class UnauthorizedException(string message) : Exception(message);

/// <summary>Autenticado, mas sem permissão para a ação (HTTP 403).</summary>
public class ForbiddenException(string message, string code = "forbidden") : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>Consentimento da finalidade desligado (HTTP 403, code <c>consent_required</c>).</summary>
public sealed class ConsentRequiredException(string message) : ForbiddenException(message, "consent_required");
