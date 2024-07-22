using Confluent.Kafka;
using Pmdevers.MinimalKafka;

namespace MinimalKafka.Tests;
public class KafkaProcessTests
{
    [Fact]
    public async Task Test()
    {
        var consumer = Substitute.For<KafkaConsumer>();
        var testMessage = new ConsumeResult<string, string>()
        {
            Message = new()
            {
                Key = "key",
                Value = "value"
            }
        };

        var token = new CancellationTokenSource();

        consumer.Consume(token.Token).ReturnsForAnyArgs(KafkaContext.Create(testMessage, EmptyServiceProvider.Instance))
            .AndDoes((s) => token.Cancel());

        var kafkaprocess = KafkaProcess.Create(new()
        {
            Consumer = consumer,
            Delegate = (context) =>
            {

                token.Cancel();
                return Task.CompletedTask;

            }
        });

        kafkaprocess.Start(token.Token);

        await Task.Delay(1000);
    }
}
