using System.ComponentModel.DataAnnotations;
using System.Net;

namespace EmsTcpServer.Configuration;

public record ServerConfig
{
    public const string SectionName = "Server";

    [Required]
    public IPAddress Host { get; init; } = null!;

    [Range(1, 65535)]
    public int Port { get; init; }

    [Range(1, 60000)]
    public int GenerationIntervalMs { get; init; }
}