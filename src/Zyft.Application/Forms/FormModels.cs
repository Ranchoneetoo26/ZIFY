
namespace Zyft.Application.Forms;

/// <summary>Opção de select/radio como lida na página.</summary>
public sealed record FieldOption(string? Value, string? Text);

/// <summary>Campo de formulário lido pela extensão (RF-11). Nada aqui é enviado ao portal.</summary>
public sealed record FormField
{
    /// <summary>Identificador do campo na leitura atual (gerado pela extensão).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>input, select, textarea…</summary>
    public string? Tag { get; init; }

    /// <summary>text, email, date, month, number, checkbox, radio, file, password…</summary>
    public string? Type { get; init; }

    public string? Name { get; init; }
    public string? HtmlId { get; init; }
    public string? Label { get; init; }
    public string? Placeholder { get; init; }
    public string? AriaLabel { get; init; }

    /// <summary>Título da seção/fieldset em que o campo está (ex.: "Experiência 2").</summary>
    public string? Section { get; init; }

    public bool Required { get; init; }
    public int? MaxLength { get; init; }

    /// <summary>Valor que o portal já tem no campo (RF-16).</summary>
    public string? CurrentValue { get; init; }

    public IReadOnlyList<FieldOption>? Options { get; init; }
}

/// <summary>Estado da sugestão no painel (RF-13).</summary>
public enum SuggestionState { Ready, Ambiguous, Missing, Blocked, Divergent }

/// <summary>Origem do valor proposto.</summary>
public enum SuggestionSource { Rule, Ai, Answer, None }

public static class SuggestionKinds
{
    public const string Profile = "profile";
    public const string Approved = "approved";
    public const string Sensitive = "sensitive";
    public const string Declaration = "declaration";
    public const string Manual = "manual";
    public const string OpenQuestion = "openQuestion";
    public const string Missing = "missing";
    public const string Unknown = "unknown";
}

/// <summary>
/// Sugestão para um campo. <see cref="Value"/> só existe quando o valor vem do perfil, de uma resposta aprovada
/// ou de dado sensível com consentimento (este sempre com <see cref="Selected"/> = false).
/// </summary>
public sealed record FieldSuggestion
{
    public required string FieldId { get; init; }
    public required string Label { get; init; }
    public required string Kind { get; init; }
    public required SuggestionState State { get; init; }
    public required SuggestionSource Source { get; init; }
    public string? ProfileKey { get; init; }
    public string? ProfileLabel { get; init; }
    public string? Value { get; init; }
    public string? OptionText { get; init; }

    /// <summary>Só em <c>ambiguous</c> e <c>divergent</c> (mínimo retorno).</summary>
    public string? ProfileValue { get; init; }

    /// <summary>high | medium.</summary>
    public string? Confidence { get; init; }

    /// <summary>Se vem marcado para preencher. Ambíguos, ausentes, bloqueados e divergentes nunca vêm marcados.</summary>
    public bool Selected { get; init; }

    public string? Note { get; init; }
}

public sealed record MappingStats(int Fields, int ByRules, int ByAi, int ByAnswers, int Ready, int Ambiguous, int Missing, int Blocked, int Divergent);

public sealed record MappingResult(IReadOnlyList<FieldSuggestion> Mappings, MappingStats Stats, bool AiUsed, long ElapsedMs);

/// <summary>Resposta aprovada disponível para reuso (RF-06).</summary>
public sealed record ApprovedAnswerRef(Guid Id, string Question, string Text);
