using System.Text.Json;
using System.Text.Json.Serialization;

namespace HR.Serialization;

internal sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string to parse {nameof(DateOnly)} but received {reader.TokenType}.");
        }

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Cannot parse an empty string to DateOnly.");
        }

        if (DateOnly.TryParse(value, out var result))
        {
            return result;
        }

        if (DateOnly.TryParseExact(value, Format, null, System.Globalization.DateTimeStyles.None, out result))
        {
            return result;
        }

        throw new JsonException($"Invalid date format. Expected '{Format}'.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format));
    }
}
