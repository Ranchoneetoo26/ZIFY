using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Zyft.Application.Common.Text;

/// <summary>Normalização de texto para comparações (sem acento, minúsculo, sem pontuação) — porte de <c>text.js</c>.</summary>
public static partial class TextTools
{
    private static readonly HashSet<string> Stop = new(
        "a o e de da do das dos em para por com no na nos nas um uma voce seu sua que qual quais the of to and in for your is".Split(' '));

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex CamelCase();

    [GeneratedRegex("[_\\-\\[\\]().:;,/\\\\|*?!\"'#]+")]
    private static partial Regex Punctuation();

    [GeneratedRegex("\\s+")]
    private static partial Regex Spaces();

    public static string Norm(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var v = CamelCase().Replace(s, "$1 $2").Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(v.Length);
        foreach (var c in v)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        var r = sb.ToString().ToLowerInvariant();
        r = Punctuation().Replace(r, " ");
        return Spaces().Replace(r, " ").Trim();
    }

    public static IReadOnlyList<string> Tokens(string? s) =>
        Norm(s).Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => t.Length > 1 && !Stop.Contains(t)).ToList();

    public static double Jaccard(string? a, string? b)
    {
        var A = new HashSet<string>(Tokens(a));
        var B = new HashSet<string>(Tokens(b));
        if (A.Count == 0 || B.Count == 0) return 0;
        var inter = A.Count(B.Contains);
        return (double)inter / (A.Count + B.Count - inter);
    }

    /// <summary>Verdadeiro se boa parte dos tokens de <paramref name="evidence"/> aparece em <paramref name="haystack"/>.</summary>
    public static bool Grounded(string? evidence, string? haystack, double ratio = 0.6) => Coverage(evidence, haystack) >= ratio;

    /// <summary>Fração dos tokens de <paramref name="needle"/> presentes em <paramref name="haystack"/>.</summary>
    public static double Coverage(string? needle, string? haystack)
    {
        var ev = Tokens(needle);
        if (ev.Count == 0) return 0;
        var hay = new HashSet<string>(Tokens(haystack));
        return (double)ev.Count(hay.Contains) / ev.Count;
    }

    public static string Truncate(string text, int max)
    {
        if (max <= 0 || text.Length <= max) return text;
        var cut = text[..Math.Max(0, max - 1)];
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > max / 2) cut = cut[..lastSpace];
        return cut.TrimEnd() + "…";
    }

    /// <summary>Extrai só o host de uma URL http(s) (o que vai para auditoria — nunca caminho ou query).
    /// Texto que não é URL http(s) (ex.: "localhost:5080", que o <see cref="Uri"/> leria como esquema "localhost")
    /// devolve null.</summary>
    public static string? HostOf(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u)
        && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps)
        && !string.IsNullOrEmpty(u.Host)
            ? u.Host
            : null;
}
