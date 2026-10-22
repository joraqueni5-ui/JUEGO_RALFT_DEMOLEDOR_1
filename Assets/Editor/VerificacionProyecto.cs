using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Herramienta de verificación (solo editor).
///   Unity.exe -batchmode -projectPath . -executeMethod VerificacionProyecto.Ejecutar -logFile -
/// Comprueba la escena, entra en Play Mode, deja trabajar al demoledor automático,
/// prueba romper/reparar bloque a bloque, el trepado, el fin de partida y el render.
/// </summary>
public static class VerificacionProyecto
{
    private const string RutaEscena = "Assets/Scenes/Universidad.unity";
    private const string RutaCaptura = "Tools/out/captura.png";
    private const int AnchoCaptura = 1280;
    private const int AltoCaptura = 720;
    private const float AspectoCaptura = (float)AnchoCaptura / AltoCaptura;
    private const int BloquesEsperados = 93;

    private const string ClaveActiva = "verificacion_activa";
    private const string ClaveFase = "verificacion_fase";
    private const string ClaveFallos = "verificacion_fallos";

    private static int fallos;
    private static int frames;

    // Estado que se conserva entre frames dentro de la misma sesión de Play Mode.
    private static Color[] pixelesIniciales;
    private static Vector2 centroPrueba;

    /// <summary>Solo diagnóstico de assets (sin entrar en Play Mode).</summary>
    public static void Diagnostico()
    {
        EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        fallos = 0;
        ValidarEnEditor();
        EditorApplication.Exit(fallos == 0 ? 0 : 1);
    }

    /// <summary>Se vuelve a enganchar el update después de recargar el dominio al entrar en Play Mode.</summary>
    [InitializeOnLoadMethod]
    private static void AlCargarDominio()
    {
        if (!SessionState.GetBool(ClaveActiva, false)) return;
        EditorApplication.update -= Actualizar;
        EditorApplication.update += Actualizar;
    }

    public static void Ejecutar()
    {
        fallos = 0;

        EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        ValidarEnEditor();

        SessionState.SetBool(ClaveActiva, true);
        SessionState.SetInt(ClaveFase, 1);
        SessionState.SetInt(ClaveFallos, fallos);

        frames = 0;
        EditorApplication.update -= Actualizar;
        EditorApplication.update += Actualizar;
        EditorApplication.isPlaying = true;
    }

    private static void Actualizar()
    {
        if (!SessionState.GetBool(ClaveActiva, false))
        {
            EditorApplication.update -= Actualizar;
            return;
        }

        fallos = SessionState.GetInt(ClaveFallos, 0);
        int fase = SessionState.GetInt(ClaveFase, 0);

        if (fase == 1)
        {
            if (!EditorApplication.isPlaying) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            frames++;
            // Se le da tiempo al demoledor a caminar y romper unos cuantos bloques.
            if (Time.timeSinceLevelLoad < 6f && frames < 12000) return;

            ProbarParte1();
            SessionState.SetInt(ClaveFallos, fallos);
            SessionState.SetInt(ClaveFase, 4);
            frames = 0;
            return;
        }

        if (fase == 4)
        {
            if (!EditorApplication.isPlaying) return;

            frames++;
            if (frames < 25) return;   // que termine la animación de reparar

            ProbarParte2();
            SessionState.SetInt(ClaveFallos, fallos);
            SessionState.SetInt(ClaveFase, 2);
            frames = 0;
            return;
        }

        if (fase == 2)
        {
            frames++;
            if (frames < 5) return;

            SessionState.SetInt(ClaveFase, 3);
            EditorApplication.isPlaying = false;
            return;
        }

        if (fase == 3)
        {
            frames++;
            if (frames < 10) return;

            SessionState.SetBool(ClaveActiva, false);
            EditorApplication.update -= Actualizar;
            Debug.Log(fallos == 0 ? "VERIFICACION OK" : "VERIFICACION CON " + fallos + " FALLO(S)");
            EditorApplication.Exit(fallos == 0 ? 0 : 1);
        }
    }

    // ---------------------------------------------------------------- editor
    private static void ValidarEnEditor()
    {
        DemoledorAutomatico demoledor = Object.FindFirstObjectByType<DemoledorAutomatico>(FindObjectsInactive.Include);
        Muneco muneco = Object.FindFirstObjectByType<Muneco>(FindObjectsInactive.Include);
        GestorJuego gestor = Object.FindFirstObjectByType<GestorJuego>(FindObjectsInactive.Include);
        GameObject edificioGO = GameObject.Find("Edificio");
        Camera camara = Camera.main;

        Comprobar(edificioGO != null, "Existe el objeto 'Edificio'");
        Comprobar(demoledor != null, "Existe el Demoledor automático");
        Comprobar(muneco != null, "Existe el Muneco");
        Comprobar(gestor != null, "Existe el GestorJuego");
        Comprobar(camara != null, "Existe la Main Camera");
        if (camara != null) Comprobar(camara.orthographic, "La camara es ortografica");

        if (edificioGO != null)
        {
            BloqueEdificio[] bloques = edificioGO.GetComponentsInChildren<BloqueEdificio>(true);
            Comprobar(bloques.Length == BloquesEsperados,
                "El edificio tiene " + BloquesEsperados + " bloques (encontrados " + bloques.Length + ")");

            HashSet<string> nombres = new HashSet<string>();
            int sinSprite = 0, sinDanado = 0;
            bool rejillaCorrecta = true;
            HashSet<string> celdas = new HashSet<string>();
            foreach (BloqueEdificio bloque in bloques)
            {
                SpriteRenderer sr = bloque.GetComponent<SpriteRenderer>();
                if (sr == null || sr.sprite == null) sinSprite++;
                else nombres.Add(sr.sprite.name);
                if (bloque.spriteAgrietado == null) sinDanado++;

                if (bloque.fila < 0 || bloque.fila > 7 || bloque.columna < 0 || bloque.columna > 11) rejillaCorrecta = false;
                if (!celdas.Add(bloque.fila + ":" + bloque.columna)) rejillaCorrecta = false;
            }

            Comprobar(sinSprite == 0, "Todos los bloques tienen sprite");
            Comprobar(sinDanado == 0, "Todos los bloques tienen sprite agrietado");
            Comprobar(nombres.Count == bloques.Length, "Cada bloque usa un trozo distinto del edificio");
            Comprobar(rejillaCorrecta, "Cada bloque tiene una celda (fila, columna) única");
        }

        if (demoledor != null)
        {
            Comprobar(demoledor.caminarIzquierda != null && demoledor.caminarIzquierda.Length == 6, "Demoledor: 6 cuadros caminando a la izquierda");
            Comprobar(demoledor.caminarDerecha != null && demoledor.caminarDerecha.Length == 6, "Demoledor: 6 cuadros caminando a la derecha");
            Comprobar(demoledor.salto != null && demoledor.salto.Length == 4, "Demoledor: 4 cuadros de salto");
            Comprobar(demoledor.golpe != null && demoledor.golpe.Length == 6, "Demoledor: 6 cuadros de golpe");
        }

        if (muneco != null)
        {
            Comprobar(muneco.caminar != null && muneco.caminar.Length == 4, "Muneco: 4 cuadros de caminata");
            Comprobar(muneco.caminarEspaldas != null && muneco.caminarEspaldas.Length == 4, "Muneco: 4 cuadros para trepar de espaldas");
            Comprobar(muneco.martillo != null && muneco.martillo.Length == 4, "Muneco: 4 cuadros de martillo");
        }

        Font fuente = null;
        try { fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { fuente = null; }
        Comprobar(fuente != null, "La fuente del HUD esta disponible");
    }

    // ---------------------------------------------------------------- juego (1)
    private static void ProbarParte1()
    {
        Edificio edificio = Edificio.Instancia;
        DemoledorAutomatico demoledor = Object.FindFirstObjectByType<DemoledorAutomatico>();
        Muneco muneco = Object.FindFirstObjectByType<Muneco>();
        GestorJuego gestor = Object.FindFirstObjectByType<GestorJuego>();

        Comprobar(edificio != null, "Edificio.Instancia existe en Play Mode");
        Comprobar(demoledor != null && muneco != null && gestor != null, "Estan el demoledor, el muñeco y el gestor");
        if (edificio == null || demoledor == null || muneco == null || gestor == null) return;

        Comprobar(edificio.Total == BloquesEsperados, "El edificio registra " + BloquesEsperados + " bloques (" + edificio.Total + ")");
        Comprobar(edificio.Columnas == 12 && edificio.Filas == 8, "La rejilla es de 12 x 8");
        Comprobar(edificio.Limites.height > 3.9f && edificio.Limites.width > 7.1f, "El edificio mide ~7.2 x 4.0");

        // ------------------ EL DEMOLEDOR TRABAJA SOLO ------------------
        int tocados = edificio.Demolidos + edificio.Agrietados;
        Comprobar(tocados >= 2, "El demoledor automatico ya rompio varios bloques sin que nadie lo controle (" + tocados + ")");
        Comprobar(demoledor.transform.position.y > 2.5f, "El demoledor esta trabajando arriba del edificio (y=" + demoledor.transform.position.y.ToString("0.0") + ")");
        Comprobar(edificio.Demolidos > 0, "Al menos un bloque quedo totalmente demolido");

        // Foto del edificio a medio demoler (evidencia del trabajo automatico).
        Texture2D enDemolicion = Renderizar(Camera.main);
        File.WriteAllBytes("Tools/out/captura_demolicion.png", enDemolicion.EncodeToPNG());
        Object.DestroyImmediate(enDemolicion);

        // Se detiene para que el resto de las pruebas sea estable.
        demoledor.Detener();

        // ------------------ UN GOLPE = UN BLOQUE ------------------
        BloqueEdificio objetivo = null;
        for (int c = 0; c < edificio.Columnas && objetivo == null; c++)
        {
            BloqueEdificio b = edificio.BloqueEn(0, c);
            if (b != null && b.Estado == EstadoBloque.Intacto) objetivo = b;
        }
        Comprobar(objetivo != null, "Queda algun bloque intacto para la prueba");
        if (objetivo == null) return;

        centroPrueba = objetivo.Area.center;
        edificio.Reiniciar();
        int antesDemolidos = edificio.Demolidos;
        int antesAgrietados = edificio.Agrietados;

        edificio.Golpear(objetivo, 1);
        Comprobar(objetivo.Estado == EstadoBloque.Agrietado, "Un golpe agrieta el bloque");
        Comprobar(edificio.Agrietados == antesAgrietados + 1, "El golpe afecta a UN solo bloque (agrietados " + antesAgrietados + " -> " + edificio.Agrietados + ")");
        Comprobar(edificio.Demolidos == antesDemolidos, "El golpe no destruye nada todavia");

        edificio.Golpear(objetivo, 1);
        Comprobar(objetivo.Estado == EstadoBloque.Destruido, "El segundo golpe destruye ese bloque");
        Comprobar(edificio.Demolidos == antesDemolidos + 1, "Se demolio exactamente un bloque");
        Comprobar(!objetivo.GetComponent<SpriteRenderer>().enabled, "El bloque destruido deja su hueco");

        // ------------------ CAPTURA DE REFERENCIA ------------------
        edificio.Reiniciar();
        Camera camara = Camera.main;
        Texture2D inicial = Renderizar(camara);
        pixelesIniciales = inicial.GetPixels();
        Object.DestroyImmediate(inicial);

        // ------------------ REPARAR DE A UN BLOQUE ------------------
        edificio.Golpear(objetivo, 2);
        Comprobar(objetivo.Estado == EstadoBloque.Destruido, "El bloque objetivo quedo destruido para reparar");

        muneco.Soltar();
        muneco.transform.position = new Vector3(centroPrueba.x, Mathf.Max(0f, centroPrueba.y - 0.45f), 0f);
        int reparaciones = gestor.Reparaciones;
        BloqueEdificio reparado = edificio.RepararMasCercano(centroPrueba, 0.55f);
        Comprobar(reparado == objetivo, "El martillazo repara el bloque que se esta tocando");
        Comprobar(objetivo.Estado == EstadoBloque.Intacto, "El bloque reparado vuelve a estar intacto");
        Comprobar(gestor.Reparaciones == reparaciones + 1, "El marcador cuenta la reparacion");
        Comprobar(edificio.Demolidos == 0, "No queda ningun bloque demolido");

        // ------------------ TREPAR ------------------
        muneco.transform.position = new Vector3(edificio.Limites.xMin + 1.5f, 0f, 0f);
        bool agarro = muneco.Agarrarse();
        Comprobar(agarro, "El muñeco se agarra a la fachada");
        Comprobar(muneco.Trepando, "El muñeco queda en modo trepar");
        Comprobar(Mathf.Abs(muneco.GetComponent<Rigidbody2D>().gravityScale) < 0.01f, "Trepando no le afecta la gravedad");

        muneco.transform.position = new Vector3(edificio.Limites.xMin + 1.5f, 2.0f, 0f);
        muneco.Soltar();
        Comprobar(!muneco.Trepando, "El muñeco se suelta de la fachada");
        Comprobar(muneco.GetComponent<Rigidbody2D>().gravityScale > 1f, "Al soltarse vuelve la gravedad");

        // ------------------ HUD ------------------
        Canvas lienzo = Object.FindFirstObjectByType<Canvas>();
        Comprobar(lienzo != null, "El HUD (Canvas) se creo");
        if (lienzo != null)
        {
            Text[] textos = lienzo.GetComponentsInChildren<Text>(true);
            Comprobar(textos.Length >= 4, "El HUD tiene textos (" + textos.Length + ")");

            bool muestraPuntos = false, muestraEstructura = false;
            foreach (Text t in textos)
            {
                if (t.text != null && t.text.Contains("PUNTOS")) muestraPuntos = true;
                if (t.text != null && t.text.Contains("ESTRUCTURA")) muestraEstructura = true;
            }
            Comprobar(muestraPuntos, "El marcador muestra los PUNTOS");
            Comprobar(muestraEstructura, "El marcador muestra la ESTRUCTURA");
        }

        Comprobar(!gestor.PartidaTerminada, "La partida sigue en curso con el edificio entero");

        // Vuelve el muñeco al suelo para la captura.
        muneco.transform.position = new Vector3(1.15f, 0f, 0f);
        muneco.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
    }

    // ---------------------------------------------------------------- juego (2)
    private static void ProbarParte2()
    {
        Edificio edificio = Edificio.Instancia;
        DemoledorAutomatico demoledor = Object.FindFirstObjectByType<DemoledorAutomatico>();
        Muneco muneco = Object.FindFirstObjectByType<Muneco>();
        GestorJuego gestor = Object.FindFirstObjectByType<GestorJuego>();
        Camera camara = Camera.main;

        if (edificio == null || demoledor == null || muneco == null || gestor == null || camara == null || pixelesIniciales == null) return;

        // ------------------ RENDER: REPARADO = COMO AL INICIO ------------------
        Texture2D actual = Renderizar(camara);
        Color[] pixelesActuales = actual.GetPixels();

        float diferencia = FraccionCambiada(pixelesIniciales, pixelesActuales, camara, edificio.Limites);
        Comprobar(diferencia < 0.06f, "El edificio reparado se ve igual que al inicio (" + diferencia.ToString("P0") + ")");

        // ------------------ RENDER: HUECO VISIBLE ------------------
        BloqueEdificio objetivo = edificio.BloqueEn(0, 0) ?? edificio.BloqueEn(0, 1);
        if (objetivo != null)
        {
            edificio.Golpear(objetivo, 2);
            Texture2D roto = Renderizar(camara);
            float cambia = FraccionCambiada(pixelesActuales, roto.GetPixels(), camara,
                new Rect(objetivo.Area.xMin - 0.1f, objetivo.Area.yMin - 0.1f, objetivo.Area.width + 0.2f, objetivo.Area.height + 0.2f));
            Comprobar(cambia > 0.3f, "Al romper un bloque se ve el hueco (" + cambia.ToString("P0") + ")");
            Object.DestroyImmediate(roto);
            edificio.RepararMasCercano(objetivo.Area.center, 0.6f);
        }

        // ------------------ RENDER: QUIEN SE VE ------------------
        SpriteRenderer dibujoMuneco = muneco.GetComponentInChildren<SpriteRenderer>();

        Texture2D conTodo = Renderizar(camara);
        Color[] conTodoPixeles = conTodo.GetPixels();
        dibujoMuneco.enabled = false;
        Texture2D sinMuneco = Renderizar(camara);
        dibujoMuneco.enabled = true;

        float munecoVisible = FraccionCambiada(conTodoPixeles, sinMuneco.GetPixels(), camara,
            new Rect(muneco.transform.position.x - 0.45f, 0f, 0.9f, 0.9f));
        Object.DestroyImmediate(sinMuneco);
        Comprobar(munecoVisible > 0.10f, "El muñeco se dibuja (" + munecoVisible.ToString("P0") + " de su zona)");

        // El demoledor está arriba, sobre la fachada.
        SpriteRenderer dibujoDemoledor = demoledor.GetComponentInChildren<SpriteRenderer>();
        dibujoDemoledor.enabled = false;
        Texture2D sinDemoledor = Renderizar(camara);
        dibujoDemoledor.enabled = true;

        Vector3 posDemoledor = demoledor.transform.position;
        float demoledorVisible = FraccionCambiada(conTodoPixeles, sinDemoledor.GetPixels(), camara,
            new Rect(posDemoledor.x - 0.6f, posDemoledor.y, 1.2f, 1.2f));
        Object.DestroyImmediate(sinDemoledor);
        Comprobar(demoledorVisible > 0.10f, "El demoledor se dibuja (" + demoledorVisible.ToString("P0") + " de su zona)");

        // El edificio completo.
        BloqueEdificio[] bloques = edificio.GetComponentsInChildren<BloqueEdificio>(true);
        foreach (BloqueEdificio b in bloques)
        {
            SpriteRenderer sr = b.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }
        Texture2D sinEdificio = Renderizar(camara);
        float edificioVisible = FraccionCambiada(conTodoPixeles, sinEdificio.GetPixels(), camara, edificio.Limites);
        Object.DestroyImmediate(sinEdificio);
        foreach (BloqueEdificio b in bloques)
        {
            SpriteRenderer sr = b.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = b.Estado != EstadoBloque.Destruido;
        }
        Comprobar(edificioVisible > 0.6f, "El edificio se dibuja (" + edificioVisible.ToString("P0") + " de su zona)");

        Color cielo = Promedio(conTodoPixeles, camara, new Rect(0.2f, 4.4f, 1.5f, 0.8f));
        Color suelo = Promedio(conTodoPixeles, camara, new Rect(3.0f, -0.6f, 2.0f, 1.0f));
        Debug.Log(string.Format("MUESTRAS cielo={0} suelo={1}", cielo, suelo));
        Comprobar(cielo.b > cielo.r + 0.1f, "El cielo se ve azul");
        Comprobar(suelo.g > 0.25f && suelo.g < 0.55f, "El suelo se dibuja en la parte baja");

        File.WriteAllBytes(RutaCaptura, conTodo.EncodeToPNG());
        Debug.Log("CAPTURA guardada en " + RutaCaptura);
        Object.DestroyImmediate(conTodo);
        Object.DestroyImmediate(actual);

        // ------------------ FIN DE PARTIDA Y REINICIO ------------------
        int demolidosAntes = edificio.Demolidos;
        foreach (BloqueEdificio b in bloques)
        {
            if (edificio.PorcentajeDestruido >= 0.75f) break;
            if (b != null && b.Estado != EstadoBloque.Destruido) edificio.Golpear(b, 99);
        }
        Debug.Log("Demolicion forzada: " + edificio.Demolidos + "/" + edificio.Total + " (" + edificio.PorcentajeDestruido.ToString("P0") + ")");
        Comprobar(edificio.Demolidos > demolidosAntes, "Se pudieron demoler bloques para la prueba");
        Comprobar(gestor.PartidaTerminada, "Al pasar el limite la partida termina (derrumbe)");
        Comprobar(!muneco.enabled, "Al terminar la partida el muñeco deja de responder");

        gestor.ReiniciarPartida();
        Comprobar(!gestor.PartidaTerminada, "Con R la partida vuelve a empezar");
        Comprobar(edificio.Intactos == edificio.Total, "Con R el edificio queda entero");
        Comprobar(gestor.Puntos == 0, "Con R los puntos vuelven a cero");
        Comprobar(muneco.enabled, "Con R el muñeco vuelve a responder");
    }

    // ---------------------------------------------------------------- render
    private static Texture2D Renderizar(Camera camara)
    {
        RenderTexture rt = new RenderTexture(AnchoCaptura, AltoCaptura, 24, RenderTextureFormat.ARGB32);
        RenderTexture anterior = camara.targetTexture;

        camara.targetTexture = rt;
        camara.Render();

        RenderTexture.active = rt;
        Texture2D textura = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGBA32, false);
        textura.ReadPixels(new Rect(0, 0, AnchoCaptura, AltoCaptura), 0, 0);
        textura.Apply();

        RenderTexture.active = null;
        camara.targetTexture = anterior;
        rt.Release();
        Object.DestroyImmediate(rt);
        return textura;
    }

    /// <summary>Convierte un rectángulo de mundo a píxeles de la captura (cámara ortográfica sin rotar).</summary>
    private static void CalcularRango(Camera camara, Rect mundo, out int x0, out int y0, out int x1, out int y1)
    {
        float alto = Mathf.Max(0.0001f, camara.orthographicSize * 2f);
        float ancho = alto * AspectoCaptura;
        Vector3 p = camara.transform.position;

        x0 = Mathf.Clamp(Mathf.RoundToInt(((mundo.xMin - p.x) / ancho + 0.5f) * AnchoCaptura), 0, AnchoCaptura - 1);
        x1 = Mathf.Clamp(Mathf.RoundToInt(((mundo.xMax - p.x) / ancho + 0.5f) * AnchoCaptura), 0, AnchoCaptura - 1);
        y0 = Mathf.Clamp(Mathf.RoundToInt(((mundo.yMin - p.y) / alto + 0.5f) * AltoCaptura), 0, AltoCaptura - 1);
        y1 = Mathf.Clamp(Mathf.RoundToInt(((mundo.yMax - p.y) / alto + 0.5f) * AltoCaptura), 0, AltoCaptura - 1);
    }

    private static float FraccionCambiada(Color[] antes, Color[] despues, Camera camara, Rect mundo)
    {
        int x0, y0, x1, y1;
        CalcularRango(camara, mundo, out x0, out y0, out x1, out y1);

        int total = 0, distintos = 0;
        for (int y = y0; y <= y1; y += 2)
        {
            for (int x = x0; x <= x1; x += 2)
            {
                int i = y * AnchoCaptura + x;
                if (i < 0 || i >= antes.Length) continue;
                Color a = antes[i], b = despues[i];
                float d = Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Max(Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b)));
                if (d > 0.03f) distintos++;
                total++;
            }
        }
        return total == 0 ? 0f : (float)distintos / total;
    }

    private static Color Promedio(Color[] pixeles, Camera camara, Rect mundo)
    {
        int x0, y0, x1, y1;
        CalcularRango(camara, mundo, out x0, out y0, out x1, out y1);

        Color suma = Color.black;
        int cuenta = 0;
        for (int y = y0; y <= y1; y += 2)
        {
            for (int x = x0; x <= x1; x += 2)
            {
                int i = y * AnchoCaptura + x;
                if (i < 0 || i >= pixeles.Length) continue;
                suma += pixeles[i];
                cuenta++;
            }
        }
        return cuenta == 0 ? Color.black : suma / cuenta;
    }

    // ---------------------------------------------------------------- util
    private static void Comprobar(bool condicion, string descripcion)
    {
        if (condicion)
        {
            Debug.Log("OK      " + descripcion);
        }
        else
        {
            fallos++;
            Debug.LogError("FALLO   " + descripcion);
        }
    }
}
