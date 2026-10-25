using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Zyft.Application.Auth;
using Zyft.Application.Common;

namespace Zyft.Api.Controllers;

/// <summary>Cadastro, login e sessão (JWT de 15 min + refresh token opaco de 7 dias com rotação).</summary>
[Route("api/v1/auth")]
[AllowAnonymous]
public sealed class AuthController : ApiControllerBase
{
    /// <summary>Cria a conta (senha: mín. 8 caracteres com letra e número) e já devolve os tokens.</summary>
    /// <response code="201">Conta criada.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="409">E-mail já cadastrado.</response>
    [HttpPost("register")]
    [EnableRateLimiting(Policies.AuthRateLimit)]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResult>> Register(RegisterCommand cmd, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await Sender.Send(cmd, ct));

    /// <summary>Login com e-mail e senha. Limitado por IP (429 ao exceder).</summary>
    /// <response code="401">E-mail ou senha incorretos.</response>
    /// <response code="429">Muitas tentativas.</response>
    [HttpPost("login")]
    [EnableRateLimiting(Policies.AuthRateLimit)]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public Task<AuthResult> Login(LoginCommand cmd, CancellationToken ct) => Sender.Send(cmd, ct);

    /// <summary>Troca o refresh token por um novo par (rotação). Reusar um token já trocado revoga a cadeia inteira.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    public Task<AuthResult> Refresh(RefreshTokenCommand cmd, CancellationToken ct) => Sender.Send(cmd, ct);

    /// <summary>Revoga o refresh token informado (idempotente).</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutCommand cmd, CancellationToken ct)
    {
        await Sender.Send(cmd, ct);
        return NoContent();
    }
}
