using System.Numerics;
using Common;
using Raylib_CsLo;

namespace MultiplayerClient;

public class GameState
{
    public required Camera2D Camera;
    public required List<Tile> Tiles;
    public required Server Server;
}

public struct Tile
{
    public required Vector2<int> Position;
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
            Tiles = [],
            Server = server
        };

        Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        Raylib.InitWindow(1800, 900, "MultiplayerExperiment");
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

        HandleTilePlacement(gameState);

        //drawing
        Raylib.BeginDrawing();

        Raylib.ClearBackground(Raylib.GRAY);

        Raylib.BeginMode2D(gameState.Camera);

        DrawWorld(gameState);

        Raylib.EndMode2D();

        Raylib.EndDrawing();
    }

    private static void DrawWorld(GameState gameState)
    {
        foreach (var tile in gameState.Tiles)
        {
            const float gap = 1;

            var pos = tile.Position.ToFloatVec() * TileSize + new Vector2(gap, gap);

            Raylib.DrawRectangleRounded(new Rectangle(pos.X, pos.Y, TileSize - gap * 2, TileSize - gap * 2), 0.1f, 10, Raylib.BLACK);
        }
    }

    private static void HandleTilePlacement(GameState gameState)
    {
        if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            var clickedTile = GetTilePositionContaining(Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), gameState.Camera));

            var idx = gameState.Tiles.FindIndex(x => x.Position == clickedTile);

            if (idx == -1)
            {
                gameState.Tiles.Add(new Tile
                {
                    Position = clickedTile
                });
                gameState.Server.Messages.Add(new Message
                {
                    Tile = clickedTile,
                    Enabled = true
                });
            }
            else
            {
                gameState.Tiles.RemoveAt(idx);
                gameState.Server.Messages.Add(new Message
                {
                    Tile = clickedTile,
                    Enabled = false
                });
            }
        }
    }

    private static Vector2<int> GetTilePositionContaining(Vector2 floatVec)
    {
        return new Vector2<int>((int)(floatVec.X / TileSize), (int)(floatVec.Y / TileSize));
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

            camera.zoom += (wheel*zoomIncrement);
            if (camera.zoom < zoomIncrement) camera.zoom = zoomIncrement;
        }
    }

    public static Vector2 ToFloatVec(this Vector2<int> vec)
    {
        return new Vector2(vec.X, vec.Y);
    }
}
