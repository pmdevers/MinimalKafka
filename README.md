# MinimalKafka

![Github Release](https://img.shields.io/github/v/release/pmdevers/MinimalKafka) 
![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/pmdevers/MinimalKafka/.github%2Fworkflows%2Fbuild-publish.yml) 
![GitHub License](https://img.shields.io/github/license/pmdevers/MinimalKafka) 
![Github Issues Open](https://img.shields.io/github/issues/pmdevers/MinimalKafka) 
![Github Pull Request Open](https://img.shields.io/github/issues-pr/pmdevers/MinimalKafka) 
[![Scheduled Code Security Testing](https://github.com/pmdevers/MinimalKafka/actions/workflows/security-analysis.yml/badge.svg?event=schedule)](https://github.com/pmdevers/MinimalKafka/actions/workflows/security-analysis.yml)



MinimalKafka is a Kafka consumer library designed to consume messages similarly to a Minimal API. This repository provides a streamlined and efficient way to work with Kafka consumers in .NET 8.0.

![Alt text](/assets/logo_256_background.png "Minimal Kafka logo")

## Features

- Simple and minimalistic Kafka consumer setup
- Built to integrate seamlessly with .NET 8.0 applications
- Utilizes the Confluent.Kafka for robust Kafka interactions

## Installation

To install MinimalKafka, use the NuGet package manager:

```bash
dotnet add package MinimalKafka
```
## Usage

### Setting Up a Kafka Consumer

Below is a basic example of how to set up a Kafka consumer using MinimalKafka. For more detailed examples, refer to the `example` folder in the repository.

```csharp
using MinimalKafka;

var builder = WebApplication.CreateBuilder(args);

.Services.AddMinimalKafka(config => 
{ 
    config.WithBootstrapServers("localhost:9092")
          .WithGroupId(Guid.NewGuid().ToString())
          .WithOffsetReset(AutoOffsetReset.Earliest); 
});

var app = builder.Build();

app.MapTopic("topic.name", (string key, string value) => {
    
    // Implement your code here
    return Task.CompletedTask;

}).WithGroupId("Topic 2");

await app.RunAsync();

```

## Contribution

Contributions are welcome! Please submit a pull request or open an issue to discuss your ideas or improvements.

## License

This project is licensed under the MIT License.

## Contact

For any questions or support, please open an issue in the repository.