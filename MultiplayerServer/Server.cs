using System.Net;
using System.Net.Sockets;
using Common;
using MemoryPack;

namespace MultiplayerServer;

public struct Client
{
    public TcpClient TcpClient;
}

public class Server
{
    public static void ListenForConnections(List<Client> clients)
    {
        var ipEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 51234);
        var listener = new TcpListener(ipEndpoint);
        listener.Start();

        Console.WriteLine("Listening for Clients");

        while (true)
        {
            TcpClient tcpClient = listener.AcceptTcpClient();

            Console.WriteLine("Client connected");

            var client = new Client
            {
                TcpClient = tcpClient
            };

            clients.Add(client);

            ListenForMessages(client).ContinueWith(_ => clients.Remove(client));
        }
    }

    private static async Task ListenForMessages(Client client)
    {
        var stream = client.TcpClient.GetStream();

        while (client.TcpClient.Connected)
        {
            var message = await MemoryPackSerializer.DeserializeAsync<Message>(stream);
            Console.WriteLine($"Got the message: {message}");
        }

        Console.WriteLine("Stop listening for messages");
    }
}
