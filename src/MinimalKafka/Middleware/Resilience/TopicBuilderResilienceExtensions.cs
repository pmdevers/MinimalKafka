namespace MinimalKafka.Middleware.Resilience;

/// <summary>
/// Extension methods for adding resilience middleware to a topic consumer pipeline.
/// </summary>
public static class TopicBuilderResilienceExtensions
{
    extension(TopicBuilder builder)
    {
        /// <summary>Retries the rest of the pipeline with exponential backoff.</summary>
        public TopicBuilder WithRetry(Action<RetryOptions>? configure = null)
        {
            var options = new RetryOptions();
            configure?.Invoke(options);
            if (options.MaxAttempts < 1)
            {
                throw new ArgumentException("MaxAttempts must be at least 1.", nameof(configure));
            }
            return builder.Use<RetryMiddleware>(options);
        }

        /// <summary>
        /// Sends messages that fail the rest of the pipeline to <paramref name="deadLetterTopic"/>.
        /// If <paramref name="deadLetterTopic"/> is not specified, defaults to the original topic name with <c>-dlq</c> suffix.
        /// Add it before <see cref="WithRetry"/> so it only fires once retries are exhausted.
        /// </summary>
        public TopicBuilder WithDeadLetter(string? deadLetterTopic = null)
        {
            return builder.Use<DeadLetterMiddleware>(deadLetterTopic ?? builder.Topic + "-dlq");
        }
    }
}