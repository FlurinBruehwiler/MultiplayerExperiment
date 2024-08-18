﻿using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Common;
using MemoryPack;

namespace MultiplayerClient;

public class Server
{
    public required BlockingCollection<Message> MessagesToSend;
    public required ConcurrentBag<Message> MessagesToProcess;
    public required TcpClient TcpClient;
}

public static class Multiplayer
{
    public static Server Run()
    {
        var ipEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 51234);

        var tcpClient = new TcpClient();

        tcpClient.Connect(ipEndpoint);

        var server = new Server
        {
            MessagesToSend = new BlockingCollection<Message>(),
            MessagesToProcess = new ConcurrentBag<Message>(),
            TcpClient = tcpClient
        };

        //send
        var sendThread = new Thread(() => SendMessageThread(server))
        {
            IsBackground = true
        };
        sendThread.Start();

        //listen
        var listenThread = new Thread(() => ReceiveMessageThread(server))
        {
            IsBackground = true
        };
        listenThread.Start();

        return server;
    }

    private static unsafe void ReceiveMessageThread(Server server)
    {
        while (true)
        {
            var message = Networking.GetNextMessage(server.TcpClient).GetAwaiter().GetResult();
            server.MessagesToProcess.Add(message);
        }
    }

    private static unsafe void SendMessageThread(Server server)
    {
        while (true)
        {
            var message = server.MessagesToSend.Take();

            Console.WriteLine($"Sending message {message}");

            Networking.SendMessage(server.TcpClient, message);
        }
    }
}
