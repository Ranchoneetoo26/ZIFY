using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyft.Application.Catalog;

namespace Zyft.Api.Controllers;

/// <summary>Catálogo por área: sugestões de cargos, setores, ferramentas, competências, link e registro profissional.</summary>
[Route("api/v1/catalog")]
[Authorize(Policy = Policies.ExtensionOrWeb)]
public sealed class CatalogController : ApiControllerBase
{
    /// <summary>Lista as áreas (Saúde, Tecnologia, Vendas e comercial…).</summary>
    [HttpGet("areas")]
    public Task<IReadOnlyList<AreaDto>> Areas(CancellationToken ct) => Sender.Send(new ListAreasQuery(), ct);
}
