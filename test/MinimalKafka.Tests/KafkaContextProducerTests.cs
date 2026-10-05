using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Internals;
using MinimalKafka.Serializers;
using System.Text.Json;

namespace MinimalKafka.Tests;

public class KafkaContextProducerTests
{
    [Fact]
    public async Task Invoke_Should_Produce_Serialized_Message_For_Record_Message()
    {
        var producer = Substitute.For<IProducer<byte[], byte[]>>();
        producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<byte[], byte[]>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeliveryResult<byte[], byte[]>()));

        var serviceProvider = CreateServiceProvider();
        var sut = new KafkaProducer(serviceProvider, producer);
        var message = new ProduceMessage(
            "prefix-documents",
            "key",
            new OutputMessage("key", KafkaFile.Create("file.pdf", "application/pdf", "payload"u8.ToArray())),
            []);

        await sut.Invoke(message);

        await producer.Received(1).ProduceAsync(
            "prefix-documents",
            Arg.Is<Message<byte[], byte[]>>(x => x.Key.Length > 0 && x.Value.Length > 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invoke_Should_Produce_Serialized_Message_For_Class_Message()
    {
        var producer = Substitute.For<IProducer<byte[], byte[]>>();
        producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<byte[], byte[]>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeliveryResult<byte[], byte[]>()));

        var serviceProvider = CreateServiceProvider();
        var sut = new KafkaProducer(serviceProvider, producer);
        var message = new ProduceMessage(
            "documents",
            "key",
            new OutputClassMessage
            {
                Key = "key",
                PdfDocument = KafkaFile.Create("file.pdf", "application/pdf", "payload"u8.ToArray())
            },
            []);

        await sut.Invoke(message);

        await producer.Received(1).ProduceAsync(
            "documents",
            Arg.Is<Message<byte[], byte[]>>(x => x.Key.Length > 0 && x.Value.Length > 0),
            Arg.Any<CancellationToken>());
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(JsonSerializerOptions.Default);
        services.AddSingleton<ISerializerFactory, SystemTextJsonSerializerFactory>();
        services.AddTransient(typeof(IKafkaSerializer<>), typeof(KafkaSerializerProxy<>));
        return services.BuildServiceProvider();
    }

    private sealed class OutputClassMessage
    {
        public required string Key { get; init; }

        public required KafkaFile PdfDocument { get; init; }
    }

    private sealed record OutputMessage(string Key, KafkaFile PdfDocument);
}
