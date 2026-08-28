using System.ComponentModel.DataAnnotations;
using System.Net;

namespace EmsTcpClient.Configuration;

public record ClientConfig
{
    public const string SectionName = "Client";
    
    [Required]
    public string Host { get; init; } = null!;
    
    [Range(1, 65535)]
    public int Port { get; init; }

    [Required]
    public string OutputFile { get; init; } = null!;
    
    [Range(1, 60000)]
    public int ReconnectDelayMs { get; init; }
    
    public bool UseExponentialBackoff { get; init; }
    
    [Range(1, 10)]
    public int MaxReconnectionAttempts { get; init; }
    
    [Range(100, 60000)]
    public int MaxReconnectDelayMs { get; init; }
}