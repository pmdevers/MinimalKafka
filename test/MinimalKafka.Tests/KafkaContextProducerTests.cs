using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Internals;
using MinimalKafka.Serializers;
using System.Text.Json;

namespace MinimalKafka.Tests;

public class KafkaContextProducerTests
{
    [Fact]
    public async Task ProduceAsync_Should_Store_File_For_Record_Message()
    {
        // Arrange
        var fileStore = Substitute.For<IKafkaFileStore>();
        var producer = Substitute.For<IProducer<byte[], byte[]>>();
        producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<byte[], byte[]>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeliveryResult<byte[], byte[]>()));

        var serviceProvider = CreateServiceProvider(fileStore);
        var sut = new KafkaContextProducer(serviceProvider, producer, topic => $"prefix-{topic}");
        var message = new OutputMessage("key", KafkaFile.Create("file.pdf", "application/pdf", "payload"u8.ToArray()));

        // Act
        await sut.ProduceAsync("documents", message.Key, message);

        // Assert
        await fileStore.Received(1).StoreAsync(message.PdfDocument.Id, message.PdfDocument.Data);
        await producer.Received(1).ProduceAsync(
            "prefix-documents",
            Arg.Is<Message<byte[], byte[]>>(x => x.Key.Length > 0 && x.Value.Length > 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProduceAsync_Should_Store_File_For_Class_Message()
    {
        // Arrange
        var fileStore = Substitute.For<IKafkaFileStore>();
        var producer = Substitute.For<IProducer<byte[], byte[]>>();
        producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<byte[], byte[]>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeliveryResult<byte[], byte[]>()));

        var serviceProvider = CreateServiceProvider(fileStore);
        var sut = new KafkaContextProducer(serviceProvider, producer, topic => topic);
        var message = new OutputClassMessage
        {
            Key = "key",
            PdfDocument = KafkaFile.Create("file.pdf", "application/pdf", "payload"u8.ToArray())
        };

        // Act
        await sut.ProduceAsync("documents", message.Key, message);

        // Assert
        await fileStore.Received(1).StoreAsync(message.PdfDocument.Id, message.PdfDocument.Data);
        await producer.Received(1).ProduceAsync(
            "documents",
            Arg.Is<Message<byte[], byte[]>>(x => x.Key.Length > 0 && x.Value.Length > 0),
            Arg.Any<CancellationToken>());
    }

    private static ServiceProvider CreateServiceProvider(IKafkaFileStore fileStore)
    {
        var services = new ServiceCollection();
        services.AddSingleton(JsonSerializerOptions.Default);
        services.AddSingleton<ISerializerFactory, SystemTextJsonSerializerFactory>();
        services.AddTransient(typeof(IKafkaSerializer<>), typeof(KafkaSerializerProxy<>));
        services.AddTransient<IKafkaHydrationService, KafkaHydrationService>();
        services.AddSingleton(fileStore);
        return services.BuildServiceProvider();
    }

    private sealed class OutputClassMessage
    {
        public required string Key { get; init; }

        public required KafkaFile PdfDocument { get; init; }
    }

    private sealed record OutputMessage(string Key, KafkaFile PdfDocument);
}
