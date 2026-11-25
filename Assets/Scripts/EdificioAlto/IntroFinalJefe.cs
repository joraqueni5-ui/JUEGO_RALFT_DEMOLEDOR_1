using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// Cinemática de entrada del NIVEL FINAL, como la previa de una pelea:
///   1) franjas negras de cine; la cámara parte del estudiante en la calle y sube por toda la fachada temblando;
///   2) arriba, el demoledor ruge en la terraza: "¡Esta vez no escapan!";
///   3) pantalla VS: el personaje elegido (con su carrera) contra EL DEMOLEDOR, con sus colores;
///   4) la cámara vuelve al estudiante y "¡A PELEAR!".
/// El juego queda congelado mientras tanto; tocando la pantalla o con cualquier tecla se salta.
/// En multijugador la ve el anfitrión (el invitado espera unos segundos). InstaladorExtras la agrega al nivel final.
/// </summary>
public class IntroFinalJefe : MonoBehaviour
{
    /// <summary>Mientras dura, el muñeco no recibe golpes y el juego está congelado.</summary>
    public static bool EnCurso { get; private set; }

    private Camera cam;
    private CamaraSeguimiento seguimiento;
    private Muneco muneco;
    private DemoledorAutomatico dem;
    private Canvas lienzo;
    private RectTransform franjaArriba, franjaAbajo;
    private Text subtitulo, letrero;
    private Image destello;
    private bool saltar, terminado;
    private Font fuente;

    private IEnumerator Start()
    {
        if (RedJuego.EsInvitadoEnPartida) { Destroy(this); yield break; }
        EnCurso = true;
        yield return null;   // que todo el nivel termine de arrancar
        cam = Camera.main;
        seguimiento = cam != null ? cam.GetComponent<CamaraSeguimiento>() : null;
        muneco = FindAnyObjectByType<Muneco>();
        dem = FindAnyObjectByType<DemoledorAutomatico>();
        if (cam == null || muneco == null || dem == null) { Terminar(); yield break; }
        fuente = FabricaUIArcade.Fuente != null ? FabricaUIArcade.Fuente : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CrearInterfaz();
        yield return Secuencia();
        Terminar();
    }

    private void OnDestroy() { if (EnCurso && !terminado) { EnCurso = false; Time.timeScale = 1f; } }

    private void Update()
    {
        if (!EnCurso || terminado) return;
        Time.timeScale = 0f;   // nada se mueve (ni si alguien abre la pausa y la cierra)
        Keyboard k = Keyboard.current;
        Mouse m = Mouse.current;
        Touchscreen t = Touchscreen.current;
        if ((k != null && k.anyKey.wasPressedThisFrame) || (m != null && m.leftButton.wasPressedThisFrame) ||
            (t != null && t.primaryTouch.press.wasPressedThisFrame))
            saltar = true;
    }

    // ------------------------------------------------------------ la secuencia
    private IEnumerator Secuencia()
    {
        Vector3 inicio = cam.transform.position;
        float yJugador = inicio.y;
        float yDem = dem.transform.position.y + 0.9f;
        bool hayMusica = GestorAudio.ExisteMusica("musica_jefe_final");
        if (hayMusica) GestorAudio.ForzarMusica("musica_jefe_final");

        // 1) Franjas de cine y "NIVEL FINAL".
        letrero.text = "NIVEL FINAL";
        letrero.color = new Color(1f, 0.85f, 0.3f, 0f);
        yield return Durante(0.45f, k =>
        {
            Franjas(k);
            letrero.color = new Color(1f, 0.85f, 0.3f, k);
        });
        if (saltar) yield break;

        // 2) La cámara sube por la fachada, temblando cada vez más.
        Subtitulo("La última batalla: la fachada de Unifranz.");
        GestorAudio.Reproducir(SndExtra.Rafaga, 0.7f);
        yield return Durante(2.3f, k =>
        {
            float y = Mathf.Lerp(yJugador, yDem, Mathf.SmoothStep(0f, 1f, k));
            float temblor = 0.04f + 0.08f * k;
            cam.transform.position = new Vector3(inicio.x + Mathf.Sin(Time.unscaledTime * 47f) * temblor, y, inicio.z);
            letrero.color = new Color(1f, 0.85f, 0.3f, 1f - Mathf.Clamp01((k - 0.6f) / 0.4f));
        });
        if (saltar) yield break;

        // 3) El demoledor ruge en la terraza.
        GestorAudio.Reproducir(SndExtra.RugidoDemoledor);
        Destello(new Color(1f, 0.3f, 0.2f), 0.6f);
        Subtitulo("EL DEMOLEDOR:  ¡Esta vez no escapan, estudiantes! ¡La U es MÍA!");
        yield return Durante(1.6f, k =>
        {
            float f = 0.22f * (1f - k);
            cam.transform.position = new Vector3(inicio.x + Mathf.Sin(Time.unscaledTime * 60f) * f, yDem + Mathf.Cos(Time.unscaledTime * 53f) * f, inicio.z);
        });
        if (saltar) yield break;

        // 4) Pantalla VS.
        Subtitulo("");
        yield return PantallaVS();
        if (saltar) yield break;

        // 5) Vuelve al estudiante: ¡A PELEAR!
        yield return Durante(1f, k =>
        {
            cam.transform.position = new Vector3(inicio.x, Mathf.Lerp(yDem, yJugador, Mathf.SmoothStep(0f, 1f, k)), inicio.z);
        });
        letrero.text = "¡A PELEAR!";
        letrero.fontSize = 120;
        GestorAudio.Reproducir(Snd.MenuConfirmar);
        GestorAudio.Reproducir(SndExtra.Oleada, 0.8f);
        Destello(Color.white, 0.8f);
        yield return Durante(0.9f, k =>
        {
            letrero.color = new Color(1f, 0.85f, 0.3f, 1f);
            letrero.rectTransform.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, Mathf.Clamp01(k * 4f));
        });
        yield return Durante(0.35f, k => { Franjas(1f - k); letrero.color = new Color(1f, 0.85f, 0.3f, 1f - k); });
    }

    private IEnumerator PantallaVS()
    {
        Personajes.Ficha f = Personajes.Actual;
        Color colorJ = FuriaFachada.ColorDe(f != null ? f.id : "");
        RectTransform vs = Panel("VS", lienzo.transform);
        Estirar(vs);

        // Mitad del estudiante (desde la izquierda) y mitad del demoledor (desde la derecha), en diagonal.
        RectTransform izq = Panel("Izquierda", vs);
        Image fondoI = izq.gameObject.AddComponent<Image>();
        fondoI.color = new Color(colorJ.r * 0.55f, colorJ.g * 0.55f, colorJ.b * 0.55f, 0.95f);
        izq.anchorMin = new Vector2(0f, 0f); izq.anchorMax = new Vector2(0.5f, 1f);
        izq.offsetMin = new Vector2(-120f, -200f); izq.offsetMax = new Vector2(60f, 200f);
        izq.localRotation = Quaternion.Euler(0f, 0f, -6f);
        RectTransform der = Panel("Derecha", vs);
        Image fondoD = der.gameObject.AddComponent<Image>();
        fondoD.color = new Color(0.55f, 0.08f, 0.08f, 0.95f);
        der.anchorMin = new Vector2(0.5f, 0f); der.anchorMax = new Vector2(1f, 1f);
        der.offsetMin = new Vector2(-60f, -200f); der.offsetMax = new Vector2(120f, 200f);
        der.localRotation = Quaternion.Euler(0f, 0f, -6f);

        Sprite[] serieJ = f != null ? Personajes.Serie(f, "abajo") : null;
        Image retratoJ = Retrato(vs, serieJ != null ? serieJ[0] : null, new Vector2(-480f, -60f), 560f);
        SpriteRenderer srDem = dem.GetComponentInChildren<SpriteRenderer>();
        Sprite[] golpe = ArteExtra.Serie("Menu/demoledor_golpe_", 6);
        Image retratoD = Retrato(vs, golpe != null ? golpe[0] : srDem != null ? srDem.sprite : null, new Vector2(480f, -40f), 620f);
        retratoD.rectTransform.localScale = new Vector3(-1f, 1f, 1f);   // mirando al estudiante

        Text nombreJ = TextoVS(vs, (f != null ? f.nombre : "ESTUDIANTE").ToUpperInvariant(), 64, Color.white, new Vector2(-480f, -330f));
        Text carreraJ = TextoVS(vs, f != null ? f.carrera.ToUpperInvariant() : "UNIFRANZ", 34, colorJ, new Vector2(-480f, -395f));
        Text nombreD = TextoVS(vs, "EL DEMOLEDOR", 64, Color.white, new Vector2(480f, -330f));
        Text apodoD = TextoVS(vs, "JEFE FINAL  ·  3 FASES", 34, new Color(1f, 0.5f, 0.4f), new Vector2(480f, -395f));
        Text versus = TextoVS(vs, "VS", 220, new Color(1f, 0.85f, 0.25f), new Vector2(0f, 20f));
        versus.enabled = false;

        // Entran desde los costados.
        GestorAudio.Reproducir(Snd.MenuMover, 1f, 0.8f);
        yield return Durante(0.4f, k =>
        {
            float e = 1f - Mathf.Pow(1f - k, 3f);
            izq.anchoredPosition = new Vector2(Mathf.Lerp(-1100f, 0f, e), 0f);
            der.anchoredPosition = new Vector2(Mathf.Lerp(1100f, 0f, e), 0f);
            retratoJ.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-1400f, -480f, e), -60f);
            retratoD.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(1400f, 480f, e), -40f);
        });
        if (saltar) { Destroy(vs.gameObject); yield break; }
        // ¡VS!
        versus.enabled = true;
        GestorAudio.Reproducir(SndExtra.JefeTransforma);
        CamaraSeguimiento.Temblar(0.2f, 0.3f);
        Destello(Color.white, 0.7f);
        yield return Durante(1.5f, k =>
        {
            versus.rectTransform.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(3f, 1f, k / 0.15f) : 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 12f));
            versus.color = Color.Lerp(new Color(1f, 0.85f, 0.25f), Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 14f));
            // Los dos "respiran" (el demoledor golpea).
            retratoJ.rectTransform.localScale = new Vector3(1f, 1f + 0.03f * Mathf.Sin(Time.unscaledTime * 5f), 1f);
            if (golpe != null) retratoD.sprite = golpe[(int)(Time.unscaledTime * 10f) % golpe.Length];
        });
        // Salen.
        yield return Durante(0.3f, k =>
        {
            izq.anchoredPosition = new Vector2(Mathf.Lerp(0f, -1300f, k), 0f);
            der.anchoredPosition = new Vector2(Mathf.Lerp(0f, 1300f, k), 0f);
            retratoJ.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-480f, -1500f, k), -60f);
            retratoD.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(480f, 1500f, k), -40f);
            Color c = versus.color; c.a = 1f - k; versus.color = c;
            nombreJ.enabled = carreraJ.enabled = nombreD.enabled = apodoD.enabled = k < 0.3f;
        });
        Destroy(vs.gameObject);
    }

    // ------------------------------------------------------------ fin
    private void Terminar()
    {
        if (terminado) return;
        terminado = true;
        EnCurso = false;
        Time.timeScale = 1f;
        if (seguimiento != null) seguimiento.enabled = true;
        if (GestorAudio.ExisteMusica("musica_jefe_final")) GestorAudio.ForzarMusica("");
        if (lienzo != null) Destroy(lienzo.gameObject);
        Destroy(this);
    }

    private IEnumerator Durante(float segundos, System.Action<float> paso)
    {
        float t = 0f;
        while (t < segundos && !saltar)
        {
            t += Time.unscaledDeltaTime;
            paso(Mathf.Clamp01(t / segundos));
            if (destello != null && destello.color.a > 0f)
            {
                Color c = destello.color;
                c.a = Mathf.MoveTowards(c.a, 0f, Time.unscaledDeltaTime * 2.2f);
                destello.color = c;
            }
            yield return null;
        }
        if (!saltar) paso(1f);
    }

    // ------------------------------------------------------------ interfaz
    private void CrearInterfaz()
    {
        if (seguimiento != null) seguimiento.enabled = false;
        GameObject g = new GameObject("IntroFinalJefe", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        lienzo = g.GetComponent<Canvas>();
        lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
        lienzo.sortingOrder = 300;
        CanvasScaler cs = g.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;

        franjaArriba = Franja("Arriba", new Vector2(0f, 1f));
        franjaAbajo = Franja("Abajo", new Vector2(0f, 0f));
        Franjas(0f);

        subtitulo = TextoVS(franjaAbajo, "", 40, Color.white, Vector2.zero);
        subtitulo.rectTransform.anchorMin = subtitulo.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        letrero = TextoVS(lienzo.transform, "", 96, new Color(1f, 0.85f, 0.3f), new Vector2(0f, 300f));

        destello = new GameObject("Destello", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        destello.transform.SetParent(lienzo.transform, false);
        Estirar(destello.rectTransform);
        destello.color = new Color(1f, 1f, 1f, 0f);
        destello.raycastTarget = false;

        Text omitir = TextoVS(lienzo.transform, Application.isMobilePlatform ? "Toca para saltar" : "Cualquier tecla para saltar", 26,
            new Color(1f, 1f, 1f, 0.6f), Vector2.zero);
        RectTransform ro = omitir.rectTransform;
        ro.anchorMin = ro.anchorMax = new Vector2(1f, 1f);
        ro.anchoredPosition = new Vector2(-260f, -40f);
    }

    private RectTransform Franja(string nombre, Vector2 ancla)
    {
        GameObject g = new GameObject("Franja" + nombre, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(lienzo.transform, false);
        g.GetComponent<Image>().color = Color.black;
        g.GetComponent<Image>().raycastTarget = false;
        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = new Vector2(0f, ancla.y);
        rt.anchorMax = new Vector2(1f, ancla.y);
        rt.pivot = new Vector2(0.5f, ancla.y);
        rt.sizeDelta = new Vector2(0f, 150f);
        return rt;
    }

    private void Franjas(float k)
    {
        float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(k), 2f);
        if (franjaArriba != null) franjaArriba.anchoredPosition = new Vector2(0f, Mathf.Lerp(150f, 0f, e));
        if (franjaAbajo != null) franjaAbajo.anchoredPosition = new Vector2(0f, Mathf.Lerp(-150f, 0f, e));
    }

    private void Subtitulo(string s) { if (subtitulo != null) subtitulo.text = s; }

    private void Destello(Color c, float alfa) { if (destello != null) destello.color = new Color(c.r, c.g, c.b, alfa); }

    private static RectTransform Panel(string nombre, Transform padre)
    {
        GameObject g = new GameObject(nombre, typeof(RectTransform));
        g.transform.SetParent(padre, false);
        return (RectTransform)g.transform;
    }

    private static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static Image Retrato(Transform padre, Sprite s, Vector2 pos, float alto)
    {
        GameObject g = new GameObject("Retrato", typeof(RectTransform), typeof(Image));
        g.transform.SetParent(padre, false);
        Image im = g.GetComponent<Image>();
        im.sprite = s;
        im.preserveAspect = true;
        im.raycastTarget = false;
        if (s == null) im.color = new Color(1f, 1f, 1f, 0f);
        RectTransform rt = im.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        float ancho = s != null ? alto * s.rect.width / Mathf.Max(1f, s.rect.height) : alto * 0.5f;
        rt.sizeDelta = new Vector2(ancho, alto);
        rt.anchoredPosition = pos;
        return im;
    }

    private Text TextoVS(Transform padre, string texto, int tam, Color c, Vector2 pos)
    {
        GameObject g = new GameObject("Texto", typeof(RectTransform), typeof(Text), typeof(Outline));
        g.transform.SetParent(padre, false);
        Text t = g.GetComponent<Text>();
        t.font = fuente;
        t.text = texto;
        t.fontSize = tam;
        t.color = c;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        Outline o = g.GetComponent<Outline>();
        o.effectColor = Color.black;
        o.effectDistance = new Vector2(4f, -4f);
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1600f, tam * 1.4f);
        rt.anchoredPosition = pos;
        return t;
    }
}
