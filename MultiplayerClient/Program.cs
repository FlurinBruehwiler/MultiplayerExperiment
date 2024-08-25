using System.Numerics;
using Common;
using Raylib_CsLo;
using Color = Raylib_CsLo.Color;
using Rectangle = Raylib_CsLo.Rectangle;

namespace MultiplayerClient;

public enum PlacementState
{
    None,
    Add,
    Remove
}

public class GameState
{
    public required DateTimeOffset LastSave;
    public required Camera2D Camera;
    // public required List<Tile> Tiles;
    public required SaveState SaveState;
    public required PlacementState PlacementState;

    public required Server? Server;
}

public static class Program
{
    public const float TileSize = 50;

    public static void Main()
    {
        var server = Multiplayer.Run();

        var gameState = new GameState
        {
            Camera = new Camera2D
            {
                zoom = 1,
                offset = Vector2.Zero,
                target = Vector2.Zero,
                rotation = 0
            },
            SaveState = new SaveState
            {
                Tiles = []
            },
            Server = server,
            LastSave = DateTimeOffset.Now,
            PlacementState = PlacementState.None
        };

        if (server is null)
        {
            var saveState = SaveSystem.LoadFromDisk();
            if (saveState != null)
            {
                gameState.SaveState = saveState;
            }
        }

        Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        Raylib.InitWindow(900, 450, "MultiplayerExperiment");
        Raylib.SetTargetFPS(60);

        //Main loop

        while (!Raylib.WindowShouldClose())
        {
            Frame(gameState);
        }
    }

    private static void Frame(GameState gameState)
    {
        //input handling
        HandleNavigation(ref gameState.Camera);

        if (gameState.Server != null)
        {
            HandleNetworkMessages(gameState);
        }

        HandleTilePlacement(gameState);

        if (gameState.Server == null)
        {
            RunSaveSystem(gameState);
        }

        //drawing
        Raylib.BeginDrawing();

        Raylib.ClearBackground(Raylib.GRAY);

        DrawUi(gameState);

        Raylib.BeginMode2D(gameState.Camera);

        DrawWorld(gameState);

        Raylib.EndMode2D();

        Raylib.EndDrawing();
    }

    private static void DrawUi(GameState gameState)
    {
        Color textColor = Raylib.BLACK;
        const int borderOffset = 10;
        var topOffset = borderOffset;

        string mode = gameState.Server == null ? "local" : gameState.Server.TcpClient.Connected ? "remote" : "remote (disconnected)";

        Raylib.DrawText(mode, borderOffset, topOffset, 20, textColor);
        topOffset += 30;

        var fps = 1 / Raylib.GetFrameTime();

        Raylib.DrawText($"FPS: {(int)fps}", borderOffset,  topOffset, 20, textColor);
        topOffset += 30;
    }

    private static void RunSaveSystem(GameState gameState)
    {
        if ((DateTimeOffset.Now - gameState.LastSave).TotalSeconds > 5)
        {
            Console.WriteLine("AutoSave");
            SaveSystem.SafeToDisk(gameState.SaveState);
            gameState.LastSave = DateTimeOffset.Now;
        }
        else if(Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL) && Raylib.IsKeyPressed(KeyboardKey.KEY_S))
        {
            Console.WriteLine("Manual Save");
            SaveSystem.SafeToDisk(gameState.SaveState);
            gameState.LastSave = DateTimeOffset.Now;
        }
    }

    private static void HandleNetworkMessages(GameState gameState)
    {
        if (gameState.Server == null)
            return;

        while (gameState.Server.MessagesToProcess.TryTake(out var message))
        {
            SaveSystem.UpdateStateWithMessage(ref gameState.SaveState, message);
        }
    }

    private static void DrawWorld(GameState gameState)
    {
        foreach (var tile in gameState.SaveState.Tiles)
        {
            const float gap = 1;

            var pos = tile.Position.ToFloatVec() * TileSize + new Vector2(gap, gap);

            Raylib.DrawRectangleRounded(new Rectangle(pos.X, pos.Y, TileSize - gap * 2, TileSize - gap * 2), 0.1f, 10, Raylib.BLACK);
        }
    }

    private static void HandleTilePlacement(GameState gameState)
    {
        if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
        {
            var clickedTile = GetTilePositionContaining(Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), gameState.Camera));

            var idx = gameState.SaveState.Tiles.FindIndex(x => x.Position == clickedTile);

            if (idx == -1)
            {
                if (gameState.PlacementState != PlacementState.Remove)
                {
                    gameState.PlacementState = PlacementState.Add;

                    gameState.SaveState.Tiles.Add(new Tile
                    {
                        Position = clickedTile
                    });
                    gameState.Server?.MessagesToSend.Add(new Message
                    {
                        Tile = clickedTile,
                        Enabled = true
                    });
                }
            }
            else
            {
                if (gameState.PlacementState != PlacementState.Add)
                {
                    gameState.PlacementState = PlacementState.Remove;

                    gameState.SaveState.Tiles.RemoveAt(idx);
                    gameState.Server?.MessagesToSend.Add(new Message
                    {
                        Tile = clickedTile,
                        Enabled = false
                    });
                }
            }
        }
        else
        {
            gameState.PlacementState = PlacementState.None;
        }
    }

    private static Vector2<int> GetTilePositionContaining(Vector2 floatVec)
    {
        var res = new Vector2<int>((int)(floatVec.X / TileSize), (int)(floatVec.Y / TileSize));
        if (floatVec.Y < 0)
        {
            res.Y--;
        }

        if (floatVec.X < 0)
        {
            res.X--;
        }

        return res;
    }

    static void HandleNavigation(ref Camera2D camera)
    {
        if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_RIGHT))
        {
            var delta = Raylib.GetMouseDelta();
            delta = RayMath.Vector2Scale(delta, -1.0f/camera.zoom);

            camera.target += delta;
        }

        // Zoom based on mouse wheel
        var wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0)
        {
            // Get the world point that is under the mouse
            var mouseWorldPos = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), camera);

            // Set the offset to where the mouse is
            camera.offset = Raylib.GetMousePosition();

            // Set the target to match, so that the camera maps the world space point
            // under the cursor to the screen space point under the cursor at any zoom
            camera.target = mouseWorldPos;

            // Zoom increment
            const float zoomIncrement = 0.125f;

            camera.zoom += wheel * zoomIncrement;
            if (camera.zoom < zoomIncrement) camera.zoom = zoomIncrement;
        }
    }

    public static Vector2 ToFloatVec(this Vector2<int> vec)
    {
        return new Vector2(vec.X, vec.Y);
    }
}
