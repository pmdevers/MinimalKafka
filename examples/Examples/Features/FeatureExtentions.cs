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

            stream.MapPost("/orders", StreamExamples.CreateOrderAsync);
            stream.MapPost("/payments", StreamExamples.CreatePaymentAsync);
            stream.MapGet("/results", StreamExamples.GetResults);
            stream.MapDelete("/results", StreamExamples.ClearResults);

            app.MapStream<Guid, StreamExamples.OrderReceived>(StreamExamples.OrdersTopic)
                .Join<Guid, StreamExamples.PaymentReceived>(StreamExamples.PaymentsTopic)
                .OnKey()
                .Into(StreamExamples.ProcessJoinAsync);

            app.MapStream<Guid, StreamExamples.OrderPaymentSummary>(StreamExamples.ResultsTopic)
                .Into(StreamExamples.TrackResultAsync);

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