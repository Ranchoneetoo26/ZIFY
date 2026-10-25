using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Zyft.Application.Common;

namespace Zyft.Api.Infrastructure;

/// <summary>
/// Aceita datas "AAAA-MM-DD", "AAAA-MM" (input type=month), "MM/AAAA" e "DD/MM/AAAA"; escreve sempre "AAAA-MM-DD".
/// </summary>
public sealed class LenientDateOnlyConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        if (Dates.TryParseLoose(s, out var d)) return d;
        throw new JsonException($"Data inválida: \"{s}\". Use AAAA-MM-DD ou AAAA-MM.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}
