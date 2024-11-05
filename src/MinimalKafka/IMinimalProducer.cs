using Confluent.Kafka;

namespace MinimalKafka;

public interface IMinimalProducer<TKey, TValue>
{
    /// <summary>
    ///     Asynchronously send a single message to a
    ///     Kafka topic. The partition the message is
    ///     sent to is determined by the partitioner
    ///     defined using the 'partitioner' configuration
    ///     property.
    /// </summary>
    /// <param name="message">The message to produce.</param>
    /// <param name="cancellationToken">
    ///     A cancellation token to observe whilst waiting
    ///     the returned task to complete.
    /// </param>
    /// <returns>
    ///     A Task which will complete with a delivery
    ///     report corresponding to the produce request,
    ///     or an exception if an error occured.
    /// </returns>
    /// <exception cref="T:Confluent.Kafka.ProduceException`2">
    ///     Thrown in response to any produce request
    ///     that was unsuccessful for any reason
    ///     (excluding user application logic errors).
    ///     The Error property of the exception provides
    ///     more detailed information.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     Thrown in response to invalid argument values.
    /// </exception>
    Task<DeliveryResult<TKey, TValue>> ProduceAsync(
        Message<TKey, TValue> message,
        CancellationToken cancellationToken = default);
}