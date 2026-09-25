using Microsoft.AspNetCore.Mvc;
using MinimalKafka;

namespace FileTransfer.Features.Files;

public class GetFiles
{
    public static async Task Handle(
        [FromServices] IKafkaFileStore store)
    {
    }

    public record Response(string Identifier, string Filename, string ContentType, int Length);
}
