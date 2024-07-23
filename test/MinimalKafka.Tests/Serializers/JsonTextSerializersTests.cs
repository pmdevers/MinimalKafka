using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using MinimalKafka.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MinimalKafka.Tests.Serializers;
public class JsonTextSerializerTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultJsonOptionsAndNullLogger()
    {
        // Arrange & Act
        var serializer = new JsonTextSerializer<string>(null);

        // Assert
        serializer.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldUseProvidedJsonOptions()
    {
        // Arrange
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var serializer = new JsonTextSerializer<string>(jsonOptions);

        // Assert
        serializer.Should().NotBeNull();
    }

    [Fact]
    public void Serialize_ShouldReturnSerializedData()
    {
        // Arrange
        var data = "test";
        var serializer = new JsonTextSerializer<string>(null);
        var context = new SerializationContext();

        // Act
        var result = serializer.Serialize(data, context);

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
        var serializer = new JsonTextSerializer<string>(null);
        var context = new SerializationContext();

        // Act
        var result = serializer.Deserialize(jsonData, false, context);

        // Assert
        result.Should().Be(data);
    }

    [Fact]
    public void Deserialize_ShouldHandleNullData()
    {
        // Arrange
        var serializer = new JsonTextSerializer<string>(null);
        var context = new SerializationContext();

        // Act
        var result = serializer.Deserialize([], true, context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ShouldHandleIgnoreType()
    {
        // Arrange
        var serializer = new JsonTextSerializer<Ignore>(null);
        var context = new SerializationContext();

        // Act
        var result = serializer.Deserialize([], false, context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ShouldLogErrorOnJsonException()
    {
        // Arrange
        var invalidJson = new byte[] { 0x01, 0x02, 0x03 };
        var logger = Substitute.For<ILogger<JsonTextSerializer<string>>>();
        var serializer = new JsonTextSerializer<string>(null, logger);
        var context = new SerializationContext();

        // Act
        Action act = () =>
        {
            _ = serializer.Deserialize(invalidJson, false, context);
        };

        // Assert
        act.Should().Throw<JsonException>();
        logger.Received(1);
    }
}
