using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Importa el arte de los pisos 1 y 2 (Assets/Sprites/Pisos) y lo asigna a sus DefinicionPiso
/// (menú Tools → Demoledor Unifranz → Aplicar arte de pisos 1 y 2).
///
/// - Deja cada PNG como sprite pixel art: 50 px por unidad, filtro Point, sin compresión y con
///   el pivote que usa el juego (objetos: esquina del marco; plataformas: arriba a la izquierda;
///   fondo: centro).
/// - Pone objetos, plataformas y fondo en los assets Piso1_Gastronomia y Piso2_Informatica.
/// - Las fichas informativas solo se rellenan si siguen con el texto provisional: lo que ya
///   hayas escrito a mano no se pisa.
/// No modifica ninguna escena. Se puede ejecutar varias veces.
///
/// El arte actual es PROVISIONAL (lo genera Tools/generar_arte_pisos.py). Para usar arte propio,
/// reemplaza los PNG manteniendo nombre y medidas: objetos 75 x 64, plataformas 40 x 12, fondo 704 x 396.
/// </summary>
public static class ArtePisos
{
    private const string CarpetaArte = "Assets/Sprites/Pisos";
    private const string CarpetaDatos = "Assets/Data/Pisos";

    // Pivote de los objetos: esquina superior izquierda del marco (14, 12 dentro del lienzo de 75 x 64).
    private static readonly Vector2 PivoteObjeto = new Vector2(14f / 75f, 1f - 12f / 64f);

    private struct Ficha
    {
        public string asset, descripcion, dato;
        public string[] objetos;
        public Color colorFondo;
    }

    private static readonly Ficha[] Fichas =
    {
        new Ficha
        {
            asset = "Piso1_Gastronomia",
            objetos = new[] { "Horno", "Refrigerador", "Estufa", "Mesa de trabajo" },
            descripcion = "Aquí se aprende a cocinar de forma profesional: técnicas de cocina, higiene, manejo de alimentos y organización de una cocina.",
            dato = "En una cocina profesional todo se deja listo antes de empezar. Se llama \"mise en place\" y evita accidentes.",
            colorFondo = new Color(0.23f, 0.20f, 0.20f, 1f),
        },
        new Ficha
        {
            asset = "Piso2_Informatica",
            objetos = new[] { "Monitor", "Servidor", "Computadora", "Impresora" },
            descripcion = "Aquí se aprende a crear programas, administrar redes y bases de datos, y proteger la información.",
            dato = "Haz copias de seguridad de tus archivos importantes: es la mejor defensa contra perder información.",
            colorFondo = new Color(0.08f, 0.10f, 0.17f, 1f),
        },
    };

    [MenuItem("Tools/Demoledor Unifranz/Aplicar arte de pisos 1 y 2", false, 23)]
    public static void Aplicar()
    {
        if (!AssetDatabase.IsValidFolder(CarpetaArte))
        {
            EditorUtility.DisplayDialog("Arte de pisos",
                "No existe " + CarpetaArte + ".\n\nGenera el arte con:\npython Tools/generar_arte_pisos.py", "OK");
            return;
        }

        AssetDatabase.Refresh();
        int importados = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { CarpetaArte }))
        {
            if (ConfigurarImportador(AssetDatabase.GUIDToAssetPath(guid))) importados++;
        }

        int asignados = 0;
        string faltantes = "";
        for (int i = 0; i < Fichas.Length; i++)
        {
            Ficha f = Fichas[i];
            DefinicionPiso piso = AssetDatabase.LoadAssetAtPath<DefinicionPiso>(CarpetaDatos + "/" + f.asset + ".asset");
            if (piso == null) { faltantes += "\n• " + f.asset + " (ejecuta 'Crear definiciones de piso')"; continue; }
            if (!AsignarArte(piso, i + 1, f)) { faltantes += "\n• Faltan sprites del piso " + (i + 1); continue; }
            asignados++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Arte de pisos",
            "Listo.\n\n• PNG configurados: " + importados + "\n• Pisos con arte asignado: " + asignados + " de 2" +
            (faltantes.Length > 0 ? "\n\nProblemas:" + faltantes : "") +
            "\n\nNo se modificó ninguna escena.", "OK");
    }

    private static bool ConfigurarImportador(string ruta)
    {
        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) return false;

        string nombre = Path.GetFileNameWithoutExtension(ruta);
        SpriteAlignment alineacion;
        Vector2 pivote;
        if (nombre.Contains("_objeto")) { alineacion = SpriteAlignment.Custom; pivote = PivoteObjeto; }
        else if (nombre.Contains("_plataforma")) { alineacion = SpriteAlignment.TopLeft; pivote = new Vector2(0f, 1f); }
        else { alineacion = SpriteAlignment.Center; pivote = new Vector2(0.5f, 0.5f); }

        TextureImporterSettings ajustes = new TextureImporterSettings();
        imp.ReadTextureSettings(ajustes);

        bool igual = imp.textureType == TextureImporterType.Sprite
                     && imp.spriteImportMode == SpriteImportMode.Single
                     && Mathf.Approximately(imp.spritePixelsPerUnit, 50f)
                     && imp.filterMode == FilterMode.Point
                     && imp.textureCompression == TextureImporterCompression.Uncompressed
                     && !imp.mipmapEnabled
                     && ajustes.spriteAlignment == (int)alineacion
                     && (alineacion != SpriteAlignment.Custom || Vector2.Distance(ajustes.spritePivot, pivote) < 0.0001f);
        if (igual) return false;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 50f;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.maxTextureSize = 2048;

        imp.ReadTextureSettings(ajustes);
        ajustes.spriteAlignment = (int)alineacion;
        ajustes.spritePivot = pivote;
        ajustes.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(ajustes);
        imp.SaveAndReimport();
        return true;
    }

    private static bool AsignarArte(DefinicionPiso piso, int numero, Ficha ficha)
    {
        ObjetoPiso[] objetos = new ObjetoPiso[4];
        for (int t = 0; t < 4; t++)
        {
            Sprite[] estados = new Sprite[3];
            for (int e = 0; e < 3; e++)
            {
                estados[e] = Cargar(string.Format("piso{0}_objeto{1}_{2}", numero, t, e));
                if (estados[e] == null) return false;
            }
            objetos[t] = new ObjetoPiso { nombre = ficha.objetos[t], sprites = estados };
        }

        Sprite[] plataformas = new Sprite[3];
        for (int e = 0; e < 3; e++)
        {
            plataformas[e] = Cargar(string.Format("piso{0}_plataforma_{1}", numero, e));
            if (plataformas[e] == null) return false;
        }
        Sprite fondo = Cargar(string.Format("piso{0}_fondo", numero));
        if (fondo == null) return false;

        Undo.RecordObject(piso, "Aplicar arte de piso");
        piso.objetos = objetos;
        piso.plataformas = plataformas;
        piso.fondo = fondo;
        piso.tinte = Color.white;
        piso.colorFondo = ficha.colorFondo;

        // Las fichas solo se completan si siguen con el texto provisional.
        if (string.IsNullOrEmpty(piso.fichaDescripcion) || piso.fichaDescripcion.StartsWith("Ficha por completar"))
            piso.fichaDescripcion = ficha.descripcion;
        if (string.IsNullOrEmpty(piso.fichaDatoUtil) || piso.fichaDatoUtil.StartsWith("Dato por completar"))
            piso.fichaDatoUtil = ficha.dato;

        EditorUtility.SetDirty(piso);
        return true;
    }

    private static Sprite Cargar(string nombre)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaArte + "/" + nombre + ".png");
    }
}
