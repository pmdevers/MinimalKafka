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
    public async Task MapStream_Join_OnPredicate_ShouldInvokeProcessorFromRightSide()
    {
        var provider = BuildServiceProvider(out _);
        IApplicationBuilder app = new ApplicationBuilder(provider);
        var registry = provider.GetRequiredService<TopicRegistry>();
        var results = new List<(Guid Key, ValueJoinedOrder? Order, ValueJoinedPayment? Payment, string Topic)>();

        app.MapStream<Guid, ValueJoinedOrder>("orders")
            .InnerJoin<Guid, ValueJoinedPayment>("payments")
            .On((order, payment) => order.CustomerCode == payment.CustomerCode)
            .Into((context, key, value) =>
            {
                results.Add((key, value.Item1, value.Item2, context.TopicName));
                return Task.CompletedTask;
            });

        var order = new ValueJoinedOrder(Guid.Parse("44444444-4444-4444-4444-444444444444"), "customer-a");
        var payment = new ValueJoinedPayment(Guid.Parse("55555555-5555-5555-5555-555555555555"), "customer-a", 25m);

        await registry.Topics["orders"].Handler(await CreateContextAsync(provider, "orders", order.Id, order));
        await registry.Topics["payments"].Handler(await CreateContextAsync(provider, "payments", payment.Id, payment));

        results.Should().HaveCount(1);
        results[0].Topic.Should().Be("payments");
        results[0].Key.Should().Be(payment.Id);
        results[0].Order.Should().Be(order);
        results[0].Payment.Should().Be(payment);
    }

    [Fact]
    public async Task MapStream_Join_OnPredicate_ShouldInvokeProcessorForEachStoredMatch()
    {
        var provider = BuildServiceProvider(out _);
        IApplicationBuilder app = new ApplicationBuilder(provider);
        var registry = provider.GetRequiredService<TopicRegistry>();
        var results = new List<(Guid Key, ValueJoinedOrder? Order, ValueJoinedPayment? Payment, string Topic)>();

        app.MapStream<Guid, ValueJoinedOrder>("orders")
            .InnerJoin<Guid, ValueJoinedPayment>("payments")
            .On((order, payment) => order.CustomerCode == payment.CustomerCode)
            .Into((context, key, value) =>
            {
                results.Add((key, value.Item1, value.Item2, context.TopicName));
                return Task.CompletedTask;
            });

        var payment1 = new ValueJoinedPayment(Guid.Parse("66666666-6666-6666-6666-666666666666"), "customer-b", 10m);
        var payment2 = new ValueJoinedPayment(Guid.Parse("77777777-7777-7777-7777-777777777777"), "customer-b", 20m);
        var order = new ValueJoinedOrder(Guid.Parse("88888888-8888-8888-8888-888888888888"), "customer-b");

        await registry.Topics["payments"].Handler(await CreateContextAsync(provider, "payments", payment1.Id, payment1));
        await registry.Topics["payments"].Handler(await CreateContextAsync(provider, "payments", payment2.Id, payment2));
        await registry.Topics["orders"].Handler(await CreateContextAsync(provider, "orders", order.Id, order));

        results.Should().HaveCount(2);
        results.Should().OnlyContain(result => result.Topic == "orders" && result.Key == order.Id && result.Order == order);
        results.Select(result => result.Payment).Should().BeEquivalentTo([payment1, payment2]);
    }

    [Fact]
    public async Task MapStream_Join_OnPredicate_WithoutKeyParameter_ShouldSupportDifferentCurrentKeyTypes()
    {
        var provider = BuildServiceProvider(out _);
        IApplicationBuilder app = new ApplicationBuilder(provider);
        var registry = provider.GetRequiredService<TopicRegistry>();
        var results = new List<(string Topic, JoinLocation? Location, JoinPlayerPosition? Position)>();

        app.MapStream<int, JoinLocation>("locations")
            .InnerJoin<Guid, JoinPlayerPosition>("positions")
            .On((location, position) => location.Id == position.LocationId)
            .Into((context, value) =>
            {
                results.Add((context.TopicName, value.Item1, value.Item2));
                return Task.CompletedTask;
            });

        var location = new JoinLocation(7, "seven");
        var position = new JoinPlayerPosition(Guid.Parse("99999999-9999-9999-9999-999999999999"), 7);

        await registry.Topics["locations"].Handler(await CreateContextAsync(provider, "locations", location.Id, location));
        await registry.Topics["positions"].Handler(await CreateContextAsync(provider, "positions", position.PlayerId, position));

        results.Should().HaveCount(1);
        results[0].Topic.Should().Be("positions");
        results[0].Location.Should().Be(location);
        results[0].Position.Should().Be(position);
    }

    [Fact]
    public void MapStream_Join_OnPredicate_WithKeyParameter_ShouldThrow_WhenKeyTypesDiffer()
    {
        var provider = BuildServiceProvider(out _);
        IApplicationBuilder app = new ApplicationBuilder(provider);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            app.MapStream<int, JoinLocation>("locations")
                .InnerJoin<Guid, JoinPlayerPosition>("positions")
                .On((location, position) => location.Id == position.LocationId)
                .Into((context, key, value) => Task.CompletedTask));

        exception.Message.Should().Contain("requires both topics to use the same key type");
    }

    [Fact]
    public async Task MapStream_Join_OnPredicate_ShouldYieldUnmatchedResultForLeftJoin()
    {
        var provider = BuildServiceProvider(out _);
        IApplicationBuilder app = new ApplicationBuilder(provider);
        var registry = provider.GetRequiredService<TopicRegistry>();
        var results = new List<(Guid Key, ValueJoinedOrder? Order, ValueJoinedPayment? Payment, string Topic)>();

        app.MapStream<Guid, ValueJoinedOrder>("orders")
            .Join<Guid, ValueJoinedPayment>("payments")
            .On((order, payment) => order.CustomerCode == payment.CustomerCode)
            .Into((context, key, value) =>
            {
                results.Add((key, value.Item1, value.Item2, context.TopicName));
                return Task.CompletedTask;
            });

        var unmatchedPayment = new ValueJoinedPayment(Guid.Parse("99999999-9999-9999-9999-999999999999"), "customer-x", 15m);
        var unmatchedOrder = new ValueJoinedOrder(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "customer-y");

        await registry.Topics["payments"].Handler(await CreateContextAsync(provider, "payments", unmatchedPayment.Id, unmatchedPayment));
        results.Clear();

        await registry.Topics["orders"].Handler(await CreateContextAsync(provider, "orders", unmatchedOrder.Id, unmatchedOrder));

        results.Should().HaveCount(1);
        results[0].Topic.Should().Be("orders");
        results[0].Key.Should().Be(unmatchedOrder.Id);
        results[0].Order.Should().Be(unmatchedOrder);
        results[0].Payment.Should().BeNull();
    }

    [Fact]
    public async Task MapStream_Branch_ShouldRouteMatchingBranchOrDefault()
    {
        var provider = BuildServiceProvider(out var producer);
        IApplicationBuilder app = new ApplicationBuilder(provider);
        var registry = provider.GetRequiredService<TopicRegistry>();

        app.MapStream<Guid, RoutedOrder>("orders")
            .Branch(branches =>
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
            TKey? key,
            TValue? value,
            Headers? headers = null,
            string? format = null,
            CancellationToken cancellationToken = default
            )
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

    private sealed record ValueJoinedOrder(Guid Id, string CustomerCode);

    private sealed record ValueJoinedPayment(Guid Id, string CustomerCode, decimal Amount);

    private sealed record JoinLocation(int Id, string Name);

    private sealed record JoinPlayerPosition(Guid PlayerId, int LocationId);

    private sealed record RoutedOrder(Guid Id, string Type)
    {
        public static readonly RoutedOrder Priority = new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "priority");
        public static readonly RoutedOrder Standard = new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "standard");
    }
}
