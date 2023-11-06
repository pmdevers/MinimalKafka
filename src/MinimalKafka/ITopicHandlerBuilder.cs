using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalKafka;


internal interface ITopicHandlerBuilder
{
    IServiceProvider ServiceProvider { get; }

    ICollection<ITopicDatasource> DataSources { get; }
}
