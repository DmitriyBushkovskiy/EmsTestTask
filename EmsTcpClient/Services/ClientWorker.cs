using System.Net.Sockets;
using EmsTcpClient.Configuration;
using Microsoft.Extensions.Options;

namespace EmsTcpClient.Services;

public class ClientWorker(
    ILogger<ClientWorker> logger,
    IOptions<ClientConfig> clientConfig
    ) : BackgroundService
{
    private readonly ClientConfig _config = clientConfig.Value;
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var reconnectDelayMs = _config.ReconnectDelayMs;
        var reconnectCount = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation($"Connecting to {_config.Host}:{_config.Port}");

                using var client = new TcpClient();
                await client.ConnectAsync(_config.Host, _config.Port, cancellationToken);

                logger.LogInformation("Connected");

                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream);
                await using var fileStream = new FileStream(_config.OutputFile, FileMode.Append, FileAccess.Write, FileShare.Read);
                await using var writer = new StreamWriter(fileStream);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line is null)
                    {
                        logger.LogWarning("Server disconnected");
                        break;
                    }

                    await writer.WriteLineAsync(line);

                    await writer.FlushAsync(cancellationToken);

                    logger.LogDebug($"Received: {line}");
                    
                    reconnectDelayMs = _config.ReconnectDelayMs;
                    reconnectCount = 0;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex)
            {
                logger.LogError($"Connection error: {ex.Message}");
            }
            catch (IOException ex)
            {
                logger.LogError($"I/O error: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error: {ex.Message}");
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                if (reconnectCount >= _config.MaxReconnectionAttempts)
                    break;
                
                reconnectCount++;
                
                logger.LogWarning($"Reconnect. Attempt {reconnectCount}");
                
                if (_config.UseExponentialBackoff)
                {
                    reconnectDelayMs = Math.Min(reconnectDelayMs * 2, _config.MaxReconnectDelayMs);
                    logger.LogDebug($"Reconnect delay increased to {reconnectDelayMs} ms");
                }
                
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(reconnectDelayMs), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        logger.LogWarning("Client stopped");
    }
}