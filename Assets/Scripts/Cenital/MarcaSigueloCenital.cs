using UnityEngine;

/// <summary>
/// Marca verde "¡SÍGUELO!" que aparece en el punto de salto del último piso (sin ascensor) cuando el demoledor salta por la baranda.
/// El jugador DEBE caminar hasta ella para superar el piso; si queda fuera de pantalla, una flecha verde lo señala.
/// </summary>
public class MarcaSigueloCenital : MonoBehaviour
{
    public static MarcaSigueloCenital Actual { get; private set; }

    [Tooltip("Distancia (baldosas) a la que el jugador alcanza la marca.")]
    [Min(0.3f)] public float radio = 1.0f;

    private PisoCenital piso;
    private JugadorCenital jugador;
    private SpriteRenderer disco;
    private TextMesh texto;
    private bool alcanzada;

    /// <summary>Crea la marca en 'posicion' (la quita sola al reiniciar el piso o al alcanzarla).</summary>
    public static MarcaSigueloCenital Crear(Vector2 posicion, PisoCenital piso)
    {
        Quitar();
        GameObject g = new GameObject("MarcaSiguelo");
        g.transform.position = new Vector3(posicion.x, posicion.y, 0f);
        MarcaSigueloCenital m = g.AddComponent<MarcaSigueloCenital>();
        m.piso = piso;
        Actual = m;

        int orden = Provisional.OrdenY(posicion.y) + 4;
        m.disco = Provisional.Crear(g.transform, "Disco", Provisional.Circulo, new Color(0.25f, 0.95f, 0.4f, 0.55f), orden);
        m.texto = Provisional.CrearTexto(g.transform, "Texto", "¡SÍGUELO!", 0.45f, new Color(0.7f, 1f, 0.7f), orden + 1);
        m.texto.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        return m;
    }

    public static void Quitar()
    {
        if (Actual != null) Destroy(Actual.gameObject);
        Actual = null;
    }

    private void Update()
    {
        float pulso = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
        float d = radio * 2f * (0.85f + 0.15f * pulso);
        disco.transform.localScale = new Vector3(d, d, 1f);
        disco.color = new Color(0.25f, 0.95f, 0.4f, 0.35f + 0.3f * pulso);

        if (alcanzada || piso == null || piso.Terminado) return;
        if (jugador == null) jugador = FindFirstObjectByType<JugadorCenital>();
        if (jugador == null) return;
        if (Vector2.Distance(jugador.Centro, (Vector2)transform.position) <= radio)
        {
            alcanzada = true;
            piso.JugadorLlegoAlAscensor();
        }
    }

    private void OnDestroy() { if (Actual == this) Actual = null; }
}
