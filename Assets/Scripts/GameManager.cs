using UnityEngine;

/// <summary>
/// Game-over detection and restart. On game over: pause time and show the
/// Game Over panel. On R: destroy the world root and rebuild it via
/// Bootstrap.BuildWorld() for a full state reset (no scene reload — the
/// bootstrap RuntimeInitializeOnLoadMethod only fires once per Play session).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    GameObject _root;
    HudController _hud;

    public bool IsGameOver { get; private set; }

    public void Init(GameObject root, HudController hud)
    {
        _root = root;
        _hud = hud;
    }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (IsGameOver && Input.GetKeyDown(KeyCode.R))
        {
            Restart();
        }
    }

    public void TriggerGameOver()
    {
        if (IsGameOver)
        {
            return;
        }

        IsGameOver = true;
        Time.timeScale = 0f;
        if (_hud != null)
        {
            _hud.ShowGameOver();
        }
    }

    public void Restart()
    {
        var oldRoot = _root;
        _root = null;

        if (oldRoot != null)
        {
            // Deactivate first so the old camera/audio listener stop
            // immediately (Destroy is deferred to end of frame).
            oldRoot.SetActive(false);
            Destroy(oldRoot);
        }

        Bootstrap.BuildWorld();
    }
}
