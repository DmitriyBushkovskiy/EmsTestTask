using System.Net.Sockets;
using System.Text;
using EmsTcpServer.Configuration;
using EmsTcpServer.Interfaces;
using Microsoft.Extensions.Options;

namespace EmsTcpServer.Services;

public class ServerWorker(
    ILogger<ServerWorker> logger,
    IOptions<ServerConfig> serverConfig,
    IMarkingCodeGenerator generator
    )
    : BackgroundService
{
    private readonly ServerConfig _config = serverConfig.Value;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(_config.Host, _config.Port);
        listener.Start();
        
        logger.LogInformation($"Server started on port {_config.Port}");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Waiting for client...");

                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }

                logger.LogInformation($"Client connected: {client.Client.RemoteEndPoint}");

                _ = Task.Run(() => HandleClientAsync(client, _config.GenerationIntervalMs, cancellationToken));
            }
        }
        finally
        {
            listener.Stop();
        }
    }
    
    private async Task HandleClientAsync(TcpClient client, int generationIntervalMs, CancellationToken cancellationToken)
    {
        using var _ = client;
        try
        {
            await using var stream = client.GetStream();
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.AutoFlush = true;
            while (!cancellationToken.IsCancellationRequested)
            {
                var code = generator.Generate();
                await writer.WriteLineAsync(code);

                logger.LogDebug($"Sent: {code}");

                await Task.Delay(TimeSpan.FromMilliseconds(generationIntervalMs), cancellationToken);
            }
        }
        catch (IOException)
        {
            logger.LogWarning("Client disconnected.");
        }
        catch (SocketException)
        {
            logger.LogWarning("Client disconnected.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
        }
    }
}



