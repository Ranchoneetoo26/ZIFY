using Zyft.Domain.Common;

namespace Zyft.Domain.Onboarding;

/// <summary>
/// Progresso do onboarding em passos ("passaporte"). Guarda só os IDS dos passos respondidos e pulados — as respostas
/// vão para o Perfil Mestre pelos endpoints do perfil. Passos pulados viram alerta no Perfil Mestre.
/// </summary>
public sealed class OnboardingProgress : Entity
{
    /// <summary>Ids de passo aceitos, na ordem do onboarding.</summary>
    public static readonly IReadOnlyList<string> Steps = new[]
    {
        "nome", "nascimento", "contato", "endereco", "area", "cargos", "setores", "registro", "links", "nivel", "modelo", "local",
        "contrato", "disponibilidade", "salario", "mobilidade", "experiencia", "diaadia", "ferramentas", "formacao", "cursos",
        "competencias", "idiomas", "titulo", "resumo", "combinados",
    };

    private OnboardingProgress() { }

    public Guid UserId { get; private set; }

    /// <summary>Ids separados por vírgula (lista fechada, sem conteúdo pessoal).</summary>
    public string Answered { get; private set; } = string.Empty;

    public string Skipped { get; private set; } = string.Empty;
    public string? LastStep { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public IReadOnlyList<string> AnsweredSteps => Split(Answered);
    public IReadOnlyList<string> SkippedSteps => Split(Skipped);

    public static OnboardingProgress Start(Guid userId) => new() { UserId = userId };

    /// <summary>
    /// Substitui o progresso. Um passo respondido sai da lista de pulados. Devolve true quando o onboarding acabou de ser
    /// concluído (para registrar o evento <c>onboarding.completed</c> uma vez só).
    /// </summary>
    public bool Update(IEnumerable<string>? answered, IEnumerable<string>? skipped, string? lastStep, bool completed, DateTime now)
    {
        var a = Normalize(answered);
        var s = Normalize(skipped).Where(x => !a.Contains(x)).ToList();
        var last = string.IsNullOrWhiteSpace(lastStep) ? null : lastStep.Trim().ToLowerInvariant();
        if (last is not null && !Steps.Contains(last))
            throw new DomainException("onboarding.invalid_step", $"Passo desconhecido: {last}.");

        Answered = string.Join(',', a);
        Skipped = string.Join(',', s);
        LastStep = last;
        if (completed && CompletedAt is null)
        {
            CompletedAt = now;
            return true;
        }

        return false;
    }

    private static List<string> Normalize(IEnumerable<string>? ids)
    {
        var list = new List<string>();
        foreach (var raw in ids ?? Array.Empty<string>())
        {
            var id = raw?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!Steps.Contains(id)) throw new DomainException("onboarding.invalid_step", $"Passo desconhecido: {raw}.");
            if (!list.Contains(id)) list.Add(id);
        }

        // Mantém a ordem do onboarding (estável para a interface).
        return list.OrderBy(x => Steps.ToList().IndexOf(x)).ToList();
    }

    private static IReadOnlyList<string> Split(string csv) =>
        string.IsNullOrEmpty(csv) ? Array.Empty<string>() : csv.Split(',', StringSplitOptions.RemoveEmptyEntries);
}
