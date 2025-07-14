namespace MinimalKafka.Serializers;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IKafkaSerializer<T>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public byte[] Serialize(T value);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public T Deserialize(ReadOnlySpan<byte> value);
}
