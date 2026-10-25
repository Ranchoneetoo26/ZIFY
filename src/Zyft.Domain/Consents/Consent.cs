using Zyft.Domain.Common;

namespace Zyft.Domain.Consents;

/// <summary>Finalidades de tratamento separadas e desligáveis (RF-07).</summary>
public enum ConsentPurpose
{
    /// <summary>(a) IA para textos livres.</summary>
    AiText = 1,

    /// <summary>(b) Análise de vaga.</summary>
    JobAnalysis = 2,

    /// <summary>(c) Uso de dados sensíveis — começa desligado.</summary>
    SensitiveData = 3,
}

public sealed class Consent : Entity
{
    private Consent() { }

    public Guid UserId { get; private set; }
    public ConsentPurpose Purpose { get; private set; }
    public bool Granted { get; private set; }

    public static Consent Create(Guid userId, ConsentPurpose purpose, bool granted) =>
        new() { UserId = userId, Purpose = purpose, Granted = granted };

    /// <summary>
    /// Padrões do cadastro: TODAS as finalidades começam desligadas (opt-in, como prometido na Entrega 1); a pessoa
    /// liga (a) e (b) na tela de Boas-vindas. (c) dados sensíveis só por decisão explícita (RF-07, D-01).
    /// </summary>
    public static IEnumerable<Consent> Defaults(Guid userId)
    {
        yield return Create(userId, ConsentPurpose.AiText, false);
        yield return Create(userId, ConsentPurpose.JobAnalysis, false);
        yield return Create(userId, ConsentPurpose.SensitiveData, false);
    }

    public void Set(bool granted) => Granted = granted;
}
