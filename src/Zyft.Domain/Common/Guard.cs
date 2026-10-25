namespace Zyft.Domain.Common;

internal static class Guard
{
    public static string Required(string? value, string code, string message)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException(code, message);
        return value.Trim();
    }

    public static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
