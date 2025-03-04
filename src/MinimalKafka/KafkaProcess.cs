using Microsoft.Extensions.Logging;
using MinimalKafka.Helpers;

namespace MinimalKafka;
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
        _consumer.Subscribe();
            
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var context = _consumer.Consume(cancellationToken);

                if (context is EmptyKafkaContext)
                {

                    _consumer.Logger.EmptyContext();

                    continue;
                }

                _handler.Invoke(context);
            }
        }
        catch(Exception ex)
        {
            _consumer.Logger.UnknownProcessException(ex.Message);
            throw new KafkaProcesException(ex, "Unknown Process error.");
        }
        finally
        {
            _consumer.Logger.DropOutOfConsumeLoop();
            _consumer.Close();
        }
           
    }

    public void Stop()
    {
        _consumer.Close();
    }
}

public class KafkaProcesException(Exception ex, string message) : Exception(message, ex)
{
}