namespace MinimalKafka;

/// <summary>
/// 
/// </summary>
public interface IKafkaProducer
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task ProduceAsync(KafkaContext ctx, CancellationToken ct);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="topic"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="header"></param>
    /// <returns></returns>
    Task ProduceAsync<TKey, TValue>(string topic, TKey key, TValue value, Dictionary<string, string>? header = null);
}

    