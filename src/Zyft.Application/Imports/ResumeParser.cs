using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Zyft.Application.Common;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Profiles;
using static Zyft.Application.Common.Text.TextTools;

namespace Zyft.Application.Imports;

public interface IResumeParser
{
    Task<(ResumeProposal Proposal, bool AiUsed)> ParseAsync(string text, CancellationToken ct = default);
}

/// <summary>
/// Converte o texto do currículo em proposta (porte de <c>importer.js</c>). Com IA: JSON com schema, "copie só o
/// que está escrito". Sem IA: heurística determinística (contatos, links, competências, idiomas). Dados sensíveis
/// não são extraídos em nenhum dos caminhos (RF-03).
/// </summary>
public sealed partial class ResumeParser(ILlmClient llm, ILogger<ResumeParser> logger) : IResumeParser
{
    public async Task<(ResumeProposal Proposal, bool AiUsed)> ParseAsync(string text, CancellationToken ct = default)
    {
        if (llm.IsEnabled)
        {
            try
            {
                return (await ParseWithAiAsync(text, ct), true);
            }
            catch (LlmUnavailableException ex)
            {
                logger.LogWarning("IA indisponível na importação; usando heurística: {Reason}", ex.Message);
            }
        }

        var p = ParseHeuristic(text);
        return (p with { Warnings = p.Warnings.Append("Proposta feita sem IA: confira e complete experiências e formação manualmente.").ToList() }, false);
    }

    private async Task<ResumeProposal> ParseWithAiAsync(string text, CancellationToken ct)
    {
        var str = new { type = "string" };
        object Arr(object props, string[] req) => new { type = "array", items = new { type = "object", properties = props, required = req } };
        var schema = new
        {
            type = "object",
            properties = new
            {
                personal = new
                {
                    type = "object",
                    properties = new { fullName = str, email = str, phone = str, city = str, state = str, linkedin = str, github = str, portfolio = str, headline = str, summary = str, desiredRoles = str },
                    required = new[] { "fullName", "email", "phone", "city", "state" },
                },
                experiences = Arr(new { company = str, role = str, start = str, end = str, current = new { type = "boolean" }, activities = str, tools = str, results = str }, new[] { "company", "role", "start", "end", "current" }),
                education = Arr(new { institution = str, course = str, degree = str, start = str, end = str, status = str }, new[] { "institution", "course" }),
                courses = Arr(new { name = str, institution = str, year = str, hours = str }, new[] { "name" }),
                certifications = Arr(new { name = str, issuer = str, year = str }, new[] { "name" }),
                skills = new { type = "array", items = str },
                languages = Arr(new { language = str, level = str }, new[] { "language", "level" }),
            },
            required = new[] { "personal", "experiences", "education", "courses", "certifications", "skills", "languages" },
        };
        var prompt = $"""
            Converta o currículo abaixo em JSON estruturado.
            Regras: copie apenas o que está escrito; não complete nem invente; campos ausentes ficam "" ou [].
            Datas no formato MM/AAAA quando possível. "current" = true se a experiência é atual.
            Não extraia dados sensíveis (gênero, etnia, deficiência, estado civil, religião, saúde etc.).

            Currículo:
            "{(text.Length > 12000 ? text[..12000] : text)}"
            """;
        var r = await llm.CompleteJsonAsync(new LlmJsonRequest(prompt, schema, LlmPrompts.SystemRules, 0.1, TimeSpan.FromMinutes(5)), ct);

        static string? S(JsonElement e, string name) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && v.GetString()!.Trim().Length > 0 ? v.GetString()!.Trim() : null;
        static IEnumerable<JsonElement> A(JsonElement e, string name) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array ? v.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Object) : Enumerable.Empty<JsonElement>();

        var personal = r.TryGetProperty("personal", out var pe) ? pe : default;
        var city = S(personal, "city");
        var state = S(personal, "state");
        var m = city is null ? null : CityUf().Match(city);
        if (m is { Success: true }) { city = m.Groups[1].Value; state ??= m.Groups[2].Value; }

        var proposal = new ResumeProposal
        {
            Profile = new ProposalProfile
            {
                FullName = TitleCaseIfUpper(S(personal, "fullName")),
                Email = S(personal, "email"),
                Phone = S(personal, "phone"),
                City = city,
                State = state,
                LinkedinUrl = S(personal, "linkedin"),
                GithubUrl = S(personal, "github"),
                PortfolioUrl = S(personal, "portfolio"),
                Headline = S(personal, "headline"),
                Summary = S(personal, "summary"),
                DesiredRoles = S(personal, "desiredRoles"),
            },
            Experiences = A(r, "experiences").Select(e => new ProposalExperience(
                S(e, "company"), S(e, "role"), IsoMonth(S(e, "start")),
                e.TryGetProperty("current", out var c) && c.ValueKind == JsonValueKind.True ? null : IsoMonth(S(e, "end")),
                e.TryGetProperty("current", out var c2) && c2.ValueKind == JsonValueKind.True,
                S(e, "activities"), S(e, "tools"), S(e, "results"))).ToList(),
            Educations = A(r, "education").Select(e => new ProposalEducation(
                S(e, "institution"), S(e, "course"), EnumName<EducationLevel>(S(e, "degree")), EnumName<EducationStatus>(S(e, "status")), IsoMonth(S(e, "start")), IsoMonth(S(e, "end")))).ToList(),
            Courses = A(r, "courses").Select(c => new ProposalCourse(S(c, "name"), S(c, "institution"), Hours(S(c, "hours")), IsoMonth(S(c, "year"))))
                .Concat(A(r, "certifications").Select(c => new ProposalCourse(S(c, "name"), S(c, "issuer"), null, IsoMonth(S(c, "year")), "Certification")))
                .Where(c => c.Name is not null).ToList(),
            Skills = r.TryGetProperty("skills", out var sk) && sk.ValueKind == JsonValueKind.Array
                ? sk.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Select(x => new ProposalSkill(x)).ToList()
                : new List<ProposalSkill>(),
            Languages = A(r, "languages").Select(l => new ProposalLanguage(S(l, "language"), EnumName<LanguageProficiency>(S(l, "level")))).Where(l => l.Name is not null).ToList(),
        };
        return proposal;
    }

    // ------------------------------------------------------------ heurística

    [GeneratedRegex(@"[\w.+-]+@[\w-]+\.[\w.-]+")] private static partial Regex Email();
    [GeneratedRegex(@"(\+?55\s?)?\(?\d{2}\)?\s?9?\d{4}[-\s]?\d{4}")] private static partial Regex Phone();
    [GeneratedRegex(@"(https?://)?(www\.)?linkedin\.com/in/[\w\-%]+/?", RegexOptions.IgnoreCase)] private static partial Regex Linkedin();
    [GeneratedRegex(@"(https?://)?(www\.)?github\.com/[\w\-]+/?", RegexOptions.IgnoreCase)] private static partial Regex Github();
    [GeneratedRegex(@"^(.*?)\s*[-/,]\s*([A-Z]{2})$")] private static partial Regex CityUf();
    [GeneratedRegex(@"\b(habilidades|competencias|skills|conhecimentos|ferramentas)\b")] private static partial Regex SkillsHeader();
    [GeneratedRegex(@"\b(idiomas?|languages?)\b")] private static partial Regex LanguagesHeader();
    [GeneratedRegex(@"\b(experiencia|formacao|educacao|cursos|certificac\w*|objetivo|resumo|contato|projetos)\b")] private static partial Regex OtherHeader();
    [GeneratedRegex(@"^\s*[-•*·▪●]\s*")] private static partial Regex Bullet();

    private static readonly string[] KnownLanguages = { "Inglês", "Espanhol", "Francês", "Alemão", "Italiano", "Japonês", "Mandarim", "Português", "Libras" };

    internal static ResumeProposal ParseHeuristic(string text)
    {
        var lines = text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        var name = lines.FirstOrDefault(l => l.Length is > 3 and < 70 && !l.Contains('@') && !Regex.IsMatch(l, @"\d") && l.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length is >= 2 and <= 6);

        var skills = new List<string>();
        var languages = new List<ProposalLanguage>();
        string? section = null;
        foreach (var l in lines)
        {
            var n = Norm(l);
            if (l.Length < 40 && SkillsHeader().IsMatch(n) && !l.Contains(':')) { section = "skills"; continue; }
            if (l.Length < 40 && LanguagesHeader().IsMatch(n) && !l.Contains(':')) { section = "languages"; continue; }
            if (l.Length < 40 && OtherHeader().IsMatch(n)) { section = null; continue; }

            var content = Bullet().Replace(l, string.Empty);
            if (section == "skills")
                skills.AddRange(content.Split(new[] { ',', ';', '•', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(s => s.Length is > 1 and < 50));

            foreach (var lang in KnownLanguages)
            {
                if (!Norm(content).Contains(Norm(lang)) || languages.Any(x => x.Name == lang)) continue;
                if (section != "languages" && !Regex.IsMatch(Norm(content), @"basico|intermediario|avancado|fluente|nativo")) continue;
                var level = new[] { "Básico", "Intermediário", "Avançado", "Fluente", "Nativo" }.FirstOrDefault(x => Norm(content).Contains(Norm(x)));
                languages.Add(new ProposalLanguage(lang, EnumName<LanguageProficiency>(level)));
            }
        }

        string? Find(Regex re) => re.Match(text) is { Success: true } m ? m.Value.Trim() : null;
        return new ResumeProposal
        {
            Profile = new ProposalProfile
            {
                FullName = TitleCaseIfUpper(name),
                Email = Find(Email())?.ToLowerInvariant(),
                Phone = Find(Phone()),
                LinkedinUrl = Find(Linkedin()),
                GithubUrl = Find(Github()),
            },
            Skills = skills.Distinct(StringComparer.OrdinalIgnoreCase).Take(40).Select(x => new ProposalSkill(x)).ToList(),
            Languages = languages,
        };
    }

    // ------------------------------------------------------------ limpezas determinísticas

    internal static string? IsoMonth(string? text) =>
        text is not null && Dates.TryParseLoose(text, out var d) ? d.ToString("yyyy-MM", CultureInfo.InvariantCulture) : null;

    private static int? Hours(string? text) =>
        text is not null && Regex.Match(text, @"\d+") is { Success: true } m && int.TryParse(m.Value, out var h) && h > 0 ? h : null;

    /// <summary>Rótulo livre ("Graduação", "cursando") → nome do enum ("Undergraduate"); mantém o texto se não reconhecer.</summary>
    internal static string? EnumName<TEnum>(string? text) where TEnum : struct, Enum =>
        DisplayNames.TryParse<TEnum>(text, out var v) ? v.ToString() : text;

    internal static string? TitleCaseIfUpper(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name != name.ToUpperInvariant()) return name;
        var t = CultureInfo.GetCultureInfo("pt-BR").TextInfo.ToTitleCase(name.ToLowerInvariant());
        return Regex.Replace(t, @"\b(Da|De|Do|Das|Dos|E)\b", x => x.Value.ToLowerInvariant());
    }
}
