using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Zyft.Application.Common;
using Zyft.Application.Common.Interfaces;
using Zyft.Application.Common.Text;
using Zyft.Application.Profiles;
using Zyft.Domain.Sensitive;
using static Zyft.Application.Common.Text.TextTools;

namespace Zyft.Application.Forms;

/// <summary>Entrada do de-para.</summary>
public sealed record MappingInput
{
    public required IReadOnlyList<FormField> Fields { get; init; }
    public required ProfileFactSet Profile { get; init; }
    public IReadOnlyList<ApprovedAnswerRef> ApprovedAnswers { get; init; } = Array.Empty<ApprovedAnswerRef>();

    /// <summary>Consentimento (c). Sem ele nenhum valor sensível é devolvido.</summary>
    public bool SensitiveConsent { get; init; }

    /// <summary>Valores sensíveis já decifrados — só preenchido pelo chamador quando <see cref="SensitiveConsent"/> é verdadeiro.</summary>
    public IReadOnlyDictionary<SensitiveDataKind, string> SensitiveValues { get; init; } = new Dictionary<SensitiveDataKind, string>();

    public bool UseAi { get; init; } = true;
}

public interface IFieldMapper
{
    Task<MappingResult> MapAsync(MappingInput input, CancellationToken ct = default);
}

/// <summary>
/// De-para entre campos da página e o Perfil Mestre (RF-12; ADR-0004, ADR-0006). Porte de <c>mapper.js</c>:
/// (1) regras determinísticas PT/EN; (2) IA só aponta a CHAVE do perfil para o que sobrou;
/// (3) validação: a chave existe, o valor vem do perfil e a opção de select existe na página.
/// Campos de senha, CAPTCHA, arquivo, declarações e sensíveis são classificados ANTES da IA e nunca recebem valor
/// automaticamente (RF-18).
/// </summary>
public sealed partial class FieldMapper(ILlmClient llm, ILogger<FieldMapper> logger) : IFieldMapper
{
    private sealed record Match(string? Key, string? Group, string? Sub);

    private sealed class Entry(FormField field, Match? match)
    {
        public FormField Field { get; } = field;
        public Match? Rule { get; } = match;
        public int Slot { get; set; }
    }

    public async Task<MappingResult> MapAsync(MappingInput input, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var facts = input.Profile;
        var output = new Dictionary<string, FieldSuggestion>();
        var pending = new List<FormField>();

        var entries = input.Fields.Select(f => new Entry(f, RuleMatch(f))).ToList();
        AssignSlots(entries);
        var slotMap = AlignSlots(entries, facts);
        var hasLastName = entries.Any(e => e.Rule?.Key == "personal.lastName");

        foreach (var entry in entries)
        {
            var f = entry.Field;
            var text = FieldText(f);

            // ---- Campos que exigem ação humana (RF-18): classificados antes de qualquer regra ou IA ----
            if (FieldRules.IgnoredTypes.Contains(f.Type ?? string.Empty) || (FieldRules.PasswordLike.IsMatch(text) && !IsSelectLike(f)))
            {
                output[f.Id] = Blocked(f, SuggestionKinds.Manual, IsPassword(f, text)
                    ? "Campo de senha: o Zyft nunca lê nem preenche senhas."
                    : "Campo de controle da página: ignorado.");
                continue;
            }

            if (FieldRules.Captcha.IsMatch(text))
            {
                output[f.Id] = Blocked(f, SuggestionKinds.Manual, "Verificação de segurança: sempre feita por você.");
                continue;
            }

            if (string.Equals(f.Type, "file", StringComparison.OrdinalIgnoreCase))
            {
                output[f.Id] = Blocked(f, SuggestionKinds.Manual, "Anexe o arquivo manualmente.");
                continue;
            }

            if (FieldRules.Sensitive.IsMatch(text))
            {
                output[f.Id] = SensitiveSuggestion(f, text, input);
                continue;
            }

            if (string.Equals(f.Type, "checkbox", StringComparison.OrdinalIgnoreCase) && (FieldRules.Declaration.IsMatch(text) || entry.Rule is null))
            {
                output[f.Id] = Blocked(f, SuggestionKinds.Declaration, "Declarações e termos são sempre decididos por você.");
                continue;
            }

            // ---- Campo personalizado com o MESMO rótulo (a pessoa criou exatamente este campo) ----
            if (MatchCustom(f, facts, exactOnly: true) is { } exactCustom)
            {
                output[f.Id] = ResolveValue(f, exactCustom.Key, exactCustom.Value, SuggestionSource.Rule, exactCustom.Label);
                continue;
            }

            // ---- Regras determinísticas ----
            string? key = null;
            if (entry.Rule?.Group is { } group)
            {
                var idx = slotMap.TryGetValue(group, out var map) && map.TryGetValue(entry.Slot, out var mapped) ? mapped : entry.Slot;
                key = $"{group}[{idx}].{entry.Rule.Sub}";
            }
            else if (entry.Rule?.Key is { } k)
            {
                key = k == "personal.fullName" && hasLastName ? "personal.firstName" : k;
            }

            if (key is null)
            {
                // Campo personalizado com rótulo parecido (chave custom:<id>).
                if (MatchCustom(f, facts, exactOnly: false) is { } custom)
                {
                    output[f.Id] = ResolveValue(f, custom.Key, custom.Value, SuggestionSource.Rule, custom.Label);
                    continue;
                }

                pending.Add(f);
                continue;
            }

            var value = facts.Get(key)?.Value;
            // "Atribuições e realizações" num único campo: junta atividades + resultados.
            if (value is not null && key.EndsWith(".activities", StringComparison.Ordinal) && FieldRules.AchievementsHint.IsMatch(text))
            {
                var res = facts.Get(key.Replace(".activities", ".results", StringComparison.Ordinal))?.Value;
                if (res is not null) value = $"{value}\n{res}".Trim();
            }

            if (value is null && (IsFreeText(f) || key == "objective.summary"))
            {
                pending.Add(f);
                continue;
            }

            if (value is null)
            {
                output[f.Id] = Missing(f, key, $"Sem \"{ProfileFactSet.LabelOf(key)}\" no Perfil Mestre.");
                continue;
            }

            output[f.Id] = ResolveValue(f, key, value, SuggestionSource.Rule);
        }

        // ---- Respostas aprovadas para perguntas abertas (RF-06) ----
        var stillPending = new List<FormField>();
        foreach (var f in pending)
        {
            var approved = IsFreeText(f) ? BestApprovedAnswer(f, input.ApprovedAnswers) : null;
            if (approved is not null)
            {
                output[f.Id] = new FieldSuggestion
                {
                    FieldId = f.Id,
                    Label = LabelOf(f),
                    Kind = SuggestionKinds.Approved,
                    State = SuggestionState.Ready,
                    Source = SuggestionSource.Answer,
                    Value = f.MaxLength is > 0 ? Truncate(approved.Text, f.MaxLength.Value) : approved.Text,
                    Selected = false, // reuso sempre passa por revisão
                    Note = $"Resposta aprovada anteriormente para \"{approved.Question}\". Revise antes de usar.",
                };
            }
            else
            {
                stillPending.Add(f);
            }
        }

        // ---- IA aponta a chave (só para o que sobrou) ----
        var aiUsed = false;
        if (stillPending.Count > 0 && input.UseAi && llm.IsEnabled)
        {
            aiUsed = await AiResolveAsync(stillPending, facts, output, ct);
        }
        else
        {
            foreach (var f in stillPending) output[f.Id] = Unresolved(f, aiAvailable: false);
        }

        var ordered = input.Fields.Where(f => output.ContainsKey(f.Id)).Select(f => output[f.Id]).ToList();
        var stats = new MappingStats(
            Fields: ordered.Count,
            ByRules: ordered.Count(m => m.Source == SuggestionSource.Rule),
            ByAi: ordered.Count(m => m.Source == SuggestionSource.Ai),
            ByAnswers: ordered.Count(m => m.Source == SuggestionSource.Answer),
            Ready: ordered.Count(m => m.State == SuggestionState.Ready),
            Ambiguous: ordered.Count(m => m.State == SuggestionState.Ambiguous),
            Missing: ordered.Count(m => m.State == SuggestionState.Missing),
            Blocked: ordered.Count(m => m.State == SuggestionState.Blocked),
            Divergent: ordered.Count(m => m.State == SuggestionState.Divergent));
        return new MappingResult(ordered, stats, aiUsed, sw.ElapsedMilliseconds);
    }

    // ---------------------------------------------------------------- regras

    internal static string FieldText(FormField f) =>
        Norm(string.Join(' ', new[] { f.Label, f.Placeholder, f.AriaLabel, f.Name, f.HtmlId }.Where(s => !string.IsNullOrWhiteSpace(s))));

    private static string LabelOf(FormField f) =>
        new[] { f.Label, f.Placeholder, f.AriaLabel, f.Name, f.HtmlId }.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "(sem rótulo)";

    [GeneratedRegex(@"\[(\d+)\]|[_.\-](\d+)(?:[_.\-]|$)")]
    private static partial Regex NameIndex();

    [GeneratedRegex(@"\b(\d+)\b")]
    private static partial Regex SectionIndex();

    /// <summary>Índice explícito do item repetido: name="exp[1][empresa]", "exp_1_empresa" ou seção "Experiência 2".</summary>
    internal static int? ExplicitIndex(FormField f)
    {
        var m = NameIndex().Match(f.Name ?? string.Empty);
        if (m.Success) return int.Parse(m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value);
        var s = SectionIndex().Match(Norm(f.Section));
        return s.Success ? int.Parse(s.Groups[1].Value) : null;
    }

    private static Match? RuleMatch(FormField f)
    {
        var text = FieldText(f);
        var section = Norm(f.Section);
        foreach (var (key, re) in FieldRules.TopRules)
            if (!FieldRules.GenericTop.Contains(key) && re.IsMatch(text)) return new Match(key, null, null);

        foreach (var g in FieldRules.Groups)
        {
            if (!g.Section.IsMatch(section) && !g.Unique.IsMatch(text)) continue;
            var label = Norm(string.Join(' ', new[] { f.Label, f.Placeholder, f.AriaLabel }.Where(s => !string.IsNullOrWhiteSpace(s))));
            if (label.Length == 0) label = text;
            foreach (var (sub, re) in g.Fields)
                if (re.IsMatch(label) || re.IsMatch(text)) return new Match(null, g.Name, sub);
        }

        foreach (var (key, re) in FieldRules.TopRules)
            if (FieldRules.GenericTop.Contains(key) && re.IsMatch(text)) return new Match(key, null, null);
        return null;
    }

    /// <summary>Atribui o "slot" (bloco) de cada campo repetido: índice explícito (base 0 ou 1 detectada) ou contagem por subcampo.</summary>
    private static void AssignSlots(List<Entry> entries)
    {
        var minIdx = new Dictionary<string, int>();
        foreach (var e in entries.Where(e => e.Rule?.Group is not null))
        {
            if (ExplicitIndex(e.Field) is { } i)
                minIdx[e.Rule!.Group!] = Math.Min(minIdx.GetValueOrDefault(e.Rule.Group!, i), i);
        }

        var counters = new Dictionary<string, int>();
        foreach (var e in entries.Where(e => e.Rule?.Group is not null))
        {
            if (ExplicitIndex(e.Field) is { } idx)
            {
                e.Slot = idx - minIdx.GetValueOrDefault(e.Rule!.Group!, 0);
            }
            else
            {
                var c = $"{e.Rule!.Group}.{e.Rule.Sub}";
                counters[c] = counters.GetValueOrDefault(c, -1) + 1;
                e.Slot = counters[c];
            }
        }
    }

    /// <summary>
    /// Alinha blocos repetidos da página com itens do perfil pela organização/instituição. Se o portal já tem
    /// "Empresa X" no bloco 1 e o perfil tem "Empresa X" no item 2, o bloco 1 compara com o item 2. Blocos com
    /// itens que não existem no perfil recebem índices novos (sem valor), e blocos vazios recebem os itens
    /// restantes do perfil na ordem.
    /// </summary>
    private static Dictionary<string, Dictionary<int, int>> AlignSlots(List<Entry> entries, ProfileFactSet facts)
    {
        var result = new Dictionary<string, Dictionary<int, int>>();
        foreach (var g in FieldRules.Groups)
        {
            var groupEntries = entries.Where(e => e.Rule?.Group == g.Name).ToList();
            var slots = groupEntries.Select(e => e.Slot).Distinct().OrderBy(s => s).ToList();
            if (slots.Count == 0) continue;

            var items = new List<string?>();
            for (var i = 0; ; i++)
            {
                var prefix = $"{g.Name}[{i}].";
                if (!facts.Facts.Any(x => x.Key.StartsWith(prefix, StringComparison.Ordinal))) break;
                items.Add(facts.Get(prefix + g.IdentityField)?.Value);
            }

            var used = new HashSet<int>();
            var map = new Dictionary<int, int>();
            var extra = items.Count;
            foreach (var s in slots)
            {
                var cur = groupEntries.FirstOrDefault(e => e.Slot == s && e.Rule!.Sub == g.IdentityField)?.Field.CurrentValue;
                if (string.IsNullOrWhiteSpace(cur)) continue;
                var found = -1;
                for (var i = 0; i < items.Count; i++)
                {
                    if (used.Contains(i) || items[i] is null) continue;
                    if (Norm(items[i]) == Norm(cur) || Jaccard(items[i], cur) >= 0.6) { found = i; break; }
                }

                if (found >= 0) { map[s] = found; used.Add(found); }
                else map[s] = extra++;
            }

            var next = 0;
            foreach (var s in slots)
            {
                if (map.ContainsKey(s)) continue;
                while (used.Contains(next)) next++;
                map[s] = next < items.Count ? next : extra++;
                used.Add(map[s]);
            }

            result[g.Name] = map;
        }

        return result;
    }

    // ---------------------------------------------------------------- valores

    private static bool HasOptions(FormField f) =>
        string.Equals(f.Tag, "select", StringComparison.OrdinalIgnoreCase) || string.Equals(f.Type, "radio", StringComparison.OrdinalIgnoreCase)
        || string.Equals(f.Type, "select", StringComparison.OrdinalIgnoreCase);

    private static bool IsSelectLike(FormField f) => HasOptions(f) || string.Equals(f.Type, "checkbox", StringComparison.OrdinalIgnoreCase);

    private static bool IsPassword(FormField f, string text) =>
        string.Equals(f.Type, "password", StringComparison.OrdinalIgnoreCase) || FieldRules.PasswordLike.IsMatch(text);

    internal static bool IsFreeText(FormField f) =>
        string.Equals(f.Tag, "textarea", StringComparison.OrdinalIgnoreCase)
        || string.Equals(f.Type, "textarea", StringComparison.OrdinalIgnoreCase)
        || (string.Equals(f.Type ?? "text", "text", StringComparison.OrdinalIgnoreCase) && (f.Label ?? string.Empty).TrimEnd().EndsWith('?'));

    /// <summary>Escolhe a opção do select/radio que corresponde ao valor do perfil (porte de <c>pickOption</c>).</summary>
    internal static FieldOption? PickOption(IReadOnlyList<FieldOption>? options, string? value)
    {
        var v = Norm(value);
        if (v.Length == 0 || options is null || options.Count == 0) return null;
        var opts = options.Where(o => Norm(o.Text ?? o.Value).Length > 0 && !FieldRules.Placeholder.IsMatch(Norm(o.Text))).ToList();

        var exact = opts.FirstOrDefault(o => Norm(o.Text) == v || Norm(o.Value) == v);
        if (exact is not null) return exact;

        var contained = opts
            .Where(o =>
            {
                var t = Norm(o.Text);
                return t.Length > 1 && (Regex.IsMatch(v, $@"\b{Regex.Escape(t)}\b") || v.Contains(t, StringComparison.Ordinal) || t.Contains(v, StringComparison.Ordinal));
            })
            .OrderByDescending(o => Norm(o.Text).Length)
            .FirstOrDefault();
        if (contained is not null) return contained;

        if (FieldRules.Yes.IsMatch(v)) return opts.FirstOrDefault(o => FieldRules.Yes.IsMatch(Norm(o.Text)));
        if (FieldRules.No.IsMatch(v)) return opts.FirstOrDefault(o => FieldRules.No.IsMatch(Norm(o.Text)));

        var best = opts.Select(o => (o, s: Jaccard(o.Text, value))).OrderByDescending(x => x.s).FirstOrDefault();
        return best.o is not null && best.s >= 0.5 ? best.o : null;
    }

    [GeneratedRegex(@"^(\d{1,2})/(\d{1,2})/(\d{4})$")] private static partial Regex DmyRe();
    [GeneratedRegex(@"[^\d,]")] private static partial Regex NonNumeric();

    /// <summary>Adapta o valor do perfil ao tipo do input (datas, número, limite de caracteres).</summary>
    internal static string? AdaptValue(string value, FormField f)
    {
        var v = value.Trim();
        var type = (f.Type ?? string.Empty).ToLowerInvariant();
        if (type is "date" or "month")
        {
            if (!Dates.TryParseLoose(v, out var d)) return null;
            return type == "month" ? d.ToString("yyyy-MM") : d.ToString("yyyy-MM-dd");
        }

        if (type == "number")
        {
            // "R$ 2.500,00" -> "2500"; "40h" -> "40"
            var digits = NonNumeric().Replace(v, string.Empty).Split(',')[0];
            return digits.Length == 0 ? null : digits;
        }

        if (f.MaxLength is > 0 && v.Length > f.MaxLength) v = v[..f.MaxLength.Value];
        return v;
    }

    /// <summary>
    /// Compara o rótulo do campo com os rótulos dos campos personalizados (<c>custom:&lt;id&gt;</c>): igual (sem acento e
    /// pontuação) ou, se <paramref name="exactOnly"/> for falso, parecido (Jaccard ≥ 0,6 ou todos os termos presentes).
    /// </summary>
    internal static ProfileFact? MatchCustom(FormField f, ProfileFactSet facts, bool exactOnly)
    {
        var label = Norm(LabelOf(f));
        if (label.Length == 0) return null;
        ProfileFact? best = null;
        var bestScore = 0.0;
        foreach (var fact in facts.Facts)
        {
            if (!fact.Key.StartsWith("custom:", StringComparison.Ordinal)) continue;
            var cl = Norm(fact.Label);
            if (cl == label) return fact;
            if (exactOnly) continue;
            var score = Math.Max(Jaccard(fact.Label, label), Tokens(fact.Label).Count >= 2 ? Coverage(fact.Label, label) : 0);
            if (score >= 0.6 && score > bestScore) { best = fact; bestScore = score; }
        }

        return best;
    }

    private static FieldSuggestion ResolveValue(FormField f, string key, string value, SuggestionSource source, string? profileLabel = null)
    {
        var label = profileLabel ?? ProfileFactSet.LabelOf(key);
        if (HasOptions(f))
        {
            var opt = PickOption(f.Options, value);
            if (opt is null)
            {
                return new FieldSuggestion
                {
                    FieldId = f.Id, Label = LabelOf(f), Kind = SuggestionKinds.Profile, State = SuggestionState.Ambiguous, Source = source,
                    ProfileKey = key, ProfileLabel = label, ProfileValue = value, Selected = false,
                    Note = $"O perfil diz \"{value}\", mas nenhuma opção corresponde com segurança. Escolha manualmente.",
                };
            }

            var confidence = Norm(opt.Text) == Norm(value) ? "high" : "medium";
            return WithDivergence(f, new FieldSuggestion
            {
                FieldId = f.Id, Label = LabelOf(f), Kind = SuggestionKinds.Profile, State = SuggestionState.Ready, Source = source,
                ProfileKey = key, ProfileLabel = label, Value = opt.Value, OptionText = opt.Text, Confidence = confidence, Selected = true,
            }, value);
        }

        if (string.Equals(f.Type, "checkbox", StringComparison.OrdinalIgnoreCase))
        {
            return new FieldSuggestion
            {
                FieldId = f.Id, Label = LabelOf(f), Kind = SuggestionKinds.Profile, State = SuggestionState.Ready, Source = source,
                ProfileKey = key, ProfileLabel = label, Value = FieldRules.Yes.IsMatch(Norm(value)) ? "true" : "false", Confidence = "medium", Selected = true,
            };
        }

        var adapted = AdaptValue(value, f);
        if (adapted is null)
        {
            return new FieldSuggestion
            {
                FieldId = f.Id, Label = LabelOf(f), Kind = SuggestionKinds.Profile, State = SuggestionState.Missing, Source = source,
                ProfileKey = key, ProfileLabel = label, Selected = false,
                Note = $"O valor do perfil não cabe neste campo ({f.Type}). Deixe em branco ou ajuste.",
            };
        }

        return WithDivergence(f, new FieldSuggestion
        {
            FieldId = f.Id, Label = LabelOf(f), Kind = SuggestionKinds.Profile, State = SuggestionState.Ready, Source = source,
            ProfileKey = key, ProfileLabel = label, Value = adapted, Confidence = source == SuggestionSource.Rule ? "high" : "medium", Selected = true,
        }, value);
    }

    /// <summary>RF-16: se o portal já tem valor diferente, o padrão é manter o do portal (não vem marcado).</summary>
    private static FieldSuggestion WithDivergence(FormField f, FieldSuggestion s, string profileValue)
    {
        if (string.IsNullOrWhiteSpace(f.CurrentValue) || s.Value is null) return s;
        var current = Norm(f.CurrentValue);
        if (current == Norm(s.Value) || current == Norm(s.OptionText) || current == Norm(profileValue))
            return s with { Selected = false, Note = "O portal já tem este mesmo valor." };
        return s with
        {
            State = SuggestionState.Divergent,
            Selected = false,
            ProfileValue = profileValue,
            Note = "O portal já tem outro valor. Por padrão mantemos o do portal; escolha \"usar o perfil\" se preferir.",
        };
    }

    private static FieldSuggestion SensitiveSuggestion(FormField f, string text, MappingInput input)
    {
        if (!input.SensitiveConsent)
        {
            return Blocked(f, SuggestionKinds.Sensitive, "Dado sensível: preencha manualmente (uso não autorizado nas configurações).");
        }

        var kind = SensitiveKindOf(text);
        string? value = kind is { } k && input.SensitiveValues.TryGetValue(k, out var sv) ? sv : null;
        string? optionText = null;
        if (value is not null && HasOptions(f))
        {
            var opt = PickOption(f.Options, value);
            value = opt?.Value;
            optionText = opt?.Text;
        }

        return new FieldSuggestion
        {
            FieldId = f.Id, Label = LabelOf(f), Kind = SuggestionKinds.Sensitive, State = SuggestionState.Blocked, Source = value is null ? SuggestionSource.None : SuggestionSource.Rule,
            Value = value, OptionText = optionText, Selected = false,
            Note = value is null
                ? "Dado sensível: não há valor correspondente cadastrado. Preencha manualmente."
                : "Dado sensível: só é preenchido se você marcar este campo.",
        };
    }

    internal static SensitiveDataKind? SensitiveKindOf(string normText)
    {
        if (Regex.IsMatch(normText, @"orientacao sexual|sexual orientation")) return SensitiveDataKind.SexualOrientation;
        if (Regex.IsMatch(normText, @"nome social")) return SensitiveDataKind.SocialName;
        if (Regex.IsMatch(normText, @"genero|\bsexo\b|gender")) return SensitiveDataKind.Gender;
        if (Regex.IsMatch(normText, @"raca|etnia|\bcor\b|ethnicity|\brace\b")) return SensitiveDataKind.Ethnicity;
        if (Regex.IsMatch(normText, @"deficiencia|\bpcd\b|disability")) return SensitiveDataKind.Disability;
        if (Regex.IsMatch(normText, @"religi")) return SensitiveDataKind.Religion;
        if (Regex.IsMatch(normText, @"saude|doenca|laudo|health")) return SensitiveDataKind.Health;
        return null;
    }

    private static FieldSuggestion Blocked(FormField f, string kind, string note) => new()
    {
        FieldId = f.Id, Label = LabelOf(f), Kind = kind, State = SuggestionState.Blocked, Source = SuggestionSource.None, Selected = false, Note = note,
    };

    private static FieldSuggestion Missing(FormField f, string? key, string note) => new()
    {
        FieldId = f.Id, Label = LabelOf(f), Kind = SuggestionKinds.Missing, State = SuggestionState.Missing, Source = SuggestionSource.None,
        ProfileKey = key, ProfileLabel = key is null ? null : ProfileFactSet.LabelOf(key), Selected = false, Note = note,
    };

    private static FieldSuggestion Unresolved(FormField f, bool aiAvailable, string? note = null)
    {
        if (IsFreeText(f))
        {
            return new FieldSuggestion
            {
                FieldId = f.Id, Label = LabelOf(f), Kind = SuggestionKinds.OpenQuestion, State = SuggestionState.Missing, Source = SuggestionSource.None, Selected = false,
                Note = note ?? "Pergunta aberta: peça uma sugestão de texto (usa só fatos do seu perfil) ou responda você.",
            };
        }

        return new FieldSuggestion
        {
            FieldId = f.Id, Label = LabelOf(f), Kind = SuggestionKinds.Unknown, State = SuggestionState.Missing, Source = SuggestionSource.None, Selected = false,
            Note = note ?? (aiAvailable ? "Informação não está no Perfil Mestre — responda manualmente." : "Sem correspondência no perfil (IA indisponível: só regras)."),
        };
    }

    private static ApprovedAnswerRef? BestApprovedAnswer(FormField f, IReadOnlyList<ApprovedAnswerRef> answers)
    {
        var question = LabelOf(f);
        var best = answers.Select(a => (a, s: Jaccard(a.Question, question))).OrderByDescending(x => x.s).FirstOrDefault();
        return best.a is not null && best.s >= 0.6 ? best.a : null;
    }

    // ---------------------------------------------------------------- IA

    private async Task<bool> AiResolveAsync(List<FormField> pending, ProfileFactSet facts, Dictionary<string, FieldSuggestion> output, CancellationToken ct)
    {
        var ids = pending.Select(f => f.Id).ToArray();
        var schema = new
        {
            type = "object",
            properties = new
            {
                mappings = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            fieldId = new { type = "string", @enum = ids },
                            action = new { type = "string", @enum = new[] { "fill", "generate", "ask" } },
                            profileKey = new { type = new[] { "string", "null" } },
                        },
                        required = new[] { "fieldId", "action", "profileKey" },
                    },
                },
            },
            required = new[] { "mappings" },
        };

        var fieldsDesc = pending.Select(f => new
        {
            fieldId = f.Id,
            rotulo = LabelOf(f),
            tipo = HasOptions(f) ? "select" : f.Type ?? f.Tag,
            secao = f.Section,
            opcoes = f.Options?.Take(15).Select(o => o.Text).ToArray(),
        });

        var prompt = $"""
            Faça o de-para entre campos de um formulário de vaga e as chaves do Perfil Mestre.

            Chaves disponíveis no perfil (chave = rótulo: valor):
            {string.Join('\n', facts.Facts.Select(x => $"{x.Key} = {x.Label}: {(x.Value.Length > 80 ? x.Value[..80] : x.Value)}"))}

            Campos do formulário:
            {JsonSerializer.Serialize(fieldsDesc, LlmPrompts.JsonOptions)}

            Para cada campo, escolha uma ação:
            - "fill": o campo corresponde exatamente a UMA chave do perfil (informe profileKey).
            - "generate": pergunta aberta / texto livre que pode ser respondido com fatos do perfil (profileKey null).
            - "ask": a informação não está no perfil ou é uma decisão do candidato (profileKey null).
            Responda em JSON {"{"}"mappings":[...]{"}"} com um item para cada campo. Nunca escreva valores, só chaves.
            """;

        JsonElement response;
        try
        {
            response = await llm.CompleteJsonAsync(new LlmJsonRequest(prompt, schema, LlmPrompts.SystemRules, 0.1, TimeSpan.FromSeconds(60)), ct);
        }
        catch (LlmUnavailableException ex)
        {
            logger.LogWarning("IA indisponível no de-para: {Reason}", ex.Message);
            foreach (var f in pending) output[f.Id] = Unresolved(f, aiAvailable: false);
            return false;
        }

        var byField = new Dictionary<string, (string Action, string? Key)>();
        if (response.ValueKind == JsonValueKind.Object && response.TryGetProperty("mappings", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var m in arr.EnumerateArray())
            {
                if (m.ValueKind != JsonValueKind.Object) continue;
                var fid = m.TryGetProperty("fieldId", out var a) && a.ValueKind == JsonValueKind.String ? a.GetString() : null;
                var act = m.TryGetProperty("action", out var b) && b.ValueKind == JsonValueKind.String ? b.GetString() : null;
                var key = m.TryGetProperty("profileKey", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
                // Qualquer "value" que a IA devolva é ignorado de propósito (RN-02): só a chave interessa.
                if (fid is not null && act is not null) byField.TryAdd(fid, (act, key));
            }
        }

        foreach (var f in pending)
        {
            if (byField.TryGetValue(f.Id, out var m) && m.Action == "fill" && m.Key is not null && facts.Get(m.Key) is { } fact)
            {
                // Validação: a chave existe no perfil; o valor vem do perfil, não da IA.
                output[f.Id] = ResolveValue(f, fact.Key, fact.Value, SuggestionSource.Ai, fact.Label);
            }
            else if (byField.TryGetValue(f.Id, out m) && m.Action == "generate" && !HasOptions(f))
            {
                output[f.Id] = Unresolved(f, true, "Pergunta aberta: peça uma sugestão de texto (usa só fatos do seu perfil) ou responda você.") with { Kind = SuggestionKinds.OpenQuestion };
            }
            else
            {
                output[f.Id] = Unresolved(f, aiAvailable: true);
            }
        }

        return true;
    }
}
