using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Code-first entry point. Runs automatically when Play starts and builds the
/// entire game world under a single "GameRoot" object — no scene authoring or
/// editor wiring required. GameManager.Restart() destroys the root and calls
/// BuildWorld() again for a full state reset.
/// </summary>
public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        // Clear whatever the default untitled scene contains (e.g. its Main
        // Camera / Directional Light) so we never end up with duplicate
        // cameras or audio listeners. Deactivate first: Destroy is deferred.
        foreach (var rootObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            rootObject.SetActive(false);
            Object.Destroy(rootObject);
        }

        BuildWorld();
    }

    /// <summary>Builds a fresh world. Safe to call again after destroying the previous root.</summary>
    public static void BuildWorld()
    {
        // TimeScale is paused on game over and, in the editor, persists across
        // play sessions — always start unpaused.
        Time.timeScale = 1f;

        var root = new GameObject("GameRoot");

        // The world's layout, built as data. Everything below places objects
        // by asking the dungeon, rather than recomputing geometry.
        var dungeon = GauntletDungeon.Build();
        Dungeon.Current = dungeon;

        var cameraFollow = CreateCamera(root.transform, dungeon);
        CreateArena(root.transform, dungeon);
        var hud = CreateHud(root.transform);
        CreateGameManager(root, hud);
        var player = CreatePlayer(root.transform, hud, GauntletDungeon.PlayerStart(dungeon));
        cameraFollow.Init(player.transform);
        CreateSpawner(root.transform, player.transform);
    }

    static CameraFollow CreateCamera(Transform parent, Dungeon dungeon)
    {
        var go = new GameObject("Main Camera");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(0f, 0f, -10f);

        var camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = GameConfig.CameraBackgroundColor;

        // Sized to show roughly one room at a time; CameraFollow keeps the
        // player centered as they roam between rooms. Framed on the room the
        // player starts in.
        var startRoom = dungeon.Rooms[0];
        float halfHeightNeeded = startRoom.Size.y / 2f + GameConfig.WallThickness + 0.5f;
        float halfWidthNeeded = startRoom.Size.x / 2f + GameConfig.WallThickness + 0.5f;
        camera.orthographicSize = Mathf.Max(halfHeightNeeded, halfWidthNeeded / camera.aspect);

        go.AddComponent<AudioListener>();
        return go.AddComponent<CameraFollow>();
    }

    static void CreateArena(Transform parent, Dungeon dungeon)
    {
        var arena = new GameObject("Arena");
        arena.transform.SetParent(parent, false);

        float t = GameConfig.WallThickness;

        // Each room gets a floor and its own outer walls. A room's wall is
        // skipped where a doorway pierces it — the divider built below takes
        // over that side, split around the gap.
        foreach (var room in dungeon.Rooms)
        {
            CreateFloor(arena.transform, room.Name + " Floor", room.Center, room.Size);

            // Top and bottom run the full width plus the corners.
            CreateWall(arena.transform, room.Name + " Wall Top",
                new Vector2(room.Center.x, room.MaxY + t / 2f),
                new Vector2(room.Size.x + 2f * t, t));
            CreateWall(arena.transform, room.Name + " Wall Bottom",
                new Vector2(room.Center.x, room.MinY - t / 2f),
                new Vector2(room.Size.x + 2f * t, t));

            if (!HasDoorwayOnSide(dungeon, room, left: true))
            {
                CreateWall(arena.transform, room.Name + " Wall Left",
                    new Vector2(room.MinX - t / 2f, room.Center.y),
                    new Vector2(t, room.Size.y));
            }

            if (!HasDoorwayOnSide(dungeon, room, left: false))
            {
                CreateWall(arena.transform, room.Name + " Wall Right",
                    new Vector2(room.MaxX + t / 2f, room.Center.y),
                    new Vector2(t, room.Size.y));
            }
        }

        // Dividing walls, each split around its doorway. A divider spans the
        // taller of the two rooms it separates, which seals the shorter room's
        // side as well.
        foreach (var doorway in dungeon.Doorways)
        {
            CreateDivider(arena.transform, doorway,
                Mathf.Max(doorway.A.Size.y, doorway.B.Size.y));
        }

        CreateSpawnPoints(arena.transform, dungeon);

        // Treasure at the far end of the last room — the length of the dungeon
        // away from the player's start.
        Treasure.Spawn(arena.transform, GauntletDungeon.TreasureAt(dungeon));
    }

    // True when a doorway pierces the given vertical side of this room, so the
    // solid wall should be left out and a divider built instead.
    static bool HasDoorwayOnSide(Dungeon dungeon, Room room, bool left)
    {
        float edge = left ? room.MinX : room.MaxX;

        foreach (var doorway in dungeon.Doorways)
        {
            if (doorway.Other(room) == null)
            {
                continue;
            }

            // The doorway sits in the gap just outside this edge.
            if (Mathf.Abs(doorway.Position.x - edge) <= GameConfig.WallThickness)
            {
                return true;
            }
        }
        return false;
    }

    // A wall spanning the given height, centered on the doorway's x, with the
    // doorway gap punched through it and a floor strip laid across the gap.
    static void CreateDivider(Transform parent, Doorway doorway, float height)
    {
        float t = GameConfig.WallThickness;
        float x = doorway.Position.x;
        float doorCenterY = doorway.Position.y;
        float doorHalf = doorway.Width / 2f;
        string name = doorway.A.Name + " to " + doorway.B.Name;

        float topSegmentH = (height / 2f) - doorHalf;
        float bottomSegmentH = (height / 2f) - doorHalf;

        CreateWall(parent, name + " Divider Top",
            new Vector2(x, doorCenterY + doorHalf + topSegmentH / 2f),
            new Vector2(t, topSegmentH));
        CreateWall(parent, name + " Divider Bottom",
            new Vector2(x, doorCenterY - doorHalf - bottomSegmentH / 2f),
            new Vector2(t, bottomSegmentH));
        CreateFloor(parent, name + " Door Floor",
            new Vector2(x, doorCenterY),
            new Vector2(t, doorway.Width));
    }

    // Spawn points tucked into room corners so enemies never appear on top of
    // the player. The first two rooms use their far corners only; the last
    // room uses all four, to defend the treasure.
    static void CreateSpawnPoints(Transform parent, Dungeon dungeon)
    {
        float inset = GameConfig.SpawnPointCornerInset;

        for (int i = 0; i < dungeon.Rooms.Count; i++)
        {
            var room = dungeon.Rooms[i];
            bool isLastRoom = i == dungeon.Rooms.Count - 1;

            int index = 0;
            foreach (var anchor in Dungeon.CornerAnchors(room, inset))
            {
                // CornerAnchors yields left-top, left-bottom, right-top,
                // right-bottom. Rooms before the last keep only the two
                // corners furthest from the player's approach.
                if (!isLastRoom && !KeepsCorner(i, index))
                {
                    index++;
                    continue;
                }

                SpawnPoint.Create(parent, room.Name + " Spawn " + index, anchor);
                index++;
            }
        }
    }

    // Room 1 spawns on its left corners, room 2 on its right — the layout the
    // game has always had.
    static bool KeepsCorner(int roomIndex, int cornerIndex)
    {
        bool isLeftCorner = cornerIndex < 2;
        return roomIndex == 0 ? isLeftCorner : !isLeftCorner;
    }

    // Floor pieces are visual only, no collider.
    static void CreateFloor(Transform parent, string name, Vector2 center, Vector2 size)
    {
        var floor = new GameObject(name);
        floor.transform.SetParent(parent, false);
        floor.transform.position = center;
        floor.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = floor.AddComponent<SpriteRenderer>();
        renderer.sprite = PlaceholderSprites.Square(GameConfig.FloorColor);
        renderer.sortingOrder = -10;
    }

    static void CreateWall(Transform parent, string name, Vector2 center, Vector2 size)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.position = center;
        wall.transform.localScale = new Vector3(size.x, size.y, 1f);

        var renderer = wall.AddComponent<SpriteRenderer>();
        renderer.sprite = PlaceholderSprites.Square(GameConfig.WallColor);

        // No Rigidbody2D -> static collider. Projectiles identify walls as
        // "collider with no attached rigidbody".
        wall.AddComponent<BoxCollider2D>();
    }

    static HudController CreateHud(Transform parent)
    {
        var go = new GameObject("HUD");
        go.transform.SetParent(parent, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // HudController builds the HP bar and Game Over panel in Awake.
        return go.AddComponent<HudController>();
    }

    static void CreateGameManager(GameObject root, HudController hud)
    {
        var go = new GameObject("GameManager");
        go.transform.SetParent(root.transform, false);
        var gameManager = go.AddComponent<GameManager>();
        gameManager.Init(root, hud);
    }

    static GameObject CreatePlayer(Transform parent, HudController hud, Vector2 startPosition)
    {
        var go = new GameObject("Player");
        go.transform.SetParent(parent, false);
        go.transform.position = startPosition;
        go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = PlaceholderSprites.Square(GameConfig.PlayerColor);
        renderer.sortingOrder = 10;

        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        go.AddComponent<BoxCollider2D>();

        go.AddComponent<PlayerController>();
        go.AddComponent<PlayerShooting>();
        var health = go.AddComponent<PlayerHealth>();
        health.Init(hud);

        return go;
    }

    static void CreateSpawner(Transform parent, Transform player)
    {
        var go = new GameObject("EnemySpawner");
        go.transform.SetParent(parent, false);
        var spawner = go.AddComponent<EnemySpawner>();
        spawner.Init(player, parent);
    }
}
