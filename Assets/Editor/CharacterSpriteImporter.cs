using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Owns the import settings of every character sheet under
/// Assets/Resources/Sprites/: crisp 16x16 pixel art, sliced left to right
/// into frames named "&lt;sheet&gt;_&lt;i&gt;". Settings are never set by hand in
/// the inspector — this script reapplies them on every import, so deleting a
/// sheet's .meta and reimporting reproduces it exactly.
/// </summary>
public class CharacterSpriteImporter : AssetPostprocessor
{
    const string SheetFolder = "Assets/Resources/Sprites/";
    const int FrameSize = 16;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(SheetFolder, StringComparison.Ordinal))
        {
            return;
        }

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = FrameSize;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        Slice(importer);
    }

    void Slice(TextureImporter importer)
    {
        string sheet = Path.GetFileNameWithoutExtension(assetPath);
        var (width, height) = PngSize(assetPath);
        if (width % FrameSize != 0 || height != FrameSize)
        {
            Debug.LogError($"{assetPath} is {width}x{height}; character sheets must be one row of {FrameSize}x{FrameSize} frames.");
        }

        var rects = Enumerable.Range(0, width / FrameSize)
            .Select(i => new SpriteRect
            {
                name = sheet + "_" + i,
                rect = new Rect(i * FrameSize, 0, FrameSize, FrameSize),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = StableId(sheet + "_" + i),
            })
            .ToArray();

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
            .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
    }

    // Derived from the frame name rather than random, so a regenerated .meta
    // is identical to the one it replaces.
    static GUID StableId(string frameName)
    {
        using (var md5 = MD5.Create())
        {
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(frameName));
            return new GUID(string.Concat(hash.Select(b => b.ToString("x2"))));
        }
    }

    // The texture isn't loaded yet during preprocessing, so read the size
    // straight from the PNG header: width and height open the IHDR chunk.
    static (int width, int height) PngSize(string path)
    {
        using (var reader = new BinaryReader(File.OpenRead(path)))
        {
            reader.BaseStream.Seek(16, SeekOrigin.Begin);
            return (ReadBigEndian(reader), ReadBigEndian(reader));
        }
    }

    static int ReadBigEndian(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    }
}
