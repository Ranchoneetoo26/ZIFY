using System.Globalization;
using System.Text;

namespace Zyft.Domain.Common;

/// <summary>Texto que uma pessoa consegue ver. Espaços e caracteres invisíveis (largura zero, BOM, preenchedores) não contam.</summary>
public static class VisibleText
{
    public static bool IsBlank(string? value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        foreach (var rune in value.EnumerateRunes())
            if (!IsInvisible(rune)) return false;
        return true;
    }

    private static bool IsInvisible(Rune r) =>
        Rune.IsWhiteSpace(r)
        || Rune.GetUnicodeCategory(r) is UnicodeCategory.Format or UnicodeCategory.Control or UnicodeCategory.NonSpacingMark
        || r.Value is 0x115F or 0x1160 or 0x2800 or 0x3164 or 0xFFA0; // preenchedores Hangul e braille em branco
}

internal static class Guard
{
    public static string Required(string? value, string code, string message)
    {
        if (VisibleText.IsBlank(value)) throw new DomainException(code, message);
        return value!.Trim();
    }

    public static string? Optional(string? value) => VisibleText.IsBlank(value) ? null : value!.Trim();
}
