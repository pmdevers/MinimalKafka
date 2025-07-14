using MinimalKafka.Builders;
using MinimalKafka.Stream;
using MinimalKafka.Stream.Internals;

namespace MinimalKafka.Tests.Stream;
public class StreamBuilderTests
{
    private const string _mainTopic = "main-topic";
    private const string _joinTopic = "join-topic";

    [Fact]
    public void Join_ReturnsJoinBuilder_WithCorrectParams()
    {
        var builder = new KafkaBuilder(EmptyServiceProvider.Instance);
        var streamBuilder = new StreamBuilder<string, int>(builder, _mainTopic);

        var join = streamBuilder.Join<long, double>(_joinTopic);

        join.Should().BeAssignableTo<IJoinBuilder<string, int, long, double>>();
    }

    [Fact]
    public void InnerJoin_ReturnsJoinBuilder_WithCorrectParams()
    {
        var builder = new KafkaBuilder(EmptyServiceProvider.Instance);
        var streamBuilder = new StreamBuilder<string, int>(builder, _mainTopic);

        var join = streamBuilder.InnerJoin<long, double>(_joinTopic);

        join.Should().BeAssignableTo<IJoinBuilder<string, int, long, double>>();
    }

    [Fact]
    public void Into_Calls_Builder_MapTopic_And_Delegates_Handler()
    {
        var builder = new KafkaBuilder(EmptyServiceProvider.Instance);

        var streamBuilder = new StreamBuilder<string, int>(builder, _mainTopic);

        static Task Handler(KafkaContext ctx, string key, int value)
        {
            return Task.CompletedTask;
        }

        // Act
        var result = streamBuilder.Into(Handler);

               
    }
}
