using UnityEngine;

/// <summary>
/// Cámara fija del modo cenital: centra el mapa y ajusta el zoom para que se vea COMPLETO
/// en cualquier pantalla (16:9, celulares 20:9...). Si sobra espacio a los lados o arriba,
/// se ve el color de fondo. Incluye el temblor que usa el ventanal al romperse.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CamaraPisoCenital : MonoBehaviour
{
    [Tooltip("Margen (en baldosas) alrededor del mapa.")]
    public float margen = 0.4f;
    public Color colorFondo = new Color(0.07f, 0.07f, 0.1f, 1f);

    private Camera camara;
    private Rect zona = new Rect(0f, 0f, 22f, 12f);
    private bool tieneZona;
    private static float fuerzaTemblor, finTemblor, duracionTemblor;
    private Vector3 baseCentro;
    private float ultimoAspecto;
    private Canvas canvasFranja;
    private float alturaFranja;
    private float fraccionFranja;

    private void Awake()
    {
        camara = GetComponent<Camera>();
        camara.orthographic = true;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = colorFondo;
        fuerzaTemblor = 0f;
    }

    /// <summary>Define la zona del mapa que debe verse entera.</summary>
    public void Encuadrar(Rect mapa)
    {
        zona = mapa;
        tieneZona = true;
        Ajustar();
    }

    /// <summary>
    /// Reserva arriba una franja para el HUD: el mapa se dibuja solo debajo. La altura está en
    /// píxeles del lienzo de referencia y se convierte a fracción de pantalla en cada cambio de tamaño.
    /// </summary>
    public void ReservarFranjaSuperior(Canvas canvas, float alturaEnLienzo)
    {
        canvasFranja = canvas;
        alturaFranja = alturaEnLienzo;
        ActualizarFranja();
    }

    private void ActualizarFranja()
    {
        if (camara == null || canvasFranja == null || Screen.height <= 0) return;
        float fraccion = Mathf.Clamp(alturaFranja * canvasFranja.scaleFactor / Screen.height, 0f, 0.4f);
        if (Mathf.Approximately(fraccion, fraccionFranja) && camara.rect.height > 0f && camara.rect.height < 1f) return;
        fraccionFranja = fraccion;
        camara.rect = new Rect(0f, 0f, 1f, 1f - fraccion);
        if (tieneZona) Ajustar();
    }

    public static void Temblar(float fuerza, float duracion)
    {
        fuerzaTemblor = fuerza;
        duracionTemblor = Mathf.Max(0.05f, duracion);
        finTemblor = Time.unscaledTime + duracionTemblor;
    }

    private void Ajustar()
    {
        if (camara == null) return;
        float aspecto = Mathf.Max(0.5f, camara.aspect);
        float mitadAlto = zona.height * 0.5f + margen;
        float mitadAncho = zona.width * 0.5f + margen;
        camara.orthographicSize = Mathf.Max(mitadAlto, mitadAncho / aspecto);
        baseCentro = new Vector3(zona.center.x, zona.center.y, -10f);
        transform.position = baseCentro;
        ultimoAspecto = camara.aspect;
    }

    private void LateUpdate()
    {
        ActualizarFranja();
        if (!tieneZona) return;
        if (!Mathf.Approximately(ultimoAspecto, camara.aspect)) Ajustar();

        Vector3 p = baseCentro;
        if (Time.unscaledTime < finTemblor)
        {
            float k = (finTemblor - Time.unscaledTime) / duracionTemblor;
            p += (Vector3)(Random.insideUnitCircle * fuerzaTemblor * k);
        }
        transform.position = p;
    }
}
