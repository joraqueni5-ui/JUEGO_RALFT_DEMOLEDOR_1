using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Globo de diálogo o de pensamiento sobre un personaje, con el texto que aparece letra a letra y "voz".
/// La voz es un balbuceo por sílabas (como en muchos juegos) con el timbre de cada personaje; si existe una grabación
/// real en Resources/Voces/&lt;clave&gt;.wav (por ejemplo hecha con VoiceStudio), suena esa en su lugar.
/// La clave de una frase es su texto en minúsculas sin tildes ni signos, con guiones bajos (ver Burbuja.Clave).
/// </summary>
public class Burbuja : MonoBehaviour
{
    public enum Voz { Hombre, Mujer, Mayor, Villano, Mini, Ninguna }

    private static readonly Dictionary<Transform, Burbuja> activas = new Dictionary<Transform, Burbuja>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstatico() { activas.Clear(); }

    private Transform objetivo;
    private float altura, inicio, duracion, proximaSilaba;
    private string texto;
    private int visibles;
    private Voz voz;
    private TextMesh tm;
    private SpriteRenderer fondo, cola;
    private Vector2 tamano;

    private const int CaracteresPorLinea = 22;
    private const float LetrasPorSegundo = 34f;

    /// <summary>Muestra 'texto' sobre 'quien' (reemplaza el globo anterior de ese personaje).</summary>
    public static Burbuja Decir(Transform quien, string texto, Voz voz, float altura = 1.9f, bool pensamiento = false)
    {
        if (quien == null || string.IsNullOrEmpty(texto)) return null;
        Burbuja previa;
        if (activas.TryGetValue(quien, out previa) && previa != null) Destroy(previa.gameObject);

        GameObject g = new GameObject("Burbuja");
        Burbuja b = g.AddComponent<Burbuja>();
        b.objetivo = quien;
        b.altura = altura;
        b.texto = Ajustar(texto);
        b.voz = voz;
        b.inicio = Time.time;
        b.duracion = 1.6f + texto.Length / 14f;
        b.Construir(pensamiento);
        activas[quien] = b;

        // Grabación real si existe; si no, balbuceo letra a letra.
        AudioClip real = Resources.Load<AudioClip>("Voces/" + Clave(texto));
        if (real != null) { GestorAudio.ReproducirClip("voces_" + Clave(texto), real); b.voz = Voz.Ninguna; }
        return b;
    }

    /// <summary>Nombre de archivo de la grabación de una frase: "¡Como nuevo!" → como_nuevo.</summary>
    public static string Clave(string texto)
    {
        StringBuilder sb = new StringBuilder();
        string t = texto.ToLowerInvariant().Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u').Replace('ñ', 'n').Replace('ü', 'u');
        foreach (char c in t)
        {
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) sb.Append(c);
            else if (sb.Length > 0 && sb[sb.Length - 1] != '_') sb.Append('_');
            if (sb.Length >= 48) break;
        }
        return sb.ToString().Trim('_');
    }

    private static string Ajustar(string texto)
    {
        string[] palabras = texto.Split(' ');
        StringBuilder sb = new StringBuilder();
        int linea = 0;
        foreach (string p in palabras)
        {
            if (linea > 0 && linea + p.Length + 1 > CaracteresPorLinea) { sb.Append('\n'); linea = 0; }
            else if (linea > 0) { sb.Append(' '); linea++; }
            sb.Append(p);
            linea += p.Length;
        }
        return sb.ToString();
    }

    private void Construir(bool pensamiento)
    {
        string[] lineas = texto.Split('\n');
        int largo = 0;
        foreach (string l in lineas) largo = Mathf.Max(largo, l.Length);
        tamano = new Vector2(Mathf.Max(1.2f, largo * 0.15f + 0.45f), lineas.Length * 0.3f + 0.3f);

        Sprite globo = ArteExtra.Uno("Tema/burbuja");
        fondo = Provisional.Crear(transform, "Fondo", globo != null ? globo : Provisional.Cuadrado, Color.white, 700);
        if (globo != null)
        {
            fondo.drawMode = SpriteDrawMode.Sliced;
            fondo.transform.localScale = new Vector3(2.5f, 2.5f, 1f);
            fondo.size = tamano / 2.5f;
        }
        else fondo.transform.localScale = new Vector3(tamano.x, tamano.y, 1f);

        Sprite piquito = ArteExtra.Uno(pensamiento ? "Tema/nube_pensamiento" : "Tema/cola_burbuja");
        cola = Provisional.Crear(transform, "Cola", piquito, Color.white, 701);
        cola.transform.localScale = new Vector3(2.5f, 2.5f, 1f);
        cola.transform.localPosition = new Vector3(-tamano.x * 0.2f, -tamano.y * 0.5f - 0.06f, 0f);

        tm = Provisional.CrearTexto(transform, "Texto", "", 0.24f, new Color(0.08f, 0.09f, 0.16f), 702);
        tm.fontStyle = FontStyle.Bold;
    }

    private void LateUpdate()
    {
        if (objetivo == null) { Destroy(gameObject); return; }
        transform.position = objetivo.position + new Vector3(0f, altura + tamano.y * 0.5f, 0f);

        // Letra a letra, con una sílaba de voz cada dos letras.
        int nuevas = Mathf.Min(texto.Length, (int)((Time.time - inicio) * LetrasPorSegundo));
        if (nuevas != visibles)
        {
            visibles = nuevas;
            tm.text = texto.Substring(0, visibles);
            if (voz != Voz.Ninguna && Time.time >= proximaSilaba && visibles > 0 && char.IsLetter(texto[visibles - 1]))
            {
                proximaSilaba = Time.time + 0.075f;
                GestorAudio.Reproducir(NombreVoz(voz, texto[visibles - 1]), 0.55f, Random.Range(0.94f, 1.08f));
            }
        }

        // Aparece con un pequeño rebote y se va desvaneciendo.
        float t = Time.time - inicio;
        float escala = t < 0.12f ? Mathf.Lerp(0.6f, 1.08f, t / 0.12f) : (t < 0.2f ? Mathf.Lerp(1.08f, 1f, (t - 0.12f) / 0.08f) : 1f);
        transform.localScale = new Vector3(escala, escala, 1f);
        float resto = duracion - t;
        if (resto <= 0f) { Destroy(gameObject); return; }
        float a = Mathf.Clamp01(resto / 0.3f);
        fondo.color = new Color(1f, 1f, 1f, a);
        cola.color = new Color(1f, 1f, 1f, a);
        Color c = tm.color; c.a = a; tm.color = c;
    }

    public static string NombreVoz(Voz v, char letra)
    {
        string tipo = v == Voz.Mujer ? "mujer" : v == Voz.Mayor ? "mayor" : v == Voz.Villano ? "villano" : v == Voz.Mini ? "mini" : "hombre";
        int k;
        switch (char.ToLowerInvariant(letra))
        {
            case 'a': case 'á': k = 0; break;
            case 'e': case 'é': k = 1; break;
            case 'i': case 'í': k = 2; break;
            case 'o': case 'ó': k = 3; break;
            case 'u': case 'ú': k = 4; break;
            default: k = Random.Range(0, 5); break;
        }
        return "voz_" + tipo + "_" + k;
    }

    private void OnDestroy()
    {
        if (objetivo != null)
        {
            Burbuja b;
            if (activas.TryGetValue(objetivo, out b) && b == this) activas.Remove(objetivo);
        }
    }
}
