using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Partícula simple con cuadros: sube o vuela, gira un poco y se desvanece (vapor, humo, chispas).</summary>
public class ParticulaSimple : MonoBehaviour
{
    private SpriteRenderer sr;
    private Sprite[] cuadros;
    private Vector2 velocidad;
    private float vida, inicio, giro, gravedad, escala0, escala1;
    private Color color;

    // Reciclaje: crear y destruir objetos a cada rato traba en celulares lentos; se reutilizan los que terminaron.
    private static readonly Stack<ParticulaSimple> libres = new Stack<ParticulaSimple>();

    public static ParticulaSimple Crear(Sprite[] cuadros, Vector2 pos, Vector2 velocidad, float vida, int orden,
        Color color, float escalaInicial = 1f, float escalaFinal = 1f, float gravedad = 0f, Transform padre = null)
    {
        if (cuadros == null || cuadros.Length == 0) return null;
        ParticulaSimple p = null;
        while (p == null && libres.Count > 0) p = libres.Pop();   // las que se destruyeron con la escena salen como null
        GameObject g;
        if (p != null)
        {
            g = p.gameObject;
            g.transform.SetParent(padre, true);
            g.transform.rotation = Quaternion.identity;
            p.sr.flipX = false;
            p.sr.enabled = true;
            EspejoAnfitrion.Redecidir(p.sr);
            g.SetActive(true);
        }
        else
        {
            g = new GameObject("Particula");
            if (padre != null) g.transform.SetParent(padre, true);
            p = g.AddComponent<ParticulaSimple>();
            p.sr = g.AddComponent<SpriteRenderer>();
        }
        g.transform.position = pos;
        p.sr.sprite = cuadros[0];
        p.sr.sortingOrder = orden;
        p.sr.color = color;
        p.cuadros = cuadros;
        p.velocidad = velocidad;
        p.vida = Mathf.Max(0.05f, vida);
        p.inicio = Time.time;
        p.giro = Random.Range(-90f, 90f);
        p.gravedad = gravedad;
        p.escala0 = escalaInicial;
        p.escala1 = escalaFinal;
        p.color = color;
        g.transform.localScale = Vector3.one * escalaInicial;
        return p;
    }

    private void Update()
    {
        float k = (Time.time - inicio) / vida;
        if (k >= 1f)
        {
            if (libres.Count < 160) { gameObject.SetActive(false); libres.Push(this); }
            else Destroy(gameObject);
            return;
        }
        velocidad.y -= gravedad * Time.deltaTime;
        transform.position += (Vector3)(velocidad * Time.deltaTime);
        transform.Rotate(0f, 0f, giro * Time.deltaTime);
        float e = Mathf.Lerp(escala0, escala1, k);
        transform.localScale = new Vector3(e, e, 1f);
        sr.sprite = cuadros[Mathf.Min(cuadros.Length - 1, (int)(k * cuadros.Length))];
        Color c = color;
        c.a *= k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
        sr.color = c;
    }
}

/// <summary>
/// "Pausa de impacto": en los golpes fuertes el tiempo casi se detiene unas centésimas para que el golpe se sienta.
/// No hace nada si el juego está en pausa o en cámara lenta por otra cosa.
/// </summary>
public class ImpactoPausa : MonoBehaviour
{
    private static ImpactoPausa inst;
    private const float Escala = 0.05f;
    private float hasta;
    private bool activa;

    public static void Breve(float segundos)
    {
        if (segundos <= 0f) return;
        if (!activa_() && !Mathf.Approximately(Time.timeScale, 1f)) return;   // pausa, transformación, etc.
        if (inst == null)
        {
            GameObject g = new GameObject("[ImpactoPausa]");
            inst = g.AddComponent<ImpactoPausa>();
        }
        inst.hasta = Mathf.Max(inst.hasta, Time.unscaledTime + segundos);
        if (!inst.activa) inst.StartCoroutine(inst.Rutina());
    }

    private static bool activa_() { return inst != null && inst.activa; }

    private IEnumerator Rutina()
    {
        activa = true;
        Time.timeScale = Escala;
        while (Time.unscaledTime < hasta)
        {
            if (!Mathf.Approximately(Time.timeScale, Escala)) { activa = false; yield break; }   // alguien pausó
            yield return null;
        }
        if (Mathf.Approximately(Time.timeScale, Escala)) Time.timeScale = 1f;
        activa = false;
    }

    private void OnDestroy()
    {
        if (activa && Mathf.Approximately(Time.timeScale, Escala)) Time.timeScale = 1f;
        if (inst == this) inst = null;
    }
}
