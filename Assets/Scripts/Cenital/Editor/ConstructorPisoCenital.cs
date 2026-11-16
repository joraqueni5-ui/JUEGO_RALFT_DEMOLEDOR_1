using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Utilidades de arte provisional (baldosas y sprites de 32 px) que usa ConstructorPisoDesdeMapa al reconstruir un mapa.</summary>
public static class ConstructorPisoCenital
{
    private const string CarpetaArte = "Assets/Arte/PisoCenital";

    internal static void AsegurarCarpeta(string padre, string nombre)
    {
        if (!AssetDatabase.IsValidFolder(padre + "/" + nombre)) AssetDatabase.CreateFolder(padre, nombre);
    }

    internal static Tile CrearBaldosa(string nombre, Texture2D textura, bool solida)
    {
        Sprite sprite = CrearSprite(nombre, textura, 32);
        string ruta = CarpetaArte + "/" + nombre + "_tile.asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(ruta);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, ruta);
        }
        tile.sprite = sprite;
        tile.colliderType = solida ? Tile.ColliderType.Grid : Tile.ColliderType.None;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    /// <summary>Guarda una textura como sprite en la carpeta de arte provisional.</summary>
    internal static Sprite CrearSpritePublico(string nombre, Texture2D textura, int pixelesPorUnidad)
    {
        return CrearSprite(nombre, textura, pixelesPorUnidad);
    }

    /// <summary>Guarda la textura como PNG (pixel art, 32 px por unidad) y devuelve su sprite.</summary>
    private static Sprite CrearSprite(string nombre, Texture2D textura, int pixelesPorUnidad)
    {
        string ruta = CarpetaArte + "/" + nombre + ".png";
        File.WriteAllBytes(ruta, textura.EncodeToPNG());
        Object.DestroyImmediate(textura);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);

        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = pixelesPorUnidad;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.isReadable = true;   // las paredes delgadas copian el suelo vecino
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    internal static Texture2D Lienzo(int ancho, int alto, Color relleno)
    {
        Texture2D t = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        Color[] px = new Color[ancho * alto];
        for (int i = 0; i < px.Length; i++) px[i] = relleno;
        t.SetPixels(px);
        return t;
    }

    /// <summary>Suelo de losetas: cuadros de 16 px alternando dos tonos.</summary>
    internal static Texture2D Tablero(Color a, Color b)
    {
        Texture2D t = Lienzo(32, 32, a);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                if (((x / 16) + (y / 16)) % 2 == 1) t.SetPixel(x, y, b);
        t.Apply();
        return t;
    }

    internal static Texture2D Borde(Color relleno, Color borde)
    {
        Texture2D t = Lienzo(32, 32, relleno);
        for (int i = 0; i < 32; i++)
        {
            t.SetPixel(i, 0, borde); t.SetPixel(i, 31, borde);
            t.SetPixel(0, i, borde); t.SetPixel(31, i, borde);
        }
        t.Apply();
        return t;
    }
}
