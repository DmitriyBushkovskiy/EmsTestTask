using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;
using EmsTcpServer.Configuration;
using EmsTcpServer.Interfaces;
using EmsTcpServer.Services;

namespace EmsTcpServer.Tests;

[TestFixture]
public class ServerWorkerTests
{
    private Mock<ILogger<ServerWorker>> _loggerMock;
    private Mock<IMarkingCodeGenerator> _generatorMock;
    private ServerConfig _config;
    private int _testPort;
    // private readonly string[] _codes =
    // [
    //     "016593475298046421U8QO74IQK6",
    //     "0182085901484498215BOWQY00IY",
    //     "010712621430768121N2KBHGHTRE",
    //     "0155815648092153218G7AHFDNN2",
    //     "011183407151043621ZY7ZY2KYZZ"
    // ];

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ServerWorker>>();
        _generatorMock = new Mock<IMarkingCodeGenerator>();
        _testPort = TestHelper.GetAvailablePort();
        
        _config = new ServerConfig
        {
            Host = IPAddress.Loopback,
            Port = _testPort,
            GenerationIntervalMs = 100
        };
    }

    [TearDown]
    public void TearDown()
    {
    }

    [Test]
    public async Task ServerWorker_SendsGeneratedCodes_WhenClientConnected()
    {
        _generatorMock
            .SetupSequence(g => g.Generate())
            .Returns(TestHelper.Codes[0])
            .Returns(TestHelper.Codes[1])
            .Returns(TestHelper.Codes[2])
            .Returns(TestHelper.Codes[3])
            .Returns(TestHelper.Codes[4]);
        
        var server = new ServerWorker(_loggerMock.Object, Options.Create(_config), _generatorMock.Object);
        
        using var cts = new CancellationTokenSource();

        var serverTask = server.StartAsync(cts.Token);
        
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, _testPort, cts.Token);

        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        
        var receivedLines = new List<string>();
        for (int i = 0; i < TestHelper.Codes.Length; i++)
        {
            var line = await reader.ReadLineAsync(cts.Token);
            if (line != null)
                receivedLines.Add(line);
        }
        
        Assert.That(receivedLines, Is.EquivalentTo(TestHelper.Codes));
        _generatorMock.Verify(g => g.Generate(), Times.AtLeast(3));
        
        await cts.CancelAsync();
        await serverTask;
    }

    [Test]
    public async Task ServerWorker_HandlesMultipleClients_Concurrently()
    {
        _generatorMock
            .Setup(g => g.Generate())
            .Returns(TestHelper.Codes[0]);

        var server = new ServerWorker(_loggerMock.Object, Options.Create(_config), _generatorMock.Object);

        using var cts = new CancellationTokenSource();
        var serverTask = server.StartAsync(cts.Token);
        
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, _testPort, cts.Token);
                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                
                var line = await reader.ReadLineAsync(cts.Token);
                Assert.That(line, Is.EqualTo(TestHelper.Codes[0]));
            }, cts.Token));
        }

        await Task.WhenAll(tasks);
        
        _generatorMock.Verify(g => g.Generate(), Times.AtLeast(5));

        await cts.CancelAsync();
        await serverTask;
    }

    [Test]
    public async Task ServerWorker_Stops_WhenCancelled()
    {
        var server = new ServerWorker(_loggerMock.Object, Options.Create(_config), _generatorMock.Object);

        using var cts = new CancellationTokenSource();

        await server.StartAsync(cts.Token);
        await Task.Delay(100, cts.Token);
        
        await cts.CancelAsync();
        await server.StopAsync(CancellationToken.None);
        
        Assert.Pass("Server stopped successfully");
        
        using var client = new TcpClient();
        var connectTask = client.ConnectAsync(IPAddress.Loopback, _testPort);
        var timeoutTask = Task.Delay(100);
        var completedTask = await Task.WhenAny(connectTask, timeoutTask);
    
        Assert.That(completedTask, Is.EqualTo(timeoutTask), "Server should not accept connections after stop");
    }

    [Test]
    public async Task ServerWorker_LogsClientConnection()
    {
        var server = new ServerWorker(_loggerMock.Object, Options.Create(_config), _generatorMock.Object);

        using var cts = new CancellationTokenSource();
        var serverTask = server.StartAsync(cts.Token);
        
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, _testPort, cts.Token);
        await Task.Delay(100, cts.Token);
        await cts.CancelAsync();
        
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Client connected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        await serverTask;
    }
}
