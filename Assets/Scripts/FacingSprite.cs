using UnityEngine;

/// <summary>Anything that faces a direction a sprite should turn to show.</summary>
public interface IFacing
{
    Vector2 Facing { get; }
}

/// <summary>
/// Shows the frame of a character sheet that matches its owner's facing,
/// mirroring the right-hand poses for the left-hand directions. Swaps
/// SpriteRenderer.sprite directly — no Animator, which would need an
/// editor-authored controller asset.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FacingSprite : MonoBehaviour
{
    SpriteRenderer _renderer;
    Sprite[] _frames;
    IFacing _source;

    /// <summary>frames may be null when the sheet failed to load (already logged); nothing is drawn then.</summary>
    public void Init(Sprite[] frames, IFacing source)
    {
        _renderer = GetComponent<SpriteRenderer>();
        _frames = frames;
        _source = source;
        Apply();
    }

    void LateUpdate()
    {
        Apply();
    }

    void Apply()
    {
        if (_frames == null || _source == null)
        {
            return;
        }

        var pose = FacingPose.For(_source.Facing);
        _renderer.sprite = _frames[pose.Index];
        _renderer.flipX = pose.FlipX;
    }
}
