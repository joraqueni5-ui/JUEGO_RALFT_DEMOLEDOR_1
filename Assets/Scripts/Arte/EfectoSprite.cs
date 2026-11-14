using UnityEngine;

/// <summary>
/// Reproduce una animación de cuadros sueltos sobre un SpriteRenderer (unos 10 a 12 cuadros por segundo).
/// Con 'bucle' se repite; sin él se destruye al terminar (si 'destruirAlTerminar').
/// </summary>
public class AnimadorSprite : MonoBehaviour
{
    public Sprite[] cuadros;
    [Min(1f)] public float cuadrosPorSegundo = 12f;
    public bool bucle;
    public bool destruirAlTerminar = true;
    [Tooltip("Sigue animándose con el juego en pausa (tiempo sin escala).")]
    public bool ignorarPausa;

    private SpriteRenderer dibujo;
    private float tiempo;

    private void Awake() { dibujo = GetComponent<SpriteRenderer>(); }

    private void OnEnable() { tiempo = 0f; Aplicar(); }

    /// <summary>Reparte los cuadros a lo largo de 'segundos' (en vez de a un ritmo fijo).</summary>
    public void AjustarADuracion(float segundos)
    {
        if (cuadros != null && cuadros.Length > 0 && segundos > 0.01f) cuadrosPorSegundo = Mathf.Max(1f, cuadros.Length / segundos);
    }

    private void Update()
    {
        tiempo += ignorarPausa ? Time.unscaledDeltaTime : Time.deltaTime;
        Aplicar();
    }

    private void Aplicar()
    {
        if (cuadros == null || cuadros.Length == 0) return;
        if (dibujo == null) dibujo = GetComponent<SpriteRenderer>();
        if (dibujo == null) return;

        int n = Mathf.FloorToInt(tiempo * cuadrosPorSegundo);
        if (!bucle && n >= cuadros.Length)
        {
            if (destruirAlTerminar) Destroy(gameObject);
            else dibujo.sprite = cuadros[cuadros.Length - 1];
            return;
        }
        dibujo.sprite = cuadros[bucle ? n % cuadros.Length : n];
    }
}

/// <summary>Atajos para crear efectos animados con los sprites de ArteNiveles.</summary>
public static class EfectoSprite
{
    /// <summary>Un efecto de una sola vez en 'posicion'. Devuelve null si no hay cuadros (el llamador usa su figura provisional).</summary>
    public static SpriteRenderer Reproducir(Sprite[] cuadros, Vector3 posicion, int orden, float escala = 1f, float cuadrosPorSegundo = 12f, Transform padre = null)
    {
        cuadros = ArteNiveles.Serie(cuadros);
        if (cuadros == null) return null;

        GameObject g = new GameObject("Efecto");
        if (padre != null) g.transform.SetParent(padre, false);
        g.transform.position = posicion;
        g.transform.localScale = new Vector3(escala, escala, 1f);
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = cuadros[0];
        sr.sortingOrder = orden;
        AnimadorSprite a = g.AddComponent<AnimadorSprite>();
        a.cuadros = cuadros;
        a.cuadrosPorSegundo = cuadrosPorSegundo;
        a.bucle = false;
        a.destruirAlTerminar = true;
        return sr;
    }

    /// <summary>Anima en bucle un renderer que ya existe. Devuelve false si no hay cuadros.</summary>
    public static bool EnBucle(SpriteRenderer sr, Sprite[] cuadros, float cuadrosPorSegundo = 10f)
    {
        cuadros = ArteNiveles.Serie(cuadros);
        if (sr == null || cuadros == null) return false;

        AnimadorSprite a = sr.GetComponent<AnimadorSprite>();
        if (a == null) a = sr.gameObject.AddComponent<AnimadorSprite>();
        a.cuadros = cuadros;
        a.cuadrosPorSegundo = cuadrosPorSegundo;
        a.bucle = true;
        a.destruirAlTerminar = false;
        sr.sprite = cuadros[0];
        return true;
    }
}

/// <summary>Un trozo de escombro que salta hacia afuera, cae y se desvanece (con los sprites de trozos_escombro).</summary>
public class TrozoVolador : MonoBehaviour
{
    private Vector2 velocidad;
    private float tiempo, vida = 0.7f;
    private SpriteRenderer dibujo;

    /// <summary>Lanza 'cantidad' trozos desde 'posicion'. No hace nada si no hay sprites de trozos.</summary>
    public static void Lanzar(Vector3 posicion, int cantidad, int orden = 305)
    {
        ArteNiveles arte = ArteNiveles.Actual;
        Sprite[] trozos = arte != null ? ArteNiveles.Serie(arte.trozosEscombro) : null;
        if (trozos == null) return;

        for (int i = 0; i < cantidad; i++)
        {
            GameObject g = new GameObject("Trozo");
            g.transform.position = posicion;
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = trozos[Random.Range(0, trozos.Length)];
            sr.sortingOrder = orden;
            TrozoVolador t = g.AddComponent<TrozoVolador>();
            t.dibujo = sr;
            float ang = (i + Random.value * 0.6f) / cantidad * Mathf.PI * 2f;
            t.velocidad = new Vector2(Mathf.Cos(ang) * Random.Range(1.2f, 2.6f), Mathf.Abs(Mathf.Sin(ang)) * Random.Range(2f, 3.4f) + 1f);
            t.vida = Random.Range(0.5f, 0.8f);
        }
    }

    private void Update()
    {
        tiempo += Time.deltaTime;
        velocidad.y -= 9f * Time.deltaTime;
        transform.position += (Vector3)(velocidad * Time.deltaTime);
        transform.Rotate(0f, 0f, 360f * Time.deltaTime);
        if (dibujo != null) dibujo.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - (tiempo - vida * 0.5f) / (vida * 0.5f)));
        if (tiempo >= vida) Destroy(gameObject);
    }
}
