using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using RalftDemoledor.Game;
using RalftDemoledor.UI;

/// <summary>
/// Herramienta de editor que genera los recursos de interfaz (sprites),
/// arma las escenas del menu, del nivel y del fin de juego, configura el
/// Build Settings, compila el ejecutable de Windows y toma las capturas.
/// </summary>
public static class GeneradorJuego
{
    const string DirEscenas = "Assets/Scenes";
    const string DirSprites = "Assets/UI/Sprites";
    const string RutaMenu = DirEscenas + "/MenuPrincipal.unity";
    const string RutaNivel = DirEscenas + "/Nivel1.unity";
    const string RutaFin = DirEscenas + "/FinJuego.unity";
    const string RutaEdificio = "Assets/Edificio/edificio.aseprite";
    const string RutaVentanas = "Assets/Edificio/ventanas.aseprite";
    const string RutaJugadorFrente = "Assets/personaje/Movimiento Abajo.aseprite";
    const string RutaEnemigoCamina = "Assets/Animaciones/Enemigo/animacion_caminata_derecha.aseprite";

    static Font fuenteUi;

    [MenuItem("Ralft Demoledor/Generar interfaz, escenas y ejecutable")]
    public static void ConstruirTodo()
    {
        int codigo = 0;

        try
        {
            fuenteUi = CargarFuente();
            CrearSprites();
            CrearEscenaMenu();
            CrearEscenaNivel();
            CrearEscenaFinJuego();
            ConfigurarBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            CompilarEjecutable();
            CapturarPantallas();
            Debug.Log("=== GENERADOR: proceso completado correctamente ===");
        }
        catch (Exception error)
        {
            codigo = 1;
            Debug.LogError("=== GENERADOR: ERROR -> " + error.Message + "\n" + error.StackTrace);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(codigo);
        }
    }

    static Font CargarFuente()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (f == null)
        {
            f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (f == null)
        {
            f = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Verdana" }, 32);
        }

        return f;
    }

    // ---------------- Recursos graficos ----------------

    static void CrearSprites()
    {
        CrearSprite("fondo_menu", 1280, 720, PixFondoMenu, 100f, FilterMode.Bilinear, Vector4.zero);
        CrearSprite("fondo_nivel", 1280, 720, PixFondoNivel, 64f, FilterMode.Bilinear, Vector4.zero);
        CrearSprite("boton", 384, 88, PixBoton, 100f, FilterMode.Bilinear, new Vector4(28f, 22f, 28f, 22f));
        CrearSprite("boton_rojo", 384, 88, PixBotonRojo, 100f, FilterMode.Bilinear, new Vector4(28f, 22f, 28f, 22f));
        CrearSprite("panel", 640, 440, PixPanel, 100f, FilterMode.Bilinear, new Vector4(36f, 36f, 36f, 36f));
        CrearSprite("parede", 64, 64, PixPared, 64f, FilterMode.Point, Vector4.zero);
        CrearSprite("personaje", 64, 64, PixPersonaje, 64f, FilterMode.Point, Vector4.zero);
        CrearSprite("enemigo", 64, 64, PixEnemigo, 64f, FilterMode.Point, Vector4.zero);
        CrearSprite("ladrillo", 48, 48, PixLadrillo, 64f, FilterMode.Point, Vector4.zero);
        CrearSprite("muro_h", 512, 64, PixMuroHorizontal, 64f, FilterMode.Point, Vector4.zero);
        CrearSprite("muro_v", 64, 512, PixMuroVertical, 64f, FilterMode.Point, Vector4.zero);
    }

    static void CrearSprite(string nombre, int ancho, int alto, Func<int, int, Color> pixel,
                            float ppu, FilterMode filtro, Vector4 borde)
    {
        Texture2D tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        Color[] px = new Color[ancho * alto];

        for (int y = 0; y < alto; y++)
        {
            for (int x = 0; x < ancho; x++)
            {
                px[y * ancho + x] = pixel(x, y);
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);

        Directory.CreateDirectory(DirSprites);
        string ruta = DirSprites + "/" + nombre + ".png";
        File.WriteAllBytes(ruta, png);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);

        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = filtro;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.wrapMode = TextureWrapMode.Clamp;

        if (borde != Vector4.zero)
        {
            imp.spriteBorder = borde;
        }

        imp.SaveAndReimport();
    }

    static void Rect(Color[] px, int ancho, int alto, int x0, int y0, int x1, int y1, Color c)
    {
        for (int y = Mathf.Max(0, y0); y < Mathf.Min(alto, y1); y++)
        {
            for (int x = Mathf.Max(0, x0); x < Mathf.Min(ancho, x1); x++)
            {
                px[y * ancho + x] = c;
            }
        }
    }

    static float DistanciaRedondeada(int x, int y, int ancho, int alto, int radio)
    {
        float cx = Mathf.Clamp(x, radio, ancho - 1 - radio);
        float cy = Mathf.Clamp(y, radio, alto - 1 - radio);
        return Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
    }

    static Color[] Redondeado(int ancho, int alto, int radio, int grosor,
                              Color rellenoAbajo, Color rellenoArriba, Color borde)
    {
        Color[] px = new Color[ancho * alto];

        for (int y = 0; y < alto; y++)
        {
            Color relleno = Color.Lerp(rellenoAbajo, rellenoArriba, (float)y / alto);

            for (int x = 0; x < ancho; x++)
            {
                float d = DistanciaRedondeada(x, y, ancho, alto, radio);

                if (d > radio)
                {
                    px[y * ancho + x] = new Color(0f, 0f, 0f, 0f);
                }
                else if (d > radio - grosor)
                {
                    px[y * ancho + x] = borde;
                }
                else
                {
                    px[y * ancho + x] = relleno;
                }
            }
        }

        return px;
    }

    static Color PixFondoMenu(int x, int y)
    {
        float t = (float)y / 720f;
        Color base1 = Color.Lerp(new Color(0.07f, 0.07f, 0.13f), new Color(0.20f, 0.14f, 0.28f), t);

        int fila = y / 45;
        int desfase = (fila % 2) * 48;
        int dentroX = (x + desfase) % 96;
        int dentroY = y % 45;

        if (dentroX < 4 || dentroY < 4)
        {
            return base1 * 0.72f;
        }

        float variacion = 0.96f + ((fila * 7 + dentroX / 96) % 4) * 0.02f;
        return base1 * variacion;
    }

    static Color PixFondoNivel(int x, int y)
    {
        int tile = 64;
        int dentroX = x % tile;
        int dentroY = y % tile;
        Color piso = new Color(0.13f, 0.12f, 0.16f);

        if (dentroX < 2 || dentroY < 2)
        {
            return piso * 0.65f;
        }

        return ((x / tile + y / tile) % 2 == 0) ? piso * 1.05f : piso;
    }

    static Color PixBoton(int x, int y)
    {
        return BaseBoton(x, y, new Color(0.16f, 0.30f, 0.62f), new Color(0.30f, 0.55f, 0.95f), new Color(0.62f, 0.80f, 1f));
    }

    static Color PixBotonRojo(int x, int y)
    {
        return BaseBoton(x, y, new Color(0.55f, 0.16f, 0.16f), new Color(0.85f, 0.30f, 0.24f), new Color(1f, 0.68f, 0.55f));
    }

    static Color BaseBoton(int x, int y, Color abajo, Color arriba, Color borde)
    {
        Color relleno = Color.Lerp(abajo, arriba, (float)y / 88f);
        float d = DistanciaRedondeada(x, y, 384, 88, 22);

        if (d > 22f)
        {
            return new Color(0f, 0f, 0f, 0f);
        }

        if (d > 19f)
        {
            return borde;
        }

        if (y > 74 && y < 80)
        {
            return Color.Lerp(relleno, Color.white, 0.25f);
        }

        if (y < 10)
        {
            return relleno * 0.8f;
        }

        return relleno;
    }

    static Color PixPanel(int x, int y)
    {
        Color relleno = Color.Lerp(new Color(0.10f, 0.11f, 0.18f, 0.94f), new Color(0.20f, 0.18f, 0.30f, 0.96f), (float)y / 440f);
        float d = DistanciaRedondeada(x, y, 640, 440, 30);

        if (d > 30f) return new Color(0f, 0f, 0f, 0f);
        if (d > 25f) return new Color(0.70f, 0.62f, 0.95f, 0.95f);
        return relleno;
    }

    static Color PixPared(int x, int y)
    {
        int fila = y / 16;
        int dentroX = (x + (fila % 2) * 16) % 32;
        int dentroY = y % 16;

        if (dentroX < 3 || dentroY < 3) return new Color(0.35f, 0.20f, 0.16f);
        return new Color(0.62f, 0.30f, 0.22f) * (0.92f + ((fila + dentroX / 32) % 3) * 0.04f);
    }

    static void Bloque(Color[] px, int x0, int y0, int x1, int y1, Color c)
    {
        Rect(px, 64, 64, x0, y0, x1, y1, c);
    }

    static Color PixPersonaje(int x, int y)
    {
        Color[] px = new Color[64 * 64];
        Bloque(px, 16, 44, 48, 58, new Color(0.30f, 0.18f, 0.10f));
        Bloque(px, 20, 38, 44, 50, new Color(0.98f, 0.80f, 0.63f));
        Bloque(px, 24, 44, 28, 47, Color.black);
        Bloque(px, 36, 44, 40, 47, Color.black);
        Bloque(px, 26, 34, 38, 36, new Color(0.65f, 0.25f, 0.25f));
        Bloque(px, 18, 18, 46, 36, new Color(0.28f, 0.48f, 0.32f));
        Bloque(px, 22, 12, 42, 30, new Color(0.20f, 0.32f, 0.62f));
        Bloque(px, 10, 20, 18, 34, new Color(0.28f, 0.48f, 0.32f));
        Bloque(px, 46, 20, 54, 34, new Color(0.28f, 0.48f, 0.32f));
        Bloque(px, 12, 6, 26, 12, new Color(0.12f, 0.10f, 0.12f));
        Bloque(px, 38, 6, 52, 12, new Color(0.12f, 0.10f, 0.12f));
        Bloque(px, 50, 34, 62, 40, new Color(0.75f, 0.60f, 0.30f));
        Bloque(px, 54, 40, 60, 56, new Color(0.75f, 0.60f, 0.30f));
        return px[y * 64 + x];
    }

    static Color PixEnemigo(int x, int y)
    {
        Color[] px = new Color[64 * 64];
        Bloque(px, 10, 8, 54, 50, new Color(0.72f, 0.22f, 0.18f));
        Bloque(px, 10, 8, 54, 14, new Color(0.45f, 0.12f, 0.10f));
        Bloque(px, 6, 22, 14, 46, new Color(0.20f, 0.20f, 0.22f));
        Bloque(px, 50, 22, 58, 46, new Color(0.20f, 0.20f, 0.22f));
        Bloque(px, 18, 50, 46, 58, new Color(0.95f, 0.78f, 0.20f));
        Bloque(px, 22, 32, 30, 42, new Color(1f, 0.95f, 0.60f));
        Bloque(px, 34, 32, 42, 42, new Color(1f, 0.95f, 0.60f));
        Bloque(px, 26, 20, 40, 26, new Color(0.45f, 0.12f, 0.10f));
        return px[y * 64 + x];
    }

    static Color PixLadrillo(int x, int y)
    {
        if (x % 24 < 3 || y % 24 < 3) return new Color(0.45f, 0.20f, 0.14f);
        if (x < 3 || y < 3 || x > 44 || y > 44) return new Color(0.95f, 0.60f, 0.30f);
        return new Color(0.78f, 0.34f, 0.20f);
    }

    static Color PixMuroHorizontal(int x, int y)
    {
        int fila = y / 16;
        int dentroX = (x + (fila % 2) * 16) % 32;
        int dentroY = y % 16;

        if (dentroY < 3) return new Color(0.30f, 0.17f, 0.14f);
        if (dentroX < 3) return new Color(0.38f, 0.22f, 0.18f);
        return new Color(0.58f, 0.29f, 0.22f) * (0.90f + ((fila + dentroX / 32) % 3) * 0.05f);
    }

    static Color PixMuroVertical(int x, int y)
    {
        return PixMuroHorizontal(y, x);
    }

    // ---------------- Recursos de Aseprite ----------------

    static List<Sprite> SpritesDe(string ruta)
    {
        List<Sprite> lista = new List<Sprite>();
        UnityEngine.Object[] recursos = AssetDatabase.LoadAllAssetsAtPath(ruta);

        foreach (UnityEngine.Object recurso in recursos)
        {
            Sprite sprite = recurso as Sprite;

            if (sprite != null) lista.Add(sprite);
        }

        lista.Sort(delegate (Sprite a, Sprite b) { return string.CompareOrdinal(a.name, b.name); });
        return lista;
    }

    static Sprite SpriteAseprite(string ruta, int indice)
    {
        List<Sprite> lista = SpritesDe(ruta);

        if (lista.Count == 0)
        {
            Debug.LogError("[ASEPRITE] sin sprites en " + ruta);
            return null;
        }

        Sprite elegido = lista[Mathf.Clamp(indice, 0, lista.Count - 1)];
        Debug.Log("[ASEPRITE] " + ruta + " -> " + elegido.name + " (" + lista.Count + " fotogramas)");
        return elegido;
    }

    static AnimationClip ClipAseprite(string ruta)
    {
        UnityEngine.Object[] recursos = AssetDatabase.LoadAllAssetsAtPath(ruta);

        foreach (UnityEngine.Object recurso in recursos)
        {
            AnimationClip clip = recurso as AnimationClip;

            if (clip != null) return clip;
        }

        return null;
    }

    static RuntimeAnimatorController ControladorAseprite(string ruta)
    {
        UnityEngine.Object[] recursos = AssetDatabase.LoadAllAssetsAtPath(ruta);

        foreach (UnityEngine.Object recurso in recursos)
        {
            RuntimeAnimatorController controlador = recurso as RuntimeAnimatorController;

            if (controlador != null) return controlador;
        }

        return null;
    }

    /// <summary>
    /// Devuelve true si el clip anima la posicion o la escala del objeto, en
    /// cuyo caso no conviene conectarlo porque pelea con el movimiento.
    /// </summary>
    static bool ClipMueveElObjeto(AnimationClip clip)
    {
        if (clip == null) return true;

        foreach (EditorCurveBinding enlace in AnimationUtility.GetCurveBindings(clip))
        {
            if (enlace.propertyName.Contains("m_LocalPosition") ||
                enlace.propertyName.Contains("m_LocalScale") ||
                enlace.propertyName.Contains("m_AnchoredPosition"))
            {
                return true;
            }
        }

        return false;
    }

    static void AgregarAnimador(GameObject objeto, string ruta)
    {
        AnimationClip clip = ClipAseprite(ruta);
        RuntimeAnimatorController controlador = ControladorAseprite(ruta);

        if (clip == null || controlador == null)
        {
            Debug.Log("[ANIMACION] " + ruta + ": se usa un fotograma fijo");
            return;
        }

        if (ClipMueveElObjeto(clip))
        {
            Debug.Log("[ANIMACION] " + ruta + ": el clip anima posicion o escala, se usa un fotograma fijo");
            return;
        }

        Animator animador = objeto.AddComponent<Animator>();
        animador.runtimeAnimatorController = controlador;
        animador.applyRootMotion = false;
        Debug.Log("[ANIMACION] " + ruta + ": animacion conectada");
    }

    static Sprite CargarSprite(string nombre)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(DirSprites + "/" + nombre + ".png");
    }

    // ---------------- Ayudas de interfaz ----------------

    static Canvas CrearCanvas(string nombre, int orden)
    {
        GameObject go = new GameObject(nombre, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = orden;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    static RectTransform CrearImagen(Transform padre, string nombre, Sprite sprite, Vector2 tamano, Vector2 pos, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tamano;
        rt.anchoredPosition = pos;

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;

        if (sprite != null && sprite.border != Vector4.zero)
        {
            img.type = Image.Type.Sliced;
        }

        return rt;
    }
    static Text CrearTexto(Transform padre, string nombre, string contenido, int tamano, Vector2 pos,
                           Vector2 tamanoRect, TextAnchor alineacion, Color color, bool negrita)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(padre, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tamanoRect;
        rt.anchoredPosition = pos;

        Text t = go.GetComponent<Text>();
        t.font = fuenteUi;
        t.fontSize = tamano;
        t.text = contenido;
        t.alignment = alineacion;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        if (negrita) t.fontStyle = FontStyle.Bold;
        return t;
    }

    static Button CrearBoton(Transform padre, string nombre, string etiqueta, Vector2 pos, Vector2 tamano, bool rojo)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tamano;
        rt.anchoredPosition = pos;

        Image img = go.GetComponent<Image>();
        img.sprite = CargarSprite(rojo ? "boton_rojo" : "boton");
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        Button boton = go.GetComponent<Button>();
        boton.targetGraphic = img;

        ColorBlock colores = boton.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color(0.88f, 0.93f, 1f);
        colores.pressedColor = new Color(0.70f, 0.78f, 0.95f);
        colores.selectedColor = Color.white;
        colores.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colores.fadeDuration = 0.08f;
        boton.colors = colores;

        CrearTexto(rt, "Etiqueta", etiqueta, 38, new Vector2(0f, 2f), new Vector2(tamano.x - 40f, 60f),
                   TextAnchor.MiddleCenter, Color.white, true);
        return boton;
    }

    static void Conectar(Button boton, UnityAction accion)
    {
        UnityEventTools.AddPersistentListener(boton.onClick, accion);
    }

    static void CrearEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    static void NuevaEscena()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static void CrearCamara(string nombre, Color fondo, bool ortografica, float tamano)
    {
        GameObject go = new GameObject(nombre, typeof(Camera));
        go.tag = "MainCamera";
        Camera cam = go.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = fondo;
        cam.orthographic = ortografica;
        cam.orthographicSize = tamano;
        go.transform.position = new Vector3(0f, 0f, -10f);
    }
    static void CrearEscenaMenu()
    {
        NuevaEscena();
        CrearCamara("Main Camera", new Color(0.05f, 0.05f, 0.08f, 1f), false, 5f);

        Canvas canvas = CrearCanvas("CanvasMenu", 0);
        GameObject controlador = new GameObject("MenuPrincipal", typeof(MainMenuController));

        CrearImagen(canvas.transform, "Fondo", SpriteAseprite(RutaEdificio, 0), new Vector2(1920f, 1080f), Vector2.zero, new Color(0.40f, 0.38f, 0.52f, 1f));
        CrearImagen(canvas.transform, "BarraInferior", null, new Vector2(1920f, 90f), new Vector2(0f, -495f), new Color(0f, 0f, 0f, 0.55f));

        Text titulo = CrearTexto(canvas.transform, "Titulo", "DEMOLEDOR UNIFRANZ", 132, new Vector2(0f, 310f),
                                 new Vector2(1700f, 170f), TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.35f), true);
        titulo.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.75f);

        Text subtitulo = CrearTexto(canvas.transform, "Subtitulo", "Juego 2D - Unifranz - Hito 3: Implementacion de interfaz de usuario", 42,
                                    new Vector2(0f, 205f), new Vector2(1600f, 70f), TextAnchor.MiddleCenter, new Color(0.88f, 0.90f, 1f), false);
        subtitulo.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);

        MainMenuController menu = controlador.GetComponent<MainMenuController>();

        Button btnJugar = CrearBoton(canvas.transform, "BotonJugar", "JUGAR", new Vector2(0f, 30f), new Vector2(440f, 96f), false);
        Button btnControles = CrearBoton(canvas.transform, "BotonControles", "CONTROLES", new Vector2(0f, -90f), new Vector2(440f, 96f), false);
        Button btnSalir = CrearBoton(canvas.transform, "BotonSalir", "SALIR", new Vector2(0f, -210f), new Vector2(440f, 96f), true);
        Conectar(btnJugar, menu.Jugar);
        Conectar(btnControles, menu.AlternarControles);
        Conectar(btnSalir, menu.Salir);

        RectTransform panel = CrearImagen(canvas.transform, "PanelControles", CargarSprite("panel"), new Vector2(920f, 620f), Vector2.zero, Color.white);
        CrearTexto(panel, "TituloControles", "CONTROLES", 62, new Vector2(0f, 230f), new Vector2(800f, 80f), TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.35f), true);
        CrearTexto(panel, "TextoControles",
                   "W A S D o flechas : mover al demoledor\nEscape : abrir el menu de pausa\nObjetivo : recoger los 5 ladrillos\nCuidado : los demoledores quitan una vida",
                   36, new Vector2(0f, 20f), new Vector2(820f, 340f), TextAnchor.UpperCenter, Color.white, false);
        Button btnCerrar = CrearBoton(panel, "BotonCerrar", "CERRAR", new Vector2(0f, -220f), new Vector2(340f, 90f), false);
        Conectar(btnCerrar, menu.AlternarControles);
        menu.panelControles = panel.gameObject;

        CrearTexto(canvas.transform, "Version", "v1.0  -  Equipo: Shaden, Jhonatan, Alejandro, Jorge", 30,
                   new Vector2(0f, -495f), new Vector2(1500f, 50f), TextAnchor.MiddleCenter, new Color(0.82f, 0.84f, 0.92f, 0.9f), false);

        CrearEventSystem();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), RutaMenu);
        Debug.Log("Escena creada: " + RutaMenu);
    }

    static void CrearEscenaNivel()
    {
        NuevaEscena();
        CrearCamara("Main Camera", new Color(0.05f, 0.05f, 0.07f, 1f), true, 1.85f);

        // Fondo: el edificio dibujado por el equipo.
        GameObject fondo = new GameObject("Edificio", typeof(SpriteRenderer));
        fondo.GetComponent<SpriteRenderer>().sprite = SpriteAseprite(RutaEdificio, 0);
        fondo.GetComponent<SpriteRenderer>().sortingOrder = -20;
        fondo.transform.position = new Vector3(0f, 0f, 5f);
        fondo.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

        // Ventanas del edificio como decoracion de la pared del fondo.
        Sprite spriteVentana = SpriteAseprite(RutaVentanas, 0);

        for (int i = 0; i < 2; i++)
        {
            GameObject ventana = new GameObject("Ventana" + (i + 1), typeof(SpriteRenderer));
            ventana.GetComponent<SpriteRenderer>().sprite = spriteVentana;
            ventana.GetComponent<SpriteRenderer>().sortingOrder = -15;
            ventana.transform.position = new Vector3(i == 0 ? -1.55f : 1.55f, 0.95f, 3f);
            ventana.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
        }

        // Muros de la sala de demolicion.
        CrearMuro("MuroArriba", CargarSprite("muro_h"), new Vector2(0f, 1.7f), new Vector2(6.6f, 0.5f));
        CrearMuro("MuroAbajo", CargarSprite("muro_h"), new Vector2(0f, -1.7f), new Vector2(6.6f, 0.5f));
        CrearMuro("MuroIzquierda", CargarSprite("muro_v"), new Vector2(-3.05f, 0f), new Vector2(0.5f, 3.4f));
        CrearMuro("MuroDerecha", CargarSprite("muro_v"), new Vector2(3.05f, 0f), new Vector2(0.5f, 3.4f));

        // Personaje principal con el sprite del equipo.
        GameObject jugador = new GameObject("Jugador", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(PlayerController));
        jugador.GetComponent<SpriteRenderer>().sprite = SpriteAseprite(RutaJugadorFrente, 0);
        jugador.GetComponent<SpriteRenderer>().sortingOrder = 4;
        jugador.transform.position = new Vector3(-2.3f, -1.05f, 0f);
        jugador.GetComponent<PlayerController>().velocidad = 2.6f;

        CircleCollider2D colJugador = jugador.GetComponent<CircleCollider2D>();
        colJugador.radius = 0.16f;
        colJugador.offset = new Vector2(0f, -0.12f);

        Rigidbody2D rb = jugador.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        AgregarAnimador(jugador, RutaJugadorFrente);

        // Ladrillos recolectables.
        Vector2[] posiciones = new Vector2[]
        {
            new Vector2(-1.9f, 0.95f), new Vector2(0.2f, 1.15f), new Vector2(2.2f, 0.15f),
            new Vector2(-1.6f, -0.25f), new Vector2(1.1f, -1.15f)
        };

        for (int i = 0; i < posiciones.Length; i++)
        {
            GameObject ladrillo = new GameObject("Ladrillo" + (i + 1), typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Recolectable));
            ladrillo.GetComponent<SpriteRenderer>().sprite = CargarSprite("ladrillo");
            ladrillo.GetComponent<SpriteRenderer>().sortingOrder = 2;
            ladrillo.transform.position = posiciones[i];
            ladrillo.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            ladrillo.GetComponent<CircleCollider2D>().radius = 0.35f;
            ladrillo.GetComponent<CircleCollider2D>().isTrigger = true;
        }

        // Enemigos: el demoledor del equipo.
        CrearEnemigo("Demoledor1", new Vector2(-1.2f, -0.95f), new Vector2(2.3f, -0.95f));
        CrearEnemigo("Demoledor2", new Vector2(-2.4f, 0.85f), new Vector2(1.4f, 0.85f));
        CrearEnemigo("Demoledor3", new Vector2(2.5f, 0.8f), new Vector2(2.5f, -1.1f));

        // --- Interfaz del nivel -------------------------------------------
        Canvas hud = CrearCanvas("CanvasHUD", 5);
        CrearImagen(hud.transform, "BarraSuperior", null, new Vector2(1920f, 110f), new Vector2(0f, 485f), new Color(0f, 0f, 0f, 0.55f));
        Text textoPuntaje = CrearTexto(hud.transform, "TextoPuntaje", "PUNTAJE: 0", 46, new Vector2(-560f, 462f), new Vector2(600f, 70f), TextAnchor.MiddleLeft, Color.white, true);
        Text textoObjetivo = CrearTexto(hud.transform, "TextoObjetivo", "LADRILLOS: 0 / 5", 46, new Vector2(0f, 462f), new Vector2(700f, 70f), TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.35f), true);
        Text textoVidas = CrearTexto(hud.transform, "TextoVidas", "VIDAS: 3", 46, new Vector2(600f, 462f), new Vector2(600f, 70f), TextAnchor.MiddleRight, Color.white, true);
        CrearTexto(hud.transform, "Ayuda", "W A S D o flechas para moverte   |   Escape para pausar   |   Recoge 5 ladrillos y evita a los demoledores",
                   32, new Vector2(0f, -495f), new Vector2(1800f, 60f), TextAnchor.MiddleCenter, new Color(0.90f, 0.92f, 1f), false);

        HUDController hudController = hud.gameObject.AddComponent<HUDController>();
        hudController.textoPuntaje = textoPuntaje;
        hudController.textoVidas = textoVidas;
        hudController.textoObjetivo = textoObjetivo;

        GameObject objetoPausa = new GameObject("MenuPausa", typeof(PauseMenuController));
        RectTransform raizPausa = CrearImagen(hud.transform, "PanelPausa", null, new Vector2(1920f, 1080f), Vector2.zero, new Color(0f, 0f, 0f, 0.72f));
        RectTransform caja = CrearImagen(raizPausa, "CajaPausa", CargarSprite("panel"), new Vector2(760f, 640f), Vector2.zero, Color.white);
        CrearTexto(caja, "TituloPausa", "PAUSA", 88, new Vector2(0f, 230f), new Vector2(600f, 110f), TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.35f), true);
        CrearTexto(caja, "DetallePausa", "El juego esta detenido", 36, new Vector2(0f, 150f), new Vector2(600f, 60f), TextAnchor.MiddleCenter, new Color(0.85f, 0.88f, 1f), false);

        PauseMenuController pausa = objetoPausa.GetComponent<PauseMenuController>();
        Button btnReanudar = CrearBoton(caja, "BotonReanudar", "REANUDAR", new Vector2(0f, 50f), new Vector2(460f, 96f), false);
        Button btnReiniciar = CrearBoton(caja, "BotonReiniciar", "REINICIAR NIVEL", new Vector2(0f, -70f), new Vector2(460f, 96f), false);
        Button btnMenu = CrearBoton(caja, "BotonMenuPrincipal", "MENU PRINCIPAL", new Vector2(0f, -190f), new Vector2(460f, 96f), true);
        Conectar(btnReanudar, pausa.Reanudar);
        Conectar(btnReiniciar, pausa.ReiniciarNivel);
        Conectar(btnMenu, pausa.VolverAlMenu);
        pausa.panel = raizPausa.gameObject;
        raizPausa.gameObject.SetActive(false);

        GameObject objetoManager = new GameObject("GameManager", typeof(GameManager));
        GameManager gm = objetoManager.GetComponent<GameManager>();
        gm.hud = hudController;
        gm.menuPausa = pausa;

        CrearEventSystem();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), RutaNivel);
        Debug.Log("Escena creada: " + RutaNivel);
    }

    static void CrearMuro(string nombre, Sprite sprite, Vector2 posicion, Vector2 escala)
    {
        GameObject muro = new GameObject(nombre, typeof(SpriteRenderer), typeof(BoxCollider2D));
        muro.GetComponent<SpriteRenderer>().sprite = sprite;
        muro.GetComponent<SpriteRenderer>().sortingOrder = 1;
        muro.transform.position = posicion;
        muro.transform.localScale = new Vector3(escala.x, escala.y, 1f);
    }

    static void CrearEnemigo(string nombre, Vector2 inicio, Vector2 fin)
    {
        GameObject enemigo = new GameObject(nombre, typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(EnemyPatrol));
        enemigo.GetComponent<SpriteRenderer>().sprite = SpriteAseprite(RutaEnemigoCamina, 0);
        enemigo.GetComponent<SpriteRenderer>().sortingOrder = 3;
        enemigo.transform.position = inicio;

        Rigidbody2D rb = enemigo.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        CircleCollider2D col = enemigo.GetComponent<CircleCollider2D>();
        col.radius = 0.2f;
        col.offset = new Vector2(0f, -0.1f);
        col.isTrigger = true;

        EnemyPatrol patrulla = enemigo.GetComponent<EnemyPatrol>();
        patrulla.puntoA = inicio;
        patrulla.puntoB = fin;
        patrulla.velocidad = 1.1f;
        AgregarAnimador(enemigo, RutaEnemigoCamina);
    }

    static void CrearEscenaFinJuego()
    {
        NuevaEscena();
        CrearCamara("Main Camera", new Color(0.05f, 0.05f, 0.08f, 1f), false, 5f);

        Canvas canvas = CrearCanvas("CanvasFinJuego", 0);
        GameObject controlador = new GameObject("ControladorFinJuego", typeof(GameOverController));

        CrearImagen(canvas.transform, "Fondo", SpriteAseprite(RutaEdificio, 0), new Vector2(1920f, 1080f), Vector2.zero, new Color(0.72f, 0.70f, 0.78f, 1f));

        RectTransform caja = CrearImagen(canvas.transform, "CajaResultado", CargarSprite("panel"), new Vector2(1000f, 700f), new Vector2(0f, 20f), Color.white);
        Text titulo = CrearTexto(caja, "TituloResultado", "GAME OVER", 110, new Vector2(0f, 230f), new Vector2(900f, 130f), TextAnchor.MiddleCenter, new Color(0.95f, 0.35f, 0.30f), true);
        titulo.gameObject.AddComponent<Shadow>();
        Text puntaje = CrearTexto(caja, "TextoPuntajeFinal", "PUNTAJE FINAL: 0", 54, new Vector2(0f, 110f), new Vector2(880f, 70f), TextAnchor.MiddleCenter, Color.white, true);
        Text detalle = CrearTexto(caja, "TextoDetalle", "Ralph se quedo sin vidas", 38, new Vector2(0f, 30f), new Vector2(880f, 70f), TextAnchor.MiddleCenter, new Color(0.86f, 0.88f, 1f), false);

        GameOverController script = controlador.GetComponent<GameOverController>();
        Button btnReintentar = CrearBoton(caja, "BotonReintentar", "REINTENTAR", new Vector2(0f, -90f), new Vector2(460f, 96f), false);
        Button btnMenu = CrearBoton(caja, "BotonMenuPrincipal", "MENU PRINCIPAL", new Vector2(0f, -210f), new Vector2(460f, 96f), true);
        Conectar(btnReintentar, script.Reintentar);
        Conectar(btnMenu, script.VolverAlMenu);

        script.textoTitulo = titulo;
        script.textoPuntaje = puntaje;
        script.textoDetalle = detalle;

        CrearTexto(canvas.transform, "PiePagina", "Hito 3 - Interfaz de usuario - Demoledor Unifranz", 30,
                   new Vector2(0f, -495f), new Vector2(1400f, 50f), TextAnchor.MiddleCenter, new Color(0.90f, 0.92f, 1f), false);

        CrearEventSystem();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), RutaFin);
        Debug.Log("Escena creada: " + RutaFin);
    }
    static void ConfigurarBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(RutaMenu, true),
            new EditorBuildSettingsScene(RutaNivel, true),
            new EditorBuildSettingsScene(RutaFin, true)
        };

        PlayerSettings.companyName = "Demoledor Unifranz";
        PlayerSettings.productName = "Demoledor Unifranz";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.Minimal);
    }

    static void CompilarEjecutable()
    {
        string raiz = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string carpeta = Path.GetFullPath(Path.Combine(raiz, "..", "ejecutable"));
        Directory.CreateDirectory(carpeta);

        string salida = Path.Combine(carpeta, "DemoledorUnifranz.exe");
        BuildPlayerOptions opciones = new BuildPlayerOptions
        {
            scenes = new[] { RutaMenu, RutaNivel, RutaFin },
            locationPathName = salida,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        UnityEditor.Build.Reporting.BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        Debug.Log("[BUILD] resultado: " + reporte.summary.result + " -> " + salida);

        if (reporte.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new Exception("La compilacion del ejecutable fallo: " + reporte.summary.result);
        }
    }

    static string CarpetaCaps()
    {
        string raiz = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.GetFullPath(Path.Combine(raiz, "..", "caps"));
    }

    static void CapturarPantallas()
    {
        string caps = CarpetaCaps();

        CapturarEscena(RutaMenu, caps + "/01_menu_principal/01_menu_principal.jpg", null);

        CapturarEscena(RutaNivel, caps + "/02_nivel_hud/02_nivel_hud.jpg", () => PrepararNivel(300, 2, 3, false));

        CapturarEscena(RutaNivel, caps + "/03_menu_pausa/03_menu_pausa.jpg", () =>
        {
            PrepararNivel(300, 2, 3, true);

            PauseMenuController pausa = UnityEngine.Object.FindFirstObjectByType<PauseMenuController>();

            if (pausa != null) pausa.Pausar();
        });

        CapturarEscena(RutaFin, caps + "/04_pantalla_game_over/04_pantalla_game_over.jpg", () =>
        {
            GameSession.Victoria = false;
            GameSession.Puntaje = 300;
            GameSession.Ladrillos = 3;
            PrepararFinJuego(false);
        });

        CapturarEscena(RutaFin, caps + "/05_pantalla_victoria/05_pantalla_victoria.jpg", () =>
        {
            GameSession.Victoria = true;
            GameSession.Puntaje = 500;
            GameSession.Ladrillos = 5;
            PrepararFinJuego(true);
        });

        CapturarEscena(RutaMenu, caps + "/06_panel_controles/06_panel_controles.jpg", () =>
        {
            MainMenuController menu = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();

            if (menu != null) menu.AlternarControles();
        });
    }

    static void PrepararNivel(int puntaje, int vidas, int ladrillos, bool pausado)
    {
        GameSession.IniciarPartida(5, Escenas.Nivel1);
        GameSession.Puntaje = puntaje;
        GameSession.Vidas = vidas;
        GameSession.Ladrillos = ladrillos;

        GameManager gm = UnityEngine.Object.FindFirstObjectByType<GameManager>();

        if (gm != null) gm.RefrescarHud();
    }

    static void PrepararFinJuego(bool victoria)
    {
        GameOverController script = UnityEngine.Object.FindFirstObjectByType<GameOverController>();

        if (script == null) return;

        if (script.textoTitulo != null)
        {
            script.textoTitulo.text = victoria ? "VICTORIA" : "GAME OVER";
            script.textoTitulo.color = victoria ? new Color(0.55f, 0.95f, 0.45f) : new Color(0.95f, 0.35f, 0.30f);
        }

        if (script.textoPuntaje != null)
        {
            script.textoPuntaje.text = "PUNTAJE FINAL: " + GameSession.Puntaje;
        }

        if (script.textoDetalle != null)
        {
            script.textoDetalle.text = victoria
                ? "Ralph completo la demolicion: " + GameSession.Ladrillos + " / " + GameSession.MetaLadrillos + " ladrillos"
                : "Ralph se quedo sin vidas: " + GameSession.Ladrillos + " / " + GameSession.MetaLadrillos + " ladrillos";
        }
    }

    static void CapturarEscena(string rutaEscena, string archivo, Action preparar)
    {
        EditorSceneManager.OpenScene(rutaEscena, OpenSceneMode.Single);

        if (preparar != null) preparar();

        int ancho = 1600;
        int alto = 900;

        GameObject objetoCamara = new GameObject("CamaraCaptura");
        Camera cam = objetoCamara.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.06f, 1f);
        cam.orthographic = true;
        cam.orthographicSize = 5.4f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;
        objetoCamara.transform.position = new Vector3(0f, 0f, -10f);

        Camera[] camaras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);

        foreach (Camera otra in camaras)
        {
            if (otra != cam) otra.enabled = false;
        }

        RenderTexture rt = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;

        Canvas[] lienzos = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Canvas lienzo in lienzos)
        {
            lienzo.renderMode = RenderMode.ScreenSpaceCamera;
            lienzo.worldCamera = cam;
            lienzo.planeDistance = 8f;
        }

        Canvas.ForceUpdateCanvases();
        cam.Render();

        RenderTexture anterior = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D captura = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
        captura.ReadPixels(new Rect(0f, 0f, ancho, alto), 0, 0);
        captura.Apply();
        RenderTexture.active = anterior;

        Directory.CreateDirectory(Path.GetDirectoryName(archivo));
        File.WriteAllBytes(archivo, captura.EncodeToJPG(90));

        cam.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(captura);
        UnityEngine.Object.DestroyImmediate(objetoCamara);
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);

        Time.timeScale = 1f;
        Debug.Log("[CAPTURA] " + archivo);
    }
}
