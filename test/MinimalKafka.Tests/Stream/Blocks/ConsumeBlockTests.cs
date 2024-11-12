using MinimalKafka.Builders;
using MinimalKafka.Stream.Blocks;
using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Tests.Stream.Blocks;
public class ConsumeBlockTests
{
    public class Ctor
    {
        [Fact]
        public void Should_Call_KafkaBuilder_MapTopic()
        {
            var kafkabuilder =  Substitute.For<IKafkaBuilder>();
            var datasource = Substitute.For<IKafkaDataSource>();
            var conventions = new KafkaConventionBuilder([], []);

            datasource.AddTopicDelegate(Arg.Any<string>(), Arg.Any<Delegate>())
                .Returns(conventions);


            kafkabuilder.MetaData.Returns([]);
            kafkabuilder.DataSource.Returns(datasource);
                        
            var block = new ConsumeBlock<string, string>(kafkabuilder, "test");

            block.Builder.Should().Be(conventions);
            datasource.Received(1).AddTopicDelegate("test", Arg.Any<Delegate>());
        }
    }
}

public class TestDataSource : IKafkaDataSource
{
    public IServiceProvider ServiceProvider => EmptyServiceProvider.Instance;

    public Dictionary<string, Delegate> Topics = [];

    public KafkaConventionBuilder AddTopicDelegate(string topicName, Delegate handler)
    {
        Topics.Add(topicName, handler);
        return new KafkaConventionBuilder([], []);
    }

    public IEnumerable<IKafkaProcess> GetProceses()
    {
        yield return Substitute.For<IKafkaProcess>();
    }
}

