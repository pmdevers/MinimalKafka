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

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="topic"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="headers"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task ProduceAsync<TKey, TValue>(string topic, TKey key, TValue value, Dictionary<string, string>? headers = null, CancellationToken token = default);
}

    