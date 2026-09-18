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
        var list = new List<KafkaFile>();

        foreach (var file in files)
        {
            using var stream = new MemoryStream();
            file.CopyTo(stream);

            var kFile = KafkaFile.Create(file.FileName, file.ContentType, stream.ToArray());


            await producer.ProduceAsync("file-upload", Guid.NewGuid(), new
            {
                File = kFile
            });

        }

        return TypedResults.Ok(list);
    }

    public static async Task Consumer([FromValue] FileUpload fileUpload)
    {

    }

    public record FileUpload(KafkaFile File);

}



