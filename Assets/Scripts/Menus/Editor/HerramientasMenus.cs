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
    /// <summary>Construye y guarda MenuPrincipal y FinJuego de una sola vez.</summary>
    [MenuItem(RaizMenuTools + "Construir Todos los Menús", false, 0)]
    public static void MenuConstruirTodo()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Demoledor Unifranz", "Sal del Play Mode antes de construir los menús.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (AbrirEscena(ConstructorMenuPrincipal.RutaEscena, false))
        {
            ConstructorMenuPrincipal.Construir();
            Terminar("Menú principal construido.", true, false);
        }
        if (AbrirEscena(ConstructorFinJuego.RutaEscena, false))
        {
            ConstructorFinJuego.Construir();
            Terminar("Pantalla de fin de juego construida.", true, false);
        }
        AbrirEscena(ConstructorMenuPrincipal.RutaEscena, false);

        string faltan = EscenasFaltantes();
        EditorUtility.DisplayDialog("Demoledor Unifranz",
            "Listo: MenuPrincipal y FinJuego construidos y guardados.\n" +
            "La pausa se crea sola al jugar en Universidad.\n\nDale Play desde MenuPrincipal." +
            (string.IsNullOrEmpty(faltan) ? "" : "\n\nAVISO: faltan escenas en Build Settings:\n" + faltan),
            "OK");
    }

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

        Font titulos, cuerpo;
        CargarFuentes(out titulos, out cuerpo);
        FabricaUIArcade.Fuente = titulos;
        FabricaUIArcade.FuenteCuerpo = cuerpo;
    }

    public static void Conectar(Button boton, UnityEngine.Events.UnityAction accion)
    {
        UnityEventTools.AddPersistentListener(boton.onClick, accion);
    }

    public static Button Boton(Transform padre, string nombre, string texto, Sprite sprite, Vector2 posicion, Vector2 tamano, int fuente, Sprite icono = null)
    {
        Button b = FabricaUIArcade.CrearBoton(padre, nombre, texto, sprite, tamano, fuente, icono);
        FabricaUIArcade.Centrar((RectTransform)b.transform, posicion, tamano);
        return b;
    }

    /// <summary>Franja de peligro horizontal dentro de una caja (pegada arriba o abajo).</summary>
    public static Image FranjaEnCaja(RectTransform caja, Sprite franja, bool arriba, float margen, float alto)
    {
        return FabricaUIArcade.Franja(caja, franja, arriba, margen, alto);
    }

    // ------------------------------------------------------------ recursos
    private const string RutaFuenteTitulos = DirFuentes + "/Silkscreen-Bold.ttf";
    private const string RutaFuenteCuerpo = DirFuentes + "/Silkscreen-Regular.ttf";

    /// <summary>
    /// Carga la fuente pixel (Silkscreen, licencia OFL) y la configura para que se vea
    /// nítida (sin suavizado). Si no está, usa la fuente integrada de Unity.
    /// </summary>
    public static void CargarFuentes(out Font titulos, out Font cuerpo)
    {
        // La fuente viaja en el repositorio como .bytes (los .ttf van por Git LFS);
        // la primera vez se copia como .ttf para que Unity la importe como fuente.
        CrearTtfDesdeBytes(RutaFuenteTitulos);
        CrearTtfDesdeBytes(RutaFuenteCuerpo);

        titulos = PrepararFuente(RutaFuenteTitulos);
        cuerpo = PrepararFuente(RutaFuenteCuerpo);
        if (titulos == null)
        {
            titulos = FabricaUIArcade.FuenteIntegrada();
            Debug.LogWarning("[Menus] No se encontró " + RutaFuenteTitulos + ": se usa la fuente integrada.");
        }
        if (cuerpo == null) cuerpo = titulos;
    }

    private static void CrearTtfDesdeBytes(string rutaTtf)
    {
        string origen = Path.ChangeExtension(rutaTtf, ".bytes");
        if (File.Exists(rutaTtf) || !File.Exists(origen)) return;
        File.Copy(origen, rutaTtf);
        AssetDatabase.ImportAsset(rutaTtf, ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("[Menus] Fuente creada: " + rutaTtf);
    }

    private static Font PrepararFuente(string ruta)
    {
        TrueTypeFontImporter imp = AssetImporter.GetAtPath(ruta) as TrueTypeFontImporter;
        if (imp == null) return null;
        if (imp.fontRenderingMode != FontRenderingMode.HintedRaster || imp.fontTextureCase != FontTextureCase.Dynamic)
        {
            imp.fontRenderingMode = FontRenderingMode.HintedRaster;   // pixeles nítidos, sin suavizado
            imp.fontTextureCase = FontTextureCase.Dynamic;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Font>(ruta);
    }

    private struct DefSprite
    {
        public string nombre;
        public Func<Texture2D> dibujar;
        public Vector4 borde;
        public bool repetir;
        public bool pixel;

        public DefSprite(string nombre, Func<Texture2D> dibujar, Vector4 borde, bool repetir = false, bool pixel = true)
        {
            this.nombre = nombre;
            this.dibujar = dibujar;
            this.borde = borde;
            this.repetir = repetir;
            this.pixel = pixel;
        }
    }

    private static DefSprite Ico(string nombre, string[] mapa)
    {
        return new DefSprite(nombre, () => GeneradorPixelArt.Icono(mapa, nombre), Vector4.zero);
    }

    private static DefSprite[] Definiciones()
    {
        Vector4 bb = GeneradorPixelArt.BordeBoton, bp = GeneradorPixelArt.BordePanel, bs = GeneradorPixelArt.BordeBarra;
        return new[]
        {
            // Botones y marcos.
            new DefSprite("boton_px_amarillo", () => GeneradorPixelArt.Boton(PaletaArcade.Amarillo, "boton_px_amarillo"), bb),
            new DefSprite("boton_px_naranja", () => GeneradorPixelArt.Boton(PaletaArcade.Naranja, "boton_px_naranja"), bb),
            new DefSprite("boton_px_azul", () => GeneradorPixelArt.Boton(PaletaArcade.AzulClaro, "boton_px_azul"), bb),
            new DefSprite("boton_px_rojo", () => GeneradorPixelArt.Boton(PaletaArcade.Rojo, "boton_px_rojo"), bb),
            new DefSprite("boton_px_morado", () => GeneradorPixelArt.Boton(PaletaArcade.Morado, "boton_px_morado"), bb),
            new DefSprite("marco_seleccion", GeneradorPixelArt.MarcoSeleccion, bb),
            new DefSprite("panel_px", () => GeneradorPixelArt.Panel(PaletaArcade.Amarillo, "panel_px"), bp),
            new DefSprite("panel_px_rojo", () => GeneradorPixelArt.Panel(PaletaArcade.Rojo, "panel_px_rojo"), bp),
            new DefSprite("panel_px_morado", () => GeneradorPixelArt.Panel(PaletaArcade.Morado, "panel_px_morado"), bp),
            new DefSprite("cartel_px", GeneradorPixelArt.Cartel, GeneradorPixelArt.BordeCartel),
            new DefSprite("franja_px", GeneradorPixelArt.Franja, Vector4.zero, true),
            new DefSprite("ladrillo_px", GeneradorPixelArt.Ladrillo, Vector4.zero),
            new DefSprite("muro_px", GeneradorPixelArt.Muro, Vector4.zero, true),
            new DefSprite("hueco_px", GeneradorPixelArt.Hueco, Vector4.zero),
            // Controles.
            new DefSprite("barra_px", GeneradorPixelArt.Barra, bs),
            new DefSprite("relleno_px", GeneradorPixelArt.Relleno, bs),
            new DefSprite("perilla_px", GeneradorPixelArt.Perilla, Vector4.zero),
            new DefSprite("casilla_px", GeneradorPixelArt.Casilla, GeneradorPixelArt.BordeCasilla),
            new DefSprite("marca_px", GeneradorPixelArt.Marca, Vector4.zero),
            // Iconos.
            Ico("icono_jugar", GeneradorPixelArt.IconoJugar),
            Ico("icono_opciones", GeneradorPixelArt.IconoOpciones),
            Ico("icono_ayuda", GeneradorPixelArt.IconoAyuda),
            Ico("icono_salir", GeneradorPixelArt.IconoSalir),
            Ico("icono_trofeo", GeneradorPixelArt.IconoTrofeo),
            Ico("icono_reiniciar", GeneradorPixelArt.IconoReiniciar),
            Ico("icono_casa", GeneradorPixelArt.IconoCasa),
            Ico("icono_flecha", GeneradorPixelArt.IconoFlecha),
            Ico("icono_estrella", GeneradorPixelArt.IconoEstrella),
            Ico("icono_pausa", GeneradorPixelArt.IconoPausa),
            Ico("icono_arriba", GeneradorPixelArt.IconoArriba),
            Ico("icono_abajo", GeneradorPixelArt.IconoAbajo),
            // Medallas.
            new DefSprite("medalla_bronce", () => GeneradorPixelArt.Medalla(PaletaArcade.Hex(0xC8743A), "medalla_bronce"), Vector4.zero),
            new DefSprite("medalla_plata", () => GeneradorPixelArt.Medalla(PaletaArcade.Hex(0xC9D2E0), "medalla_plata"), Vector4.zero),
            new DefSprite("medalla_oro", () => GeneradorPixelArt.Medalla(PaletaArcade.Hex(0xF0B41E), "medalla_oro"), Vector4.zero),
            // Fondos suaves (no pixel).
            new DefSprite("vineta", GeneradorSpritesArcade.Vineta, Vector4.zero, false, false),
            new DefSprite("sombra_lateral", GeneradorPixelArt.SombraLateral, Vector4.zero, false, false),
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
            imp.filterMode = def.pixel ? FilterMode.Point : FilterMode.Bilinear;
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

    /// <summary>Genera los PNG que falten y devuelve el juego de sprites completo.</summary>
    public static KitSpritesArcade CargarKit()
    {
        GenerarSprites(false);
        return new KitSpritesArcade
        {
            botonAmarillo = S("boton_px_amarillo"),
            botonNaranja = S("boton_px_naranja"),
            botonAzul = S("boton_px_azul"),
            botonRojo = S("boton_px_rojo"),
            botonMorado = S("boton_px_morado"),
            marcoSeleccion = S("marco_seleccion"),
            panel = S("panel_px"),
            panelRojo = S("panel_px_rojo"),
            panelMorado = S("panel_px_morado"),
            cartel = S("cartel_px"),
            franja = S("franja_px"),
            ladrillo = S("ladrillo_px"),
            muro = S("muro_px"),
            hueco = S("hueco_px"),
            vineta = S("vineta"),
            sombraLateral = S("sombra_lateral"),
            barra = S("barra_px"),
            relleno = S("relleno_px"),
            perilla = S("perilla_px"),
            casilla = S("casilla_px"),
            marca = S("marca_px"),
            iconoJugar = S("icono_jugar"),
            iconoOpciones = S("icono_opciones"),
            iconoAyuda = S("icono_ayuda"),
            iconoSalir = S("icono_salir"),
            iconoTrofeo = S("icono_trofeo"),
            iconoReiniciar = S("icono_reiniciar"),
            iconoCasa = S("icono_casa"),
            iconoFlecha = S("icono_flecha"),
            iconoEstrella = S("icono_estrella"),
            iconoPausa = S("icono_pausa"),
            iconoArriba = S("icono_arriba"),
            iconoAbajo = S("icono_abajo"),
            medallaBronce = S("medalla_bronce"),
            medallaPlata = S("medalla_plata"),
            medallaOro = S("medalla_oro"),
        };
    }

    /// <summary>
    /// Agrega a la escena el objeto RecursosMenus con las fuentes y los sprites, para que
    /// los menús creados por código (la pausa en Universidad) usen los mismos recursos.
    /// </summary>
    public static RecursosMenus AgregarRecursos(Transform padre, KitSpritesArcade kit, Font titulos, Font cuerpo)
    {
        GameObject objeto = new GameObject("RecursosMenus");
        objeto.transform.SetParent(padre, false);
        RecursosMenus r = objeto.AddComponent<RecursosMenus>();
        r.kit = kit;
        r.fuenteTitulos = titulos;
        r.fuenteCuerpo = cuerpo;
        return r;
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
