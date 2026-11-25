using System.Collections;
using UnityEngine;

/// <summary>
/// Festejo al superar un piso: la cámara se acerca a los estudiantes, que saltan de alegría y gritan algo, llueve
/// confeti, explotan fuegos artificiales arriba de la pantalla y aparece "¡PISO SALVADO!". Dura unos segundos y después
/// GestorJuego muestra las estrellas. También sirve para el final de la fachada (Confeti / Fuegos).
/// </summary>
public static class CelebracionPiso
{
    private static readonly string[] Gritos =
    {
        "¡LO LOGRAMOS!", "¡PISO SALVADO!", "¡ESO, EQUIPO!", "¡CHAU, DEMOLEDOR!", "¡APROBADO CON 100!", "¡NI UNA GRIETA MÁS!",
        "¡QUE NO SE CAIGA LA U!", "¡A LA SIGUIENTE!",
    };

    /// <summary>La secuencia completa (en tiempo real: funciona aunque el juego esté en cámara lenta).</summary>
    public static IEnumerator Reproducir(PisoCenital piso)
    {
        bool antes = JugadorCenital.EntradaBloqueada;
        JugadorCenital.EntradaBloqueada = true;

        GestorAudio.Reproducir(Snd.JingleVictoria);
        AvisoUlti.Mostrar("¡PISO SALVADO!", 2.6f, false, new Color(0.45f, 1f, 0.55f));

        // Cada estudiante grita algo distinto.
        int k = Random.Range(0, Gritos.Length);
        foreach (JugadorCenital j in JugadorCenital.Todos)
        {
            if (j == null || !j.gameObject.activeInHierarchy) continue;
            Burbuja.Decir(j.transform, Gritos[k % Gritos.Length], j.Ficha != null ? j.Ficha.Voz : Burbuja.Voz.Hombre, 1.6f);
            k++;
            Confeti(j.Centro + Vector2.up * 0.8f, 26, 3.2f);
        }

        Vector2 foco;
        bool hayFoco = CamaraPisoCenital.PosicionPropia(out foco);
        float inicio = Time.unscaledTime;
        float proximoFuego = 0f;
        const float dur = 2.8f;
        while (Time.unscaledTime - inicio < dur)
        {
            float t = (Time.unscaledTime - inicio) / dur;
            if (hayFoco) CamaraPisoCenital.Enfocar(foco + Vector2.up * 0.4f, Mathf.Lerp(0.85f, 0.62f, Mathf.Clamp01(t * 2.5f)));
            if (Time.unscaledTime >= proximoFuego)
            {
                proximoFuego = Time.unscaledTime + Random.Range(0.35f, 0.6f);
                Fuego();
                ConfetiDelCielo(10);
            }
            yield return null;
        }
        CamaraPisoCenital.SoltarEnfoque();
        JugadorCenital.EntradaBloqueada = antes;
    }

    private static Sprite[] confeti, chispas;

    /// <summary>Estallido de confeti en un punto (salta hacia arriba y cae dando vueltas).</summary>
    public static void Confeti(Vector2 punto, int cantidad, float fuerza)
    {
        if (confeti == null) confeti = ArteExtra.Serie("Mejoras/confeti_", 4);
        if (confeti == null) return;
        for (int i = 0; i < cantidad; i++)
        {
            Sprite s = confeti[Random.Range(0, confeti.Length)];
            Vector2 v = new Vector2(Random.Range(-1f, 1f) * fuerza * 0.6f, Random.Range(0.6f, 1.2f) * fuerza);
            ParticulaSimple.Crear(new[] { s }, punto, v, Random.Range(1.4f, 2.2f), 520, Color.white, 1.4f, 1.1f, 6f);
        }
    }

    /// <summary>Confeti que cae desde arriba de la pantalla.</summary>
    public static void ConfetiDelCielo(int cantidad)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        if (confeti == null) confeti = ArteExtra.Serie("Mejoras/confeti_", 4);
        if (confeti == null) return;
        float alto = cam.orthographicSize, ancho = alto * cam.aspect;
        Vector3 c = cam.transform.position;
        for (int i = 0; i < cantidad; i++)
        {
            Sprite s = confeti[Random.Range(0, confeti.Length)];
            Vector2 p = new Vector2(c.x + Random.Range(-ancho, ancho), c.y + alto + 0.3f);
            ParticulaSimple.Crear(new[] { s }, p, new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-1.6f, -0.8f)), Random.Range(2f, 3f), 520,
                Color.white, 1.3f, 1.3f, 0.3f);
        }
    }

    /// <summary>Un fuego artificial en la parte alta de la pantalla.</summary>
    public static void Fuego()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        if (chispas == null) chispas = ArteExtra.Serie("Mejoras/chispa_", 3);
        if (chispas == null) return;
        float alto = cam.orthographicSize, ancho = alto * cam.aspect;
        Vector3 c = cam.transform.position;
        Vector2 centro = new Vector2(c.x + Random.Range(-ancho * 0.7f, ancho * 0.7f), c.y + Random.Range(alto * 0.3f, alto * 0.8f));
        Color[] colores = { new Color(1f, 0.45f, 0.4f), new Color(1f, 0.9f, 0.35f), new Color(0.45f, 0.9f, 1f), new Color(0.6f, 1f, 0.5f), new Color(1f, 0.55f, 1f) };
        Color col = colores[Random.Range(0, colores.Length)];
        int n = 18;
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(2.2f, 3f);
            ParticulaSimple.Crear(chispas, centro, v, Random.Range(0.7f, 1f), 525, col, 1.6f, 0.8f, 2.5f);
        }
        GestorAudio.Reproducir(Snd.Estrella1, 0.6f, Random.Range(0.8f, 1.2f));
    }
}
