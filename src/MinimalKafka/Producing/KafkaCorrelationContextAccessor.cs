namespace MinimalKafka.Producing;

internal interface IKafkaCorrelationContextAccessor
{
    string? CurrentCorrelationId { get; set; }

    IDisposable BeginScope(string? correlationId);
}

internal sealed class KafkaCorrelationContextAccessor : IKafkaCorrelationContextAccessor
{
    private static readonly AsyncLocal<string?> _currentCorrelationId = new();

    public string? CurrentCorrelationId
    {
        get => _currentCorrelationId.Value;
        set => _currentCorrelationId.Value = value;
    }

    public IDisposable BeginScope(string? correlationId)
    {
        var previous = CurrentCorrelationId;
        CurrentCorrelationId = correlationId;
        return new CorrelationScope(this, previous);
    }

    private sealed class CorrelationScope(KafkaCorrelationContextAccessor accessor, string? previousCorrelationId) : IDisposable
    {
        public void Dispose() => accessor.CurrentCorrelationId = previousCorrelationId;
    }
}
