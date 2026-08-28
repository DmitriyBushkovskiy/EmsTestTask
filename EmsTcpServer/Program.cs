using System.ComponentModel;
using System.Net;
using Common;
using Common.Extensions;
using EmsTcpServer.Configuration;
using EmsTcpServer.Interfaces;
using EmsTcpServer.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<ServerWorker>();
builder.Services.AddConsoleLogger();

TypeDescriptor.AddAttributes(typeof(IPAddress), new TypeConverterAttribute(typeof(IpAddressTypeConverter)));

builder.Services
    .AddOptions<ServerConfig>()
    .Bind(builder.Configuration.GetSection(ServerConfig.SectionName))
    .ValidateDataAnnotations();

builder.Services.AddSingleton<IMarkingCodeGenerator, MarkingCodeGenerator>();

var host = builder.Build();
host.Run();