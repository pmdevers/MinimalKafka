using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalKafka;
public interface ITopicBuilder
{
    IApplicationBuilder ApplicationBuilder { get; }
    
    IServiceProvider ServiceProvider { get; }

    IList<ITopic> Topics { get; }

    IConsumer Build();
}

public interface IConsumer
{
}

public interface ITopic
{
}
