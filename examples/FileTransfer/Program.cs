using FileTransfer.Configuration;
using FileTransfer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddLoggerConfigs();

using var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
var startupLogger = loggerFactory.CreateLogger<Program>();

builder
    .AddServiceConfigs(startupLogger)
    .AddOptionsConfigs(startupLogger)
    .AddInfrastructure(startupLogger);

var app = builder.Build();

app.UseAppMiddleware();

await app.RunAsync();
