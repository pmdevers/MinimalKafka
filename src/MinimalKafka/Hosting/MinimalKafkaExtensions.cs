using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinimalKafka.Hosting;
using MinimalKafka.Middleware;
using MinimalKafka.Producing;
using MinimalKafka.Runtime;
using MinimalKafka.Serialization;
using System.Text.RegularExpressions;

namespace MinimalKafka;

/// <summary>Extension methods for registering and configuring MinimalKafka services.</summary>
public static class MinimalKafkaExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the Kafka consumer and producer and lets <paramref name="configure"/> customize them.</summary>
        public IServiceCollection AddMinimalKafka(Action<IMinimalKafkaBuilder>? configure = null)
        {
            services.AddOptions<KafkaConsumerOptions>();
            services.AddOptions<SerializationOptions>();
            services.AddOptions<TopicNamingOptions>();

            services.TryAddSingleton<IKafkaSerializerRegistry, MessageSerializerRegistry>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IKafkaSerializer, JsonMessageSerializer>());
            services.TryAddSingleton<ITopicNamingConvention, TopicNamingConvention>();
            services.TryAddSingleton<TopicRegistry>();
            services.TryAddSingleton<IKafkaProducer, KafkaMessageProducer>();
            services.AddHostedService<KafkaConsumerBackgroundService>();

            configure?.Invoke(new MinimalKafkaBuilder(services));
            return services;
        }
    }

    extension(IMinimalKafkaBuilder builder)
    {
        /// <summary>Configures the raw librdkafka settings.</summary>
        public IMinimalKafkaBuilder WithConfiguration(Action<KafkaConsumerOptions> configure)
        {
            builder.Services.Configure(configure);
            return builder;
        }


        /// <summary>Configures <see cref="KafkaConsumerOptions"/> from an <see cref="IConfiguration"/> source.</summary>
        /// <param name="configuration">The configuration section or root containing Kafka settings.</param>
        public IMinimalKafkaBuilder WithConfiguration(IConfiguration configuration)
        {
            builder.Services.Configure<KafkaConsumerOptions>(configuration);
            return builder;
        }

        /// <summary>Sets the consumer auto offset reset behavior.</summary>
        /// <param name="value">The auto offset reset behavior to use when no committed offset exists.</param>
        /// <returns>The current builder.</returns>
        public IMinimalKafkaBuilder WithAutoOffsetReset(AutoOffsetReset value)
        {
            builder.Services.Configure<KafkaConsumerOptions>(x =>
            {
                var config = new ConsumerConfig(x.Consumer)
                {
                    AutoOffsetReset = value
                };
                x.Consumer = config.ToDictionary();
            });
            return builder;
        }

        /// <summary>Sets the Kafka client id used by the consumer/producer client configuration.</summary>
        /// <param name="value">The client id to send to the Kafka broker.</param>
        /// <returns>The current builder.</returns>
        public IMinimalKafkaBuilder WithClientId(string value)
        {
            builder.Services.Configure<KafkaConsumerOptions>(x =>
            {
                var config = new ClientConfig(x.Common)
                {
                    ClientId = value
                };
                x.Common = config.ToDictionary();
            });
            return builder;
        }

        /// <summary>Sets the Kafka client id used by the consumer/producer client configuration.</summary>
        /// <param name="value">The client id to send to the Kafka broker.</param>
        /// <returns>The current builder.</returns>
        public IMinimalKafkaBuilder WithGroupId(string value)
        {
            builder.Services.Configure<KafkaConsumerOptions>(x =>
            {
                var config = new ConsumerConfig(x.Consumer)
                {
                    GroupId = value
                };
                x.Consumer = config.ToDictionary();
            });
            return builder;
        }

        /// <summary>Sets the topic naming convention used for both consumer registrations and producer calls.</summary>
        /// <param name="configure"></param>
        /// <returns>The current builder.</returns>
        public IMinimalKafkaBuilder WithTopicNaming(Action<TopicNamingOptions>? configure = null)
        {
            if (configure is not null)
            {
                builder.Services.Configure(configure);
            }
            return builder;
        }

        private IMinimalKafkaBuilder ConfigureHandlers(Action<KafkaConsumerHandlers> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configure);

            builder.Services.Configure<KafkaConsumerOptions>(options => configure(options.Handlers));
            return builder;
        }

        /// <summary>Sets the Kafka statistics handler for the consumer.</summary>
        /// <param name="statisticsHandler">The delegate invoked when Kafka emits consumer statistics.</param>
        /// <returns>The current builder.</returns>
        public IMinimalKafkaBuilder WithStatisticsHandler(Action<IConsumer<byte[], byte[]>, string> statisticsHandler) =>
            ConfigureHandlers(builder, handlers => handlers.StatisticsHandler = statisticsHandler);

        /// <summary>Sets the Kafka error handler for the consumer.</summary>
        /// <param name="errorHandler">The delegate invoked when the consumer reports an error.</param>
        /// <returns>The current builder.</returns>
        public IMinimalKafkaBuilder WithErrorHandler(Action<IConsumer<byte[], byte[]>, Error> errorHandler) =>
            ConfigureHandlers(builder, handlers => handlers.ErrorHandler = errorHandler);

        /// <summary>Sets the Kafka log handler for the consumer.</summary>
        /// <param name="logHandler">The delegate invoked when the consumer emits a log message.</param>
        /// <returns>The current builder.</returns>
        public IMinimalKafkaBuilder WithLogHandler(Action<IConsumer<byte[], byte[]>, LogMessage> logHandler) =>
            ConfigureHandlers(builder, handlers => handlers.LogHandler = logHandler);

        /// <summary>Sets the partition assigned handler for the consumer.</summary>
        /// <param name="partitionsAssignedHandler">The delegate invoked when partitions are assigned to the consumer.</param>
        /// <returns>The current builder.</returns>
        public IMinimalKafkaBuilder WithPartitionsAssignedHandler(Action<IConsumer<byte[], byte[]>, List<TopicPartition>> partitionsAssignedHandler) =>
            ConfigureHandlers(builder, handlers => handlers.PartitionsAssignedHandler = partitionsAssignedHandler);

        /// <summary>Sets the partition lost handler for the consumer.</summary>
        /// <param name="partitionsLostHandler">The delegate invoked when assigned partitions are lost.</param>
        /// <returns>The current builder.</returns>
        public IMinimalKafkaBuilder WithPartitionsLostHandler(Action<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>> partitionsLostHandler) =>
            ConfigureHandlers(builder, handlers => handlers.PartitionsLostHandler = partitionsLostHandler);

        /// <summary>Sets the partition revoked handler for the consumer.</summary>
        /// <param name="partitionsRevokedHandler">The delegate invoked when assigned partitions are revoked.</param>
        /// <returns>The current builder.</returns>
        public IMinimalKafkaBuilder WithPartitionsRevokedHandler(Action<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>> partitionsRevokedHandler) =>
            ConfigureHandlers(builder, handlers => handlers.PartitionsRevokedHandler = partitionsRevokedHandler);

        /// <summary>Sets the OAuth bearer token refresh handler for the consumer.</summary>
        /// <param name="oAuthBearerTokenRefreshHandler">The delegate invoked when the consumer requires an OAuth bearer token refresh.</param>
        /// <returns>The current builder.</returns>
        public IMinimalKafkaBuilder WithOAuthBearerTokenRefreshHandler(Action<IConsumer<byte[], byte[]>, string> oAuthBearerTokenRefreshHandler) =>
            ConfigureHandlers(builder, handlers => handlers.OAuthBearerTokenRefreshHandler = oAuthBearerTokenRefreshHandler);

        /// <summary>Adds a global consumer middleware. Middleware runs in the order it is added.</summary>
        public IMinimalKafkaBuilder UseConsumerMiddleware<TMiddleware>()
            where TMiddleware : class, IConsumerMiddleware
        {
            builder.Services.AddScoped<IConsumerMiddleware, TMiddleware>();
            return builder;
        }

        /// <summary>Adds a producer middleware. Middleware runs in the order it is added.</summary>
        public IMinimalKafkaBuilder UseProducerMiddleware<TMiddleware>()
            where TMiddleware : class, IProducerMiddleware
        {
            builder.Services.AddScoped<IProducerMiddleware, TMiddleware>();
            return builder;
        }
    }
}

internal interface ITopicNamingConvention
{
    string Apply(string topic);
}

/// <summary>Options that control how logical topic names are converted to physical Kafka topic names.</summary>
public sealed class TopicNamingOptions
{
    /// <summary>Prepended to every topic, for example "prod." or "team-orders.".</summary>
    public string? Prefix { get; set; }

    /// <summary>Appended to every topic.</summary>
    public string? Suffix { get; set; }

    /// <summary>Lower-cases the logical name before the prefix and suffix are added.</summary>
    public bool Lowercase { get; set; }

    /// <summary>Replaces <see cref="char"/> separators in the logical name, for example '_' with '.'.</summary>
    public Dictionary<char, char> ReplaceCharacters { get; } = [];

    /// <summary>Runs on the logical name before the prefix and suffix are added.</summary>
    public Func<string, string>? Transform { get; set; }

    /// <summary>Logical names for which no convention is applied, such as topics owned by another team.</summary>
    public HashSet<string> Exclude { get; } = new(StringComparer.Ordinal);

    /// <summary>When set, a resulting name that does not match is rejected. Kafka allows only [a-zA-Z0-9._-].</summary>
    public string ValidationPattern { get; set; } = "^[a-zA-Z0-9._-]{1,249}$";
}

internal sealed class TopicNamingConvention(IOptions<TopicNamingOptions> options) : ITopicNamingConvention
{
    private readonly TopicNamingOptions _options = options.Value;
    private readonly Regex? _validation = string.IsNullOrEmpty(options.Value.ValidationPattern)
        ? null
        : new Regex(options.Value.ValidationPattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public string Apply(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        if (_options.Exclude.Contains(topic))
        {
            return topic;
        }

        var name = topic;
        foreach (var (from, to) in _options.ReplaceCharacters)
        {
            name = name.Replace(from, to);
        }
        if (_options.Lowercase)
        {
            name = name.ToLowerInvariant();
        }
        if (_options.Transform is not null)
        {
            name = _options.Transform(name);
        }
        name = _options.Prefix + name + _options.Suffix;

        if (_validation is not null && !_validation.IsMatch(name))
        {
            throw new InvalidOperationException(
                $"Topic '{topic}' resolves to '{name}', which violates the naming convention.");
        }
        return name;
    }
}


internal sealed class DelegatingTopicNameConvention(Func<string, string> convention) : ITopicNamingConvention
{
    public string Apply(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        var value = convention(topic);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException("The topic naming convention must return a non-empty topic name.")
            : value;
    }
}
