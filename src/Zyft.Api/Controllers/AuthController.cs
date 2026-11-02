using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Zyft.Application.Auth;
using Zyft.Application.Common;

namespace Zyft.Api.Controllers;

/// <summary>
/// Cadastro, login e sessão (JWT de 15 min + refresh token opaco de 7 dias com rotação). As rotas são anônimas,
/// exceto as marcadas com <c>[Authorize]</c> (o <c>[AllowAnonymous]</c> fica por ação para não anular essas).
/// </summary>
[Route("api/v1/auth")]
public sealed class AuthController : ApiControllerBase
{
    /// <summary>Cria a conta (senha: mín. 8 caracteres com letra e número) e já devolve os tokens.</summary>
    /// <response code="201">Conta criada.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="409">E-mail já cadastrado.</response>
    [HttpPost("register")]
    [AllowAnonymous]
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
    [AllowAnonymous]
    [EnableRateLimiting(Policies.AuthRateLimit)]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public Task<AuthResult> Login(LoginCommand cmd, CancellationToken ct) => Sender.Send(cmd, ct);

    /// <summary>Troca o refresh token por um novo par (rotação). Reusar um token já trocado revoga a cadeia inteira.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    public Task<AuthResult> Refresh(RefreshTokenCommand cmd, CancellationToken ct) => Sender.Send(cmd, ct);

    /// <summary>Revoga o refresh token informado (idempotente).</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutCommand cmd, CancellationToken ct)
    {
        await Sender.Send(cmd, ct);
        return NoContent();
    }

    /// <summary>Confirma o e-mail com o token recebido no link (<c>/#/verificar?token=...</c>). Uso único, vale 24 h.</summary>
    /// <response code="204">E-mail verificado.</response>
    /// <response code="400">Token inválido, expirado ou já usado (<c>errors.token</c>).</response>
    [HttpPost("verify")]
    [AllowAnonymous]
    [EnableRateLimiting(Policies.AuthRateLimit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Verify(VerifyEmailCommand cmd, CancellationToken ct)
    {
        await Sender.Send(cmd, ct);
        return NoContent();
    }

    /// <summary>Reenvia o link de confirmação de e-mail para a pessoa logada; o link anterior deixa de valer.</summary>
    /// <response code="202">Link enviado.</response>
    /// <response code="409">E-mail já verificado (<c>email.already_verified</c>).</response>
    /// <response code="429">Muitas tentativas.</response>
    [HttpPost("verify/resend")]
    [Authorize(Policy = Policies.WebUser)]
    [EnableRateLimiting(Policies.AuthRateLimit)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendVerification(CancellationToken ct)
    {
        await Sender.Send(new ResendEmailVerificationCommand(), ct);
        return Accepted();
    }
}
