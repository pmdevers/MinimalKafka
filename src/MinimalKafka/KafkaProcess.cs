using Microsoft.Extensions.Logging;
using System.Threading;

namespace Pmdevers.MinimalKafka;
public interface IKafkaProcess
{
    void Start(CancellationToken cancellationToken);
    void Stop();
}

public class KafkaProcessOptions
{
    public KafkaConsumer Consumer { get; set; } = new NoConsumer();
    public KafkaDelegate Delegate { get; set; } = (context) => Task.CompletedTask;
}
public class KafkaProcess : IKafkaProcess
{
    private readonly KafkaConsumer _consumer;
    private readonly KafkaDelegate _handler;

    private KafkaProcess(
        KafkaConsumer consumer,
        KafkaDelegate handler
    )
    {
        _consumer = consumer;
        _handler = handler;
    }

    public static KafkaProcess Create(KafkaProcessOptions options)
        => new(options.Consumer, options.Delegate);

    public void Start(CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(() =>
        {
            _consumer.Subscribe();

            while (!cancellationToken.IsCancellationRequested)
            {
                var context = _consumer.Consume(cancellationToken);

                if (context is EmptyKafkaContext)
                {

                    _consumer.Logger.LogError("Empty Context!");
                    continue;
                }

                _handler.Invoke(context);

            }
            _consumer.Logger.LogInformation("Dropping out of consume loop");
        });
    }

    public void Stop()
    {
        _consumer.Close();
    }
}