using MemoryPack;

namespace Common;

[MemoryPackable]
public partial class SaveState : IMessage
{
    public required List<Tile> Tiles;
}

[MemoryPackable]
public partial struct Tile
{
    public required Vector2<int> Position;
}

public static class SaveSystem
{
    public static void UpdateStateWithMessage(ref SaveState saveState, IMessage message)
    {
        if (message is SaveState s)
        {
            saveState = s;
        }
        else if (message is Message m)
        {
            var idx = saveState.Tiles.FindIndex(x => x.Position == m.Tile);

            if (idx == -1 && m.Enabled)
            {
                saveState.Tiles.Add(new Tile
                {
                    Position = m.Tile
                });
            }
            else if(idx != -1 && !m.Enabled)
            {
                saveState.Tiles.RemoveAt(idx);
            }
        }
    }

    public static SaveState? LoadFromDisk()
    {
        var path = GetSavePath();

        if (!File.Exists(path))
        {
            return null;
        }

        var data = File.ReadAllBytes(path);
        var saveState = MemoryPackSerializer.Deserialize<SaveState>(data);

        return saveState;
    }

    public static void SafeToDisk(SaveState serverState)
    {
        var data = MemoryPackSerializer.Serialize(serverState);

        File.WriteAllBytes(GetSavePath(), data);
    }

    private static string GetSavePath()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "level.dat");
    }
}
