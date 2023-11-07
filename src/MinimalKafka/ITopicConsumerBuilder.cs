using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalKafka;

public interface ITopicConsumerBuilder
{
    IServiceProvider ServiceProvider { get; }

    TopicDataSource? DataSource { get; set; }

    List<object> Metadata { get; }
}
