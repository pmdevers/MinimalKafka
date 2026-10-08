using Examples.Domain.Abstraction;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Examples.Domain;

public class Movie
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Genre Genre { get; set; }
}

[JsonConverter(typeof(ValueObjectJsonConverter))]
public readonly record struct Genre(string Value) : IValueObject<Genre>
{
    public string Value { get; init; }
       = string.IsNullOrWhiteSpace(Value)
       ? throw new ArgumentException("Value cannot be empty.", nameof(Value))
       : Value;

    public static Genre Horror => new(nameof(Horror));
    public static Genre Comedy => new(nameof(Comedy));
    public static Genre Thriller => new(nameof(Thriller));

    public override string ToString()
        => Value;

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? formatProvider, [MaybeNullWhen(false)] out Genre result)
    {
        if (string.IsNullOrEmpty(s))
        {
            result = default;
            return false;
        }
        result = new Genre(s);
        return true;
    }

    public static implicit operator string(Genre g) => g.Value;
    public static implicit operator Genre(string g) => new(g);
}
