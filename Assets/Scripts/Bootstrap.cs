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

        var cameraFollow = CreateCamera(root.transform);
        CreateArena(root.transform);
        var hud = CreateHud(root.transform);
        CreateGameManager(root, hud);
        var player = CreatePlayer(root.transform, hud);
        cameraFollow.Init(player.transform);
        CreateSpawner(root.transform, player.transform);
    }

    static CameraFollow CreateCamera(Transform parent)
    {
        var go = new GameObject("Main Camera");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(0f, 0f, -10f);

        var camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = GameConfig.CameraBackgroundColor;

        // Sized to show roughly one room at a time; CameraFollow keeps the
        // player centered as they roam between rooms.
        float halfHeightNeeded = GameConfig.ArenaHeight / 2f + GameConfig.WallThickness + 0.5f;
        float halfWidthNeeded = GameConfig.ArenaWidth / 2f + GameConfig.WallThickness + 0.5f;
        camera.orthographicSize = Mathf.Max(halfHeightNeeded, halfWidthNeeded / camera.aspect);

        go.AddComponent<AudioListener>();
        return go.AddComponent<CameraFollow>();
    }

    static void CreateArena(Transform parent)
    {
        var arena = new GameObject("Arena");
        arena.transform.SetParent(parent, false);

        float halfW = GameConfig.ArenaWidth / 2f;
        float halfH = GameConfig.ArenaHeight / 2f;
        float t = GameConfig.WallThickness;

        // Room 1 (the original arena). Its right side is the dividing wall.
        CreateFloor(arena.transform, "Floor", Vector2.zero, new Vector2(GameConfig.ArenaWidth, GameConfig.ArenaHeight));
        CreateWall(arena.transform, "Wall Top", new Vector2(0f, halfH + t / 2f), new Vector2(GameConfig.ArenaWidth + 2f * t, t));
        CreateWall(arena.transform, "Wall Bottom", new Vector2(0f, -(halfH + t / 2f)), new Vector2(GameConfig.ArenaWidth + 2f * t, t));
        CreateWall(arena.transform, "Wall Left", new Vector2(-(halfW + t / 2f), 0f), new Vector2(t, GameConfig.ArenaHeight));

        // Room 2 (bigger, attached to the right).
        float r2HalfW = GameConfig.Room2Width / 2f;
        float r2HalfH = GameConfig.Room2Height / 2f;
        float cx = GameConfig.Room2CenterX;

        CreateFloor(arena.transform, "Floor 2", new Vector2(cx, 0f), new Vector2(GameConfig.Room2Width, GameConfig.Room2Height));
        CreateWall(arena.transform, "Wall 2 Top", new Vector2(cx, r2HalfH + t / 2f), new Vector2(GameConfig.Room2Width + 2f * t, t));
        CreateWall(arena.transform, "Wall 2 Bottom", new Vector2(cx, -(r2HalfH + t / 2f)), new Vector2(GameConfig.Room2Width + 2f * t, t));
        CreateWall(arena.transform, "Wall 2 Right", new Vector2(cx + r2HalfW + t / 2f, 0f), new Vector2(t, GameConfig.Room2Height));

        // Dividing wall between the rooms, split around a doorway at y = 0.
        // Covers room 2's full (taller) left side, which also seals room 1's right.
        float doorHalf = GameConfig.DoorHeight / 2f;
        float segmentH = r2HalfH - doorHalf;
        CreateWall(arena.transform, "Wall Divider Top", new Vector2(GameConfig.DividerX, doorHalf + segmentH / 2f), new Vector2(t, segmentH));
        CreateWall(arena.transform, "Wall Divider Bottom", new Vector2(GameConfig.DividerX, -(doorHalf + segmentH / 2f)), new Vector2(t, segmentH));
        CreateFloor(arena.transform, "Door Floor", new Vector2(GameConfig.DividerX, 0f), new Vector2(t, GameConfig.DoorHeight));
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

    static GameObject CreatePlayer(Transform parent, HudController hud)
    {
        var go = new GameObject("Player");
        go.transform.SetParent(parent, false);
        go.transform.position = Vector3.zero;
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
