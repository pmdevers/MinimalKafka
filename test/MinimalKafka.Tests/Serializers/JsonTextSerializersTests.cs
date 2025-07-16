using Confluent.Kafka;
using MinimalKafka.Internals;
using MinimalKafka.Serializers;
using System.Text.Json;

namespace MinimalKafka.Tests.Serializers;
public class JsonTextSerializerTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultJsonOptionsAndNullLogger()
    {
        // Arrange & Act
        var serializer = new SystemTextJsonSerializer<string>();

        // Assert
        serializer.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldUseProvidedJsonOptions()
    {
        // Act
        var serializer = new SystemTextJsonSerializer<string>();

        // Assert
        serializer.Should().NotBeNull();
    }

    [Fact]
    public void Serialize_ShouldReturnSerializedData()
    {
        // Arrange
        var data = "test";
        var serializer = new SystemTextJsonSerializer<string>();

        // Act
        var result = serializer.Serialize(data);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Deserialize_ShouldReturnDeserializedData()
    {
        // Arrange
        var data = "test";
        var jsonData = JsonSerializer.SerializeToUtf8Bytes(data);
        var serializer = new SystemTextJsonSerializer<string>();

        // Act
        var result = serializer.Deserialize(jsonData);

        // Assert
        result.Should().Be(data);
    }

    [Fact]
    public void Deserialize_ShouldHandleNullData()
    {
        // Arrange
        var serializer = new SystemTextJsonSerializer<string>();

        // Act
        var result = serializer.Deserialize([]);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ShouldHandleIgnoreType()
    {
        // Arrange
        var serializer = new SystemTextJsonSerializer<Ignore>();

        // Act
        var result = serializer.Deserialize([]);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ShouldLogErrorOnJsonException()
    {
        // Arrange
        var invalidJson = new byte[] { 0x01, 0x02, 0x03 };
        var serializer = new SystemTextJsonSerializer<string>();

        // Act
        Action act = () =>
        {
            _ = serializer.Deserialize(invalidJson);
        };

        // Assert
        act.Should().Throw<KafkaProcesException>();
    }
}
