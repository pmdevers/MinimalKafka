using FileTransfer.Configuration;
using FileTransfer.Features.AntiForgery;
using FileTransfer.Features.Files;
using Microsoft.Extensions.Options;

namespace FileTransfer.Features;

public static class FeatureExtentions
{
    extension(WebApplication app)
    {
        public WebApplication MapApiEndpoints()
        {
            var options = app.Services.GetRequiredService<IOptions<FileTransferOptions>>();

            var antiForgery = app.MapGroup("/antiforgery");

            antiForgery.MapGet("/token", GetToken.Handle);

            var files = app.MapGroup("/files");

            files.MapGet("/", GetFiles.Handle);
            files.MapPost("/", UploadFile.Handle);

            return app;
        }
    }
}