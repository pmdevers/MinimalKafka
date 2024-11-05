﻿using Microsoft.Extensions.Logging;

namespace MinimalKafka;
public interface IKafkaProcess
{
    Task Start(CancellationToken cancellationToken);
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

    public Task Start(CancellationToken cancellationToken)
    {
        return Task.Factory.StartNew(() =>
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
        }, cancellationToken);
    }

    public void Stop()
    {
        _consumer.Close();
    }
}