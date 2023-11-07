using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalKafka;

public interface ITopicConventionBuilder
{
    void Add(Action<ITopicConsumerBuilder> convention);

    void Finally(Action<ITopicConsumerBuilder> finalConvention);
}
