using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;

namespace Zyft.Api.Infrastructure;

/// <summary>Usuário do token JWT (claims <c>sub</c>, <c>scope</c>, <c>device_id</c>).</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private System.Security.Claims.ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid? UserIdOrNull => Guid.TryParse(Principal?.FindFirst("sub")?.Value, out var id) ? id : null;

    public Guid UserId => UserIdOrNull ?? throw new UnauthorizedException("Faça login para continuar.");

    public string? Scope => Principal?.FindFirst("scope")?.Value;

    public Guid? DeviceId => Guid.TryParse(Principal?.FindFirst("device_id")?.Value, out var id) ? id : null;
}
