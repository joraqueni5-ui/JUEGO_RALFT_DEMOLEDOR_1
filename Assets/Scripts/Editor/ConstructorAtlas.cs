using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Atlas de sprites (Sprite Atlas V2): junta en una sola textura los objetos y decoración de los pisos y los efectos del
/// juego. Así la GPU dibuja muchos sprites seguidos sin cambiar de textura (menos llamadas de dibujo = más FPS en celulares).
/// No cambia cómo se ve nada: mismo filtro (punto, píxeles nítidos), sin compresión y con margen entre sprites.
/// Se crean una sola vez (si ya existen no se tocan); ConstructorBuilds lo llama antes de construir.
/// </summary>
public static class ConstructorAtlas
{
    private const string Carpeta = "Assets/Atlas";

    private static readonly string[][] Atlas =
    {
        new[] { "AtlasPisos", "Assets/Sprites/Pisos" },
        new[] { "AtlasEfectos", "Assets/Resources/Extras/Mejoras", "Assets/Resources/Extras/Peligros", "Assets/Resources/Extras/Vida",
                "Assets/Resources/Extras/Dano", "Assets/Resources/Extras/Carteles", "Assets/Resources/Extras/Vista",
                "Assets/Resources/Extras/Efectos", "Assets/Resources/Extras/Detalles", "Assets/Resources/Extras/Pared",
                "Assets/Resources/Extras/Martillos", "Assets/Resources/Extras/Poderes", "Assets/Resources/Extras/Accesorios",
                "Assets/Resources/Extras/Mini", "Assets/Resources/Extras/Ira" },
    };

    [MenuItem("Demoledor/Crear atlas de sprites")]
    private static void CrearDesdeMenu()
    {
        StringBuilder log = new StringBuilder();
        Asegurar(log);
        Debug.Log("[Atlas]\n" + log);
    }

    public static void Asegurar(StringBuilder log)
    {
        try
        {
            if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets", "Atlas");
            foreach (string[] a in Atlas)
            {
                string ruta = Carpeta + "/" + a[0] + ".spriteatlasv2";
                if (File.Exists(ruta)) continue;

                List<UnityEngine.Object> carpetas = new List<UnityEngine.Object>();
                for (int i = 1; i < a.Length; i++)
                {
                    UnityEngine.Object c = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(a[i]);
                    if (c != null) carpetas.Add(c);
                }
                if (carpetas.Count == 0) continue;

                SpriteAtlasAsset atlas = new SpriteAtlasAsset();
                atlas.Add(carpetas.ToArray());
                SpriteAtlasAsset.Save(atlas, ruta);
                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);

                SpriteAtlasImporter imp = AssetImporter.GetAtPath(ruta) as SpriteAtlasImporter;
                if (imp != null)
                {
                    imp.includeInBuild = true;
                    SpriteAtlasPackingSettings empaque = imp.packingSettings;
                    empaque.enableRotation = false;
                    empaque.enableTightPacking = false;
                    empaque.padding = 4;
                    imp.packingSettings = empaque;
                    SpriteAtlasTextureSettings textura = imp.textureSettings;
                    textura.filterMode = FilterMode.Point;
                    textura.generateMipMaps = false;
                    textura.readable = false;
                    textura.sRGB = true;
                    imp.textureSettings = textura;
                    TextureImporterPlatformSettings plataforma = imp.GetPlatformSettings("DefaultTexturePlatform");
                    plataforma.maxTextureSize = 4096;
                    plataforma.textureCompression = TextureImporterCompression.Uncompressed;
                    imp.SetPlatformSettings(plataforma);
                    imp.SaveAndReimport();
                }
                log.AppendLine("Atlas creado: " + a[0] + " (" + carpetas.Count + " carpetas)");
            }
        }
        catch (Exception e)
        {
            log.AppendLine("Atlas: no se pudo crear (" + e.Message + ")");
        }
    }
}
