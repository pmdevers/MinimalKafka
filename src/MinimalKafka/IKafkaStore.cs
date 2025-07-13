using MinimalKafka.Internals;
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
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    ValueTask<byte[]> AddOrUpdate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    ValueTask<TValue?> FindByKey<TKey, TValue>(TKey key);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    IAsyncEnumerable<TValue> FindAsync<TValue>(Func<TValue, bool> value);
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
    