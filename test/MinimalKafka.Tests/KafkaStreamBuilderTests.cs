using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Stream;

namespace MinimalKafka.Tests;

public class KafkaStreamBuilderTests
{
    public class ApiTest
    {
        [Fact]
        public Task MapStream_Should_Call_MapTopic()
        {
            var builder = Substitute.For<IKafkaBuilder>();

            builder.MapStream<Guid, string>("string");

            
        }
    }
}

