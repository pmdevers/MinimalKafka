using Confluent.Kafka;
using System.Collections.Concurrent;

namespace MinimalKafka.Metadata.Internals;
internal class ConsumerHandlerMetadata : IConsumerHandlerMetadata
{
    public Func<object, List<TopicPartition>, IEnumerable<TopicPartitionOffset>> PartitionsAssignedHandler { get; set; }
        = (_, partitions) => {
            Console.WriteLine($"Assigned partitions: [{string.Join(", ", partitions)}]");
            // Default behavior: return the assigned partitions with unset offset
            return partitions.Select(p => new TopicPartitionOffset(p, Offset.Unset));
        };
    public Func<object, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>> PartitionsLostHandler { get; set; } 
        = (_, partitions) => {
            Console.WriteLine($"Lost partitions: [{string.Join(", ", partitions)}]");
            return [];
           };
    public Action<object, List<TopicPartitionOffset>> PartitionsRevokedHandler { get; set; }
        = (_, part) => { 
            foreach(var p in part)
            {
                Console.WriteLine($"[REVOKED] {p.Topic} - {p.Partition}");
            }
    };
    public Action<object, string> StatisticsHandler { get; set; }
        = (_, e) => Console.WriteLine($"[STATS] {e}");
    public Action<object, Error> ErrorHandler { get; set; }
        = (_, e) => Console.WriteLine($"[{GetLocal(e)}] {e.Code} - {e.Reason}");

    private static string GetLocal(Error e)
    {
        string logVar = string.Empty;
        if (e.IsLocalError)
        {
            logVar = $"{logVar}LOCAL";
        }
        if(e.IsBrokerError) 
        {
            logVar = $"{logVar}BROKER";
        }

        return logVar;
    }

    public Action<object, LogMessage> LogHandler { get; set; }
        = (_, e) => Console.WriteLine($"[{e.Level}] {e.Name} - {e.Message}");
}