using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the screen-space HUD (HP bar + hidden Game Over panel) from code in
/// Awake, on the Canvas object Bootstrap creates. The HP bar fill is an
/// anchor-driven Image so no sprite assets are needed.
/// </summary>
public class HudController : MonoBehaviour
{
    RectTransform _hpFill;
    GameObject _gameOverPanel;
    GameObject _winPanel;

    void Awake()
    {
        BuildHpBar();
        _gameOverPanel = BuildEndPanel("GameOverPanel", "GAME OVER", GameConfig.EnemyColor);
        _winPanel = BuildEndPanel("WinPanel", "YOU WIN!", GameConfig.TreasureColor);
    }

    public void SetHealth(float fraction)
    {
        _hpFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    public void ShowGameOver()
    {
        _gameOverPanel.SetActive(true);
    }

    public void ShowWin()
    {
        _winPanel.SetActive(true);
    }

    void BuildHpBar()
    {
        var back = CreateImage("HpBarBack", transform, GameConfig.HpBarBackColor);
        var backRect = back.rectTransform;
        backRect.anchorMin = new Vector2(0f, 1f);
        backRect.anchorMax = new Vector2(0f, 1f);
        backRect.pivot = new Vector2(0f, 1f);
        backRect.anchoredPosition = new Vector2(24f, -24f);
        backRect.sizeDelta = new Vector2(320f, 28f);

        var fill = CreateImage("HpBarFill", back.transform, GameConfig.HpBarFillColor);
        _hpFill = fill.rectTransform;
        _hpFill.anchorMin = Vector2.zero;
        _hpFill.anchorMax = Vector2.one;
        _hpFill.offsetMin = new Vector2(3f, 3f);
        _hpFill.offsetMax = new Vector2(-3f, -3f);
    }

    // Shared layout for both end-of-round panels: a full-screen dim, a big
    // colored title, and the restart hint. Starts hidden.
    GameObject BuildEndPanel(string name, string title, Color titleColor)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(transform, false);

        var dim = panel.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.7f);
        dim.raycastTarget = false;
        var dimRect = dim.rectTransform;
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;

        CreateText(name + "Title", panel.transform, title,
            96, titleColor, new Vector2(0f, 50f), new Vector2(1200f, 140f));
        CreateText(name + "Hint", panel.transform, "Press R to restart",
            40, Color.white, new Vector2(0f, -60f), new Vector2(1200f, 80f));

        panel.SetActive(false);
        return panel;
    }

    static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static Text CreateText(string name, Transform parent, string content,
        int fontSize, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;

        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        return text;
    }
}
