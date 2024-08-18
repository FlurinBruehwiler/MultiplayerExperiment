namespace MultiplayerServer;

public static class Program
{
    public static void Main()
    {
        var clients = new List<Client>();

        Server.ListenForConnections(clients);
    }
}

