using Microsoft.Extensions.Options;

namespace MinimalKafka.Runtime;

/// <summary>Controls whether the MinimalKafka consumer loop is actively processing messages.</summary>
public interface IConsumerRuntimeControl
{
    /// <summary>Gets whether consumers are currently running.</summary>
    bool IsRunning { get; }

    /// <summary>Resumes consumer message processing.</summary>
    void Start();

    /// <summary>Pauses consumer message processing.</summary>
    void Stop();
}

internal sealed class ConsumerRuntimeControl(IOptions<KafkaConsumerOptions> options) : IConsumerRuntimeControl
{
    private int _isRunning = options.Value.StartAtStartup ? 1 : 0;

    public bool IsRunning => Volatile.Read(ref _isRunning) == 1;

    public void Start() => Interlocked.Exchange(ref _isRunning, 1);

    public void Stop() => Interlocked.Exchange(ref _isRunning, 0);
}
