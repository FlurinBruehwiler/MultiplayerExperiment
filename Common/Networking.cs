using System.Net.Sockets;
using MemoryPack;

namespace Common;

public static class Networking
{
    public static unsafe void SendMessage(TcpClient tcpClient, Message message)
    {
        var binaryMessage = MemoryPackSerializer.Serialize(message);

        var stream = tcpClient.GetStream();

        var length = binaryMessage.Length;

        stream.Write(new Span<byte>(&length, 4));

        stream.Write(binaryMessage);
    }

    public static async ValueTask<Message> GetNextMessage(TcpClient tcpClient)
    {
        var stream = tcpClient.GetStream();

        byte[] header = new byte[4]; //todo pool

        //read header of 4 bytes, which indicates the length
        await stream.ReadExactlyAsync(header);

        var length = ConvertToInt(header);

        byte[] messageAsBytes = new byte[length]; //todo pool

        await stream.ReadExactlyAsync(messageAsBytes);

        return MemoryPackSerializer.Deserialize<Message>(messageAsBytes);
    }

    private static unsafe int ConvertToInt(byte[] array)
    {
        fixed (byte* firstChar = array)
        {
            int* i = (int*)firstChar;
            return *i;
        }
    }
}
