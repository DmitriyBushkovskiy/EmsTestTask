using System.Net;
using System.Net.Sockets;

namespace Common;

public static class TestHelper
{
    public static readonly string[] Codes =
    [
        "016593475298046421U8QO74IQK6",
        "0182085901484498215BOWQY00IY",
        "010712621430768121N2KBHGHTRE",
        "0155815648092153218G7AHFDNN2",
        "011183407151043621ZY7ZY2KYZZ"
    ];
    
    public static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}