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
    /// <summary>
    /// Cria a conta (senha: mín. 8 caracteres com letra e número) e já devolve os tokens. Exige
    /// <c>acceptTerms: true</c> e <c>termsVersion</c> igual à vigente (<c>GET /auth/terms</c>).
    /// </summary>
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

    /// <summary>Versão vigente dos termos de uso e da política de privacidade (enviar em <c>termsVersion</c> no cadastro).</summary>
    [HttpGet("terms")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TermsInfo), StatusCodes.Status200OK)]
    public TermsInfo Terms() => new(Domain.Users.Terms.CurrentVersion);

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

    /// <summary>
    /// Troca a senha da pessoa logada. Encerra as outras sessões (refresh tokens e extensões pareadas) e devolve um par
    /// de tokens novo para esta sessão: o cliente deve guardar o novo <c>refreshToken</c>.
    /// </summary>
    /// <response code="200">Senha trocada; corpo igual ao do login.</response>
    /// <response code="400">Senha atual incorreta (<c>errors.currentPassword</c>) ou nova senha fraca/igual (<c>errors.newPassword</c>).</response>
    [HttpPost("password")]
    [Authorize(Policy = Policies.WebUser)]
    [EnableRateLimiting(Policies.AuthRateLimit)]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public Task<AuthResult> ChangePassword(ChangePasswordCommand cmd, CancellationToken ct) => Sender.Send(cmd, ct);

    /// <summary>
    /// Pede o link de redefinição de senha (<c>/#/redefinir-senha?token=...</c>, vale 30 min). Responde sempre 202,
    /// exista ou não conta com o e-mail.
    /// </summary>
    /// <response code="202">Pedido aceito (mesma resposta para e-mail cadastrado ou não).</response>
    /// <response code="400">E-mail ausente ou mal formado.</response>
    /// <response code="429">Muitas tentativas.</response>
    [HttpPost("password/forgot")]
    [AllowAnonymous]
    [EnableRateLimiting(Policies.AuthRateLimit)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand cmd, CancellationToken ct)
    {
        await Sender.Send(cmd, ct);
        return Accepted();
    }

    /// <summary>Define a senha nova com o token do link; encerra todas as sessões e extensões pareadas da conta.</summary>
    /// <response code="204">Senha redefinida; faça login com a senha nova.</response>
    /// <response code="400">Token inválido, expirado ou já usado (<c>errors.token</c>) ou senha fraca (<c>errors.newPassword</c>).</response>
    [HttpPost("password/reset")]
    [AllowAnonymous]
    [EnableRateLimiting(Policies.AuthRateLimit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand cmd, CancellationToken ct)
    {
        await Sender.Send(cmd, ct);
        return NoContent();
    }
}
