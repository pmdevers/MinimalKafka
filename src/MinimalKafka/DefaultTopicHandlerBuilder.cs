using Microsoft.AspNetCore.Builder;

namespace MinimalKafka;

public sealed class DefaultTopicHandlerBuilder : ITopicHandlerBuilder
{
    public DefaultTopicHandlerBuilder(IApplicationBuilder applicationBuilder)
    {
        ApplicationBuilder = applicationBuilder;
        DataSources = new List<ITopicDatasource>();
    }

    public IApplicationBuilder ApplicationBuilder { get; }
    public ICollection<ITopicDatasource> DataSources { get; }
    public IServiceProvider ServiceProvider => ApplicationBuilder.ApplicationServices;
}
