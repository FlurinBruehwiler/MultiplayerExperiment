using System.Net;
using System.Net.Sockets;
using Common;

namespace MultiplayerServer;

public struct Client
{
    public TcpClient TcpClient;
}

public class Server
{
    public static void ListenForConnections(List<Client> clients)
    {
        var listener = new TcpListener(IPAddress.Any, 51234);
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

            ListenForMessages(clients, client).ContinueWith(_ => clients.Remove(client));
        }
    }

    private static async Task ListenForMessages(List<Client> clients, Client client)
    {
        Console.WriteLine("Listening for messages");

        while (client.TcpClient.Connected)
        {
            var message = await Networking.GetNextMessage(client.TcpClient);

            Console.WriteLine($"Got the message: {message}");

            PublishMessageToClients(message, clients);
        }

        Console.WriteLine("Stop listening for messages");
    }

    private static void PublishMessageToClients(Message message, List<Client> clients)
    {
        foreach (var client in clients)
        {
            Networking.SendMessage(client.TcpClient, message);
        }
    }
}
