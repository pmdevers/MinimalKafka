using MinimalKafka.Helpers;

namespace MinimalKafka;
public interface IKafkaProcess
{
    Task Start(CancellationToken cancellationToken);
    Task Stop();
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

    public async Task Start(CancellationToken cancellationToken)
    {
        _consumer.Subscribe();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _consumer.Consume(_handler, cancellationToken);
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
        }
        
    }

    public async Task Stop()
    {
        _consumer.Close();
        await Task.CompletedTask;
    }
}

public class KafkaProcesException(Exception ex, string message) : Exception(message, ex)
{
}