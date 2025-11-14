using SqsConsumer;

var builder = Host.CreateApplicationBuilder(args);

// Register the background worker service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
