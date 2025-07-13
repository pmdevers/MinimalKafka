namespace MinimalKafka;

/// <summary>
/// 
/// </summary>
public interface IKafkaProducer
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ProduceAsync(KafkaContext context, CancellationToken cancellationToken);
}

    