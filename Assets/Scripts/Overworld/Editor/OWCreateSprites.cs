using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Génère les sprites de base pour l'Overworld dans Assets/Sprites/Overworld/.
/// Menu : Overworld / Generate Sprites
/// </summary>
public static class OWCreateSprites
{
    private const string OutputFolder = "Assets/Sprites/Overworld";

    [MenuItem("Overworld/Generate Sprites")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(OutputFolder);

        CreateSquareSprite("Portal_White",  Color.white,                       64);
        CreateSquareSprite("Portal_Cyan",   new Color(0.2f, 0.9f, 1f),        64);
        CreateSquareSprite("Portal_Blue",   new Color(0.3f, 0.5f, 1f),        64);
        CreateSquareSprite("Square_White",  Color.white,                       32);
        CreateCircleSprite("Circle_White",  Color.white,                       64);
        CreateCircleSprite("Ball_White",    Color.white,                       64);

        AssetDatabase.Refresh();
        Debug.Log($"[OWCreateSprites] Sprites générés dans {OutputFolder}");
        EditorUtility.DisplayDialog("Sprites Overworld",
            $"Sprites créés dans {OutputFolder}", "OK");
    }

    // ── Carré ─────────────────────────────────────────────────────────────────

    private static void CreateSquareSprite(string name, Color color, int size)
    {
        string path = $"{OutputFolder}/{name}.png";
        if (File.Exists(path)) return;

        var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pix  = new Color[size * size];
        for (int i = 0; i < pix.Length; i++) pix[i] = color;
        tex.SetPixels(pix);
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        ConfigureAsSprite(path, size);
    }

    // ── Cercle ────────────────────────────────────────────────────────────────

    private static void CreateCircleSprite(string name, Color color, int size)
    {
        string path = $"{OutputFolder}/{name}.png";
        if (File.Exists(path)) return;

        var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pix    = new Color[size * size];
        float cx   = size * 0.5f;
        float cy   = size * 0.5f;
        float r    = size * 0.47f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist     = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float aa       = Mathf.Clamp01(r - dist + 0.5f);  // anti-aliasing 1px
                pix[y * size + x] = new Color(color.r, color.g, color.b, aa);
            }
        }

        tex.SetPixels(pix);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        ConfigureAsSprite(path, size);
    }

    // ── Import settings ───────────────────────────────────────────────────────

    private static void ConfigureAsSprite(string path, int size)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) return;

        importer.textureType        = TextureImporterType.Sprite;
        importer.spriteImportMode   = SpriteImportMode.Single;
        importer.spritePivot        = new Vector2(0.5f, 0.5f);
        importer.spritePixelsPerUnit = size;
        importer.filterMode         = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }
}
