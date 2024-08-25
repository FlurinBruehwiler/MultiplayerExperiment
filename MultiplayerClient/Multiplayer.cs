using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Common;

namespace MultiplayerClient;

public class Server
{
    public required BlockingCollection<IMessage> MessagesToSend;
    public required ConcurrentBag<IMessage> MessagesToProcess;
    public required TcpClient TcpClient;
}

public static class Multiplayer
{
    public static Server? Run()
    {
#if DEBUG
        var ip = IPAddress.Parse("127.0.0.1");
#else
        var ip = IPAddress.Parse("98.71.24.184");
#endif

        var ipEndpoint = new IPEndPoint(ip, 51234);

        var tcpClient = new TcpClient();

        try
        {
            tcpClient.Connect(ipEndpoint);
        }
        catch
        {
            Console.WriteLine("Server not available, using client only");
            return null;
        }

        var server = new Server
        {
            MessagesToSend = new BlockingCollection<IMessage>(),
            MessagesToProcess = new ConcurrentBag<IMessage>(),
            TcpClient = tcpClient
        };

        //send
        var sendThread = new Thread(() => SendMessageThread(server))
        {
            IsBackground = true
        };
        sendThread.Start();

        //listen
        _ = Task.Run(() => ReceiveMessageThread(server));

        return server;
    }

    private static async Task ReceiveMessageThread(Server server)
    {
        while (server.TcpClient.Connected)
        {
            var message = await Networking.GetNextMessage(server.TcpClient);
            if(message is null)
                continue;
            server.MessagesToProcess.Add(message);
        }
    }

    private static void SendMessageThread(Server server)
    {
        while (true)
        {
            var message = server.MessagesToSend.Take();

            if(!server.TcpClient.Connected)
                break;

            Console.WriteLine($"Sending message {message}");

            Networking.SendMessage(server.TcpClient, message);
        }
    }
}
