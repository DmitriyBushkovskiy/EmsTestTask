using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;
using EmsTcpClient.Configuration;
using EmsTcpClient.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Moq;

namespace EmsTcpClient.Tests;

[TestFixture]
public class ClientWorkerTests
{
    private Mock<ILogger<ClientWorker>> _loggerMock;
    private ClientConfig _config;
    private int _testPort;
    private string _tempFile;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ClientWorker>>();
        _testPort = TestHelper.GetAvailablePort();
        _tempFile = Path.GetTempFileName();
        
        _config = new ClientConfig
        {
            Host = "127.0.0.1",
            Port = _testPort,
            OutputFile = _tempFile,
            ReconnectDelayMs = 100,
            MaxReconnectionAttempts = 3,
            UseExponentialBackoff = false,
            MaxReconnectDelayMs = 5000
        };
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }

    [Test]
    public async Task ClientWorker_ReceivesAndWritesData_WhenServerSends()
    {
        using var cts = new CancellationTokenSource();
        var client = new ClientWorker(_loggerMock.Object, Options.Create(_config));
        
        var server = new TcpListener(IPAddress.Loopback, _testPort);
        server.Start();
        
        var serverTask = Task.Run(async () =>
        {
            using var clientSocket = await server.AcceptTcpClientAsync();
            await using var stream = clientSocket.GetStream();
            await using var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.AutoFlush = true;
            
            await writer.WriteLineAsync(TestHelper.Codes[0]);
            await Task.Delay(50);
            await writer.WriteLineAsync(TestHelper.Codes[1]);
            await Task.Delay(50);
            await writer.WriteLineAsync(TestHelper.Codes[2]);
            
            await Task.Delay(100);
        });
        
        await client.StartAsync(cts.Token);
        await Task.Delay(500);
        await client.StopAsync(cts.Token);
        
        var content = await File.ReadAllLinesAsync(_tempFile);
        Assert.That(content, Contains.Item(TestHelper.Codes[0]));
        Assert.That(content, Contains.Item(TestHelper.Codes[1]));
        Assert.That(content, Contains.Item(TestHelper.Codes[2]));
        
        server.Stop();
        await serverTask;
    }

    [Test]
    public async Task ClientWorker_Reconnects_WhenConnectionLost()
    {
        using var cts = new CancellationTokenSource();
        var client = new ClientWorker(_loggerMock.Object, Options.Create(_config));

        var server = new TcpListener(IPAddress.Loopback, _testPort);
        server.Start();
        
        await client.StartAsync(cts.Token);
        await Task.Delay(200);
        
        server.Stop();
        await Task.Delay(500);
        
        server = new TcpListener(IPAddress.Loopback, _testPort);
        server.Start();

        await Task.Delay(300);
        await cts.CancelAsync();
        
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Reconnect")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
        
        server.Stop();
    }

    [Test]
    public async Task ClientWorker_Stops_WhenMaxReconnectAttemptsExceeded()
    {
        var configWithLimitedAttempts = _config with { MaxReconnectionAttempts = 2 };

        var client = new ClientWorker(_loggerMock.Object, Options.Create(configWithLimitedAttempts));
        using var cts = new CancellationTokenSource();
        
        await client.StartAsync(cts.Token);
        await Task.Delay(5000);
        
        await cts.CancelAsync();
        
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Client stopped")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Reconnect. Attempt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task ClientWorker_UsesExponentialBackoff_WhenEnabled()
    {
        var configWithBackoff = _config with { UseExponentialBackoff = true }; 

        var client = new ClientWorker(_loggerMock.Object, Options.Create(configWithBackoff));

        using var cts = new CancellationTokenSource();
        await client.StartAsync(cts.Token);
        
        await Task.Delay(5000);
        await cts.CancelAsync();
        
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Reconnect delay increased")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }
}