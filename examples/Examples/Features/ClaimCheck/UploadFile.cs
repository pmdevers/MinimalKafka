using Microsoft.AspNetCore.Mvc;
using MinimalKafka.Producing;
using System.Collections.Concurrent;

namespace Examples.Features.ClaimCheck;

public static class UploadFile
{
    private static readonly ConcurrentQueue<Response> Files = [];

    public const string Topic = "upload-file";
    public const string Route = "/";

    public static IResult GetFiles()
    {
        return TypedResults.Ok(Files.Reverse().ToArray());
    }

    public static async Task<IResult> Handle(
        [FromServices] IKafkaProducer producer,
        [FromForm] IFormFileCollection files,
        CancellationToken cancellationToken)
    {
        foreach (var file in files)
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);

            var request = new Request(
                Guid.NewGuid(),
                file.FileName,
                file.ContentType,
                stream.ToArray());

            await producer.ProduceAsync(Topic, request.Identifier, request, cancellationToken: cancellationToken);
        }

        return TypedResults.Accepted(Route);
    }

    public static Task Consumer([MinimalKafka.Attributes.FromValue] Request request)
    {
        Files.Enqueue(new Response(
            request.Identifier,
            request.FileName,
            request.ContentType,
            request.Content.Length,
            DateTimeOffset.UtcNow));

        while (Files.Count > 50 && Files.TryDequeue(out _))
        {
        }

        return Task.CompletedTask;
    }

    public sealed record Request(Guid Identifier, string FileName, string ContentType, byte[] Content);

    public sealed record Response(
        Guid Identifier,
        string FileName,
        string ContentType,
        int Length,
        DateTimeOffset ConsumedAtUtc);
}
