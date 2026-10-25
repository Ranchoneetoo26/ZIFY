using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Zyft.Api.Controllers;

/// <summary>Base dos controllers: só traduz HTTP ⇄ comandos/queries (CQRS via MediatR); nenhuma regra aqui.</summary>
[ApiController]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _sender;

    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}

public static class Policies
{
    /// <summary>Só a plataforma web (token scope=web).</summary>
    public const string WebUser = "WebUser";

    /// <summary>Plataforma web ou extensão pareada (scope=web|extension).</summary>
    public const string ExtensionOrWeb = "ExtensionOrWeb";

    public const string AuthRateLimit = "auth";

    /// <summary>Limite por pessoa nas rotas de IA/importação.</summary>
    public const string AiRateLimit = "ai";
}
