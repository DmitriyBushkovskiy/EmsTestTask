using Common.Extensions;
using EmsTcpClient.Configuration;
using EmsTcpClient.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<ClientWorker>();
builder.Services.AddConsoleLogger();

builder.Services
    .AddOptions<ClientConfig>()
    .Bind(builder.Configuration.GetSection(ClientConfig.SectionName))
    .ValidateDataAnnotations();

var host = builder.Build();
host.Run();