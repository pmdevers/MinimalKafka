using System;

namespace MinimalKafka;

/// <summary>
/// 
/// </summary>
public interface IKafkaStore
{
    /// <summary>
    /// 
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    ValueTask<byte[]> AddOrUpdate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    ValueTask<byte[]> FindByIdAsync(byte[] key);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IAsyncEnumerable<byte[]> GetItems();   
    
}

/// <summary>
/// 
/// </summary>
public interface IKafkaStoreFactory
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="consumerKey"></param>
    /// <returns></returns>
    public IKafkaStore GetStore(KafkaConsumerKey consumerKey);
}
    