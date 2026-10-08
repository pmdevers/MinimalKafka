using Examples.Features.BasicSubscribe;
using Examples.Features.Movies;
using MinimalKafka;

namespace FileTransfer.Features;

public static class FeatureExtentions
{
    extension(WebApplication app)
    {
        public WebApplication MapFeatures()
        {
            var basic = app.MapGroup("basic");

            basic.MapPost(BasicProduce.Route, BasicProduce.Handle);
            app.MapTopic(BasicSubscribe.Topic, BasicSubscribe.Handle);


            var movies = app.MapGroup("movies");

            movies.MapPost(CreateMovie.Route, CreateMovie.Handle);
            app.MapTopic(BranchByGenre.MovieTopic, BranchByGenre.Handle);

            return app;
        }
    }
}