namespace MinimalKafka.Serializers;

/// <summary>
/// 
/// </summary>
public interface ISerializerFactory
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    IKafkaSerializer<T> Create<T>();
}
