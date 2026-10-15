using System.Globalization;
using System.Text.RegularExpressions;

namespace Zyft.Application.Common;

/// <summary>Datas "de currículo": MM/AAAA, AAAA-MM, AAAA-MM-DD, DD/MM/AAAA ou AAAA.</summary>
public static partial class Dates
{
    [GeneratedRegex(@"^(\d{1,2})/(\d{1,2})/(\d{4})$")] private static partial Regex Dmy();
    [GeneratedRegex(@"^(\d{1,2})/(\d{4})$")] private static partial Regex My();
    [GeneratedRegex(@"^(\d{4})-(\d{1,2})(?:-(\d{1,2}))?")] private static partial Regex Iso();
    [GeneratedRegex(@"^(\d{4})$")] private static partial Regex Year();

    public static bool TryParseLoose(string? text, out DateOnly date)
    {
        date = default;
        var v = text?.Trim() ?? string.Empty;
        Match m;
        int y, mo = 1, d = 1;
        if ((m = Dmy().Match(v)).Success) { d = int.Parse(m.Groups[1].Value); mo = int.Parse(m.Groups[2].Value); y = int.Parse(m.Groups[3].Value); }
        else if ((m = My().Match(v)).Success) { mo = int.Parse(m.Groups[1].Value); y = int.Parse(m.Groups[2].Value); }
        else if ((m = Iso().Match(v)).Success)
        {
            y = int.Parse(m.Groups[1].Value);
            mo = int.Parse(m.Groups[2].Value);
            if (m.Groups[3].Success) d = int.Parse(m.Groups[3].Value);
        }
        else if ((m = Year().Match(v)).Success) y = int.Parse(m.Groups[1].Value);
        else return false;

        if (y < 1900 || y > 2200 || mo is < 1 or > 12 || d < 1 || d > DateTime.DaysInMonth(y, mo)) return false;
        date = new DateOnly(y, mo, d);
        return true;
    }

    public static string MonthYear(DateOnly d) => d.ToString("MM/yyyy", CultureInfo.InvariantCulture);

    public static string Full(DateOnly d) => d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
