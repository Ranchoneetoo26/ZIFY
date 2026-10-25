using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Consents;

namespace Zyft.Application.Common;

public interface IConsentGuard
{
    Task<bool> IsGrantedAsync(Guid userId, ConsentPurpose purpose, CancellationToken ct = default);

    /// <summary>Lança 403 <c>consent_required</c> se a finalidade estiver desligada (bloqueio na API, não só na interface — RF-07).</summary>
    Task EnsureGrantedAsync(Guid userId, ConsentPurpose purpose, CancellationToken ct = default);
}

public sealed class ConsentGuard(IRepository<Consent> consents) : IConsentGuard
{
    public async Task<bool> IsGrantedAsync(Guid userId, ConsentPurpose purpose, CancellationToken ct = default) =>
        (await consents.FindAsync(c => c.UserId == userId && c.Purpose == purpose, ct))?.Granted == true;

    public async Task EnsureGrantedAsync(Guid userId, ConsentPurpose purpose, CancellationToken ct = default)
    {
        if (await IsGrantedAsync(userId, purpose, ct)) return;
        var what = purpose switch
        {
            ConsentPurpose.AiText => "IA para textos livres",
            ConsentPurpose.JobAnalysis => "análise de vaga",
            _ => "uso de dados sensíveis",
        };
        throw new ConsentRequiredException($"Esta ação exige o consentimento \"{what}\". Ative-o em Configurações > Privacidade.");
    }
}
