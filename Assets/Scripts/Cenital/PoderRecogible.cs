using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Un poder en el suelo. Se recoge al tocarlo con el jugador; si nadie lo toma desaparece a los
/// 'duracionPoderEnSuelo' segundos (DefinicionPiso) y parpadea durante los últimos 'parpadeoFinalPoder'.
/// Los crea GestorPoderes con PoderRecogible.Crear.
/// </summary>
public class PoderRecogible : MonoBehaviour
{
    private static readonly List<PoderRecogible> activos = new List<PoderRecogible>();

    /// <summary>Los poderes que hay ahora en el suelo (solo lectura).</summary>
    public static IReadOnlyList<PoderRecogible> Activos { get { return activos; } }

    /// <summary>Segundos desde que apareció este poder.</summary>
    public float Edad { get { return Time.time - horaCreacion; } }

    /// <summary>Poderes que hay ahora en el suelo.</summary>
    public static int Cantidad { get { return activos.Count; } }

    public static bool Hay(PoderPiso datos)
    {
        foreach (PoderRecogible p in activos) if (p != null && p.datos == datos) return true;
        return false;
    }

    /// <summary>Posiciones de los poderes que hay en el suelo (para no encimar uno nuevo).</summary>
    public static IEnumerable<Vector2> Posiciones()
    {
        foreach (PoderRecogible p in activos) if (p != null) yield return p.transform.position;
    }

    public static void EliminarTodos()
    {
        for (int i = activos.Count - 1; i >= 0; i--)
            if (activos[i] != null) Destroy(activos[i].gameObject);
        activos.Clear();
    }

    [Tooltip("Datos del poder (los toma de la DefinicionPiso).")]
    public PoderPiso datos;

    private float finVida, parpadeoFinal, horaCreacion;
    private bool tomado;
    private Transform visual;
    private Vector3 posVisual;
    private SpriteRenderer[] dibujos;

    /// <summary>Crea un poder en 'posicion'. Vive 'vida' segundos y parpadea los últimos 'parpadeo'.</summary>
    public static PoderRecogible Crear(PoderPiso datos, Vector2 posicion, float vida, float parpadeo, float radioRecogida)
    {
        GameObject g = new GameObject("Poder " + datos.nombre);
        g.transform.position = new Vector3(posicion.x, posicion.y, 0f);

        CircleCollider2D col = g.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = radioRecogida;

        PoderRecogible p = g.AddComponent<PoderRecogible>();
        p.datos = datos;
        p.finVida = Time.time + vida;
        p.horaCreacion = Time.time;
        p.parpadeoFinal = parpadeo;
        p.CrearVisual();
        GestorAudio.Reproducir(Snd.PoderAparece);
        activos.Add(p);
        return p;
    }

    private void CrearVisual()
    {
        int orden = Provisional.OrdenY(transform.position.y - 0.4f) + 3;
        List<SpriteRenderer> lista = new List<SpriteRenderer>();

        SpriteRenderer sombra = Provisional.Crear(transform, "Sombra", Provisional.Circulo, new Color(0f, 0f, 0f, 0.3f), orden - 2);
        sombra.transform.localPosition = new Vector3(0f, -0.38f, 0f);
        sombra.transform.localScale = new Vector3(0.6f, 0.24f, 1f);
        lista.Add(sombra);

        GameObject v = new GameObject("Visual");
        v.transform.SetParent(transform, false);
        visual = v.transform;
        posVisual = visual.localPosition;

        if (datos.sprite != null)
        {
            SpriteRenderer sr = Provisional.Crear(visual, "Dibujo", datos.sprite, Color.white, orden);
            Provisional.AjustarAlArea(sr, datos.sprite, new Bounds(transform.position, new Vector3(1f, 1f, 0f)));
            lista.Add(sr);
        }
        else
        {
            SpriteRenderer borde = Provisional.Crear(visual, "Borde", Provisional.Circulo, new Color(0.1f, 0.1f, 0.12f, 1f), orden - 1);
            borde.transform.localScale = new Vector3(0.82f, 0.82f, 1f);
            SpriteRenderer disco = Provisional.Crear(visual, "Disco", Provisional.Circulo, datos.color, orden);
            disco.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            lista.Add(borde);
            lista.Add(disco);

            Provisional.CrearTexto(visual, "Simbolo", datos.simbolo, 0.5f, Color.white, orden + 1);
        }
        dibujos = lista.ToArray();
    }

    private void Update()
    {
        float restante = finVida - Time.time;
        if (restante <= 0f) { Destroy(gameObject); return; }

        if (visual != null) visual.localPosition = posVisual + new Vector3(0f, 0.2f + Mathf.Sin(Time.time * 4f) * 0.07f, 0f);

        // Parpadeo de los últimos segundos.
        bool ver = restante > parpadeoFinal || ((int)(Time.time * 10f) % 2 == 0);
        if (visual != null && visual.gameObject.activeSelf != ver) visual.gameObject.SetActive(ver);
        if (dibujos != null && dibujos.Length > 0 && dibujos[0] != null) dibujos[0].enabled = ver;
    }

    private void OnTriggerEnter2D(Collider2D otro) { Intentar(otro); }
    private void OnTriggerStay2D(Collider2D otro) { Intentar(otro); }

    private void Intentar(Collider2D otro)
    {
        if (tomado) return;
        JugadorCenital jugador = otro.GetComponentInParent<JugadorCenital>();
        if (jugador == null || !jugador.enabled) return;

        tomado = true;
        GestorAudio.Reproducir(Snd.PoderRecoger);
        string mensaje = jugador.AplicarPoder(datos);
        string texto = !string.IsNullOrEmpty(mensaje) ? mensaje : datos.mensajeRecogido;
        TextoFlotante.Mostrar(jugador.Centro + Vector2.up * 1.3f, texto, datos.colorAro);
        EfectoSprite.Reproducir(ArteNiveles.De(a => a.destelloPoder), jugador.Centro, Provisional.OrdenY(jugador.Centro.y) + 6, 1f, 12f);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        activos.Remove(this);
    }
}
