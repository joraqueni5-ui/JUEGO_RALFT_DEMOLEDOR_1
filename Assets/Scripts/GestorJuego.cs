using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DemoledorUnifranz.Game;
using System.Collections;

/// <summary>
/// Marcador del juego: puntos por reparar, porcentaje destruido y aviso de derrumbe.
/// Incluye sistema de pausa con Escape y cambio a FinJuego al perder.
/// </summary>
public class GestorJuego : MonoBehaviour
{
    [Header("Reglas")]
    [Tooltip("Si el edificio pasa de este % destruido, se cae y termina la partida.")]
    [Range(0.5f, 1f)]
    public float limiteDerrumbe = 0.7f;
    [Tooltip("Puntos que da reparar un bloque.")]
    public int puntosPorReparacion = 10;
    [Tooltip("Segundos de espera antes de ir a la escena FinJuego")]
    public float tiempoEsperaFin = 1.5f;

    [Header("Textos")]
    public string titulo = "¡AGÁRRALO! EL DEMOLEDOR NO SE DETIENE";
    public string ayudaControles =
        "A / D  caminar    ·    ESPACIO  saltar    ·    ↑  trepar    ·    ↓  bajar    ·    ENTER  reparar    ·    ESC  pausa";

    private Edificio edificio;
    private DemoledorAutomatico demoledor;
    private Muneco muneco;

    private Text textoEstado;
    private Text textoMensaje;
    private RectTransform rellenoBarra;
    private bool partidaTerminada;
    private bool estaEnPausa;
    private int puntos;
    private int reparaciones;

    // Elementos de la interfaz de Pausa
    private GameObject panelPausa;

    private const float AnchoBarra = 640f;

    private void Awake()
    {
        Physics2D.queriesStartInColliders = false;

        demoledor = Object.FindFirstObjectByType<DemoledorAutomatico>();
        muneco = Object.FindFirstObjectByType<Muneco>();
    }

    private void Start()
    {
        // Aseguramos que el tiempo corra con normalidad al iniciar
        Time.timeScale = 1f;

        edificio = Edificio.Instancia != null ? Edificio.Instancia : Object.FindFirstObjectByType<Edificio>();

        if (edificio != null)
        {
            edificio.Cambio += Refrescar;
            edificio.Reparacion += AlReparar;
        }

        CrearHud();
        Refrescar();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f; // Restaurar el tiempo por si se destruye el objeto

        if (edificio != null)
        {
            edificio.Cambio -= Refrescar;
            edificio.Reparacion -= AlReparar;
        }
    }

    private void Update()
    {
        bool reiniciarTactil = ControlesTactiles.ReiniciarPulsado();
        bool reiniciarTecla = Keyboard.current != null && Keyboard.current[Key.R].wasPressedThisFrame;

<<<<<<< HEAD
        // Detectar tecla Escape para alternar pausa (solo si la partida no ha terminado)
        if (Keyboard.current[Key.Escape].wasPressedThisFrame && !partidaTerminada)
        {
            AlternarPausa();
        }
    }

    private void AlternarPausa()
    {
        estaEnPausa = !estaEnPausa;

        if (panelPausa != null)
        {
            panelPausa.SetActive(estaEnPausa);
        }

        // Congela o reactiva la física y movimiento del juego
        Time.timeScale = estaEnPausa ? 0f : 1f;
=======
        if (reiniciarTecla || reiniciarTactil) ReiniciarPartida();
>>>>>>> 3197ae9465970105061a1230aec5c1f35a894f5f
    }

    private void AlReparar()
    {
        if (partidaTerminada) return;

        reparaciones++;
        puntos += puntosPorReparacion;
        Refrescar();
    }

    private void Refrescar()
    {
        if (edificio == null) return;

        float destruido = edificio.PorcentajeDestruido;

        if (textoEstado != null)
        {
            textoEstado.text = string.Format(
                "PUNTOS: {0}      REPARACIONES: {1}      ESTRUCTURA: {2}% destruida      BLOQUES: {3}/{4}",
                puntos, reparaciones, Mathf.RoundToInt(destruido * 100f), edificio.Demolidos, edificio.Total);
        }

        if (rellenoBarra != null)
        {
            float ancho = Mathf.Clamp01(destruido) * AnchoBarra;
            rellenoBarra.sizeDelta = new Vector2(ancho, 20f);
            Image imagen = rellenoBarra.GetComponent<Image>();
            if (imagen != null) imagen.color = Color.Lerp(new Color(0.95f, 0.75f, 0.2f), new Color(0.85f, 0.15f, 0.12f), destruido);
        }

        if (textoMensaje == null) return;

        if (!partidaTerminada && destruido >= limiteDerrumbe)
        {
            TerminarPartida();
        }

        if (partidaTerminada)
        {
            textoMensaje.text = "¡EL EDIFICIO SE CAYÓ!";
        }
        else if (destruido > 0.0001f && edificio.Demolidos == 0 && edificio.Agrietados == 0)
        {
            textoMensaje.text = "¡TODO REPARADO!";
        }
        else
        {
            textoMensaje.text = "";
        }
    }

    public bool PartidaTerminada { get { return partidaTerminada; } }
    public int Puntos { get { return puntos; } }
    public int Reparaciones { get { return reparaciones; } }
    public bool EstaEnPausa { get { return estaEnPausa; } }

    private void TerminarPartida()
    {
        if (partidaTerminada) return;
        partidaTerminada = true;

        if (demoledor != null) demoledor.Detener();
        if (muneco != null)
        {
            muneco.Soltar();
            muneco.enabled = false;
        }

        // Si estaba en pausa, restaurar escala de tiempo antes del cambio
        Time.timeScale = 1f;

        StartCoroutine(CargarFinJuego());
    }

    private IEnumerator CargarFinJuego()
    {
        yield return new WaitForSeconds(tiempoEsperaFin);
        SceneManager.LoadScene(Escenas.FinJuego);
    }

    /// <summary>
    /// Vuelve a empezar: edificio entero, demoledor desde arriba y puntos a cero.
    /// Mantiene compatibilidad con scripts de verificación y tests.
    /// </summary>
    public void ReiniciarPartida()
    {
        Time.timeScale = 1f;
        partidaTerminada = false;
        estaEnPausa = false;
        if (panelPausa != null) panelPausa.SetActive(false);

        puntos = 0;
        reparaciones = 0;

        if (edificio != null) edificio.Reiniciar();

        if (muneco != null)
        {
            muneco.enabled = true;
            muneco.Soltar();
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

        CrearTexto(raiz, titulo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f),
            new Vector2(1600f, 50f), TextAnchor.UpperCenter, 38, new Color(1f, 0.96f, 0.85f, 1f), FontStyle.Bold);

        textoEstado = CrearTexto(raiz, "", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f),
            new Vector2(1700f, 40f), TextAnchor.UpperCenter, 26, Color.white, FontStyle.Normal);

        // Barra de estructura (fondo + relleno rojo que crece con la destrucción).
        Image fondoBarra = CrearImagen(raiz, "BarraFondo", new Color(0.05f, 0.06f, 0.08f, 0.75f),
            new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(AnchoBarra + 8f, 28f));

        Image relleno = CrearImagen(raiz, "BarraRelleno", new Color(0.95f, 0.75f, 0.2f, 1f),
            new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 20f));
        rellenoBarra = relleno.rectTransform;
        rellenoBarra.anchorMin = new Vector2(0.5f, 1f);
        rellenoBarra.anchorMax = new Vector2(0.5f, 1f);
        rellenoBarra.pivot = new Vector2(0f, 0.5f);
        rellenoBarra.anchoredPosition = new Vector2(-AnchoBarra * 0.5f, -128f);

        textoMensaje = CrearTexto(raiz, "", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 260f),
            new Vector2(1500f, 100f), TextAnchor.MiddleCenter, 58, new Color(1f, 0.85f, 0.3f, 1f), FontStyle.Bold);

        CrearTexto(raiz, ayudaControles, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f),
            new Vector2(1700f, 90f), TextAnchor.LowerCenter, 24, new Color(1f, 1f, 1f, 0.88f), FontStyle.Normal);

        // --- Crear Ventana/Capa de Pausa ---
        CrearMenuPausa(raiz);
    }

    private void CrearMenuPausa(Transform raiz)
    {
        panelPausa = new GameObject("PanelPausa", typeof(RectTransform));
        panelPausa.transform.SetParent(raiz, false);

        RectTransform rtPanel = (RectTransform)panelPausa.transform;
        rtPanel.anchorMin = Vector2.zero;
        rtPanel.anchorMax = Vector2.one;
        rtPanel.sizeDelta = Vector2.zero;

        // Fondo oscuro semitransparente que cubre toda la pantalla
        Image fondoOscuro = panelPausa.AddComponent<Image>();
        fondoOscuro.color = new Color(0f, 0f, 0f, 0.7f);

        // Texto principal de pausa
        CrearTexto(panelPausa.transform, "JUEGO EN PAUSA", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 40f), new Vector2(1200f, 100f), TextAnchor.MiddleCenter, 64,
            new Color(1f, 0.9f, 0.3f, 1f), FontStyle.Bold);

        // Texto explicativo para reanudar
        CrearTexto(panelPausa.transform, "Presiona  [ ESC ]  para continuar jugando", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -40f), new Vector2(1000f, 60f), TextAnchor.MiddleCenter, 30,
            Color.white, FontStyle.Normal);

        // Inicia desactivado
        panelPausa.SetActive(false);
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