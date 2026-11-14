using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Importación de los sprites nuevos (Assets/Sprites/Pisos/*, Poderes, NivelFinal, Efectos e Interfaz): Sprite (2D and UI),
/// 32 píxeles por unidad, filtro Point, sin compresión y malla Full Rect. El pivote depende de la carpeta: centro para suelos,
/// paredes, puertas, entradas, carteles, grietas, efectos e interfaz; abajo para reparables, decoración y poderes (así se
/// ordenan por profundidad en vista cenital). Los suelos y las paredes quedan legibles (el dibujo de paredes copia su textura).
/// Los catálogos (catalogo*.png) y las hojas de cuadros (*_hoja.png) no son sprites del juego: no se tocan.
/// </summary>
public class ImportadorSpritesNiveles : AssetPostprocessor
{
    public const int PixelesPorUnidad = 32;

    private static bool EsDeEsteArte(string ruta)
    {
        if (!ruta.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) return false;
        foreach (string raiz in RutasArte.Raices)
            if (ruta.StartsWith(raiz + "/", System.StringComparison.Ordinal)) return true;
        return false;
    }

    private static bool Ignorado(string ruta)
    {
        string nombre = System.IO.Path.GetFileName(ruta).ToLowerInvariant();
        return nombre.StartsWith("catalogo") || nombre.EndsWith("_hoja.png");
    }

    /// <summary>Pivote del sprite según su carpeta y nombre.</summary>
    public static SpriteAlignment PivoteDe(string ruta)
    {
        string r = ruta.ToLowerInvariant();
        string nombre = System.IO.Path.GetFileName(r);
        if (r.Contains("/efectos/") || r.Contains("/interfaz/")) return SpriteAlignment.Center;
        if (r.Contains("/poderes/")) return SpriteAlignment.BottomCenter;
        if (r.Contains("/reparables/")) return SpriteAlignment.BottomCenter;
        if (r.Contains("/decoracion/"))
            return nombre.StartsWith("alfombra") || nombre.StartsWith("ventana_pared") ? SpriteAlignment.Center : SpriteAlignment.BottomCenter;
        return SpriteAlignment.Center;
    }

    /// <summary>Borde de 9-slice: el marco del panel DATO ÚTIL (8 px) y su pestaña (4 px). El resto de sprites no tiene borde.</summary>
    private static Vector4 BordeDe(string ruta)
    {
        string nombre = System.IO.Path.GetFileNameWithoutExtension(ruta);
        if (!ruta.Contains("/Interfaz/PanelDatos/")) return Vector4.zero;
        if (nombre == "panel_dato") return new Vector4(8f, 8f, 8f, 8f);
        if (nombre == "etiqueta_area") return new Vector4(4f, 4f, 4f, 4f);
        return Vector4.zero;
    }

    private static bool DebeSerLegible(string ruta)
    {
        string r = ruta.ToLowerInvariant();
        return r.Contains("/suelos/") || r.Contains("/paredes/");
    }

    public override uint GetVersion() { return 3; }

    private void OnPreprocessTexture()
    {
        if (!EsDeEsteArte(assetPath) || Ignorado(assetPath)) return;
        Aplicar((TextureImporter)assetImporter, assetPath);
    }

    private static void Aplicar(TextureImporter imp, string ruta)
    {
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = PixelesPorUnidad;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.isReadable = DebeSerLegible(ruta);
        imp.spriteBorder = BordeDe(ruta);

        TextureImporterSettings ajustes = new TextureImporterSettings();
        imp.ReadTextureSettings(ajustes);
        ajustes.spriteMeshType = SpriteMeshType.FullRect;
        ajustes.spriteAlignment = (int)PivoteDe(ruta);
        ajustes.spritePivot = SpritesDerivados.PivoteDe(PivoteDe(ruta));
        imp.SetTextureSettings(ajustes);
    }

    // ------------------------------------------------------------ menú de revisión
    public static void Revisar()
    {
        List<string> carpetas = new List<string>();
        foreach (string raiz in RutasArte.Raices)
        {
            if (AssetDatabase.IsValidFolder(raiz)) carpetas.Add(raiz);
            else Debug.LogWarning("[Arte] No existe la carpeta " + raiz + ".");
        }
        if (carpetas.Count == 0) return;

        int revisados = 0, corregidos = 0, ignorados = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", carpetas.ToArray()))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (!EsDeEsteArte(ruta)) continue;
            if (Ignorado(ruta)) { ignorados++; continue; }
            revisados++;

            TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (imp == null) continue;

            TextureImporterSettings actual = new TextureImporterSettings();
            imp.ReadTextureSettings(actual);
            bool bien = imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single &&
                        Mathf.Approximately(imp.spritePixelsPerUnit, PixelesPorUnidad) && imp.filterMode == FilterMode.Point &&
                        imp.textureCompression == TextureImporterCompression.Uncompressed &&
                        actual.spriteMeshType == SpriteMeshType.FullRect &&
                        imp.spriteBorder == BordeDe(ruta) && actual.spriteAlignment == (int)PivoteDe(ruta) && actual.spritePivot == SpritesDerivados.PivoteDe(PivoteDe(ruta)) && imp.isReadable == DebeSerLegible(ruta);
            if (bien) continue;

            Aplicar(imp, ruta);
            imp.SaveAndReimport();
            corregidos++;
        }

        ConfigurarOrdenPorEjeY();
        AssetDatabase.SaveAssets();
        Debug.Log("[Arte] Sprites revisados: " + revisados + ", corregidos: " + corregidos + ", ignorados (catálogos y hojas): " + ignorados +
                  ". Orden por eje Y: activado (Custom Axis 0, 1, 0).");
    }

    /// <summary>
    /// Ordena lo que está más abajo en pantalla por delante: Transparency Sort Mode = Custom Axis (0, 1, 0), en el Renderer 2D de
    /// URP y en los ajustes de gráficos del proyecto.
    /// </summary>
    public static void ConfigurarOrdenPorEjeY()
    {
        GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
        GraphicsSettings.transparencySortAxis = new Vector3(0f, 1f, 0f);

        Object datos = AssetDatabase.LoadMainAssetAtPath("Assets/Settings/Renderer2D.asset");
        if (datos == null) { Debug.LogWarning("[Arte] No encuentro Assets/Settings/Renderer2D.asset: el orden por eje Y se configuró solo en los ajustes de gráficos."); return; }

        SerializedObject so = new SerializedObject(datos);
        SerializedProperty modo = so.FindProperty("m_TransparencySortMode");
        SerializedProperty eje = so.FindProperty("m_TransparencySortAxis");
        if (modo != null) modo.intValue = (int)TransparencySortMode.CustomAxis;
        if (eje != null) eje.vector3Value = new Vector3(0f, 1f, 0f);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(datos);
    }
}
