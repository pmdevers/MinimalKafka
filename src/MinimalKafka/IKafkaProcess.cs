namespace MinimalKafka;

/// <summary>
/// 
/// </summary>
public interface IKafkaProcess
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    Task Start(CancellationToken token);
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task Stop();
}
