using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Stream;

namespace MinimalKafka.Tests;

public class KafkaStreamBuilderTests
{
    public class ApiTest
    {
        [Fact]
        public void MapStream_Should_Call_MapTopic()
        {
            var builder = new KafkaBuilder(EmptyServiceProvider.Instance);

            builder.MapStream<Guid, string>("string");

            
        }
    }
}

