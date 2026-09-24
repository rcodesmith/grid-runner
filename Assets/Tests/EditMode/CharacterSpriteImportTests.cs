using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Every committed character sheet must import as crisp 16x16 pixel art,
/// sliced into one named sub-sprite per frame. The import script enforces
/// these settings; nothing is set by hand, so a hand edit shows up here.
/// Add each new sheet to Sheets.
/// </summary>
public class CharacterSpriteImportTests
{
    static readonly object[] Sheets =
    {
        new object[] { "warrior", 5 },
        new object[] { "grunt", 5 },
        new object[] { "axe", 1 },
    };

    [TestCaseSource(nameof(Sheets))]
    public void SheetImportsAsPointFilteredSixteenPixelArt(string sheet, int frames)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(PathOf(sheet));

        Assert.That(importer, Is.Not.Null, "missing sheet " + PathOf(sheet));
        Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
        Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(16f));
        Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
        Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
        Assert.That(importer.mipmapEnabled, Is.False);
        Assert.That(importer.alphaIsTransparency, Is.True);

        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        Assert.That((width, height), Is.EqualTo((frames * 16, 16)), "one row of 16x16 frames");
    }

    [TestCaseSource(nameof(Sheets))]
    public void SheetIsSlicedIntoNamedFramesLeftToRight(string sheet, int frames)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(PathOf(sheet))
            .OfType<Sprite>()
            .OrderBy(s => s.rect.x)
            .ToArray();

        var expectedNames = Enumerable.Range(0, frames).Select(i => sheet + "_" + i);
        Assert.That(sprites.Select(s => s.name), Is.EqualTo(expectedNames));
        for (int i = 0; i < sprites.Length; i++)
        {
            Assert.That(sprites[i].rect, Is.EqualTo(new Rect(i * 16, 0, 16, 16)), sprites[i].name);
        }
    }

    [TestCaseSource(nameof(Sheets))]
    public void LoaderReturnsFramesInIndexOrder(string sheet, int frames)
    {
        var loaded = CharacterSprites.Load(sheet, frames);

        var expectedNames = Enumerable.Range(0, frames).Select(i => sheet + "_" + i);
        Assert.That(loaded.Select(s => s.name), Is.EqualTo(expectedNames));
    }

    static string PathOf(string sheet) => "Assets/Resources/Sprites/" + sheet + ".png";
}
