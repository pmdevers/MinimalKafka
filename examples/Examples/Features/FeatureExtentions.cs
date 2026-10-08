using Examples.Features.BasicSubscribe;
using Examples.Features.ClaimCheck;
using Examples.Features.Movies;
using Examples.Features.Resilience;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using MinimalKafka;
using MinimalKafka.Middleware.Resilience;

namespace FileTransfer.Features;

public static class FeatureExtentions
{
    extension(WebApplication app)
    {
        public WebApplication MapFeatures()
        {
            var antiForgery = app.MapGroup("antiforgery");

            antiForgery.MapGet("/token", GetToken.Handle);

            var basic = app.MapGroup("basic");

            basic.MapPost(BasicProduce.Route, BasicProduce.Handle);
            app.MapTopic(BasicSubscribe.Topic, BasicSubscribe.Handle);

            var movies = app.MapGroup("movies");

            movies.MapPost(CreateMovie.Route, CreateMovie.Handle);
            app.MapTopic(BranchByGenre.MovieTopic, BranchByGenre.Handle);

            var retry = app.MapGroup("retry");

            app.MapTopic(Retry.Topic, Retry.Handler)
                .WithDeadLetter()
                .WithRetry();

            var claimsCheck = app.MapGroup("claim-check");

            app.MapPost(UploadFile.Route, UploadFile.Handle);

            return app;
        }
    }
}

public static class GetToken
{
    public static IResult Handle(
        [FromServices] IAntiforgery antiforgeryService,
        HttpContext ctx)
    {
        var token = antiforgeryService.GetAndStoreTokens(ctx);
        var xsrfToken = token.RequestToken;
        return TypedResults.Content(xsrfToken, "text/plain");
    }
}