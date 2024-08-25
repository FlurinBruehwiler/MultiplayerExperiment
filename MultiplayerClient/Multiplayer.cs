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
        while (true)
        {
            var message = await Networking.GetNextMessage(server.TcpClient);
            server.MessagesToProcess.Add(message);
        }
    }

    private static void SendMessageThread(Server server)
    {
        while (true)
        {
            var message = server.MessagesToSend.Take();

            Console.WriteLine($"Sending message {message}");

            if (!Networking.SendMessage(server.TcpClient, message))
            {
                //adding was unsuccessful, adding back to the collection and wait
                server.MessagesToSend.Add(message);
                Thread.Sleep(100);
            }
        }
    }
}
