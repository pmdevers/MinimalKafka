using MinimalKafka;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalKafka();

var app = builder.Build();


app.MapTopic("hello", (string key, string value) =>
{
    Console.WriteLine(key, value);
});

// Configure the HTTP request pipeline.

app.Run();
