using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MinimalKafka.Producing;
using MinimalKafka.Serialization;
using System.Text;

namespace MinimalKafka.Tests.Producing;

public class CorrelationIdPropagationTests
{
    [Fact]
    public void KafkaContext_ShouldExposeCorrelationId_WhenHeaderExists()
    {
        var context = CreateKafkaContext([new Header(CorrelationHeaders.CorrelationIdHeaderName, Encoding.UTF8.GetBytes("corr-123"))]);

        Assert.Equal("corr-123", context.CorrelationId);
    }

    [Fact]
    public void KafkaContext_ShouldExposeNullCorrelationId_WhenHeaderMissing()
    {
        var context = CreateKafkaContext([]);

        Assert.Null(context.CorrelationId);
    }

    [Fact]
    public async Task ProduceAsync_ShouldGenerateCorrelationId_WhenMissingAndNoAmbientValue()
    {
        var (producer, capture, _) = CreateProducer();
        using (producer)
        {
            await producer.ProduceAsync("orders", Array.Empty<byte>(), Array.Empty<byte>(), headers: []);
        }

        var correlationId = CorrelationHeaders.ReadCorrelationId(capture.Headers!);
        Assert.NotNull(correlationId);
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task ProduceAsync_ShouldPreserveExplicitCorrelationId_WhenProvided()
    {
        var (producer, capture, _) = CreateProducer();
        var headers = new Headers
        {
            new(CorrelationHeaders.CorrelationIdHeaderName, Encoding.UTF8.GetBytes("explicit-correlation"))
        };

        using (producer)
        {
            await producer.ProduceAsync("orders", Array.Empty<byte>(), Array.Empty<byte>(), headers: headers);
        }

        Assert.Equal("explicit-correlation", CorrelationHeaders.ReadCorrelationId(capture.Headers!));
    }

    [Fact]
    public async Task ProduceAsync_ShouldInheritAmbientCorrelationId_WhenMissingOnOutgoingHeaders()
    {
        var (producer, capture, accessor) = CreateProducer();

        using (accessor.BeginScope("incoming-correlation"))
        {
            using (producer)
            {
                await producer.ProduceAsync("orders", Array.Empty<byte>(), Array.Empty<byte>(), headers: []);
            }
        }

        Assert.Equal("incoming-correlation", CorrelationHeaders.ReadCorrelationId(capture.Headers!));
    }

    private static KafkaContext CreateKafkaContext(Headers headers)
    {
        return new KafkaContext(
            new ConsumeResult<byte[], byte[]>
            {
                Topic = "orders",
                Message = new Message<byte[], byte[]>
                {
                    Key = [],
                    Value = [],
                    Headers = headers
                }
            },
            Substitute.For<IKafkaProducer>(),
            new ServiceCollection().BuildServiceProvider(),
            CancellationToken.None);
    }

    private static (KafkaMessageProducer Producer, CapturingProducerMiddleware Capture, IKafkaCorrelationContextAccessor Accessor) CreateProducer()
    {
        var capture = new CapturingProducerMiddleware();
        var services = new ServiceCollection();
        services.AddSingleton<IProducerMiddleware>(capture);
        var provider = services.BuildServiceProvider();

        IKafkaSerializerRegistry serializerRegistry = new MessageSerializerRegistry(
            [new JsonMessageSerializer(Options.Create(new SerializationOptions()))],
            Options.Create(new SerializationOptions()));
        ITopicNamingConvention topicNaming = new DelegatingTopicNameConvention(static topic => topic);

        var producerFactory = new TestKafkaProducerFactory();

        var accessor = new KafkaCorrelationContextAccessor();
        var producer = new KafkaMessageProducer(
            producerFactory,
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<KafkaMessageProducer>.Instance,
            serializerRegistry,
            topicNaming,
            accessor);

        return (producer, capture, accessor);
    }

    private sealed class TestKafkaProducerFactory : IKafkaProducerFactory
    {
        public IProducer<byte[], byte[]> Create()
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092"
            };

            return new ProducerBuilder<byte[], byte[]>(config).Build();
        }
    }

    private sealed class CapturingProducerMiddleware : IProducerMiddleware
    {
        public Headers? Headers { get; private set; }

        public Task InvokeAsync(ProducerContext context, ProducerDelegate next)
        {
            Headers = context.Headers;
            context.DeliveryResult = new DeliveryResult<byte[], byte[]>();
            return Task.CompletedTask;
        }
    }
}
