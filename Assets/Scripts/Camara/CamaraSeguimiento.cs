using UnityEngine;

/// <summary>
/// Cámara que sube y baja con el muñeco por el edificio alto. Mientras el muñeco
/// siga en pantalla, intenta mostrar también al demoledor de la cornisa de arriba.
/// Solo se mueve en vertical; el horizontal queda como está en la escena.
/// También hace el pequeño temblor de pantalla cuando el demoledor golpea.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CamaraSeguimiento : MonoBehaviour
{
    [Tooltip("A quién sigue (el muñeco).")]
    public Transform objetivo;
    [Tooltip("Opcional: el demoledor, para intentar mostrarlo también.")]
    public Transform demoledor;

    [Tooltip("Cuánto por encima de los pies del muñeco se centra la cámara.")]
    public float desfaseY = 1.3f;
    [Tooltip("Espacio sobre los pies del demoledor que se quiere ver (su altura y un poco más).")]
    public float altoDemoledor = 1.3f;
    [Tooltip("Margen mínimo bajo los pies del muñeco que siempre se ve.")]
    public float margenInferior = 0.25f;
    [Tooltip("Rapidez con la que alcanza la posición deseada.")]
    public float suavizado = 5f;

    [Header("Límites (y del centro de la cámara)")]
    public float yMinima = 3.1f;
    public float yMaxima = 10.8f;

    [Header("Temblor")]
    [Tooltip("Apaga o prende el temblor de pantalla de los golpes.")]
    public bool permitirTemblor = true;

    private static CamaraSeguimiento instancia;

    private Camera camara;
    private Vector3 basePosicion;     // posición sin el temblor
    private float fuerzaTemblor;
    private float duracionTemblor;
    private float tiempoTemblor;

    private void Awake()
    {
        camara = GetComponent<Camera>();
        instancia = this;
        basePosicion = transform.position;
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    private void Start()
    {
        // Empieza ya en su sitio, sin deslizarse desde donde quedó en la escena.
        basePosicion.y = Deseada();
        transform.position = basePosicion;
    }

    /// <summary>
    /// Pequeño temblor de pantalla (lo llama el demoledor en cada golpe).
    /// Si llega uno más fuerte mientras tiembla, manda el más fuerte.
    /// </summary>
    public static void Temblar(float fuerza, float duracion)
    {
        if (instancia == null || !instancia.permitirTemblor) return;
        if (instancia.tiempoTemblor > 0f && fuerza < instancia.fuerzaTemblor) return;

        instancia.fuerzaTemblor = fuerza;
        instancia.duracionTemblor = Mathf.Max(0.01f, duracion);
        instancia.tiempoTemblor = instancia.duracionTemblor;
    }

    private void LateUpdate()
    {
        if (objetivo != null)
        {
            float t = 1f - Mathf.Exp(-suavizado * Time.deltaTime);
            basePosicion.y = Mathf.Lerp(basePosicion.y, Deseada(), t);
        }

        Vector3 desplazamiento = Vector3.zero;
        if (tiempoTemblor > 0f)
        {
            tiempoTemblor -= Time.deltaTime;
            float k = Mathf.Clamp01(tiempoTemblor / duracionTemblor);   // se apaga de a poco
            desplazamiento = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * (fuerzaTemblor * k);
        }

        transform.position = basePosicion + desplazamiento;
    }

    private float Deseada()
    {
        if (objetivo == null) return basePosicion.y;

        float alto = camara != null && camara.orthographic ? camara.orthographicSize : 3.2f;
        float y = objetivo.position.y + desfaseY;

        if (demoledor != null && demoledor.gameObject.activeInHierarchy)
        {
            // Subir lo necesario para ver al demoledor, sin perder al muñeco por abajo.
            float paraVerDemoledor = demoledor.position.y + altoDemoledor - alto;
            float maximoConMuneco = objetivo.position.y - margenInferior + alto;
            y = Mathf.Max(y, Mathf.Min(paraVerDemoledor, maximoConMuneco));
        }

        return Mathf.Clamp(y, yMinima, Mathf.Max(yMinima, yMaxima));
    }
}
