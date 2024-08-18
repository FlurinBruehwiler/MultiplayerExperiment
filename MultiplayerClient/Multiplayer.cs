using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Common;
using MemoryPack;

namespace MultiplayerClient;

public class Server
{
    public required BlockingCollection<Message> Messages;
}

public static class Multiplayer
{
    public static Server Run()
    {
        var server = new Server
        {
            Messages = new BlockingCollection<Message>()
        };
        var thread = new Thread(() => RunThread(server))
        {
            IsBackground = true
        };
        thread.Start();
        return server;
    }

    private static void RunThread(Server server)
    {
        var ipEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 51234);

        var tcpClient = new TcpClient();

        tcpClient.Connect(ipEndpoint);

        var stream = tcpClient.GetStream();
        var writer = new StreamWriter(stream);

        while (true)
        {
            var message = server.Messages.Take();

            Console.WriteLine($"Sending message {message}");

            var binaryMessage = MemoryPackSerializer.Serialize(message);

            writer.Write(binaryMessage);
        }
    }
}
