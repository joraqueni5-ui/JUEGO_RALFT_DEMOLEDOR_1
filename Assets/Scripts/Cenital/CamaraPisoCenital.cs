using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cámara del modo cenital. Muestra un alto fijo de baldosas (alturaVisible) y sigue al jugador con suavidad, sin salirse nunca del
/// mapa (si el mapa cabe entero en un eje, ese eje queda centrado). Mientras se mantiene Tab (o el botón MAPA en el celular) se aleja
/// con suavidad para mostrar el piso completo y vuelve al soltar. El zoom normal usa escalas enteras (cada píxel del arte ocupa un
/// número entero de píxeles de pantalla) y la posición se ajusta a la rejilla de píxeles; durante el zoom suave se interpola sin ajustar.
/// Deja arriba la franja del HUD y, en el celular con pantalla muy ancha, bandas laterales libres para los controles.
/// Incluye el temblor que usa el ventanal al romperse.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CamaraPisoCenital : MonoBehaviour
{
    [Header("Seguimiento")]
    [Tooltip("Baldosas que se ven de alto (de la zona de juego, sin la franja del HUD). Con escala entera se redondea al zoom entero más cercano.")]
    [Min(4f)] public float alturaVisible = 11f;
    [Tooltip("Segundos que tarda la cámara en alcanzar al jugador (más alto = más suave).")]
    [Min(0f)] public float suavizadoSeguimiento = 0.15f;
    [Tooltip("Baldosas extra que se permite ver fuera del mapa (0 = nunca se ve fuera de sus bordes).")]
    public float margen = 0f;
    public Color colorFondo = new Color(0.07f, 0.07f, 0.1f, 1f);

    [Header("Ver el mapa completo")]
    public Key teclaVerMapa = Key.Tab;
    [Tooltip("Segundos que dura el alejamiento y el regreso.")]
    [Min(0.01f)] public float suavizadoZoom = 0.25f;

    [Header("Pixel perfect")]
    [Tooltip("El zoom normal usa escalas enteras y la cámara se ajusta a la rejilla de píxeles (no se aplica durante el zoom suave).")]
    public bool escalaEntera = true;
    [Tooltip("Píxeles de arte por baldosa.")]
    [Min(1)] public int pixelesPorBaldosa = 32;

    [Header("Pantallas anchas en celular")]
    [Tooltip("En celulares más anchos que 'aspectoMaximoJuego', la zona de juego se limita a ese ancho y quedan bandas laterales para los controles.")]
    public bool bandasLateralesEnCelular = true;
    [Tooltip("Proporción ancho / alto máxima de la zona de juego (16 / 9 = 1,78).")]
    public float aspectoMaximoJuego = 16f / 9f;

    private Camera camara;
    private Rect zona = new Rect(0f, 0f, 22f, 12f);
    private bool tieneZona;
    private JugadorCenital jugador;
    private float tamano, velTamano;
    private Vector2 centro, velCentro;
    private bool inicializado;
    private static float fuerzaTemblor, finTemblor, duracionTemblor;
    private Canvas canvasFranja;
    private float alturaFranja;
    private static float anchoBandaActual;
    private static float desplazamientoObjetivo, unidadesPorPixel;
    private float desplazamiento, velDesplazamiento;

    /// <summary>
    /// Baja el punto de seguimiento de la cámara 'unidades' (mundo) para que el jugador quede más arriba en pantalla, por encima de un panel
    /// que ocupa la parte baja. 0 = vuelve a su lugar. La transición dura unos 0,3 s y respeta los límites del mapa.
    /// </summary>
    public static void DesplazarSeguimiento(float unidades) { desplazamientoObjetivo = Mathf.Max(0f, unidades); }

    /// <summary>Unidades de mundo que mide un píxel de pantalla en la vista actual.</summary>
    public static float UnidadesPorPixel { get { return unidadesPorPixel; } }

    /// <summary>Ancho (px) de cada banda lateral libre a los lados de la zona de juego (0 si no hay).</summary>
    public static float AnchoBandaPx() { return anchoBandaActual; }

    private void Awake()
    {
        camara = GetComponent<Camera>();
        camara.orthographic = true;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = colorFondo;
        fuerzaTemblor = 0f;
        anchoBandaActual = 0f;
        desplazamientoObjetivo = 0f;
    }

    /// <summary>Define la zona del mapa por la que se mueve la cámara.</summary>
    public void Encuadrar(Rect mapa)
    {
        zona = mapa;
        tieneZona = true;
        inicializado = false;
    }

    /// <summary>
    /// Reserva arriba una franja para el HUD: el mapa se dibuja solo debajo. La altura está en
    /// píxeles del lienzo de referencia y se convierte a fracción de pantalla en cada cambio de tamaño.
    /// </summary>
    public void ReservarFranjaSuperior(Canvas canvas, float alturaEnLienzo)
    {
        canvasFranja = canvas;
        alturaFranja = alturaEnLienzo;
    }

    public static void Temblar(float fuerza, float duracion)
    {
        fuerzaTemblor = fuerza;
        duracionTemblor = Mathf.Max(0.05f, duracion);
        finTemblor = Time.unscaledTime + duracionTemblor;
    }

    // ------------------------------------------------------------ zona de pantalla
    private void ActualizarZonaDePantalla()
    {
        if (Screen.height <= 0 || Screen.width <= 0) return;

        float franja = canvasFranja != null ? Mathf.Clamp(alturaFranja * canvasFranja.scaleFactor / Screen.height, 0f, 0.4f) : 0f;
        float ancho = 1f, x0 = 0f;
        if (bandasLateralesEnCelular && ControlesTactiles.DebeMostrarse)
        {
            float aspectoPantalla = (float)Screen.width / Screen.height;
            if (aspectoPantalla > aspectoMaximoJuego) { ancho = aspectoMaximoJuego / aspectoPantalla; x0 = (1f - ancho) * 0.5f; }
        }
        anchoBandaActual = x0 * Screen.width;

        Rect r = new Rect(x0, 0f, ancho, 1f - franja);
        if (camara.rect != r) camara.rect = r;
    }

    // ------------------------------------------------------------ tamaño y centro
    private float AspectoZona { get { return Mathf.Max(0.5f, camara.aspect); } }

    /// <summary>Medio alto del zoom normal: 'alturaVisible' baldosas, o el zoom entero más cercano.</summary>
    private float TamanoNormal()
    {
        float alto = camara.pixelHeight;
        if (!escalaEntera || alto < 1f) return alturaVisible * 0.5f;

        float pxPorBaldosa = alto / alturaVisible;
        int zoom = Mathf.Max(1, Mathf.RoundToInt(pxPorBaldosa / pixelesPorBaldosa));
        return alto / (2f * zoom * pixelesPorBaldosa);
    }

    /// <summary>Medio alto con el que el mapa entra completo.</summary>
    private float TamanoCompleto()
    {
        float mitadAlto = zona.height * 0.5f + margen;
        float mitadAncho = zona.width * 0.5f + margen;
        return Mathf.Max(mitadAlto, mitadAncho / AspectoZona);
    }

    private Vector2 CentroDeseado(float medioAlto)
    {
        Vector2 objetivo = zona.center;
        if (jugador == null) jugador = FindFirstObjectByType<JugadorCenital>();
        if (jugador != null) objetivo = jugador.Centro;
        objetivo.y -= desplazamiento;

        float medioAncho = medioAlto * AspectoZona;
        return new Vector2(Acotar(objetivo.x, zona.xMin - margen, zona.xMax + margen, medioAncho, zona.center.x),
                           Acotar(objetivo.y, zona.yMin - margen, zona.yMax + margen, medioAlto, zona.center.y));
    }

    /// <summary>Mantiene la vista dentro de [min, max]; si el rango entero cabe en la vista, lo centra.</summary>
    private static float Acotar(float valor, float min, float max, float medio, float centroDelRango)
    {
        if (max - min <= medio * 2f) return centroDelRango;
        return Mathf.Clamp(valor, min + medio, max - medio);
    }

    private bool VerMapaPulsado()
    {
        Keyboard teclado = Keyboard.current;
        bool tecla = teclado != null && teclado[teclaVerMapa].isPressed;
        return tecla || ControlesTactiles.VerMapa;
    }

    // ------------------------------------------------------------ ciclo
    private void LateUpdate()
    {
        ActualizarZonaDePantalla();
        if (!tieneZona) return;

        float normal = TamanoNormal();
        float objetivo = VerMapaPulsado() ? Mathf.Max(TamanoCompleto(), normal) : normal;
        float dt = Time.unscaledDeltaTime;

        if (!inicializado)
        {
            tamano = normal;
            centro = CentroDeseado(tamano);
            velTamano = 0f;
            velCentro = Vector2.zero;
            inicializado = true;
        }

        tamano = Mathf.SmoothDamp(tamano, objetivo, ref velTamano, suavizadoZoom, Mathf.Infinity, dt);
        bool haciendoZoom = Mathf.Abs(tamano - objetivo) > 0.004f;
        if (!haciendoZoom) { tamano = objetivo; velTamano = 0f; }
        camara.orthographicSize = tamano;
        if (camara.pixelHeight > 0) unidadesPorPixel = 2f * tamano / camara.pixelHeight;
        desplazamiento = Mathf.SmoothDamp(desplazamiento, desplazamientoObjetivo, ref velDesplazamiento, 0.1f, Mathf.Infinity, dt);

        centro = Vector2.SmoothDamp(centro, CentroDeseado(tamano), ref velCentro, suavizadoSeguimiento, Mathf.Infinity, dt);
        Vector2 pos = centro;

        // Pixel perfect: sin zoom en curso, la cámara se queda en la rejilla de píxeles de pantalla.
        if (escalaEntera && !haciendoZoom && camara.pixelHeight > 0)
        {
            float pxPorUnidad = camara.pixelHeight / (2f * tamano);
            pos = new Vector2(Mathf.Round(pos.x * pxPorUnidad) / pxPorUnidad, Mathf.Round(pos.y * pxPorUnidad) / pxPorUnidad);
        }

        if (Time.unscaledTime < finTemblor)
        {
            float k = (finTemblor - Time.unscaledTime) / duracionTemblor;
            pos += Random.insideUnitCircle * fuerzaTemblor * k;
        }
        transform.position = new Vector3(pos.x, pos.y, -10f);
    }
}
