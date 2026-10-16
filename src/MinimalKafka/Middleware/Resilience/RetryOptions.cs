namespace MinimalKafka.Middleware.Resilience;

/// <summary>
/// Configuration for retry behavior used by <see cref="RetryMiddleware"/>.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>Total number of attempts including the first one.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Initial delay before the next retry attempt.</summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>The delay is multiplied by this after each failed attempt.</summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>Maximum delay between retry attempts.</summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Return false to fail immediately without retrying (for example for validation errors).</summary>
    public Func<Exception, bool>? ShouldRetry { get; set; }
}
