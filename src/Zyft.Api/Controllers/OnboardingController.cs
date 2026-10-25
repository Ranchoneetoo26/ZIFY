using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyft.Application.Onboarding;

namespace Zyft.Api.Controllers;

/// <summary>Progresso do onboarding em passos. As respostas vão para o perfil pelos endpoints do perfil.</summary>
[Route("api/v1/onboarding")]
[Authorize(Policy = Policies.WebUser)]
public sealed class OnboardingController : ApiControllerBase
{
    /// <summary>Passos respondidos, pulados ("Preencher mais tarde"), último passo e conclusão.</summary>
    [HttpGet]
    public Task<OnboardingDto> Get(CancellationToken ct) => Sender.Send(new GetOnboardingQuery(), ct);

    /// <summary>Substitui o progresso. completed = true registra a conclusão (uma vez).</summary>
    [HttpPut]
    [ProducesResponseType(typeof(OnboardingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<OnboardingDto> Update(UpdateOnboardingCommand cmd, CancellationToken ct) => Sender.Send(cmd, ct);
}
