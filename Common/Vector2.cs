using System.Numerics;
using MemoryPack;

namespace Common;

[MemoryPackable]
[MemoryPackUnion(0, typeof(Message))]
[MemoryPackUnion(1, typeof(SaveState))]
public partial interface IMessage;

[MemoryPackable]
public partial class Message : IMessage
{
    public Vector2<int> Tile;
    public bool Enabled;

    public override string ToString()
    {
        return $"{Tile}: {Enabled}";
    }
}

[MemoryPackable]
public partial struct Vector2<T> where T : INumber<T>
{
    public T X;
    public T Y;

    public Vector2(T x, T y)
    {
        X = x;
        Y = y;
    }

    public static bool operator ==(Vector2<T> left, Vector2<T> right)
    {
        return left.X == right.X
               && left.Y == right.Y;
    }

    public static bool operator !=(Vector2<T> left, Vector2<T> right)
    {
        return left.X == right.X
               && left.Y == right.Y;
    }

    public override string ToString()
    {
        return $"{X}, {Y}";
    }
}
