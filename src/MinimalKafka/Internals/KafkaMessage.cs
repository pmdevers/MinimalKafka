using Confluent.Kafka;
using System.Text;

namespace MinimalKafka.Internals;

internal record KafkaMessage(string Topic, byte[] Key, byte[] Value, Dictionary<string, string> Headers, Timestamp Timestamp)
{
    internal Headers GetKafkaHeaders() => 
        Headers.Aggregate(new Headers(), (h, x) => {
            h.Add(x.Key, Encoding.UTF8.GetBytes(x.Value));
            return h;
        });
};
