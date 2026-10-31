using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DemoledorUnifranz.Game;
using DemoledorUnifranz.UI.Menus;
using System.Collections;

/// <summary>
/// Marcador del juego con el edificio alto: puntos, vidas, sección y destrozo.
/// Se pierde si el muñeco se queda sin vidas o si la sección actual pasa el límite
/// de destrozo (el edificio se cae). Se gana al completar las 3 secciones.
/// Incluye sistema de pausa con Escape y cambio a FinJuego al terminar.
/// </summary>
public class GestorJuego : MonoBehaviour
{
    [Header("Reglas")]
    [Tooltip("Si la sección actual pasa de este % de destrozo, el edificio se cae y termina la partida.")]
    [Range(0.5f, 1f)]
    public float limiteDerrumbe = 0.7f;
    [Tooltip("Puntos por cada martillazo que repara una ventana o repisa.")]
    public int puntosPorReparacion = 10;
    [Tooltip("Puntos extra al completar cada sección.")]
    public int bonoPorSeccion = 100;
    [Tooltip("Vidas del muñeco al empezar.")]
    public int vidasIniciales = 3;
    [Tooltip("Segundos de espera antes de ir a la escena FinJuego")]
    public float tiempoEsperaFin = 1.5f;
    [Tooltip("Segundos para ver al demoledor en la terraza antes de ir a FinJuego al ganar.")]
    public float tiempoEsperaVictoria = 3f;

    private EdificioAlto edificio;
    private DemoledorAutomatico demoledor;
    private Muneco muneco;

    private Text textoEstado;
    private Text textoDestrozo;
    private Text textoMensaje;
    private RectTransform rellenoBarra;
    private Image[] corazones;
    private static Sprite spriteCorazon;
    private bool partidaTerminada;
    private bool estaEnPausa;
    private int puntos;
    private int reparaciones;
    private int vidas;
    private Vector3 inicioMuneco;
    private string mensajeFinal = "";
    private string mensajeTemporal = "";
    private float finMensajeTemporal;

    // Interfaz de la pausa (Assets/Scripts/Menus/MenuPausaArcade.cs).
    private MenuPausaArcade menuPausa;
    private bool munecoActivoAntesDePausa;
    private bool reactivarMuneco;

    // Espera antes de cargar FinJuego (se cancela si se reinicia la partida).
    private Coroutine rutinaFin;

    private const float AnchoBarra = 300f;

    private void Awake()
    {
        Physics2D.queriesStartInColliders = false;

        demoledor = Object.FindFirstObjectByType<DemoledorAutomatico>();
        muneco = Object.FindFirstObjectByType<Muneco>();
        if (muneco != null) inicioMuneco = muneco.transform.position;
        vidas = vidasIniciales;
    }

    private void Start()
    {
        // Aseguramos que el tiempo corra con normalidad al iniciar
        Time.timeScale = 1f;

        edificio = EdificioAlto.Instancia != null ? EdificioAlto.Instancia : Object.FindFirstObjectByType<EdificioAlto>();

        if (edificio != null)
        {
            edificio.Cambio += Refrescar;
            edificio.Reparacion += AlReparar;
            edificio.SeccionCompletada += AlCompletarSeccion;
            edificio.Salvado += AlSalvarEdificio;
        }
        if (muneco != null) muneco.Golpeado += AlGolpearMuneco;

        CrearHud();
        Refrescar();

        // ControlesTactiles se crea automáticamente solo en la primera escena que abre el juego.
        // Ahora el juego arranca en MenuPrincipal, así que aquí se asegura que existan.
        if (Object.FindAnyObjectByType<ControlesTactiles>() == null)
        {
            new GameObject("ControlesTactiles").AddComponent<ControlesTactiles>();
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f; // Restaurar el tiempo por si se destruye el objeto

        if (edificio != null)
        {
            edificio.Cambio -= Refrescar;
            edificio.Reparacion -= AlReparar;
            edificio.SeccionCompletada -= AlCompletarSeccion;
            edificio.Salvado -= AlSalvarEdificio;
        }
        if (muneco != null) muneco.Golpeado -= AlGolpearMuneco;
    }

    private void Update()
    {
        Keyboard teclado = Keyboard.current;
        bool reiniciarTactil = ControlesTactiles.ReiniciarPulsado();
        bool reiniciarTecla = teclado != null && teclado[Key.R].wasPressedThisFrame;

        // Pausa con Escape (solo si la partida no ha terminado).
        // En Android sin teclado Keyboard.current es null: se revisa antes de usarlo.
        if (teclado != null && teclado[Key.Escape].wasPressedThisFrame && !partidaTerminada)
        {
            AlternarPausa();
        }

        // Reinicio con R o con el botón táctil (no mientras el juego está en pausa).
        if ((reiniciarTecla || reiniciarTactil) && !estaEnPausa)
        {
            ReiniciarPartida();
        }

        // Borra el aviso temporal ("¡SECCIÓN COMPLETA!", "-1 VIDA") cuando se cumple su tiempo.
        if (mensajeTemporal.Length > 0 && Time.time >= finMensajeTemporal)
        {
            mensajeTemporal = "";
            Refrescar();
        }
    }

    private void LateUpdate()
    {
        // El muñeco se reactiva un cuadro después de cerrar la pausa: así el ENTER
        // que pulsó CONTINUAR no se toma también como un martillazo.
        if (!reactivarMuneco) return;
        reactivarMuneco = false;
        if (muneco != null && !partidaTerminada && !estaEnPausa) muneco.enabled = munecoActivoAntesDePausa;
    }

    private void AlternarPausa()
    {
        if (estaEnPausa) Reanudar();
        else Pausar();
    }

    /// <summary>Congela el juego y muestra el menú de pausa.</summary>
    public void Pausar()
    {
        if (estaEnPausa || partidaTerminada) return;
        estaEnPausa = true;

        // Congela la física y el movimiento del juego.
        Time.timeScale = 0f;

        // El muñeco lee el teclado en Update: se desactiva para que no se mueva ni repare.
        if (muneco != null)
        {
            munecoActivoAntesDePausa = muneco.enabled;
            muneco.enabled = false;
        }

        if (menuPausa != null && edificio != null) menuPausa.Mostrar(puntos, reparaciones, edificio.DestrozoSeccionActual);
        else if (menuPausa != null) menuPausa.Mostrar(puntos, reparaciones, 0f);
    }

    /// <summary>Cierra el menú de pausa y continúa la partida.</summary>
    public void Reanudar()
    {
        if (!estaEnPausa) return;
        estaEnPausa = false;
        Time.timeScale = 1f;
        reactivarMuneco = true;

        // Descarta los toques hechos sobre los controles táctiles mientras estaba en pausa.
        ControlesTactiles.ArribaPulsado();
        ControlesTactiles.SaltarPulsado();
        ControlesTactiles.RepararPulsado();
        ControlesTactiles.ReiniciarPulsado();

        if (menuPausa != null) menuPausa.Ocultar();
    }

    private void AlReparar()
    {
        if (partidaTerminada) return;

        reparaciones++;
        puntos += puntosPorReparacion;
        Refrescar();
    }

    private void AlGolpearMuneco()
    {
        if (partidaTerminada) return;

        vidas = Mathf.Max(0, vidas - 1);
        if (vidas <= 0)
        {
            TerminarPartida(false);
            return;
        }

        MostrarAviso("¡AY!  -1 VIDA", 1.5f);
    }

    private void AlCompletarSeccion(int nuevaSeccion)
    {
        if (partidaTerminada || edificio == null) return;

        puntos += bonoPorSeccion;
        if (nuevaSeccion < edificio.secciones)
        {
            MostrarAviso(string.Format("¡SECCIÓN {0} REPARADA! +{1}   ¡EL DEMOLEDOR SUBE!", nuevaSeccion, bonoPorSeccion), 2.5f);
        }
        else
        {
            Refrescar();
        }
    }

    private void AlSalvarEdificio()
    {
        if (partidaTerminada) return;
        partidaTerminada = true;
        mensajeFinal = "¡EDIFICIO SALVADO!";

        // El demoledor sigue en la terraza (no se detiene); el muñeco ya no se controla.
        if (muneco != null) muneco.enabled = false;

        Time.timeScale = 1f;
        GameSession.RegistrarVictoria(puntos, reparaciones, SceneManager.GetActiveScene().name);
        Refrescar();
        rutinaFin = StartCoroutine(CargarFinJuego(tiempoEsperaVictoria));
    }

    private void MostrarAviso(string texto, float segundos)
    {
        mensajeTemporal = texto;
        finMensajeTemporal = Time.time + segundos;
        Refrescar();
    }

    private void Refrescar()
    {
        if (edificio == null) return;

        float destrozo = edificio.DestrozoSeccionActual;
        int seccionVisible = Mathf.Min(edificio.SeccionActual + 1, edificio.secciones);

        if (textoEstado != null)
        {
            textoEstado.text = string.Format(
                "PUNTOS  {0}\nSECCIÓN  {1}/{2}\nVENTANAS ROTAS  {3}/{4}",
                puntos, seccionVisible, edificio.secciones,
                edificio.Terminado ? 0 : edificio.DanadasEnSeccion(edificio.SeccionActual), edificio.VentanasPorSeccion);
        }
        if (textoDestrozo != null) textoDestrozo.text = "DESTROZO  " + Mathf.RoundToInt(destrozo * 100f) + "%";

        if (corazones != null)
        {
            for (int i = 0; i < corazones.Length; i++)
            {
                if (corazones[i] == null) continue;
                corazones[i].color = i < vidas ? new Color(0.92f, 0.2f, 0.24f, 1f) : new Color(0.25f, 0.25f, 0.3f, 0.6f);
            }
        }

        if (rellenoBarra != null)
        {
            // La barra se llena hasta el límite de derrumbe.
            float lleno = Mathf.Clamp01(destrozo / Mathf.Max(0.01f, limiteDerrumbe));
            rellenoBarra.sizeDelta = new Vector2(lleno * AnchoBarra, 16f);
            Image imagen = rellenoBarra.GetComponent<Image>();
            if (imagen != null) imagen.color = Color.Lerp(new Color(0.95f, 0.75f, 0.2f), new Color(0.85f, 0.15f, 0.12f), lleno);
        }

        if (!partidaTerminada && destrozo >= limiteDerrumbe)
        {
            TerminarPartida(true);
        }

        if (textoMensaje == null) return;

        if (partidaTerminada) textoMensaje.text = mensajeFinal;
        else textoMensaje.text = mensajeTemporal;
    }

    public bool PartidaTerminada { get { return partidaTerminada; } }
    public int Puntos { get { return puntos; } }
    public int Reparaciones { get { return reparaciones; } }
    public bool EstaEnPausa { get { return estaEnPausa; } }

    /// <summary>Pierde la partida: por derrumbe (true) o porque el muñeco se quedó sin vidas (false).</summary>
    private void TerminarPartida(bool porDerrumbe)
    {
        if (partidaTerminada) return;
        partidaTerminada = true;
        mensajeFinal = porDerrumbe ? "¡EL EDIFICIO SE CAYÓ!" : "¡TE QUEDASTE SIN VIDAS!";

        if (demoledor != null) demoledor.Detener();
        if (muneco != null) muneco.enabled = false;

        // Si estaba en pausa, restaurar escala de tiempo antes del cambio
        Time.timeScale = 1f;

        // Datos reales de la partida para la pantalla FinJuego.
        string escena = SceneManager.GetActiveScene().name;
        if (porDerrumbe)
            GameSession.RegistrarDerrumbe(puntos, reparaciones, edificio != null ? edificio.DestrozoSeccionActual : 1f, escena);
        else
            GameSession.RegistrarSinVidas(puntos, reparaciones, escena);

        Refrescar();
        rutinaFin = StartCoroutine(CargarFinJuego(tiempoEsperaFin));
    }

    private IEnumerator CargarFinJuego(float espera)
    {
        yield return new WaitForSeconds(espera);
        // Con fundido; si FinJuego no estuviera en Build Settings se carga como antes.
        if (!NavegadorEscenas.Cargar(Escenas.FinJuego)) SceneManager.LoadScene(Escenas.FinJuego);
    }

    /// <summary>
    /// Vuelve a empezar: ventanas intactas, sección 1, vidas llenas y puntos a cero.
    /// Mantiene compatibilidad con scripts de verificación y tests.
    /// </summary>
    public void ReiniciarPartida()
    {
        if (rutinaFin != null)
        {
            StopCoroutine(rutinaFin);
            rutinaFin = null;
        }

        Time.timeScale = 1f;
        partidaTerminada = false;
        bool estabaEnPausa = estaEnPausa;
        estaEnPausa = false;
        reactivarMuneco = false;
        if (menuPausa != null) menuPausa.OcultarInmediato();
        if (estabaEnPausa) ControlesTactiles.ReiniciarPulsado();

        puntos = 0;
        reparaciones = 0;
        vidas = vidasIniciales;
        mensajeFinal = "";
        mensajeTemporal = "";

        EscombroCayendo.EliminarTodos();
        if (edificio != null) edificio.Reiniciar();

        if (muneco != null)
        {
            muneco.enabled = true;
            muneco.Reaparecer(inicioMuneco);
        }
        if (demoledor != null) demoledor.Reiniciar();

        Refrescar();
    }

    // ------------------------------------------------------------ HUD
    private void CrearHud()
    {
        GameObject lienzo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        lienzo.transform.SetParent(transform, false);

        Canvas canvas = lienzo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler escalador = lienzo.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        Transform raiz = lienzo.transform;

        // Panel de estado a la izquierda, bajo el botón de pausa (que está en 24, -20 y mide 112 x 96).
        Image panel = CrearImagen(raiz, "PanelEstado", new Color(0.05f, 0.06f, 0.08f, 0.6f),
            new Vector2(0f, 1f), new Vector2(24f, -128f), new Vector2(380f, 196f));
        panel.rectTransform.pivot = new Vector2(0f, 1f);

        textoEstado = CrearTexto(raiz, "", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -140f),
            new Vector2(350f, 110f), TextAnchor.UpperLeft, 28, Color.white, FontStyle.Bold);

        textoDestrozo = CrearTexto(raiz, "", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -252f),
            new Vector2(350f, 34f), TextAnchor.UpperLeft, 24, new Color(1f, 0.85f, 0.3f, 1f), FontStyle.Bold);

        // Barra de destrozo (se llena hasta el límite de derrumbe).
        Image fondoBarra = CrearImagen(raiz, "BarraFondo", new Color(0.02f, 0.02f, 0.03f, 0.8f),
            new Vector2(0f, 1f), new Vector2(42f, -290f), new Vector2(AnchoBarra + 8f, 24f));
        fondoBarra.rectTransform.pivot = new Vector2(0f, 1f);

        Image relleno = CrearImagen(raiz, "BarraRelleno", new Color(0.95f, 0.75f, 0.2f, 1f),
            new Vector2(0f, 1f), new Vector2(46f, -302f), new Vector2(0f, 16f));
        rellenoBarra = relleno.rectTransform;
        rellenoBarra.pivot = new Vector2(0f, 0.5f);

        // Vidas: un corazón por vida, arriba a la derecha (se apagan al perderlas).
        // En el celular el botón REINICIAR ocupa esa esquina, así que bajan un poco.
        int totalCorazones = Mathf.Max(1, vidasIniciales);
        corazones = new Image[totalCorazones];
        const float AnchoCorazon = 60f;
        const float SeparacionCorazon = 72f;
        float alturaCorazones = ControlesTactiles.DebeMostrarse ? -140f : -56f;
        for (int i = 0; i < totalCorazones; i++)
        {
            float x = -(40f + AnchoCorazon * 0.5f + (totalCorazones - 1 - i) * SeparacionCorazon);
            Image corazon = CrearImagen(raiz, "Vida" + (i + 1), Color.white, new Vector2(1f, 1f),
                new Vector2(x, alturaCorazones), new Vector2(AnchoCorazon, AnchoCorazon * 8f / 9f));
            corazon.sprite = SpriteCorazon();
            corazon.preserveAspect = true;
            corazones[i] = corazon;
        }

        textoMensaje = CrearTexto(raiz, "", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 260f),
            new Vector2(1500f, 100f), TextAnchor.MiddleCenter, 58, new Color(1f, 0.85f, 0.3f, 1f), FontStyle.Bold);

        // --- Menú de pausa (ventana con CONTINUAR / REINICIAR / MENÚ PRINCIPAL) ---
        menuPausa = MenuPausaArcade.Crear(this);
    }

    private static Text CrearTexto(Transform padre, string contenido, Vector2 ancla, Vector2 pivote,
        Vector2 posicion, Vector2 tamano, TextAnchor alineacion, int tamanioFuente, Color color, FontStyle estilo)
    {
        GameObject objeto = new GameObject("Texto", typeof(RectTransform));
        objeto.transform.SetParent(padre, false);

        RectTransform rt = (RectTransform)objeto.transform;
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        Text texto = objeto.AddComponent<Text>();
        texto.font = FuentePredeterminada();
        texto.text = contenido;
        texto.alignment = alineacion;
        texto.fontSize = tamanioFuente;
        texto.fontStyle = estilo;
        texto.color = color;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        texto.raycastTarget = false;

        Outline contorno = objeto.AddComponent<Outline>();
        contorno.effectColor = new Color(0f, 0f, 0f, 0.85f);
        contorno.effectDistance = new Vector2(2f, -2f);

        return texto;
    }

    private static Image CrearImagen(Transform padre, string nombre, Color color, Vector2 ancla,
        Vector2 posicion, Vector2 tamano)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.transform.SetParent(padre, false);

        RectTransform rt = (RectTransform)objeto.transform;
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        Image imagen = objeto.AddComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;
        return imagen;
    }

    /// <summary>Corazón en pixel art (9x8) creado por código: no necesita ningún archivo.</summary>
    private static Sprite SpriteCorazon()
    {
        if (spriteCorazon != null) return spriteCorazon;

        string[] dibujo =
        {
            ".XX...XX.",
            "XwwX.XooX",
            "XwooXoooX",
            "XoooooooX",
            ".XoooooX.",
            "..XoooX..",
            "...XoX...",
            "....X....",
        };

        int ancho = dibujo[0].Length;
        int alto = dibujo.Length;
        Texture2D textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < alto; y++)
        {
            string fila = dibujo[alto - 1 - y];   // la textura empieza abajo
            for (int x = 0; x < ancho; x++)
            {
                Color c;
                switch (fila[x])
                {
                    case 'X': c = new Color(0.12f, 0.08f, 0.1f, 1f); break;
                    case 'w': c = new Color(1f, 1f, 1f, 1f); break;
                    case 'o': c = new Color(0.86f, 0.86f, 0.86f, 1f); break;
                    default: c = new Color(0f, 0f, 0f, 0f); break;
                }
                textura.SetPixel(x, y, c);
            }
        }
        textura.Apply();

        spriteCorazon = Sprite.Create(textura, new Rect(0f, 0f, ancho, alto), new Vector2(0.5f, 0.5f), 100f);
        spriteCorazon.name = "Corazon";
        return spriteCorazon;
    }

    private static Font FuentePredeterminada()
    {
        Font fuente = null;
        try { fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        catch { fuente = null; }

        if (fuente == null)
        {
            try { fuente = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            catch { fuente = null; }
        }
        return fuente;
    }
}