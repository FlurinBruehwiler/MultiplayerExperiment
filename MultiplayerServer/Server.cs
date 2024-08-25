using System.Net;
using System.Net.Sockets;
using Common;

namespace MultiplayerServer;

public struct Client
{
    public TcpClient TcpClient;
}

public class ServerState
{
    public required DateTimeOffset LastSave;
    public required SaveState SaveState;
    public required List<Client> Clients;
}

public class Server
{
    public static void ListenForConnections()
    {
        var serverState = new ServerState
        {
            LastSave = DateTimeOffset.Now,
            SaveState = new SaveState
            {
                Tiles = []
            },
            Clients = []
        };

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

            serverState.Clients.Add(client);

            ListenForMessages(serverState, client).ContinueWith(_ =>
            {
                Console.WriteLine("Client disconnected");
                return serverState.Clients.Remove(client);
            });
        }
    }

    private static async Task ListenForMessages(ServerState serverState, Client client)
    {
        Console.WriteLine("Listening for messages");

        Networking.SendMessage(client.TcpClient, serverState.SaveState);

        while (client.TcpClient.Connected)
        {
            var message = await Networking.GetNextMessage(client.TcpClient);

            if(message is null)
                continue;

            SaveSystem.UpdateStateWithMessage(ref serverState.SaveState, message);

            Console.WriteLine($"Got the message: {message}");

            PublishMessageToClients(message, serverState.Clients);
        }

        Console.WriteLine("Stop listening for messages");
    }

    private static void PublishMessageToClients(IMessage message, List<Client> clients)
    {
        foreach (var client in clients)
        {
            Networking.SendMessage(client.TcpClient, message);
        }
    }
}
