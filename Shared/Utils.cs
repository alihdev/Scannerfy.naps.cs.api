using System.Net.Sockets;
using System.Net;

namespace Scannerfy.Api.Shared;

public static class Utils
{
    public static bool IsPortInUse(int port)
    {
        try
        {
            // Attempt to bind to the port
            var tcpListener = new TcpListener(IPAddress.Loopback, port);
            tcpListener.Start();
            tcpListener.Stop();
            return false; // Port is available
        }
        catch (SocketException)
        {
            return true; // Port is in use
        }
    }
}
