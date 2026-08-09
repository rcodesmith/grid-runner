using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates 1x1-world-unit square and circle sprites from runtime Texture2Ds,
/// so the project ships with zero imported art assets. Sprites are cached per
/// color; caches reset with domain reload on each Play session.
/// </summary>
public static class PlaceholderSprites
{
    const int TextureSize = 64;

    static readonly Dictionary<Color, Sprite> SquareCache = new Dictionary<Color, Sprite>();
    static readonly Dictionary<Color, Sprite> CircleCache = new Dictionary<Color, Sprite>();

    public static Sprite Square(Color color)
    {
        if (!SquareCache.TryGetValue(color, out var sprite))
        {
            sprite = Build(color, circle: false);
            SquareCache.Add(color, sprite);
        }
        return sprite;
    }

    public static Sprite Circle(Color color)
    {
        if (!CircleCache.TryGetValue(color, out var sprite))
        {
            sprite = Build(color, circle: true);
            CircleCache.Add(color, sprite);
        }
        return sprite;
    }

    static Sprite Build(Color color, bool circle)
    {
        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color[TextureSize * TextureSize];
        float center = (TextureSize - 1) * 0.5f;
        float radius = TextureSize * 0.5f - 1f;

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                if (circle)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    // 1px anti-aliased edge.
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * TextureSize + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                }
                else
                {
                    pixels[y * TextureSize + x] = color;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        var rect = new Rect(0f, 0f, TextureSize, TextureSize);
        var pivot = new Vector2(0.5f, 0.5f);
        var sprite = Sprite.Create(texture, rect, pivot, TextureSize);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
