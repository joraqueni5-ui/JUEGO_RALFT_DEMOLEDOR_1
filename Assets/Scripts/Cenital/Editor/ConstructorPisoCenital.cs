using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Genera el piso 1 visto desde arriba (menú Tools → Demoledor Unifranz → Construir Piso 1 cenital):
///  1. Completa las definiciones de piso (12 objetos, laboratorios, ataques).
///  2. Crea las baldosas provisionales (PNG de 32 x 32 px + Tile) en Assets/Arte/PisoCenital.
///  3. Arma el mapa inicial de 22 x 12 baldosas y lo guarda como prefab Mapa_Piso1
///     (Assets/Prefabs/Mapas). Ese prefab es el que el equipo edita a mano con la Tile Palette,
///     moviendo objetos en la escena; el A* lee el mapa tal como quede.
///  4. Crea la escena Assets/Scenes/PisoCenital.unity (cámara, mapa, jugador, demoledor, GestorJuego)
///     y la agrega a Build Settings.
/// Si Mapa_Piso1 ya existe NO se reemplaza (para no perder las ediciones del equipo), salvo que se pida.
/// No toca la escena Universidad ni el modo original.
/// </summary>
public static class ConstructorPisoCenital
{
    private const string CarpetaArte = "Assets/Arte/PisoCenital";
    private const string CarpetaPrefabs = "Assets/Prefabs/Mapas";
    private const string RutaPrefab = "Assets/Prefabs/Mapas/Mapa_Piso1.prefab";
    private const string RutaEscena = "Assets/Scenes/PisoCenital.unity";
    private const string RutaDefinicion = "Assets/Data/Pisos/Piso1_Gastronomia.asset";

    private const int Ancho = 22, Alto = 12;

    // Posición (esquina inferior izquierda, en baldosas) de cada objeto del piso 1, en el orden de
    // DefinicionPiso.objetosCenital: horno, cocina, mesón, refrigerador, lavaplatos, estante,
    // camilla 1, camilla 2, maniquí, microscopios, vitrina, lavamanos.
    private static readonly Vector2Int[] PosicionesObjetos =
    {
        new Vector2Int(1, 9), new Vector2Int(3, 9), new Vector2Int(3, 4), new Vector2Int(6, 9),
        new Vector2Int(1, 6), new Vector2Int(1, 1),
        new Vector2Int(15, 8), new Vector2Int(17, 8), new Vector2Int(19, 9),
        new Vector2Int(16, 4), new Vector2Int(14, 1), new Vector2Int(19, 5),
    };

    [MenuItem("Tools/Demoledor Unifranz/Construir Piso 1 cenital (mapa y escena)", false, 23)]
    public static void Construir()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Piso 1 cenital", "Sal del modo Play antes de construir.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 1) Definiciones de piso.
        int c, e, m;
        ConstructorPisos.Procesar(out c, out e, out m);
        DefinicionPiso definicion = AssetDatabase.LoadAssetAtPath<DefinicionPiso>(RutaDefinicion);
        if (definicion == null)
        {
            EditorUtility.DisplayDialog("Piso 1 cenital", "No encuentro " + RutaDefinicion + ".", "OK");
            return;
        }

        // 2) Baldosas provisionales.
        AsegurarCarpeta("Assets", "Arte");
        AsegurarCarpeta("Assets/Arte", "PisoCenital");
        AsegurarCarpeta("Assets", "Prefabs");
        AsegurarCarpeta("Assets/Prefabs", "Mapas");

        Tile sueloA = CrearBaldosa("suelo_gastronomia", Tablero(new Color(0.96f, 0.86f, 0.70f), new Color(0.90f, 0.78f, 0.60f)), false);
        Tile sueloB = CrearBaldosa("suelo_medicina", Tablero(new Color(0.84f, 0.94f, 0.96f), new Color(0.74f, 0.88f, 0.92f)), false);
        Tile sueloPasillo = CrearBaldosa("suelo_pasillo", Borde(new Color(0.80f, 0.78f, 0.74f), new Color(0.70f, 0.68f, 0.64f)), false);
        Tile puerta = CrearBaldosa("puerta", Borde(new Color(0.58f, 0.40f, 0.24f), new Color(0.42f, 0.28f, 0.16f)), false);
        Tile pared = CrearBaldosa("pared", Ladrillos(new Color(0.34f, 0.35f, 0.42f), new Color(0.22f, 0.23f, 0.29f)), true);
        Sprite ascensor = CrearSprite("ascensor_cerrado", Ascensor(), 32);

        // 3) Mapa (prefab).
        bool crearPrefab = true;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) != null)
        {
            crearPrefab = EditorUtility.DisplayDialog("Mapa_Piso1 ya existe",
                "El prefab Mapa_Piso1 ya existe y puede tener ediciones del equipo.\n\n" +
                "¿Regenerarlo desde cero? (se perderán esos cambios)", "Regenerar", "Conservar");
        }
        if (crearPrefab) ArmarPrefab(sueloA, sueloB, sueloPasillo, puerta, pared, ascensor, definicion);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
        definicion.prefabMapa = prefab;
        definicion.sueloLaboratorioA = sueloA.sprite;
        definicion.sueloLaboratorioB = sueloB.sprite;
        definicion.sueloPasillo = sueloPasillo.sprite;
        definicion.pared = pared.sprite;
        definicion.puerta = puerta.sprite;
        definicion.ascensorCerrado = ascensor;
        EditorUtility.SetDirty(definicion);
        AssetDatabase.SaveAssets();

        // 4) Escena.
        ArmarEscena(definicion, prefab);

        EditorUtility.DisplayDialog("Piso 1 cenital",
            "Listo.\n\n• Mapa: " + RutaPrefab + " (edítalo con la Tile Palette; abre el prefab).\n" +
            "• Escena: " + RutaEscena + " (ya está en Build Settings).\n" +
            "• Dale Play a la escena PisoCenital, o desde MenuPrincipal → NIVELES → Piso 1.", "OK");
    }

    // ------------------------------------------------------------ mapa
    private static void ArmarPrefab(Tile sueloA, Tile sueloB, Tile sueloPasillo, Tile puerta, Tile pared,
        Sprite ascensor, DefinicionPiso definicion)
    {
        GameObject raiz = new GameObject("Mapa_Piso1");
        raiz.AddComponent<MapaCenital>();

        GameObject objGrid = new GameObject("Grid");
        objGrid.transform.SetParent(raiz.transform, false);
        Grid grid = objGrid.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        // Suelo (sin collider).
        GameObject objSuelo = new GameObject("Suelo");
        objSuelo.transform.SetParent(objGrid.transform, false);
        Tilemap suelo = objSuelo.AddComponent<Tilemap>();
        objSuelo.AddComponent<TilemapRenderer>().sortingOrder = -200;

        // Paredes (con collider fusionado: así el A* y la física las tratan como sólido).
        GameObject objParedes = new GameObject("Paredes");
        objParedes.transform.SetParent(objGrid.transform, false);
        Tilemap paredes = objParedes.AddComponent<Tilemap>();
        objParedes.AddComponent<TilemapRenderer>().sortingOrder = -150;
        Rigidbody2D rb = objParedes.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        TilemapCollider2D tc = objParedes.AddComponent<TilemapCollider2D>();
        tc.compositeOperation = Collider2D.CompositeOperation.Merge;
        objParedes.AddComponent<CompositeCollider2D>();

        // Disposición: bordes y paredes divisorias (x = 8 y x = 13) con una puerta de 2 baldosas
        // a cada laboratorio; pasillo central de 4 baldosas (x = 9..12).
        for (int x = 0; x < Ancho; x++)
        {
            for (int y = 0; y < Alto; y++)
            {
                Vector3Int celda = new Vector3Int(x, y, 0);
                bool borde = x == 0 || x == Ancho - 1 || y == 0 || y == Alto - 1;
                bool divisoria = (x == 8 || x == 13) && y >= 1 && y <= Alto - 2;
                bool hueco = (x == 10 || x == 11) && (y == 0 || y == Alto - 1);   // ascensor y ventanal
                bool vano = (x == 8 || x == 13) && (y == 5 || y == 6);            // puertas

                if (hueco) continue;
                if (vano) { suelo.SetTile(celda, puerta); continue; }
                if (borde || divisoria) { paredes.SetTile(celda, pared); continue; }

                if (x <= 7) suelo.SetTile(celda, sueloA);
                else if (x >= 14) suelo.SetTile(celda, sueloB);
                else suelo.SetTile(celda, sueloPasillo);
            }
        }

        // Objetos reparables (los 12 de la definición).
        GameObject contenedor = new GameObject("Objetos");
        contenedor.transform.SetParent(raiz.transform, false);
        for (int i = 0; i < definicion.objetosCenital.Length && i < PosicionesObjetos.Length; i++)
        {
            ObjetoCenital ficha = definicion.objetosCenital[i];
            Vector2Int pos = PosicionesObjetos[i];
            GameObject o = new GameObject(ficha.nombre);
            o.transform.SetParent(contenedor.transform, false);
            o.transform.position = new Vector3(pos.x + ficha.tamanoBaldosas.x * 0.5f, pos.y + ficha.tamanoBaldosas.y * 0.5f, 0f);
            BoxCollider2D col = o.AddComponent<BoxCollider2D>();
            col.size = new Vector2(ficha.tamanoBaldosas.x, ficha.tamanoBaldosas.y);
            ObjetoReparable obj = o.AddComponent<ObjetoReparable>();
            obj.indice = i;
            obj.laboratorio = ficha.laboratorio;
        }

        // Ventanal (arriba) y ascensor (abajo), en el hueco de la pared central.
        GameObject ventanal = new GameObject("Ventanal");
        ventanal.transform.SetParent(raiz.transform, false);
        ventanal.transform.position = new Vector3(11f, Alto - 0.5f, 0f);
        ventanal.AddComponent<BoxCollider2D>().size = new Vector2(2f, 1f);
        VentanalCenital vc = ventanal.AddComponent<VentanalCenital>();

        GameObject asc = new GameObject("Ascensor");
        asc.transform.SetParent(raiz.transform, false);
        asc.transform.position = new Vector3(11f, 0.5f, 0f);
        asc.AddComponent<BoxCollider2D>().size = new Vector2(2f, 1f);
        SpriteRenderer sr = asc.AddComponent<SpriteRenderer>();
        sr.sprite = ascensor;
        sr.sortingOrder = -140;

        // Puntos de aparición.
        GameObject spawnJ = CrearPunto(raiz.transform, "SpawnJugador", new Vector3(11f, 1.5f, 0f));
        GameObject spawnD = CrearPunto(raiz.transform, "SpawnDemoledor", new Vector3(11f, Alto + 0.8f, 0f));
        GameObject entrada = CrearPunto(raiz.transform, "EntradaDemoledor", new Vector3(11f, Alto - 2.4f, 0f));

        MapaCenital mapa = raiz.GetComponent<MapaCenital>();
        mapa.spawnJugador = spawnJ.transform;
        mapa.spawnDemoledor = spawnD.transform;
        mapa.entradaDemoledor = entrada.transform;
        mapa.ventanal = vc;

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static GameObject CrearPunto(Transform padre, string nombre, Vector3 pos)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        g.transform.position = pos;
        return g;
    }

    // ------------------------------------------------------------ escena
    private static void ArmarEscena(DefinicionPiso definicion, GameObject prefab)
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Luz global: los sprites del proyecto (URP 2D) se ven con ella.
        GameObject luz = new GameObject("Global Light 2D");
        Light2D l2d = luz.AddComponent<Light2D>();
        l2d.lightType = Light2D.LightType.Global;
        l2d.intensity = 1f;

        // Cámara fija que encuadra el mapa entero (16:9 y 20:9).
        GameObject objCam = new GameObject("Main Camera");
        objCam.tag = "MainCamera";
        Camera cam = objCam.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.4f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.07f, 0.1f, 1f);
        cam.transform.position = new Vector3(11f, 6f, -10f);
        objCam.AddComponent<AudioListener>();
        CamaraPisoCenital camaraCenital = objCam.AddComponent<CamaraPisoCenital>();

        // Piso: lógica + mapa (instancia del prefab, editable en la escena).
        GameObject objPiso = new GameObject("PisoCenital");
        PisoCenital piso = objPiso.AddComponent<PisoCenital>();
        piso.definicion = definicion;
        piso.camara = camaraCenital;
        GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, objPiso.transform);
        MapaCenital mapa = instancia.GetComponent<MapaCenital>();
        piso.mapa = mapa;

        PhysicsMaterial2D sinRoce = CrearMaterialSinRoce();
        Vector3 posJugador = mapa.spawnJugador != null ? mapa.spawnJugador.position : new Vector3(11f, 1.5f, 0f);

        // Jugador.
        GameObject jug = new GameObject("Jugador");
        jug.transform.position = posJugador;
        Rigidbody2D rbJ = jug.AddComponent<Rigidbody2D>();
        rbJ.gravityScale = 0f;
        CircleCollider2D colJ = jug.AddComponent<CircleCollider2D>();
        colJ.radius = 0.3f;
        colJ.offset = new Vector2(0f, 0.3f);
        colJ.sharedMaterial = sinRoce;
        GameObject dibJ = new GameObject("Dibujo");
        dibJ.transform.SetParent(jug.transform, false);
        dibJ.AddComponent<SpriteRenderer>();
        JugadorCenital jugador = jug.AddComponent<JugadorCenital>();
        jugador.animaciones.caminarAbajo = Sprites("Assets/personaje/Movimiento Abajo.aseprite");
        // OJO: "Movimiento arriba.aseprite" es una paleta de 16 colores (16x4 px), no una animación.
        jugador.animaciones.caminarArriba = Sprites("Assets/personaje/Movimiento Espalda1.aseprite");
        jugador.animaciones.caminarDerecha = Sprites("Assets/personaje/Movimiento Derecha.aseprite");
        Sprite[] martillo = Sprites("Assets/personaje/Movimiento martillo.aseprite");
        jugador.animaciones.golpeAbajo = martillo;
        jugador.animaciones.golpeArriba = martillo;
        jugador.animaciones.golpeDerecha = martillo;

        // Demoledor.
        Vector3 posDem = mapa.spawnDemoledor != null ? mapa.spawnDemoledor.position : new Vector3(11f, 12.8f, 0f);
        GameObject dem = new GameObject("Demoledor");
        dem.transform.position = posDem;
        dem.AddComponent<Rigidbody2D>().gravityScale = 0f;
        CircleCollider2D colD = dem.AddComponent<CircleCollider2D>();
        colD.radius = 0.35f;            // menor que media baldosa: pasa por puertas y esquinas
        colD.offset = Vector2.zero;     // centrado, igual que los puntos del A*
        colD.sharedMaterial = sinRoce;
        GameObject dibD = new GameObject("Dibujo");
        dibD.transform.SetParent(dem.transform, false);
        dibD.AddComponent<SpriteRenderer>();
        DemoledorCenital demoledor = dem.AddComponent<DemoledorCenital>();
        Sprite[] caminar = Sprites("Assets/Animaciones/Enemigo/animacion_caminata_derecha.aseprite");
        demoledor.animaciones.caminarDerecha = caminar;
        demoledor.animaciones.caminarAbajo = caminar;
        demoledor.animaciones.caminarArriba = caminar;
        Sprite[] golpe = Sprites("Assets/Animaciones/Enemigo/demoledor_golpe.aseprite");
        demoledor.animaciones.golpeDerecha = golpe;
        demoledor.animaciones.golpeAbajo = golpe;
        demoledor.animaciones.golpeArriba = golpe;

        // Marcador, vidas, pausa y fin de partida.
        new GameObject("GestorJuego").AddComponent<GestorJuego>();

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena, RutaEscena);
        AgregarABuildSettings(RutaEscena);

        Validar(mapa, definicion);

        // La validación deja objetos temporales en la escena abierta: se recarga limpia desde el archivo.
        EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
    }

    /// <summary>Comprueba en el editor que el A* llega a cada objeto desde la entrada.</summary>
    private static void Validar(MapaCenital mapa, DefinicionPiso definicion)
    {
        mapa.Preparar(definicion);
        Vector2 desde = mapa.entradaDemoledor != null ? (Vector2)mapa.entradaDemoledor.position : mapa.Limites.center;
        int sinCamino = 0;
        foreach (ObjetoReparable o in mapa.objetos)
        {
            bool llega = false;
            foreach (Vector2 celda in mapa.CentrosTransitables)
            {
                if (Vector2.Distance(celda, o.PuntoMasCercano(celda)) > 0.8f) continue;
                if (BuscadorRutas.Buscar(mapa, desde, celda) != null) { llega = true; break; }
            }
            if (!llega) { sinCamino++; Debug.LogWarning("[PisoCenital] El demoledor no llega al objeto '" + o.name + "'."); }
        }
        Debug.Log("[PisoCenital] Mapa listo: " + mapa.CentrosTransitables.Count + " celdas transitables, " +
                  mapa.objetos.Count + " objetos, " + sinCamino + " sin camino.");
    }

    private static void AgregarABuildSettings(string ruta)
    {
        List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene s in lista)
        {
            if (s.path == ruta) { s.enabled = true; EditorBuildSettings.scenes = lista.ToArray(); return; }
        }
        lista.Add(new EditorBuildSettingsScene(ruta, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }

    // ------------------------------------------------------------ utilidades de arte
    private static PhysicsMaterial2D CrearMaterialSinRoce()
    {
        string ruta = CarpetaArte + "/SinRoce.physicsMaterial2D";
        PhysicsMaterial2D mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(ruta);
        if (mat != null) return mat;
        mat = new PhysicsMaterial2D("SinRoce") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(mat, ruta);
        return mat;
    }

    /// <summary>Sprites de un .aseprite, en orden numérico del cuadro (o null si no hay).</summary>
    private static Sprite[] Sprites(string ruta)
    {
        List<Sprite> lista = new List<Sprite>();
        foreach (Object recurso in AssetDatabase.LoadAllAssetsAtPath(ruta))
        {
            Sprite s = recurso as Sprite;
            if (s == null) continue;
            if (s.rect.width < AnimacionesCenital.LadoMinimoPx || s.rect.height < AnimacionesCenital.LadoMinimoPx)
            {
                Debug.LogWarning("[PisoCenital] " + ruta + " parece una paleta (" + s.rect.width + "x" + s.rect.height + " px): se excluye.");
                continue;
            }
            lista.Add(s);
        }
        if (lista.Count == 0)
        {
            Debug.LogWarning("[PisoCenital] Sin sprites en " + ruta + ": se usa figura provisional.");
            return null;
        }
        lista.Sort((a, b) =>
        {
            int na = NumeroFinal(a.name), nb = NumeroFinal(b.name);
            return na != nb ? na.CompareTo(nb) : string.CompareOrdinal(a.name, b.name);
        });
        return lista.ToArray();
    }

    private static int NumeroFinal(string nombre)
    {
        int fin = nombre.Length, ini = fin;
        while (ini > 0 && char.IsDigit(nombre[ini - 1])) ini--;
        int n;
        return ini < fin && int.TryParse(nombre.Substring(ini), out n) ? n : -1;
    }

    private static void AsegurarCarpeta(string padre, string nombre)
    {
        if (!AssetDatabase.IsValidFolder(padre + "/" + nombre)) AssetDatabase.CreateFolder(padre, nombre);
    }

    private static Tile CrearBaldosa(string nombre, Texture2D textura, bool solida)
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
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    private static Texture2D Lienzo(int ancho, int alto, Color relleno)
    {
        Texture2D t = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        Color[] px = new Color[ancho * alto];
        for (int i = 0; i < px.Length; i++) px[i] = relleno;
        t.SetPixels(px);
        return t;
    }

    /// <summary>Suelo de losetas: cuadros de 16 px alternando dos tonos.</summary>
    private static Texture2D Tablero(Color a, Color b)
    {
        Texture2D t = Lienzo(32, 32, a);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                if (((x / 16) + (y / 16)) % 2 == 1) t.SetPixel(x, y, b);
        t.Apply();
        return t;
    }

    private static Texture2D Borde(Color relleno, Color borde)
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

    private static Texture2D Ladrillos(Color relleno, Color junta)
    {
        Texture2D t = Lienzo(32, 32, relleno);
        for (int y = 0; y < 32; y++)
        {
            bool filaJunta = y % 8 == 0;
            int desfase = (y / 8) % 2 == 0 ? 0 : 8;
            for (int x = 0; x < 32; x++)
                if (filaJunta || (x + desfase) % 16 == 0) t.SetPixel(x, y, junta);
        }
        t.Apply();
        return t;
    }

    /// <summary>Puertas del ascensor cerradas (64 x 32 px = 2 x 1 baldosas).</summary>
    private static Texture2D Ascensor()
    {
        Texture2D t = Lienzo(64, 32, new Color(0.45f, 0.47f, 0.52f));
        Color marco = new Color(0.2f, 0.21f, 0.25f);
        for (int x = 0; x < 64; x++) { t.SetPixel(x, 0, marco); t.SetPixel(x, 31, marco); }
        for (int y = 0; y < 32; y++) { t.SetPixel(0, y, marco); t.SetPixel(63, y, marco); t.SetPixel(31, y, marco); t.SetPixel(32, y, marco); }
        t.Apply();
        return t;
    }
}
