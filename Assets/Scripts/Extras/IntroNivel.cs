using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Escena de llegada de cada piso. Al empezar, el piso está tranquilo: estudiantes y personal de distintas carreras
/// caminan por los pasillos
/// (estudiantes de varias carreras, docentes, la decana, seguridad y mantenimiento). Suena la alarma, el demoledor rompe el ventanal y la cámara lo enfoca con franjas de cine
/// ("¡LLEGÓ EL DEMOLEDOR!"); todos gritan y salen corriendo a escapar. Después la cámara vuelve al jugador y empieza el juego.
/// Si se reinicia el piso, la gente vuelve. InstaladorExtras lo agrega solo a cada piso cenital.
/// </summary>
public class IntroNivel : MonoBehaviour
{
    [Tooltip("Personas caminando antes de que llegue el demoledor.")]
    public Vector2Int cantidadGente = new Vector2Int(12, 18);
    [Tooltip("Segundos que la cámara sigue al demoledor mientras entra.")]
    public float duracionEnfoque = 2.6f;

    private PisoCenital piso;
    private DemoledorCenital demoledor;
    private readonly List<PersonaAmbiente> gente = new List<PersonaAmbiente>();
    private bool escenaHecha;
    private float finEnfoque;
    private RectTransform franjaArriba, franjaAbajo;
    private Text titulo;
    private Canvas canvas;

    private void Start()
    {
        piso = GetComponent<PisoCenital>();
        if (piso == null) piso = PisoCenital.Actual;
        demoledor = FindAnyObjectByType<DemoledorCenital>();
        CrearFranjas();
        LlenarPiso();
    }

    private void OnDestroy()
    {
        JugadorCenital.EntradaBloqueada = false;
        CamaraPisoCenital.SoltarEnfoque();
    }

    // ------------------------------------------------------------ gente
    private void LlenarPiso()
    {
        foreach (PersonaAmbiente p in gente) if (p != null) Destroy(p.gameObject);
        gente.Clear();
        if (piso == null || piso.Mapa == null) return;

        List<Vector2> celdas = piso.Mapa.CentrosTransitables;
        if (celdas == null || celdas.Count == 0) return;
        JugadorCenital jugador = FindAnyObjectByType<JugadorCenital>();
        int n = Random.Range(cantidadGente.x, cantidadGente.y + 1);
        for (int i = 0, intentos = 0; i < n && intentos < 200; intentos++)
        {
            Vector2 c = celdas[Random.Range(0, celdas.Count)];
            if (jugador != null && Vector2.Distance(c, jugador.Centro) < 2f) continue;
            Personajes.Ficha f = Personajes.AlAzar();
            PersonaAmbiente p = PersonaAmbiente.Crear(f, c, piso);
            if (p != null) { gente.Add(p); i++; }
        }
    }

    // ------------------------------------------------------------ escena
    private void Update()
    {
        if (demoledor == null) return;

        // Reinicio del piso: el demoledor vuelve a esperar afuera → vuelve la calma y la gente.
        if (escenaHecha && demoledor.Esperando && Time.time > finEnfoque + 1f)
        {
            escenaHecha = false;
            LlenarPiso();
        }

        if (!escenaHecha && demoledor.Entrando) EmpezarEscena();

        if (escenaHecha && Time.time < finEnfoque)
        {
            CamaraPisoCenital.Enfocar((Vector2)demoledor.transform.position + Vector2.up * 0.6f, 0.72f);
            float k = Mathf.Clamp01((finEnfoque - Time.time) / 0.35f);   // las franjas salen al final
            float entra = Mathf.Clamp01((Time.time - (finEnfoque - duracionEnfoque)) / 0.3f);
            PonerFranjas(Mathf.Min(entra, k));
        }
        else if (escenaHecha && CamaraPisoCenital.Enfocando)
        {
            CamaraPisoCenital.SoltarEnfoque();
            JugadorCenital.EntradaBloqueada = false;
            PonerFranjas(0f);
        }
    }

    private void EmpezarEscena()
    {
        escenaHecha = true;
        finEnfoque = Time.time + duracionEnfoque;
        JugadorCenital.EntradaBloqueada = true;
        if (titulo != null) titulo.text = "¡LLEGÓ EL DEMOLEDOR!";

        Vector2 desde = demoledor.transform.position;
        Vector2 escape = PuntoDeEscape(desde);
        if (gente.Count > 0) GestorAudio.Reproducir(SndExtra.GritoMultitud);   // "¡HAAA!" general y todos corren
        foreach (PersonaAmbiente p in gente) if (p != null) p.Huir(desde, escape);
        gente.Clear();
    }

    /// <summary>Hacia dónde corre la gente: el ascensor si hay, si no el punto transitable más lejano del demoledor.</summary>
    private Vector2 PuntoDeEscape(Vector2 desde)
    {
        MapaCenital mapa = piso.Mapa;
        if (mapa.ascensor != null) return mapa.ascensor.transform.position;
        Vector2 mejor = mapa.Limites.center;
        float lejos = -1f;
        List<Vector2> celdas = mapa.CentrosTransitables;
        for (int i = 0; i < celdas.Count; i += Mathf.Max(1, celdas.Count / 60))
        {
            float d = Vector2.Distance(celdas[i], desde);
            if (d > lejos) { lejos = d; mejor = celdas[i]; }
        }
        return mejor;
    }

    // ------------------------------------------------------------ franjas de cine
    private void CrearFranjas()
    {
        GameObject g = new GameObject("FranjasCine", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        g.transform.SetParent(transform, false);
        canvas = g.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler cs = g.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;

        franjaArriba = Franja("Arriba", 1f);
        franjaAbajo = Franja("Abajo", 0f);

        GameObject t = new GameObject("Titulo", typeof(RectTransform), typeof(Text), typeof(Outline));
        t.transform.SetParent(franjaAbajo, false);
        titulo = t.GetComponent<Text>();
        Font fuente = DemoledorUnifranz.UI.Menus.FabricaUIArcade.Fuente;
        titulo.font = fuente != null ? fuente : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titulo.fontSize = 56;
        titulo.alignment = TextAnchor.MiddleCenter;
        titulo.color = new Color(1f, 0.55f, 0.25f);
        titulo.horizontalOverflow = HorizontalWrapMode.Overflow;
        titulo.raycastTarget = false;
        t.GetComponent<Outline>().effectColor = Color.black;
        t.GetComponent<Outline>().effectDistance = new Vector2(4f, -4f);
        RectTransform rt = (RectTransform)t.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        PonerFranjas(0f);
    }

    private RectTransform Franja(string nombre, float y)
    {
        GameObject g = new GameObject("Franja" + nombre, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(canvas.transform, false);
        Image img = g.GetComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        RectTransform r = (RectTransform)g.transform;
        r.anchorMin = new Vector2(0f, y);
        r.anchorMax = new Vector2(1f, y);
        r.pivot = new Vector2(0.5f, y);
        r.sizeDelta = new Vector2(0f, 150f);
        return r;
    }

    private void PonerFranjas(float k)
    {
        if (franjaArriba == null) return;
        bool ver = k > 0.001f;
        if (canvas.enabled != ver) canvas.enabled = ver;
        float alto = 150f;
        franjaArriba.anchoredPosition = new Vector2(0f, alto * (1f - k));
        franjaAbajo.anchoredPosition = new Vector2(0f, -alto * (1f - k));
    }
}
