namespace FileTransfer.Features.Files;

public class GetFiles
{
    public static IResult Handle()
    {
        return TypedResults.Ok(Array.Empty<Response>());
    }

    public record Response(string Identifier, string Filename, string ContentType, int Length);
}
