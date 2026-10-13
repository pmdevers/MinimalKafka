using Examples.Features.BasicSubscribe;
using Examples.Features.ClaimCheck;
using Examples.Features.Movies;
using Examples.Features.Resilience;
using Examples.Features.Streams;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using MinimalKafka;
using MinimalKafka.Middleware.Resilience;
using MinimalKafka.Stream;

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

            claimsCheck.MapGet("/", UploadFile.GetFiles);
            claimsCheck.MapPost(UploadFile.Route, UploadFile.Handle);
            app.MapTopic(UploadFile.Topic, UploadFile.Consumer);

            var stream = app.MapGroup("stream");

            stream.MapPost("/orders", OrderPlacedApi.CreateAsync);
            stream.MapPost("/payments", OrderPaidApi.CreateAsync);
            stream.MapGet("/results", PaymentProcessor.GetResults);
            stream.MapDelete("/results", PaymentProcessor.ClearResults);
            stream.MapGet("/branch-results", RiskProcessor.GetResults);
            stream.MapDelete("/branch-results", RiskProcessor.ClearResults);

            app.MapStream<Guid, OrderPlaced>(StreamTopics.Orders)
                .Join<Guid, OrderPaid>(StreamTopics.Payments)
                .OnKey()
                .Into(PaymentProcessor.ProcessJoinAsync);

            app.MapStream<Guid, PayedOrder>(StreamTopics.PayedOrders)
                .Into(PaymentProcessor.TrackPayedOrderAsync);

            app.MapStream<Guid, PayedOrder>(StreamTopics.PayedOrders)
                .Branch(RiskProcessor.ConfigureBranches);

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