using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Examples.Domain.Abstraction;

public interface IValueObject<TSelf>
    where TSelf : IValueObject<TSelf>
{
    static abstract bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? formatProvider, [MaybeNullWhen(false)] out TSelf result);
}

public class ValueObjectJsonConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValueObject<>));

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueObjectInterface = typeToConvert.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValueObject<>));
        var valueType = valueObjectInterface.GetGenericArguments()[0];
        var converterType = typeof(ValueObjectConverter<>).MakeGenericType(valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

public class ValueObjectConverter<T> : JsonConverter<T>
    where T : IValueObject<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null)
        {
            throw new JsonException($"Cannot convert null to {typeof(T).Name}.");
        }

        var value = reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetDecimal().ToString(System.Globalization.CultureInfo.InvariantCulture),
            JsonTokenType.True => bool.TrueString,
            JsonTokenType.False => bool.FalseString,
            _ => throw new JsonException($"Token type '{reader.TokenType}' is not supported for {typeof(T).Name}.")
        };

        if (T.TryParse(value, null, out var result))
        {
            return result;
        }

        throw new JsonException($"Invalid {typeof(T).Name} value '{value}'.");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
