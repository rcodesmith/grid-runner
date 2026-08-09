using UnityEngine;

/// <summary>
/// Tracks the player's HP, applies contact damage with a brief
/// invulnerability window (with sprite flicker), updates the HUD, and
/// triggers game over at 0 HP.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    int _hp;
    float _invulnerableUntil;
    HudController _hud;
    SpriteRenderer _sprite;

    void Awake()
    {
        _hp = GameConfig.PlayerMaxHp;
        _sprite = GetComponent<SpriteRenderer>();
    }

    public void Init(HudController hud)
    {
        _hud = hud;
        _hud.SetHealth(1f);
    }

    public void TakeDamage(int amount)
    {
        if (_hp <= 0 || Time.time < _invulnerableUntil)
        {
            return;
        }

        _hp = Mathf.Max(0, _hp - amount);
        _invulnerableUntil = Time.time + GameConfig.InvulnerabilityWindow;

        if (_hud != null)
        {
            _hud.SetHealth((float)_hp / GameConfig.PlayerMaxHp);
        }

        if (_hp <= 0 && GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }

    void Update()
    {
        if (_sprite == null)
        {
            return;
        }

        // Flicker while invulnerable, solid otherwise.
        bool invulnerable = _hp > 0 && Time.time < _invulnerableUntil;
        _sprite.enabled = !invulnerable || Mathf.FloorToInt(Time.unscaledTime * 12f) % 2 == 0;
    }
}
