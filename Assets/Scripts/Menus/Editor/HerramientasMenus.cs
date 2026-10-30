using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DemoledorUnifranz.Game;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// Herramientas comunes para construir los menús desde el editor
/// (menú Tools → Demoledor Unifranz):
/// - Genera los sprites de los menús como PNG en Assets/Sprites/Menus.
/// - Abre escenas, desactiva (sin borrar) el canvas anterior y permite deshacer.
/// - Revisa que las escenas estén en Build Settings (no las cambia).
/// Nunca abre ni modifica Nivel1 ni Universidad.
/// </summary>
public static class HerramientasMenus
{
    public const string RaizMenuTools = "Tools/Demoledor Unifranz/";
    public const string DirSprites = "Assets/Sprites/Menus";
    public const string DirFuentes = "Assets/Sprites/Menus/Fuentes";

    // ------------------------------------------------------------ menú Tools
    [MenuItem(RaizMenuTools + "Regenerar Sprites de Menús", false, 40)]
    public static void MenuRegenerarSprites()
    {
        GenerarSprites(true);
        EditorUtility.DisplayDialog("Demoledor Unifranz", "Sprites regenerados en " + DirSprites + ".", "OK");
    }

    [MenuItem(RaizMenuTools + "Revisar Build Settings", false, 41)]
    public static void MenuRevisarBuildSettings()
    {
        string faltan = EscenasFaltantes();
        EditorUtility.DisplayDialog("Demoledor Unifranz - Build Settings",
            string.IsNullOrEmpty(faltan)
                ? "MenuPrincipal, Universidad y FinJuego están en Build Settings."
                : "Faltan en Build Settings:\n" + faltan + "\nAgrégalas en File → Build Profiles → Scene List " +
                  "(MenuPrincipal primero). Esta herramienta no lo cambia sola.",
            "OK");
    }

    // ------------------------------------------------------------ escenas
    /// <summary>Abre la escena indicada (pidiendo guardar la actual si hace falta).</summary>
    public static bool AbrirEscena(string ruta, bool preguntar)
    {
        if (ruta.Contains("Nivel1") || ruta.Contains("Universidad"))
            throw new InvalidOperationException("Esta herramienta no modifica las escenas de juego.");

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Demoledor Unifranz", "Sal del Play Mode antes de construir los menús.", "OK");
            return false;
        }

        if (!File.Exists(ruta))
        {
            EditorUtility.DisplayDialog("Demoledor Unifranz", "No se encontró la escena " + ruta + ".", "OK");
            return false;
        }

        if (SceneManager.GetActiveScene().path == ruta) return true;
        if (preguntar && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
        return true;
    }

    /// <summary>Marca la escena como modificada y, si se pide, la guarda.</summary>
    public static void Terminar(string mensaje, bool guardar, bool dialogo)
    {
        Scene escena = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        if (guardar) EditorSceneManager.SaveScene(escena);

        string faltan = EscenasFaltantes();
        string aviso = string.IsNullOrEmpty(faltan) ? "" : "\n\nAVISO: faltan escenas en Build Settings:\n" + faltan;
        Debug.Log("[Menus] " + mensaje + aviso.Replace("\n", " "));

        if (dialogo)
        {
            string pie = guardar ? "\n\nLa escena se guardó." : "\n\nRevísala y guárdala con Ctrl+S (se puede deshacer con Ctrl+Z).";
            EditorUtility.DisplayDialog("Demoledor Unifranz", mensaje + pie + aviso, "OK");
        }
    }

    public static string EscenasFaltantes()
    {
        HashSet<string> presentes = new HashSet<string>();
        foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
            if (e.enabled) presentes.Add(Path.GetFileNameWithoutExtension(e.path));

        string faltan = "";
        foreach (string nombre in new[] { Escenas.MenuPrincipal, Escenas.Nivel1, Escenas.FinJuego })
            if (!presentes.Contains(nombre) && !faltan.Contains("• " + nombre + "\n")) faltan += "• " + nombre + "\n";
        return faltan;
    }

    public static GameObject BuscarRaiz(string nombre)
    {
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name == nombre) return go;
        return null;
    }

    /// <summary>Busca un componente por nombre de objeto en la escena (incluye inactivos).</summary>
    public static T Buscar<T>(string nombre) where T : Component
    {
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (T c in go.GetComponentsInChildren<T>(true))
                if (c.name == nombre) return c;
        return null;
    }

    /// <summary>
    /// Borra lo que la herramienta creó antes (para poder volver a ejecutarla) y
    /// desactiva, sin borrar, el canvas anterior del equipo.
    /// </summary>
    public static void PrepararEscena(string raizNueva, string canvasAnterior)
    {
        GameObject previa = BuscarRaiz(raizNueva);
        if (previa != null) Undo.DestroyObjectImmediate(previa);

        GameObject anterior = BuscarRaiz(canvasAnterior);
        if (anterior != null && anterior.activeSelf)
        {
            Undo.RecordObject(anterior, "Desactivar canvas anterior");
            anterior.SetActive(false);
            Debug.Log("[Menus] Se desactivó (no se borró) el canvas anterior '" + canvasAnterior + "'.");
        }

        if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = FabricaUIArcade.AsegurarEventSystem().gameObject;
            Undo.RegisterCreatedObjectUndo(es, "Crear EventSystem");
        }

        FabricaUIArcade.Fuente = CargarFuente();
    }

    public static void Conectar(Button boton, UnityEngine.Events.UnityAction accion)
    {
        UnityEventTools.AddPersistentListener(boton.onClick, accion);
    }

    public static Button Boton(Transform padre, string nombre, string texto, Sprite sprite, Vector2 posicion, Vector2 tamano, int fuente)
    {
        Button b = FabricaUIArcade.CrearBoton(padre, nombre, texto, sprite, tamano, fuente);
        FabricaUIArcade.Centrar((RectTransform)b.transform, posicion, tamano);
        return b;
    }

    /// <summary>Franja de peligro horizontal dentro de una caja (pegada arriba o abajo).</summary>
    public static Image FranjaEnCaja(RectTransform caja, Sprite franja, bool arriba, float margen, float alto)
    {
        Image f = FabricaUIArcade.CrearImagen(caja, arriba ? "FranjaSuperior" : "FranjaInferior", franja, Color.white);
        f.type = Image.Type.Tiled;
        RectTransform rt = f.rectTransform;
        float y = arriba ? 1f : 0f;
        rt.anchorMin = new Vector2(0f, y);
        rt.anchorMax = new Vector2(1f, y);
        rt.pivot = new Vector2(0.5f, y);
        rt.offsetMin = new Vector2(margen, arriba ? -margen - alto : margen);
        rt.offsetMax = new Vector2(-margen, arriba ? -margen : margen + alto);
        return f;
    }

    // ------------------------------------------------------------ recursos
    private static Font CargarFuente()
    {
        if (AssetDatabase.IsValidFolder(DirFuentes))
        {
            string[] guids = AssetDatabase.FindAssets("t:Font", new[] { DirFuentes });
            if (guids.Length > 0)
            {
                Font f = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (f != null)
                {
                    Debug.Log("[Menus] Fuente de los menús: " + f.name);
                    return f;
                }
            }
        }
        return FabricaUIArcade.FuenteIntegrada();
    }

    private struct DefSprite
    {
        public string nombre;
        public Func<Texture2D> dibujar;
        public Vector4 borde;
        public bool repetir;

        public DefSprite(string nombre, Func<Texture2D> dibujar, Vector4 borde, bool repetir = false)
        {
            this.nombre = nombre;
            this.dibujar = dibujar;
            this.borde = borde;
            this.repetir = repetir;
        }
    }

    private static DefSprite[] Definiciones()
    {
        return new[]
        {
            new DefSprite("boton_amarillo", () => GeneradorSpritesArcade.Boton(PaletaArcade.Amarillo, "boton_amarillo"), KitSpritesArcade.BordeBoton),
            new DefSprite("boton_naranja", () => GeneradorSpritesArcade.Boton(PaletaArcade.Naranja, "boton_naranja"), KitSpritesArcade.BordeBoton),
            new DefSprite("boton_azul", () => GeneradorSpritesArcade.Boton(PaletaArcade.AzulClaro, "boton_azul"), KitSpritesArcade.BordeBoton),
            new DefSprite("boton_rojo", () => GeneradorSpritesArcade.Boton(PaletaArcade.Rojo, "boton_rojo"), KitSpritesArcade.BordeBoton),
            new DefSprite("panel_arcade", GeneradorSpritesArcade.Panel, KitSpritesArcade.BordePanel),
            new DefSprite("franja_peligro", GeneradorSpritesArcade.FranjaPeligro, Vector4.zero, true),
            new DefSprite("ladrillo_menu", GeneradorSpritesArcade.Ladrillo, Vector4.zero),
            new DefSprite("barra", GeneradorSpritesArcade.Barra, KitSpritesArcade.BordeBarra),
            new DefSprite("barra_relleno", GeneradorSpritesArcade.Relleno, KitSpritesArcade.BordeBarra),
            new DefSprite("perilla", GeneradorSpritesArcade.Perilla, Vector4.zero),
            new DefSprite("casilla", GeneradorSpritesArcade.Casilla, KitSpritesArcade.BordeCasilla),
            new DefSprite("marca", GeneradorSpritesArcade.Marca, Vector4.zero),
            // Menú principal.
            new DefSprite("muro_ladrillo", GeneradorSpritesArcade.MuroLadrillo, Vector4.zero, true),
            new DefSprite("cartel_obra", GeneradorSpritesArcade.Cartel, KitSpritesArcade.BordeCartel),
            new DefSprite("hueco_muro", GeneradorSpritesArcade.Hueco, Vector4.zero),
            new DefSprite("vineta", GeneradorSpritesArcade.Vineta, Vector4.zero),
        };
    }

    /// <summary>Guarda los PNG de los menús (solo los que falten, salvo que se fuerce).</summary>
    public static void GenerarSprites(bool forzar)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder(DirSprites))
            AssetDatabase.CreateFolder("Assets/Sprites", "Menus");

        foreach (DefSprite def in Definiciones())
        {
            string ruta = DirSprites + "/" + def.nombre + ".png";
            if (!forzar && File.Exists(ruta)) continue;

            Texture2D tex = def.dibujar();
            File.WriteAllBytes(ruta, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = def.repetir ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.spriteBorder = def.borde;

            TextureImporterSettings ajustes = new TextureImporterSettings();
            imp.ReadTextureSettings(ajustes);
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            ajustes.spriteAlignment = (int)SpriteAlignment.Center;
            imp.SetTextureSettings(ajustes);
            imp.SaveAndReimport();
        }
    }

    public static Sprite S(string nombre)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(DirSprites + "/" + nombre + ".png");
        if (s == null) Debug.LogWarning("[Menus] No se encontró el sprite " + nombre + ".png");
        return s;
    }

    /// <summary>Genera los PNG que falten y devuelve el juego de sprites cargado.</summary>
    public static KitSpritesArcade CargarKit()
    {
        GenerarSprites(false);
        return new KitSpritesArcade
        {
            botonAmarillo = S("boton_amarillo"),
            botonNaranja = S("boton_naranja"),
            botonAzul = S("boton_azul"),
            botonRojo = S("boton_rojo"),
            panel = S("panel_arcade"),
            franja = S("franja_peligro"),
            ladrillo = S("ladrillo_menu"),
            barra = S("barra"),
            relleno = S("barra_relleno"),
            perilla = S("perilla"),
            casilla = S("casilla"),
            marca = S("marca"),
        };
    }

    /// <summary>Carga los sprites de una animación del equipo (por ejemplo Assets/Arte/Demoledor/golpe_0..5).</summary>
    public static Sprite[] CargarCuadros(string rutaBase, int cantidad)
    {
        List<Sprite> lista = new List<Sprite>();
        for (int i = 0; i < cantidad; i++)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(rutaBase + i + ".png");
            if (s != null) lista.Add(s);
        }
        if (lista.Count == 0) Debug.LogWarning("[Menus] No se encontraron los cuadros " + rutaBase + "0.." + (cantidad - 1));
        return lista.ToArray();
    }
}
