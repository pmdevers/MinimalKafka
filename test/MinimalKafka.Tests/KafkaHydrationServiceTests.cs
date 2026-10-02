namespace MinimalKafka.Tests;

public class KafkaHydrationServiceTests
{
    [Fact]
    public async Task DeHydrate_Should_Store_KafkaFile_For_Class_Message()
    {
        // Arrange
        var fileStore = Substitute.For<IKafkaFileStore>();
        var sut = new KafkaHydrationService(fileStore);
        var message = new ClassMessage
        {
            File = KafkaFile.Create("file.txt", "text/plain", "payload"u8.ToArray())
        };

        // Act
        await sut.DeHydrateAsync(message);

        // Assert
        await fileStore.Received(1).StoreAsync(message.File.Id, message.File.Data);
    }

    [Fact]
    public async Task DeHydrate_Should_Store_KafkaFile_For_Record_Message()
    {
        // Arrange
        var fileStore = Substitute.For<IKafkaFileStore>();
        var sut = new KafkaHydrationService(fileStore);
        var message = new RecordMessage(KafkaFile.Create("file.txt", "text/plain", "payload"u8.ToArray()));

        // Act
        await sut.DeHydrateAsync(message);

        // Assert
        await fileStore.Received(1).StoreAsync(message.File.Id, message.File.Data);
    }

    [Fact]
    public async Task ReHydrate_Should_Load_KafkaFile_Data_For_Class_Message()
    {
        // Arrange
        var fileStore = Substitute.For<IKafkaFileStore>();
        var sut = new KafkaHydrationService(fileStore);
        var expectedData = "rehydrated"u8.ToArray();
        var message = new ClassMessage
        {
            File = KafkaFile.Create("file.txt", "text/plain")
        };

        fileStore.LoadAsync(message.File.Id)
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(expectedData));

        // Act
        await sut.ReHydrateAsync(message);

        // Assert
        message.File.Data.ToArray().Should().Equal(expectedData);
        await fileStore.Received(1).LoadAsync(message.File.Id);
    }

    [Fact]
    public async Task ReHydrate_Should_Load_KafkaFile_Data_For_Record_Message()
    {
        // Arrange
        var fileStore = Substitute.For<IKafkaFileStore>();
        var sut = new KafkaHydrationService(fileStore);
        var expectedData = "rehydrated"u8.ToArray();
        var message = new RecordMessage(KafkaFile.Create("file.txt", "text/plain"));

        fileStore.LoadAsync(message.File.Id)
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(expectedData));

        // Act
        await sut.ReHydrateAsync(message);

        // Assert
        message.File.Data.ToArray().Should().Equal(expectedData);
        await fileStore.Received(1).LoadAsync(message.File.Id);
    }

    [Fact]
    public async Task DeHydrate_Should_Store_KafkaFile_For_OutputMessage()
    {
        // Arrange
        var fileStore = Substitute.For<IKafkaFileStore>();
        var sut = new KafkaHydrationService(fileStore);
        var message = new OutputMessage("key", KafkaFile.Create("file.pdf", "application/pdf", "payload"u8.ToArray()));

        // Act
        await sut.DeHydrateAsync(message);

        // Assert
        await fileStore.Received(1).StoreAsync(message.PdfDocument.Id, message.PdfDocument.Data);
    }

    [Fact]
    public async Task ReHydrate_Should_Load_KafkaFile_Data_For_OutputMessage()
    {
        // Arrange
        var fileStore = Substitute.For<IKafkaFileStore>();
        var sut = new KafkaHydrationService(fileStore);
        var expectedData = "rehydrated"u8.ToArray();
        var message = new OutputMessage("key", KafkaFile.Create("file.pdf", "application/pdf"));

        fileStore.LoadAsync(message.PdfDocument.Id)
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(expectedData));

        // Act
        await sut.ReHydrateAsync(message);

        // Assert
        message.PdfDocument.Data.ToArray().Should().Equal(expectedData);
        await fileStore.Received(1).LoadAsync(message.PdfDocument.Id);
    }

    private sealed class ClassMessage
    {
        public required KafkaFile File { get; init; }
    }

    private sealed record RecordMessage(KafkaFile File);

    public record OutputMessage(string Key, KafkaFile PdfDocument);
}
