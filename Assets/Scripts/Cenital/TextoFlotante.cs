using UnityEngine;

/// <summary>
/// Mensaje corto que sube y se desvanece sobre un punto del mapa ("¡INVENCIBLE!", "+1 VIDA"...).
/// Se crea con TextoFlotante.Mostrar y se destruye solo.
/// </summary>
public class TextoFlotante : MonoBehaviour
{
    private TextMesh frente, sombra;
    private float inicio, duracion, velocidad;
    private Color color;

    /// <summary>Muestra 'texto' sobre 'posicion' durante 'duracion' segundos, subiendo 'velocidad' baldosas por segundo.</summary>
    public static TextoFlotante Mostrar(Vector2 posicion, string texto, Color color, float duracion = 1.8f,
                                        float altura = 0.5f, float velocidad = 0.6f)
    {
        if (string.IsNullOrEmpty(texto)) return null;

        GameObject g = new GameObject("TextoFlotante");
        TextoFlotante t = g.AddComponent<TextoFlotante>();
        t.inicio = Time.time;
        t.duracion = Mathf.Max(0.2f, duracion);
        t.velocidad = velocidad;
        t.color = color;

        // El texto no debe salirse del mapa por los lados.
        float mitadAncho = texto.Length * altura * 0.32f;
        PisoCenital piso = PisoCenital.Actual;
        if (piso != null && piso.Mapa != null)
        {
            Rect lim = piso.Mapa.Limites;
            posicion.x = Mathf.Clamp(posicion.x, lim.xMin + mitadAncho, lim.xMax - mitadAncho);
        }
        g.transform.position = new Vector3(posicion.x, posicion.y, 0f);

        t.sombra = Provisional.CrearTexto(g.transform, "Sombra", texto, altura, new Color(0f, 0f, 0f, 0.85f), 499);
        t.sombra.transform.localPosition = new Vector3(0.04f, -0.04f, 0f);
        t.frente = Provisional.CrearTexto(g.transform, "Texto", texto, altura, color, 500);
        return t;
    }

    private void Update()
    {
        float k = (Time.time - inicio) / duracion;
        if (k >= 1f) { Destroy(gameObject); return; }

        transform.position += new Vector3(0f, velocidad * Time.deltaTime, 0f);
        float alfa = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
        if (frente != null) frente.color = new Color(color.r, color.g, color.b, alfa);
        if (sombra != null) sombra.color = new Color(0f, 0f, 0f, 0.85f * alfa);
    }
}
