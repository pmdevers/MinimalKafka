using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Producing;
using MinimalKafka.Runtime;
using MinimalKafka.Serialization;
using MinimalKafka.Stream;
using System.Text;

namespace MinimalKafka.Tests;

public class StreamProcessingTests
{
    [Fact]
    public void AddMinimalKafka_ShouldRegisterInMemoryStreamStoreFactory()
    {
        var services = new ServiceCollection();

        services.AddMinimalKafka();

        using var provider = services.BuildServiceProvider();
        var storeFactory = provider.GetRequiredService<IKafkaStoreFactory>();

        storeFactory.Should().BeOfType<InMemoryKafkaStoreFactory>();
    }

    [Fact]
    public async Task MapStream_Into_ShouldPersistStateAndInvokeProcessor()
    {
        var provider = BuildServiceProvider(out _);
        IApplicationBuilder app = new ApplicationBuilder(provider);
        var registry = provider.GetRequiredService<TopicRegistry>();
        OrderCreated? stored = null;

        app.MapStream<Guid, OrderCreated>("orders")
            .Into(async (context, key, value) =>
            {
                key.Should().Be(value.Id);
                stored = await context.GetTopicStore("orders").FindByKeyAsync<Guid, OrderCreated>(key);
            });

        await registry.Topics["orders"].Handler(await CreateContextAsync(provider, "orders", OrderCreated.Sample.Id, OrderCreated.Sample));

        stored.Should().Be(OrderCreated.Sample);
    }

    [Fact]
    public async Task MapStream_Join_OnKey_ShouldInvokeProcessorFromBothSides()
    {
        var provider = BuildServiceProvider(out _);
        IApplicationBuilder app = new ApplicationBuilder(provider);
        var registry = provider.GetRequiredService<TopicRegistry>();
        var results = new List<(Guid Key, OrderCreated? Order, PaymentReceived? Payment, string Topic)>();

        app.MapStream<Guid, OrderCreated>("orders")
            .Join<Guid, PaymentReceived>("payments")
            .OnKey()
            .Into((context, key, value) =>
            {
                results.Add((key, value.Item1, value.Item2, context.TopicName));
                return Task.CompletedTask;
            });

        await registry.Topics["orders"].Handler(await CreateContextAsync(provider, "orders", OrderCreated.Sample.Id, OrderCreated.Sample));
        await registry.Topics["payments"].Handler(await CreateContextAsync(provider, "payments", PaymentReceived.Sample.OrderId, PaymentReceived.Sample));

        results.Should().HaveCount(2);
        results[0].Topic.Should().Be("orders");
        results[0].Order.Should().Be(OrderCreated.Sample);
        results[0].Payment.Should().BeNull();
        results[1].Topic.Should().Be("payments");
        results[1].Key.Should().Be(OrderCreated.Sample.Id);
        results[1].Order.Should().Be(OrderCreated.Sample);
        results[1].Payment.Should().Be(PaymentReceived.Sample);
    }

    [Fact]
    public async Task MapStream_SplitInto_ShouldRouteMatchingBranchOrDefault()
    {
        var provider = BuildServiceProvider(out var producer);
        IApplicationBuilder app = new ApplicationBuilder(provider);
        var registry = provider.GetRequiredService<TopicRegistry>();

        app.MapStream<Guid, RoutedOrder>("orders")
            .SplitInto(branches =>
            {
                branches.Branch((_, value) => value.Type == "priority").To("priority-orders");
                branches.DefaultBranch("standard-orders");
            });

        await registry.Topics["orders"].Handler(await CreateContextAsync(provider, "orders", RoutedOrder.Priority.Id, RoutedOrder.Priority));
        await registry.Topics["orders"].Handler(await CreateContextAsync(provider, "orders", RoutedOrder.Standard.Id, RoutedOrder.Standard));

        producer.Messages.Should().HaveCount(2);
        producer.Messages[0].Topic.Should().Be("priority-orders");
        producer.Messages[0].Key.Should().Be(RoutedOrder.Priority.Id);
        producer.Messages[0].Value.Should().Be(RoutedOrder.Priority);
        producer.Messages[1].Topic.Should().Be("standard-orders");
        producer.Messages[1].Key.Should().Be(RoutedOrder.Standard.Id);
        producer.Messages[1].Value.Should().Be(RoutedOrder.Standard);
    }

    private static ServiceProvider BuildServiceProvider(out TestKafkaProducer producer)
    {
        var services = new ServiceCollection();
        producer = new TestKafkaProducer();
        services.AddSingleton<IKafkaProducer>(producer);
        services.AddMinimalKafka();
        return services.BuildServiceProvider();
    }

    private static async Task<KafkaContext> CreateContextAsync<TKey, TValue>(
        IServiceProvider provider,
        string topic,
        TKey key,
        TValue value)
    {
        var headers = new Headers();
        var keyBytes = await SerializeAsync(provider, topic, key, headers);
        var valueBytes = await SerializeAsync(provider, topic, value, headers);

        return new KafkaContext(
            new ConsumeResult<byte[], byte[]>
            {
                Topic = topic,
                Message = new Message<byte[], byte[]>
                {
                    Key = keyBytes,
                    Value = valueBytes,
                    Headers = headers
                }
            },
            provider.GetRequiredService<IKafkaProducer>(),
            provider,
            CancellationToken.None);
    }

    private static Task<byte[]> SerializeAsync<T>(IServiceProvider provider, string topic, T value, Headers headers)
    {
        return value switch
        {
            byte[] bytes => Task.FromResult(bytes),
            string text => Task.FromResult(Encoding.UTF8.GetBytes(text)),
            _ => provider.GetRequiredService<IKafkaSerializerRegistry>()
                .Get(null)
                .SerializeAsync(value!, topic, headers, CancellationToken.None)
        };
    }

    private sealed class TestKafkaProducer : IKafkaProducer
    {
        public List<ProducedMessage> Messages { get; } = [];

        public Task<DeliveryResult<byte[], byte[]>> ProduceAsync<TKey, TValue>(
            string topic,
            TKey key,
            TValue value,
            Headers? headers = null,
            CancellationToken cancellationToken = default,
            string? format = null)
        {
            Messages.Add(new ProducedMessage(topic, key, value, format));
            return Task.FromResult(new DeliveryResult<byte[], byte[]>
            {
                Topic = topic,
                Message = new Message<byte[], byte[]>
                {
                    Headers = headers ?? []
                }
            });
        }
    }

    private sealed record ProducedMessage(string Topic, object? Key, object? Value, string? Format);

    private sealed record OrderCreated(Guid Id, string Description)
    {
        public static readonly OrderCreated Sample = new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "created");
    }

    private sealed record PaymentReceived(Guid OrderId, decimal Amount)
    {
        public static readonly PaymentReceived Sample = new(Guid.Parse("11111111-1111-1111-1111-111111111111"), 19.95m);
    }

    private sealed record RoutedOrder(Guid Id, string Type)
    {
        public static readonly RoutedOrder Priority = new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "priority");
        public static readonly RoutedOrder Standard = new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "standard");
    }
}
