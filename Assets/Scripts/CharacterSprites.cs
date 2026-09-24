using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Loads a character sheet from Resources/Sprites by name. CharacterSpriteImporter
/// slices each sheet into frames named "&lt;sheet&gt;_&lt;i&gt;"; this returns them in
/// index order. A missing sheet or wrong frame count is logged as an error —
/// there is deliberately no fallback to a placeholder.
/// </summary>
public static class CharacterSprites
{
    /// <summary>The sheet's frames in index order, or null (after logging) if it is unusable.</summary>
    public static Sprite[] Load(string sheet, int expectedFrames)
    {
        var frames = Resources.LoadAll<Sprite>("Sprites/" + sheet)
            .OrderBy(s => FrameIndex(sheet, s.name))
            .ToArray();

        if (frames.Length == 0)
        {
            Debug.LogError($"Character sheet '{sheet}' not found at Resources/Sprites/{sheet}.png.");
            return null;
        }

        if (frames.Length != expectedFrames)
        {
            Debug.LogError($"Character sheet '{sheet}' has {frames.Length} frames; expected {expectedFrames}.");
            return null;
        }

        return frames;
    }

    static int FrameIndex(string sheet, string frameName)
    {
        string prefix = sheet + "_";
        return frameName.StartsWith(prefix, StringComparison.Ordinal)
            && int.TryParse(frameName.Substring(prefix.Length), out int index)
                ? index
                : int.MaxValue;
    }
}
