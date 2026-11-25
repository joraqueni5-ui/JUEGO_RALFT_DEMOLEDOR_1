using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// La FURIA DEL ESTUDIANTE también en el nivel final (la fachada). Cada ventana reparada llena la barra; llena, se activa
/// con el botón FURIA (celular), tocando la barra o con K + L (PC). Durante unos segundos:
///   - "¡FURIA DE ODONTOLOGÍA!" (la carrera del personaje elegido), destello y una onda que hace polvo todos los
///     escombros que vienen cayendo; el demoledor se asusta.
///   - aura del color de la carrera con los símbolos de su materia girando alrededor;
///   - no le hacen daño, camina y trepa más rápido y cada martillazo arregla de más (la ventana y las de al lado).
/// InstaladorExtras lo agrega en el nivel final.
/// </summary>
public class FuriaFachada : MonoBehaviour
{
    public static FuriaFachada Instancia { get; private set; }
    /// <summary>Está activa (la usa el muñeco: sin daño, martillazo más fuerte).</summary>
    public static bool Activa { get { return Instancia != null && Instancia.activa; } }

    public float duracion = 9f;
    public float porReparacion = 9f;

    public float Carga { get { return carga; } }
    public bool Lista { get { return carga >= 100f && !activa; } }
    public Color ColorFuria { get { return color; } }

    private Muneco muneco;
    private GestorJuego gestor;
    private Personajes.Ficha ficha;
    private Color color = new Color(0.3f, 0.9f, 1f);
    private float carga, finFuria;
    private bool activa;
    private float velBase, trepaBase;

    private SpriteRenderer aura;
    private Sprite[] auraCuadros;
    private readonly List<SpriteRenderer> simbolos = new List<SpriteRenderer>();

    // Interfaz.
    private RectTransform barra;
    private Image relleno, destello;
    private Text etiqueta, aviso, titulo;
    private float finTitulo;

    private void Awake() { Instancia = this; }
    private void OnDestroy() { if (Instancia == this) Instancia = null; }

    private void Start()
    {
        if (RedJuego.EsInvitadoEnPartida) { Destroy(this); return; }   // el invitado no maneja el muñeco de la fachada
        muneco = FindAnyObjectByType<Muneco>();
        gestor = FindAnyObjectByType<GestorJuego>();
        ficha = Personajes.Actual;
        color = ColorDe(ficha != null ? ficha.id : "");
        if (muneco != null)
        {
            velBase = muneco.velocidad;
            trepaBase = muneco.velocidadTrepar;
            auraCuadros = ArteExtra.Serie("Ira/aura_", 4);
            aura = Provisional.Crear(muneco.transform, "AuraFuria", auraCuadros != null ? auraCuadros[0] : Provisional.Circulo, color, 4);
            aura.enabled = false;
            List<Sprite> lista = new List<Sprite>();
            string id = ficha != null ? ficha.id : "sistemas";
            for (int i = 0; i < 6; i++) { Sprite s = ArteExtra.Uno("Ira/simbolo_" + id + "_" + i); if (s != null) lista.Add(s); }
            if (lista.Count == 0) for (int i = 0; i < 3; i++) { Sprite s = ArteExtra.Uno("Ira/simbolo_sistemas_" + i); if (s != null) lista.Add(s); }
            for (int i = 0; i < 6 && lista.Count > 0; i++)
            {
                SpriteRenderer s = Provisional.Crear(muneco.transform, "SimboloFuria" + i, lista[i % lista.Count], color, 40);
                s.enabled = false;
                simbolos.Add(s);
            }
        }
        CrearInterfaz();
    }

    public static Color ColorDe(string id)
    {
        switch (id)
        {
            case "medicina": return new Color(1f, 0.35f, 0.45f);
            case "administracion": return new Color(0.35f, 1f, 0.5f);
            case "gastronomia": return new Color(1f, 0.6f, 0.15f);
            case "derecho": return new Color(1f, 0.85f, 0.3f);
            case "arquitectura": return new Color(0.45f, 0.6f, 1f);
            case "odontologia": return new Color(0.6f, 1f, 0.95f);
            case "comunicacion": return new Color(1f, 0.4f, 0.9f);
            default: return new Color(0.3f, 0.9f, 1f);
        }
    }

    /// <summary>El muñeco reparó una ventana.</summary>
    public static void AlReparar()
    {
        if (Instancia == null || Instancia.activa) return;
        Instancia.carga = Mathf.Min(100f, Instancia.carga + Instancia.porReparacion);
    }

    private bool Pausado { get { return gestor != null && (gestor.EstaEnPausa || gestor.PartidaTerminada); } }

    private void Update()
    {
        if (muneco == null) return;
        if (!Pausado && !activa && carga >= 100f)
        {
            bool tactil = ControlesTactiles.FuriaPulsada();
            Keyboard k = Keyboard.current;
            bool teclas = k != null && k.kKey.isPressed && k.lKey.isPressed;
            if (tactil || teclas) Activar();
        }
        else ControlesTactiles.FuriaPulsada();   // un toque cuando no está lista no se guarda

        if (activa)
        {
            if (Time.time >= finFuria || Pausado && gestor.PartidaTerminada) Terminar();
            else AnimarAura();
        }
        ActualizarInterfaz();
    }

    public void Activar()
    {
        if (activa || carga < 100f || muneco == null || Pausado) return;
        activa = true;
        finFuria = Time.time + duracion;
        muneco.velocidad = velBase * 1.35f;
        muneco.velocidadTrepar = trepaBase * 1.45f;
        GestorAudio.Reproducir(SndExtra.JefeTransforma, 0.9f, 1.2f);
        GestorAudio.Reproducir(Snd.PoderRecoger);
        CamaraSeguimiento.Temblar(0.18f, 0.4f);
        string carrera = ficha != null ? ficha.carrera.ToUpperInvariant() : "LA U";
        if (titulo != null) { titulo.text = "¡FURIA DE " + carrera + "!"; titulo.enabled = true; finTitulo = Time.unscaledTime + 1.8f; }
        if (destello != null) destello.color = new Color(color.r, color.g, color.b, 0.75f);

        // La onda hace polvo todos los escombros que vienen cayendo.
        Sprite[] humo = ArteExtra.Serie("Mejoras/humo_", 4);
        foreach (EscombroCayendo e in FindObjectsByType<EscombroCayendo>(FindObjectsInactive.Exclude))
        {
            if (e == null) continue;
            if (humo != null) ParticulaSimple.Crear(humo, e.transform.position, Vector2.up * 0.5f, 0.6f, 60, new Color(1f, 1f, 1f, 0.8f), 0.4f, 1f);
            Destroy(e.gameObject);
        }
        SpriteRenderer onda = EfectoSprite.Reproducir(ArteExtra.Serie("Efectos/onda_empujon_", 4), muneco.CentroCuerpo, 60, 3.2f, 12f);
        if (onda != null) onda.color = color;
        DemoledorAutomatico dem = FindAnyObjectByType<DemoledorAutomatico>();
        if (dem != null) Burbuja.Decir(dem.transform, Random.value < 0.5f ? "¡¿QUÉ ES ESO?!" : "¡NO, ESA FURIA NO!", Burbuja.Voz.Villano, 1.6f);
    }

    private void Terminar()
    {
        activa = false;
        carga = 0f;
        if (muneco != null) { muneco.velocidad = velBase; muneco.velocidadTrepar = trepaBase; }
        if (aura != null) aura.enabled = false;
        foreach (SpriteRenderer s in simbolos) if (s != null) s.enabled = false;
    }

    private void AnimarAura()
    {
        float t = Time.time;
        if (aura != null)
        {
            aura.enabled = true;
            if (auraCuadros != null) aura.sprite = auraCuadros[(int)(t * 12f) % auraCuadros.Length];
            float e = 1.25f + 0.08f * Mathf.Sin(t * 10f);
            float tam = aura.sprite != null ? Mathf.Max(0.01f, aura.sprite.bounds.size.y) : 1f;
            aura.transform.localScale = new Vector3(e * 1.4f / tam, e * 1.6f / tam, 1f);
            aura.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            float queda = finFuria - Time.time;
            float a = queda < 2f ? (Mathf.FloorToInt(t * 10f) % 2 == 0 ? 0.35f : 0.75f) : 0.7f;   // parpadea cuando se acaba
            aura.color = new Color(color.r, color.g, color.b, a);
        }
        for (int i = 0; i < simbolos.Count; i++)
        {
            SpriteRenderer s = simbolos[i];
            if (s == null) continue;
            s.enabled = true;
            float ang = t * 2.4f + i * Mathf.PI * 2f / simbolos.Count;
            s.transform.localPosition = new Vector3(Mathf.Cos(ang) * 0.75f, 0.55f + Mathf.Sin(ang) * 0.5f, 0f);
            float tam = s.sprite != null ? Mathf.Max(0.01f, s.sprite.bounds.size.y) : 1f;
            s.transform.localScale = Vector3.one * (0.28f / tam);
            s.sortingOrder = Mathf.Sin(ang) > 0f ? 3 : 40;   // pasan por detrás y por delante
        }
    }

    // ------------------------------------------------------------ interfaz
    private void CrearInterfaz()
    {
        GameObject g = new GameObject("BarraFuriaFachada", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        g.transform.SetParent(transform, false);
        Canvas canvas = g.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        CanvasScaler cs = g.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;
        Font fuente = DemoledorUnifranz.UI.Menus.FabricaUIArcade.Fuente;
        if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Image fondo = Nueva("Barra", canvas.transform, ArteExtra.Uno("Tema/panel_serio"), Color.white);
        fondo.type = Image.Type.Sliced;
        fondo.pixelsPerUnitMultiplier = 0.25f;
        barra = fondo.rectTransform;
        barra.anchorMin = barra.anchorMax = new Vector2(0.5f, 0f);
        barra.pivot = new Vector2(0.5f, 0f);
        barra.anchoredPosition = new Vector2(0f, 26f);
        barra.sizeDelta = new Vector2(560f, 46f);
        fondo.raycastTarget = true;
        Button b = fondo.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(Activar);

        relleno = Nueva("Relleno", barra, null, color);
        RectTransform rr = relleno.rectTransform;
        rr.anchorMin = new Vector2(0f, 0f);
        rr.anchorMax = new Vector2(0f, 1f);
        rr.pivot = new Vector2(0f, 0.5f);
        rr.offsetMin = new Vector2(10f, 10f);
        rr.offsetMax = new Vector2(10f, -10f);

        etiqueta = Texto("Etiqueta", barra, fuente, 20, Color.white);
        etiqueta.rectTransform.anchorMin = etiqueta.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        etiqueta.rectTransform.anchoredPosition = new Vector2(0f, 16f);
        etiqueta.text = "FURIA DE " + (ficha != null ? ficha.carrera.ToUpperInvariant() : "LA U") + (Application.isMobilePlatform ? "" : "   (K + L)");

        aviso = Texto("Aviso", barra, fuente, 34, Color.white);
        aviso.rectTransform.anchorMin = aviso.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        aviso.rectTransform.anchoredPosition = new Vector2(0f, 58f);
        aviso.text = "¡FURIA LISTA!";
        aviso.enabled = false;

        destello = Nueva("Destello", canvas.transform, null, new Color(1f, 1f, 1f, 0f));
        destello.rectTransform.anchorMin = Vector2.zero;
        destello.rectTransform.anchorMax = Vector2.one;
        destello.rectTransform.offsetMin = destello.rectTransform.offsetMax = Vector2.zero;
        destello.raycastTarget = false;

        titulo = Texto("Titulo", canvas.transform, fuente, 84, color);
        titulo.rectTransform.anchorMin = titulo.rectTransform.anchorMax = new Vector2(0.5f, 0.68f);
        titulo.enabled = false;
    }

    private static Image Nueva(string nombre, Transform padre, Sprite sprite, Color c)
    {
        GameObject g = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(padre, false);
        Image im = g.GetComponent<Image>();
        im.sprite = sprite;
        im.color = c;
        im.raycastTarget = false;
        return im;
    }

    private static Text Texto(string nombre, Transform padre, Font fuente, int tam, Color c)
    {
        GameObject g = new GameObject(nombre, typeof(RectTransform), typeof(Text), typeof(Outline));
        g.transform.SetParent(padre, false);
        Text t = g.GetComponent<Text>();
        t.font = fuente;
        t.fontSize = tam;
        t.color = c;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        g.GetComponent<Outline>().effectColor = Color.black;
        g.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);
        t.rectTransform.sizeDelta = new Vector2(1400f, 100f);
        return t;
    }

    private void ActualizarInterfaz()
    {
        if (barra == null) return;
        bool ocultar = gestor != null && gestor.PartidaTerminada;
        if (barra.gameObject.activeSelf == ocultar) barra.gameObject.SetActive(!ocultar);
        float k = activa ? Mathf.Clamp01((finFuria - Time.time) / duracion) : carga / 100f;
        float ancho = (barra.sizeDelta.x - 20f) * k;
        if (!Mathf.Approximately(relleno.rectTransform.sizeDelta.x, ancho)) relleno.rectTransform.sizeDelta = new Vector2(ancho, 0f);
        bool lista = Lista;
        relleno.color = lista ? Color.Lerp(color, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f)) : color;
        if (aviso.enabled != lista) aviso.enabled = lista;
        if (lista) aviso.rectTransform.localScale = Vector3.one * (1f + 0.08f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)));
        if (titulo.enabled)
        {
            float q = finTitulo - Time.unscaledTime;
            if (q <= 0f) titulo.enabled = false;
            else titulo.rectTransform.localScale = Vector3.one * (1f + 0.25f * Mathf.Clamp01((q - 1.5f) / 0.3f));
        }
        if (destello.color.a > 0.001f)
            destello.color = new Color(destello.color.r, destello.color.g, destello.color.b, Mathf.MoveTowards(destello.color.a, 0f, Time.unscaledDeltaTime * 2.5f));
    }
}
