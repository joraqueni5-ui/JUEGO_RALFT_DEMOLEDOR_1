using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta el edificio alto en la escena Universidad (menú Tools → Demoledor Unifranz).
///
/// - Borra el edificio viejo de bloques y su interior oscuro (ya no se usan).
/// - Crea "EdificioAlto": la fachada (edificio_alto.png) y sus 45 ventanas
///   (9 pisos x 5 columnas, 3 secciones) con el componente EdificioAlto.
/// - Coloca al demoledor en la primera cornisa y le asigna el sprite de escombro.
/// - Hace que la cámara siga al muñeco y que el fondo la acompañe.
///
/// Se puede ejecutar varias veces: rehace el edificio alto desde cero.
/// No toca los menús, ni al muñeco, ni las animaciones del demoledor.
/// </summary>
public static class ConstructorEdificioAlto
{
    private const string RutaEscena = "Assets/Scenes/Universidad.unity";
    private const string RutaFachada = "Assets/Sprites/Edificio/edificio_alto.png";
    private const string RutaEscombro = "Assets/Sprites/Edificio/escombro.png";
    private const string RutaVentanas = "Assets/Sprites/Ventanas/ventana_{0}_{1}.png";

    // Medidas del dibujo (píxeles) y escala (50 píxeles = 1 unidad).
    private const float Ppu = 50f;
    private const int AltoDibujo = 700;
    private const int AnchoDibujo = 386;

    // Esquina inferior izquierda del edificio en el mundo (la calle está en y = 0;
    // el dibujo tiene 1 píxel de contorno por debajo de la vereda).
    private const float EdificioX = 1.75f;
    private const float EdificioY = -0.02f;

    // Esquina superior izquierda del marco de cada ventana, en píxeles del dibujo.
    private static readonly int[] ColumnasX = { 22, 84, 156, 218, 280 };
    private static readonly int[] PisosY = { 539, 481, 423, 357, 299, 241, 175, 117, 59 };

    // Líneas donde se para el demoledor (píxeles): cornisa 1, cornisa 2 y techo.
    private static readonly int[] CornisasY = { 405, 223, 41 };

    // Zona por donde trepa el muñeco y terraza verde del techo (píxeles).
    private const int FachadaX0 = 1, FachadaX1 = 362, FachadaYTecho = 41, FachadaYSuelo = 699;
    private const int TerrazaX0 = 241, TerrazaX1 = 340;

    private const int OrdenFachada = 1;
    private const int OrdenVentanas = 2;

    private const float TamanoCamara = 3.7f;
    private static readonly Color ColorCielo = new Color(104f / 255f, 138f / 255f, 186f / 255f, 1f);

    [MenuItem("Tools/Demoledor Unifranz/Construir Edificio Alto (Universidad)", false, 20)]
    public static void Construir()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Edificio alto", "Sal del modo Play antes de construir el edificio.", "OK");
            return;
        }

        if (!AbrirUniversidad()) return;

        Sprite fachada = AssetDatabase.LoadAssetAtPath<Sprite>(RutaFachada);
        Sprite escombro = AssetDatabase.LoadAssetAtPath<Sprite>(RutaEscombro);
        Sprite[,] ventanas = new Sprite[4, 3];
        bool faltan = fachada == null || escombro == null;
        for (int t = 0; t < 4; t++)
        {
            for (int e = 0; e < 3; e++)
            {
                ventanas[t, e] = AssetDatabase.LoadAssetAtPath<Sprite>(string.Format(RutaVentanas, t, e));
                if (ventanas[t, e] == null) faltan = true;
            }
        }
        if (faltan)
        {
            EditorUtility.DisplayDialog("Edificio alto",
                "Faltan sprites en Assets/Sprites/Edificio o Assets/Sprites/Ventanas.\n\nHaz git pull y espera a que Unity termine de importar.", "OK");
            return;
        }

        // 1) Edificio viejo de bloques e interior oscuro: ya no se usan, se borran.
        Borrar("Edificio");
        Borrar("Interior");

        // 2) Edificio alto nuevo (si ya existía se rehace).
        GameObject viejo = GameObject.Find("EdificioAlto");
        if (viejo != null) Undo.DestroyObjectImmediate(viejo);

        GameObject raiz = new GameObject("EdificioAlto");
        Undo.RegisterCreatedObjectUndo(raiz, "Construir edificio alto");
        raiz.transform.position = new Vector3(EdificioX, EdificioY, 0f);

        GameObject objFachada = new GameObject("Fachada");
        objFachada.transform.SetParent(raiz.transform, false);
        SpriteRenderer srFachada = objFachada.AddComponent<SpriteRenderer>();
        srFachada.sprite = fachada;
        srFachada.sortingOrder = OrdenFachada;

        GameObject contenedor = new GameObject("Ventanas");
        contenedor.transform.SetParent(raiz.transform, false);

        for (int piso = 0; piso < PisosY.Length; piso++)
        {
            for (int col = 0; col < ColumnasX.Length; col++)
            {
                int tipo = (piso * 3 + col * 5 + piso / 3) % 4;

                GameObject obj = new GameObject(string.Format("Ventana_{0}_{1}", piso, col));
                obj.transform.SetParent(contenedor.transform, false);
                obj.transform.localPosition = new Vector3(ColumnasX[col] / Ppu, (AltoDibujo - PisosY[piso]) / Ppu, 0f);

                SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = ventanas[tipo, 0];
                sr.sortingOrder = OrdenVentanas;

                VentanaEdificio v = obj.AddComponent<VentanaEdificio>();
                v.piso = piso;
                v.columna = col;
                v.seccion = piso / 3;
                v.dibujo = sr;
                v.sprites = new Sprite[] { ventanas[tipo, 0], ventanas[tipo, 1], ventanas[tipo, 2] };
                v.spriteTrozos = escombro;
                v.tamanoMarco = new Vector2(45f / Ppu, 37f / Ppu);
            }
        }

        EdificioAlto edificio = raiz.AddComponent<EdificioAlto>();
        edificio.columnas = ColumnasX.Length;
        edificio.pisosPorSeccion = 3;
        edificio.secciones = 3;
        edificio.fachada = Rect.MinMaxRect(FachadaX0 / Ppu, (AltoDibujo - FachadaYSuelo) / Ppu,
                                           FachadaX1 / Ppu, (AltoDibujo - FachadaYTecho) / Ppu);
        edificio.alturaCornisas = new float[CornisasY.Length];
        for (int i = 0; i < CornisasY.Length; i++) edificio.alturaCornisas[i] = (AltoDibujo - CornisasY[i]) / Ppu;
        edificio.centroColumnas = new float[ColumnasX.Length];
        for (int c = 0; c < ColumnasX.Length; c++) edificio.centroColumnas[c] = (ColumnasX[c] + 22.5f) / Ppu;
        edificio.terraza = new Vector2(TerrazaX0 / Ppu, TerrazaX1 / Ppu);

        // 3) Demoledor: en la primera cornisa, con sprite de escombro. Sus animaciones no se tocan.
        DemoledorAutomatico demoledor = Object.FindFirstObjectByType<DemoledorAutomatico>(FindObjectsInactive.Include);
        if (demoledor != null)
        {
            Undo.RecordObject(demoledor, "Construir edificio alto");
            Undo.RecordObject(demoledor.transform, "Construir edificio alto");
            demoledor.spriteEscombro = escombro;
            demoledor.tiempoCambioPiso = 0.9f;
            demoledor.transform.position = new Vector3(
                EdificioX + edificio.centroColumnas[0], EdificioY + edificio.alturaCornisas[0], demoledor.transform.position.z);
            EditorUtility.SetDirty(demoledor);
        }

        // 4) Cámara que sigue al muñeco y fondo que la acompaña.
        Muneco muneco = Object.FindFirstObjectByType<Muneco>(FindObjectsInactive.Include);
        Camera camara = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        float techo = EdificioY + AltoDibujo / Ppu;
        float yMinima = TamanoCamara - 0.6f;
        float yMaxima = techo + 0.4f - TamanoCamara;

        if (camara != null)
        {
            Undo.RecordObject(camara, "Construir edificio alto");
            Undo.RecordObject(camara.transform, "Construir edificio alto");
            camara.orthographic = true;
            camara.orthographicSize = TamanoCamara;
            camara.backgroundColor = ColorCielo;
            camara.clearFlags = CameraClearFlags.SolidColor;

            Vector3 p = camara.transform.position;
            p.x = EdificioX + AnchoDibujo / Ppu * 0.5f;
            p.y = yMinima;
            camara.transform.position = p;

            CamaraSeguimiento seguir = camara.GetComponent<CamaraSeguimiento>();
            if (seguir == null) seguir = Undo.AddComponent<CamaraSeguimiento>(camara.gameObject);
            seguir.objetivo = muneco != null ? muneco.transform : null;
            seguir.demoledor = demoledor != null ? demoledor.transform : null;
            seguir.yMinima = yMinima;
            seguir.yMaxima = yMaxima;
            EditorUtility.SetDirty(seguir);
            EditorUtility.SetDirty(camara);

            GameObject fondo = GameObject.Find("Fondo");
            if (fondo != null)
            {
                Undo.RecordObject(fondo.transform, "Construir edificio alto");
                SpriteRenderer srFondo = fondo.GetComponent<SpriteRenderer>();
                float escala = 1f;
                if (srFondo != null && srFondo.sprite != null)
                {
                    Vector2 tam = srFondo.sprite.bounds.size;
                    float altoVista = TamanoCamara * 2f;
                    float anchoVista = altoVista * 16f / 9f;
                    escala = Mathf.Max(1f, anchoVista / tam.x, altoVista / tam.y) * 1.04f;
                }
                fondo.transform.localScale = new Vector3(escala, escala, 1f);
                fondo.transform.position = new Vector3(p.x, p.y, fondo.transform.position.z);

                FondoParalaje paralaje = fondo.GetComponent<FondoParalaje>();
                if (paralaje == null) paralaje = Undo.AddComponent<FondoParalaje>(fondo);
                paralaje.camara = camara.transform;
                EditorUtility.SetDirty(paralaje);
            }
        }

        Scene escena = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        bool guardada = EditorSceneManager.SaveScene(escena);
        Selection.activeGameObject = raiz;

        EditorUtility.DisplayDialog("Edificio alto",
            "Listo: edificio alto con 45 ventanas en 3 secciones.\n\n" +
            "• Se borró el edificio viejo de bloques y su interior.\n" +
            "• Demoledor en la primera cornisa, cámara siguiendo al muñeco.\n" +
            (guardada ? "• La escena Universidad se guardó.\n" : "• Guarda la escena (Ctrl+S).\n") +
            (demoledor == null ? "\n⚠ No se encontró el Demoledor en la escena.\n" : "") +
            (muneco == null ? "\n⚠ No se encontró el Muneco en la escena.\n" : "") +
            "\nDale Play desde MenuPrincipal para probar.", "OK");
    }

    private static bool AbrirUniversidad()
    {
        Scene activa = SceneManager.GetActiveScene();
        if (activa.path == RutaEscena) return true;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        return SceneManager.GetActiveScene().path == RutaEscena;
    }

    /// <summary>Borra un objeto raíz de la escena por su nombre (aunque esté apagado).</summary>
    private static void Borrar(string nombre)
    {
        foreach (GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (obj != null && obj.name == nombre)
            {
                Undo.DestroyObjectImmediate(obj);
                return;
            }
        }
    }
}
