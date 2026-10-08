using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MinimalKafka.Hosting;
using MinimalKafka.Middleware;
using MinimalKafka.Producing;
using MinimalKafka.Runtime;
using MinimalKafka.Serialization;

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
            services.TryAddSingleton<IKafkaSerializerRegistry, MessageSerializerRegistry>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IKafkaSerializer, JsonMessageSerializer>());
            services.TryAddSingleton<ITopicNameConvention, IdentityTopicNameConvention>();
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
        /// <param name="convention">A delegate that maps logical topic names to physical Kafka topic names.</param>
        /// <returns>The current builder.</returns>
        public IMinimalKafkaBuilder WithTopicNameConvention(Func<string, string> convention)
        {
            ArgumentNullException.ThrowIfNull(convention);
            builder.Services.RemoveAll<ITopicNameConvention>();
            builder.Services.AddSingleton<ITopicNameConvention>(new DelegatingTopicNameConvention(convention));
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

internal interface ITopicNameConvention
{
    string Normalize(string topic);
}

internal sealed class IdentityTopicNameConvention : ITopicNameConvention
{
    public string Normalize(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        return topic.ToLowerInvariant();
    }
}

internal sealed class DelegatingTopicNameConvention(Func<string, string> convention) : ITopicNameConvention
{
    public string Normalize(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        var value = convention(topic);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException("The topic naming convention must return a non-empty topic name.")
            : value;
    }
}
