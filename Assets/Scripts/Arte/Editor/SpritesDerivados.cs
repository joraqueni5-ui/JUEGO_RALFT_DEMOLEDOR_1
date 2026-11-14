using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Genera arte NUEVO a partir de los sprites originales (que no se tocan): una copia girada un cuarto de vuelta, o una
/// baldosa repetida para cubrir varias celdas. Se guardan como PNG en Assets/Arte/PisoCenital/Derivados con la misma
/// importación del arte (32 px por unidad, filtro Point, sin compresión).
/// </summary>
public static class SpritesDerivados
{
    private const int Lado = 32;

    /// <summary>Medida del sprite en baldosas.</summary>
    public static Vector2Int Baldosas(Sprite s)
    {
        float ppu = Mathf.Max(1f, s.pixelsPerUnit);
        return new Vector2Int(Mathf.RoundToInt(s.rect.width / ppu), Mathf.RoundToInt(s.rect.height / ppu));
    }

    /// <summary>
    /// Devuelve el sprite listo para cubrir un grupo de 'ancho' x 'alto' baldosas: el mismo si ya mide eso, girado si mide
    /// lo contrario (un ascensor de 2 x 1 en una pared vertical), o repetido si es de una sola baldosa. 'girarBaldosa'
    /// gira cada baldosa antes de repetirla (escaleras en una pared lateral). Con cualquier otra medida devuelve el original.
    /// </summary>
    public static Sprite Ajustar(Sprite origen, int ancho, int alto, bool girarBaldosa)
    {
        if (origen == null) return null;
        string ruta = AssetDatabase.GetAssetPath(origen);
        if (string.IsNullOrEmpty(ruta)) return origen;

        Vector2Int c = Baldosas(origen);
        bool girar = false;
        int columnas = 1, filas = 1;
        if (c.x == ancho && c.y == alto) return origen;
        if (c.x == alto && c.y == ancho && ancho != alto) girar = true;
        else if (c.x == 1 && c.y == 1) { columnas = ancho; filas = alto; girar = girarBaldosa; }
        else return origen;

        string nombre = origen.name + "_" + ancho + "x" + alto + (girar ? "_g" : "");
        string destino = RutasArte.Derivados + "/" + nombre + ".png";
        Sprite existente = AssetDatabase.LoadAssetAtPath<Sprite>(destino);
        if (existente != null) return existente;

        Texture2D fuente = Leer(ruta);
        if (fuente == null) return origen;
        if (girar) fuente = GirarUnCuarto(fuente);

        Texture2D resultado = fuente;
        if (columnas > 1 || filas > 1)
        {
            resultado = new Texture2D(fuente.width * columnas, fuente.height * filas, TextureFormat.RGBA32, false);
            for (int i = 0; i < columnas; i++)
                for (int j = 0; j < filas; j++)
                    resultado.SetPixels(i * fuente.width, j * fuente.height, fuente.width, fuente.height, fuente.GetPixels());
            resultado.Apply();
            Object.DestroyImmediate(fuente);
        }
        return Guardar(resultado, destino);
    }

    /// <summary>Guarda la textura como PNG nuevo con la importación del arte y devuelve su sprite (con el pivote indicado; por omisión el centro).</summary>
    public static Sprite Guardar(Texture2D textura, string ruta, bool legible = true, SpriteAlignment alineacion = SpriteAlignment.Center)
    {
        string carpeta = Path.GetDirectoryName(ruta).Replace('\\', '/');
        Asegurar(carpeta);
        File.WriteAllBytes(ruta, textura.EncodeToPNG());
        Object.DestroyImmediate(textura);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);

        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = Lado;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.isReadable = legible;
        TextureImporterSettings ajustes = new TextureImporterSettings();
        imp.ReadTextureSettings(ajustes);
        ajustes.spriteMeshType = SpriteMeshType.FullRect;
        ajustes.spriteAlignment = (int)alineacion;
        ajustes.spritePivot = PivoteDe(alineacion);   // el pivote hay que escribirlo también: la alineación sola no lo mueve
        imp.SetTextureSettings(ajustes);
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    /// <summary>Pivote (0 a 1) de una alineación predefinida.</summary>
    public static Vector2 PivoteDe(SpriteAlignment a)
    {
        switch (a)
        {
            case SpriteAlignment.BottomLeft: return new Vector2(0f, 0f);
            case SpriteAlignment.BottomCenter: return new Vector2(0.5f, 0f);
            case SpriteAlignment.BottomRight: return new Vector2(1f, 0f);
            case SpriteAlignment.LeftCenter: return new Vector2(0f, 0.5f);
            case SpriteAlignment.RightCenter: return new Vector2(1f, 0.5f);
            case SpriteAlignment.TopLeft: return new Vector2(0f, 1f);
            case SpriteAlignment.TopCenter: return new Vector2(0.5f, 1f);
            case SpriteAlignment.TopRight: return new Vector2(1f, 1f);
            default: return new Vector2(0.5f, 0.5f);
        }
    }

    /// <summary>Lee un PNG del proyecto como textura legible (sin depender de su importación).</summary>
    public static Texture2D Leer(string ruta)
    {
        string completa = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ruta);
        if (!File.Exists(completa)) return null;
        Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Point;
        if (!ImageConversion.LoadImage(t, File.ReadAllBytes(completa))) { Object.DestroyImmediate(t); return null; }
        return t;
    }

    /// <summary>Gira la textura 90 grados en el sentido del reloj (el este pasa al sur).</summary>
    private static Texture2D GirarUnCuarto(Texture2D t)
    {
        int w = t.width, h = t.height;
        Color[] origen = t.GetPixels();
        Color[] giro = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                giro[((w - 1 - x) * h) + y] = origen[y * w + x];   // nuevo ancho = h; (x', y') = (y, w - 1 - x)
        Texture2D r = new Texture2D(h, w, TextureFormat.RGBA32, false);
        r.SetPixels(giro);
        r.Apply();
        Object.DestroyImmediate(t);
        return r;
    }

    internal static void Asegurar(string carpeta)
    {
        if (AssetDatabase.IsValidFolder(carpeta)) return;
        string padre = Path.GetDirectoryName(carpeta).Replace('\\', '/');
        Asegurar(padre);
        AssetDatabase.CreateFolder(padre, Path.GetFileName(carpeta));
    }
}
