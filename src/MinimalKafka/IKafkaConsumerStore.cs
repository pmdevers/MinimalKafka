namespace MinimalKafka;

/// <summary>
/// 
/// </summary>
public interface IKafkaConsumerStore
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    Task AddOrUpdate(byte[] key, byte[] value);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<byte[]> FindByKey(byte[] key);
}

    