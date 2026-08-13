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
    public bool HasWon { get; private set; }

    /// <summary>True once the round has ended either way; both states freeze time and offer a restart.</summary>
    public bool IsRoundOver => IsGameOver || HasWon;

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
        if (IsRoundOver && Input.GetKeyDown(KeyCode.R))
        {
            Restart();
        }
    }

    public void TriggerGameOver()
    {
        // A win already ended the round — don't overwrite it with a loss (e.g.
        // an enemy landing a final hit on the same frame the treasure is taken).
        if (IsRoundOver)
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

    public void TriggerWin()
    {
        if (IsRoundOver)
        {
            return;
        }

        HasWon = true;
        Time.timeScale = 0f;
        if (_hud != null)
        {
            _hud.ShowWin();
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
