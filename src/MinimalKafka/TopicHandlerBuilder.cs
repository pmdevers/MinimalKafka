using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalKafka;
public sealed class TopicHandlerBuilder
{
    
    public void Add(Action<ITopicBuilder> builder)
    {
        
    }
}

public interface ITopicCollection
{

}

public interface ITopicDatasource
{
}

public class TopicPattern
{
}
