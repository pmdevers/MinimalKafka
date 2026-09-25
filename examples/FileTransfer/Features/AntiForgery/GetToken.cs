using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace FileTransfer.Features.AntiForgery;

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
