using Confluent.Kafka;
using System.Collections;
using System.Text;

namespace MinimalKafka;

/// <summary>Represents Kafka message headers using string values.</summary>
public sealed class KafkaHeaders : IEnumerable<KeyValuePair<string, string?>>
{
    private readonly Headers _headers;

    /// <summary>Creates an empty header collection.</summary>
    public KafkaHeaders()
        : this(Enumerable.Empty<KeyValuePair<string, string?>>())
    {
    }

    /// <summary>Creates a header collection initialized from key/value pairs.</summary>
    public KafkaHeaders(IEnumerable<KeyValuePair<string, string?>> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        _headers = [];
        foreach (var (key, value) in values)
        {
            Add(key, value);
        }
    }

    internal KafkaHeaders(Headers headers)
    {
        _headers = headers ?? throw new ArgumentNullException(nameof(headers));
    }

    /// <summary>Gets the number of headers in the collection.</summary>
    public int Count => _headers.Count;

    /// <summary>Gets or sets the last value for a given header key.</summary>
    public string? this[string key]
    {
        get => Get(key);
        set => Set(key, value);
    }

    /// <summary>Adds a header value for <paramref name="key"/>.</summary>
    public void Add(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _headers.Add(key, value is null ? null : Encoding.UTF8.GetBytes(value));
    }

    /// <summary>Sets a single header value for <paramref name="key"/>, replacing existing values.</summary>
    public void Set(string key, string? value)
    {
        Remove(key);
        Add(key, value);
    }

    /// <summary>Gets the last header value for <paramref name="key"/>, or null when not present.</summary>
    public string? Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var bytes = _headers.LastOrDefault(item => item.Key == key)?.GetValueBytes();
        return bytes is null ? null : Encoding.UTF8.GetString(bytes);
    }

    /// <summary>Attempts to get the last header value for <paramref name="key"/>.</summary>
    public bool TryGetValue(string key, out string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var header = _headers.LastOrDefault(item => item.Key == key);
        if (header is null)
        {
            value = null;
            return false;
        }

        var bytes = header.GetValueBytes();
        value = bytes is null ? null : Encoding.UTF8.GetString(bytes);
        return true;
    }

    /// <summary>Checks whether a header with <paramref name="key"/> exists.</summary>
    public bool Contains(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _headers.Any(item => item.Key == key);
    }

    /// <summary>Removes all headers with <paramref name="key"/>.</summary>
    public void Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _headers.Remove(key);
    }

    /// <summary>Returns an enumerator over all headers as key/string-value pairs.</summary>
    public IEnumerator<KeyValuePair<string, string?>> GetEnumerator()
    {
        foreach (var header in _headers)
        {
            var bytes = header.GetValueBytes();
            yield return new KeyValuePair<string, string?>(
                header.Key,
                bytes is null ? null : Encoding.UTF8.GetString(bytes));
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal Headers AsConfluentHeaders() => _headers;

    internal static KafkaHeaders FromConfluent(Headers? headers) => new(headers ?? []);
}
