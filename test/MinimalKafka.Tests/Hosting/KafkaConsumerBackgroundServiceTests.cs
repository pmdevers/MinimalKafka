using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MinimalKafka.Attributes;
using MinimalKafka.Hosting;
using MinimalKafka.Middleware;
using MinimalKafka.Producing;
using MinimalKafka.Runtime;
using MinimalKafka.Serialization;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MinimalKafka.Tests.Hosting;

public class CatchAll : IConsumerMiddleware
{
    public async Task InvokeAsync(KafkaContext context, ConsumerDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception)
        {
        }
    }

    public class KafkaConsumerBackgroundServiceTests
    {
        [Fact]
        public async Task ExecuteAsync_WhenMessageValueIsInvalidJson_AndCatchAllHandlesIt_ShouldCommit()
        {
            var registry = new TopicRegistry();
            var handlerInvoked = false;

            Task Handle([FromValue] OrderPayload _)
            {
                handlerInvoked = true;
                return Task.CompletedTask;
            }

            var reg = registry.Add("orders", (Func<OrderPayload, Task>)Handle);

            reg.Middleware.Add((_) => new CatchAll());

            using var cts = new CancellationTokenSource();
            var consumer = new StubKafkaConsumer(
                () => cts.Cancel(),
                new ConsumeResult<byte[], byte[]>
                {
                    Topic = "orders",
                    Partition = new Partition(3),
                    Offset = new Offset(9),
                    Message = new Message<byte[], byte[]>
                    {
                        Key = Encoding.UTF8.GetBytes("customer-1"),
                        Value = Encoding.UTF8.GetBytes("{ invalid json"),
                        Headers = []
                    }
                });

            var services = new ServiceCollection();
            services.AddOptions<SerializationOptions>();
            services.AddSingleton<IKafkaSerializer, JsonMessageSerializer>();
            services.AddSingleton<IKafkaSerializerRegistry, MessageSerializerRegistry>();
            await using var provider = services.BuildServiceProvider();

            var runtimeControl = Substitute.For<IConsumerRuntimeControl>();
            runtimeControl.IsRunning.Returns(true);

            var service = new KafkaConsumerBackgroundService(
                registry,
                new StubKafkaConsumerFactory(consumer),
                provider.GetRequiredService<IServiceScopeFactory>(),
                Substitute.For<IKafkaProducer>(),
                runtimeControl,
                NullLogger<KafkaConsumerBackgroundService>.Instance);

            await InvokeExecuteAsync(service, cts.Token);

            Assert.False(handlerInvoked);
            Assert.Equal(1, consumer.CommitCallCount);
        }

        private static Task InvokeExecuteAsync(KafkaConsumerBackgroundService service, CancellationToken cancellationToken)
        {
            var method = typeof(KafkaConsumerBackgroundService)
                .GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

            return (Task)method.Invoke(service, [cancellationToken])!;
        }

        private sealed class StubKafkaConsumerFactory(StubKafkaConsumer consumer) : IKafkaConsumerFactory
        {
            public IKafkaConsumer Create() => consumer;
        }

        private sealed class StubKafkaConsumer(Action? onCommit, params ConsumeResult<byte[], byte[]>[] results) : IKafkaConsumer
        {
            private readonly Queue<ConsumeResult<byte[], byte[]>?> _results = new(results);
            private readonly Action? _onCommit = onCommit;

            public int CommitCallCount { get; private set; }

            public void Subscribe(IEnumerable<string> topics)
            {
            }

            public ConsumeResult<byte[], byte[]>? Consume(TimeSpan timeout)
                => _results.Count == 0 ? null : _results.Dequeue();

            public void Commit(ConsumeResult<byte[], byte[]> result)
            {
                CommitCallCount++;
                _onCommit?.Invoke();
            }

            public void Close()
            {
            }

            public void Dispose()
            {
            }
        }

        private sealed class OrderPayload
        {
            public string? Id { get; set; }
        }
    }
}
