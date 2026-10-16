using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Attributes;
using MinimalKafka.Producing;
using MinimalKafka.Runtime;
using MinimalKafka.Serialization;
using System.Text;
using System.Text.Json;

namespace MinimalKafka.Tests;

public class HandlerAdapterTests
{
    [Fact]
    public async Task Create_ShouldBindContext_And_Invoke_Handler()
    {
        var services = new ServiceCollection();
        var state = new HandlerState();
        services.AddSingleton(state);
        var provider = services.BuildServiceProvider();
        var token = new CancellationTokenSource().Token;
        var context = CreateContext(
            topic: "orders",
            key: Encoding.UTF8.GetBytes("customer-1"),
            value: Encoding.UTF8.GetBytes("payload"),
            headers: [new Header("trace-id", Encoding.UTF8.GetBytes("42"))],
            provider,
            token);

        var sut = HandlerAdapter.Create(Handle);

        await sut(context);

        Assert.Equal("customer-1", state.Key);
        Assert.Equal("payload", state.Value);
        Assert.Equal(42, state.TraceId);
        Assert.Same(context, state.Context);
        Assert.Equal(token, state.CancellationToken);
    }

    [Fact]
    public async Task Create_ShouldFallback_To_Key_Value_And_Services_When_Attributes_Are_Missing()
    {
        var services = new ServiceCollection();
        var state = new HandlerState();
        services.AddSingleton(state);
        var provider = services.BuildServiceProvider();
        var context = CreateContext(
            topic: "orders",
            key: Encoding.UTF8.GetBytes("customer-1"),
            value: Encoding.UTF8.GetBytes("payload"),
            headers: [],
            provider,
            CancellationToken.None);

        var sut = HandlerAdapter.Create(ImplicitHandle);

        await sut(context);

        Assert.Equal("customer-1", state.Key);
        Assert.Equal("payload", state.Value);
    }

    [Fact]
    public async Task Create_ShouldThrow_When_ValueJson_IsInvalid()
    {
        var services = new ServiceCollection();
        var state = new JsonHandlerState();
        services.AddOptions<SerializationOptions>();
        services.AddSingleton<IKafkaSerializer, JsonMessageSerializer>();
        services.AddSingleton<IKafkaSerializerRegistry, MessageSerializerRegistry>();
        services.AddSingleton(state);
        var provider = services.BuildServiceProvider();

        var context = CreateContext(
            topic: "orders",
            key: Encoding.UTF8.GetBytes("customer-1"),
            value: Encoding.UTF8.GetBytes("{ invalid json"),
            headers: [],
            provider,
            CancellationToken.None);

        var sut = HandlerAdapter.Create(HandleJsonValue);

        await Assert.ThrowsAsync<JsonException>(() => sut(context));
        Assert.False(state.Invoked);
    }

    [Fact]
    public void Create_ShouldThrow_When_Handler_Returns_Unsupported_Type()
    {
        var exception = Assert.Throws<ArgumentException>(() => HandlerAdapter.Create((Func<Task<int>>)BadHandler));
        Assert.Contains("must return either Task or void", exception.Message);
    }

    [Fact]
    public void Create_ShouldThrow_When_Parameter_Has_Multiple_Binding_Attributes()
    {
        Func<string, Task> handler = InvalidBindingHandler;

        var exception = Assert.Throws<ArgumentException>(() => HandlerAdapter.Create(handler));
        Assert.Contains("exactly one binding attribute", exception.Message);
    }

    private static Task Handle(
        [FromKey] string key,
        [FromValue] string value,
        [FromServices] HandlerState state,
        [FromHeader(Name = "trace-id")] int traceId,
        KafkaContext context,
        CancellationToken cancellationToken)
    {
        state.Key = key;
        state.Value = value;
        state.TraceId = traceId;
        state.Context = context;
        state.CancellationToken = cancellationToken;
        return Task.CompletedTask;
    }

    private static Task ImplicitHandle(string key, string value, HandlerState state)
    {
        state.Key = key;
        state.Value = value;
        return Task.CompletedTask;
    }

    private static Task HandleJsonValue([FromValue] JsonPayload value, [FromServices] JsonHandlerState state)
    {
        state.Invoked = true;
        return Task.CompletedTask;
    }

    private static Task<int> BadHandler() => Task.FromResult(1);

    private static Task InvalidBindingHandler([FromKey, FromValue] string value) => Task.CompletedTask;

    private static KafkaContext CreateContext(
        string topic,
        byte[] key,
        byte[] value,
        Headers headers,
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        return new KafkaContext(
            new ConsumeResult<byte[], byte[]>
            {
                Topic = topic,
                Message = new Message<byte[], byte[]>
                {
                    Key = key,
                    Value = value,
                    Headers = headers
                }
            },
            new TestKafkaProducer(),
            provider,
            cancellationToken);
    }

    private sealed class HandlerState
    {
        public string? Key { get; set; }

        public string? Value { get; set; }

        public int TraceId { get; set; }

        public KafkaContext? Context { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }

    private sealed class JsonHandlerState
    {
        public bool Invoked { get; set; }
    }

    private sealed class JsonPayload
    {
        public string? Id { get; set; }
    }

    private sealed class TestKafkaProducer : IKafkaProducer
    {
        public Task<DeliveryResult<byte[], byte[]>> ProduceAsync<TKey, TValue>(
            string topic,
            TKey key,
            TValue value,
            Headers? headers = null,
            CancellationToken cancellationToken = default,
            string? format = null)
        {
            throw new NotSupportedException();
        }
    }
}
