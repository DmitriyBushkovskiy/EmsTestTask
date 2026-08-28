using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsoleLogger(this IServiceCollection services)
    {
        services.AddLogging(b =>
        {
            b.AddSimpleConsole(o =>
                {
                    o.SingleLine = true;
                    o.TimestampFormat = "yyyy.MM.dd HH:mm:ss ";
                    o.ColorBehavior = LoggerColorBehavior.Enabled;
                    o.UseUtcTimestamp = true;
                });
        });
        return services;
    }
}