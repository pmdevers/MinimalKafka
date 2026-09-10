using Microsoft.AspNetCore.Mvc;
using MinimalKafka;

namespace FileTransfer.Features.Files;

public class UploadFile
{
    public static async Task<IResult> Handle(
        [FromServices] IKafkaProducer producer,
        IFormFileCollection files
    )
    {
        foreach (var file in files)
        {

        }

        return TypedResults.Accepted("/files");
    }
}
