using Microsoft.AspNetCore.Mvc;
using MinimalKafka.Producing;

namespace Examples.Features.ClaimCheck;

public static class UploadFile
{
    public const string Topic = "upload-file";
    public const string Route = "/";

    public static async Task<IResult> Handle(
        [FromServices] IKafkaProducer producer,
        [FromForm] IFormFileCollection files)
    {
        foreach (var file in files)
        {
            using var stream = new MemoryStream();
            file.CopyTo(stream);

            await producer.ProduceAsync(Topic, Guid.NewGuid(), stream.ToArray());
        }

        return TypedResults.Ok();
    }
}
