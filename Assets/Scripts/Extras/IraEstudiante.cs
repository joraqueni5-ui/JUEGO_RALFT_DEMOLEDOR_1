using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// FURIA DEL ESTUDIANTE. Cada reparación, bloqueo y mini villano vencido llena una barra. Llena, aparece
/// "¡PULSA K + L!" (en el celular, tocar la barra). Al activarla: la cámara se acerca, el tiempo se frena, destello,
/// "¡FURIA DE SISTEMAS!" y el personaje se transforma con un aura del color de su carrera y símbolos de su materia
/// girando a su alrededor (código en Sistemas, cruces y corazones en Medicina, $ y % en Administración...).
/// Mientras dura (la barra va bajando): corre más rápido, martilla mucho más rápido y lejos, cada martillazo deja el
/// objeto como nuevo y lanza una onda que ahuyenta a los mini villanos y aturde al demoledor, y no recibe daño.
/// InstaladorExtras lo agrega al jugador de cada piso.
/// </summary>
public class IraEstudiante : MonoBehaviour
{
    [Tooltip("Segundos que dura la furia.")]
    public float duracion = 10f;
    [Tooltip("Carga por cada estado reparado, por bloqueo y por mini villano vencido.")]
    public float cargaReparar = 6f, cargaBloqueo = 15f, cargaMini = 10f;

    private static readonly List<IraEstudiante> todas = new List<IraEstudiante>();
    /// <summary>La furia del jugador de este equipo está activa (después de la transformación).</summary>
    public static bool Activa
    {
        get
        {
            foreach (IraEstudiante i in todas) if (i != null && i.EsLocal && i.activa) return true;
            return false;
        }
    }

    /// <summary>Modo espejo (invitado en multijugador): solo muestra la barra y el aviso; la furia la calcula el anfitrión.</summary>
    [System.NonSerialized] public bool espejo;
    [System.NonSerialized] public Personajes.Ficha fichaEspejo;

    private Personajes.Ficha fichaUsada;
    public bool EstaActiva { get { return activa; } }

    // Estado de la furia del jugador de ESTE equipo (para el botón FURIA de los controles táctiles).
    private static IraEstudiante LocalUnica
    {
        get
        {
            foreach (IraEstudiante i in todas) if (i != null && i.EsLocal && i.isActiveAndEnabled) return i;
            return null;
        }
    }
    public static float CargaLocal { get { IraEstudiante i = LocalUnica; return i != null ? i.carga : 0f; } }
    public static bool ListaLocal { get { IraEstudiante i = LocalUnica; return i != null && i.carga >= 100f && !i.activa && !i.transformando; } }
    public static bool ActivaLocal { get { IraEstudiante i = LocalUnica; return i != null && (i.activa || i.transformando); } }
    public static Color ColorLocal { get { IraEstudiante i = LocalUnica; return i != null ? i.color : new Color(0.3f, 0.9f, 1f); } }
    public bool Transformando { get { return transformando; } }
    public float Carga { get { return carga; } }
    /// <summary>Esta furia es la del jugador de ESTE equipo (tiene barra y aviso en pantalla).</summary>
    public bool EsLocal { get { return espejo || jugador == null || !jugador.controlRemoto; } }

    private JugadorCenital jugador;
    private SpriteRenderer dibujoJugador, aura;
    private readonly List<SpriteRenderer> simbolos = new List<SpriteRenderer>();
    private Sprite[] auraCuadros, figuras, rayos;
    private SpriteRenderer rayo;
    private Color color;
    private string nombreFuria;
    private float carga, finIra, proximaOnda;
    private bool activa, transformando, avisado;
    private GestorJuego gestor;

    // Interfaz
    private Canvas canvas;
    private RectTransform barra;
    private Image relleno, fondoBarra, destello;
    private Text etiqueta, aviso, titulo;

    // Aviso GRANDE y animado en el centro: "¡DESATA TU FURIA!"  [K] + [L]
    private RectTransform grupoAviso, teclaK, teclaL;
    private Image brilloAviso, caraK, caraL;
    private Text textoAvisoTitulo, textoAvisoSub, letraK, letraL, mas;
    private Sprite spTecla, spTeclaPresionada;
    private float inicioAviso = -1f;
    private Image iconoCarrera;
    private Image iconoFuria;

    // ------------------------------------------------------------ API
    public void Sumar(float puntos)
    {
        if (activa || transformando || espejo) return;
        carga = Mathf.Min(100f, carga + puntos);
    }

    public void SumarReparo(int estados) { Sumar(cargaReparar * Mathf.Max(1, estados)); }
    public void SumarBloqueo() { Sumar(cargaBloqueo); }

    /// <summary>Un mini villano vencido cerca de 'donde': carga la furia del jugador más cercano.</summary>
    public static void MiniVencido(Vector2 donde)
    {
        JugadorCenital j = JugadorCenital.MasCercano(donde);
        if (j != null && j.ira != null) j.ira.Sumar(j.ira.cargaMini);
    }

    /// <summary>Martillazo durante la furia: onda que ahuyenta minis y aturde a los demoledores cercanos.</summary>
    public void MartillazoEn(Vector2 centro)
    {
        if (!activa || Time.time < proximaOnda) return;
        proximaOnda = Time.time + 0.45f;
        Onda(centro, 2.4f, false);
    }

    /// <summary>Invitado (espejo): el anfitrión manda cómo va su furia.</summary>
    public void FijarEspejo(float nuevaCarga, bool nuevaActiva, bool nuevoTransformando)
    {
        if (!espejo) return;
        carga = nuevaCarga;
        if (nuevoTransformando && !transformando) StartCoroutine(PresentacionEspejo());
        transformando = nuevoTransformando;
        activa = nuevaActiva;
    }

    /// <summary>En el invitado: la cámara se acerca, destello y título mientras el anfitrión lo transforma.</summary>
    private IEnumerator PresentacionEspejo()
    {
        avisado = false;
        GestorAudio.Reproducir(SndExtra.IraActivar);
        if (titulo != null) { titulo.text = nombreFuria; titulo.color = color; }
        float inicio = Time.unscaledTime;
        const float dur = 1.5f;
        while (Time.unscaledTime - inicio < dur)
        {
            float k = (Time.unscaledTime - inicio) / dur;
            Transform foco = CamaraPisoCenital.ObjetivoExterno;
            if (foco != null) CamaraPisoCenital.Enfocar((Vector2)foco.position + Vector2.up * 0.6f, Mathf.Lerp(0.8f, 0.5f, Mathf.Clamp01(k * 3f)));
            if (destello != null) destello.color = new Color(1f, 1f, 1f, k < 0.6f ? 0f : Mathf.Clamp01(1f - (k - 0.6f) * 2.5f) * 0.9f);
            if (titulo != null)
            {
                titulo.enabled = true;
                float e = k < 0.15f ? Mathf.Lerp(2.2f, 1f, k / 0.15f) : 1f + 0.03f * Mathf.Sin(Time.unscaledTime * 20f);
                titulo.rectTransform.localScale = new Vector3(e, e, 1f);
            }
            yield return null;
        }
        if (destello != null) destello.color = new Color(1f, 1f, 1f, 0f);
        if (titulo != null) titulo.enabled = false;
        CamaraPisoCenital.SoltarEnfoque();
    }

    // ------------------------------------------------------------ ciclo
    private void Awake()
    {
        todas.Add(this);
        jugador = GetComponent<JugadorCenital>();
        if (jugador != null) jugador.ira = this;
    }

    private void OnDestroy()
    {
        todas.Remove(this);
        if (transformando && Time.timeScale < 1f && Time.timeScale > 0f) Time.timeScale = 1f;
        GestorAudio.Bucle("ira", SndExtra.IraBucle, false);
    }

    private void Start()
    {
        gestor = FindAnyObjectByType<GestorJuego>();
        dibujoJugador = jugador != null ? GetComponentInChildren<SpriteRenderer>() : null;
        Personajes.Ficha f = espejo ? (fichaEspejo ?? Personajes.Actual) : jugador != null ? jugador.Ficha : Personajes.Actual;
        fichaUsada = f;
        color = ColorDe(f.id);
        nombreFuria = "¡FURIA DE " + f.carrera + "!";

        auraCuadros = ArteExtra.Serie("Ira/aura_", 4);
        rayos = ArteExtra.Serie("Ira/rayo_", 3);
        List<Sprite> lista = new List<Sprite>();
        for (int i = 0; i < 6; i++) { Sprite s = ArteExtra.Uno("Ira/simbolo_" + f.id + "_" + i); if (s != null) lista.Add(s); }
        if (lista.Count == 0) for (int i = 0; i < 3; i++) { Sprite s = ArteExtra.Uno("Ira/simbolo_sistemas_" + i); if (s != null) lista.Add(s); }
        figuras = lista.ToArray();

        if (espejo) { CrearInterfaz(); return; }   // el invitado solo ve su barra: el aura la dibuja el anfitrión

        aura = Provisional.Crear(transform, "AuraFuria", auraCuadros != null ? auraCuadros[0] : Provisional.Circulo, color, 0);
        aura.enabled = false;
        for (int i = 0; i < 7 && figuras.Length > 0; i++)
        {
            SpriteRenderer s = Provisional.Crear(transform, "Simbolo" + i, figuras[i % figuras.Length], color, 0);
            s.enabled = false;
            simbolos.Add(s);
        }
        rayo = Provisional.Crear(transform, "Rayo", rayos != null ? rayos[0] : null, Color.white, 0);
        rayo.enabled = false;
        if (EsLocal) CrearInterfaz();   // el jugador remoto no tiene barra en esta pantalla
    }

    private static Color ColorDe(string id)
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

    private void Update()
    {
        bool pausa = gestor != null && (gestor.EstaEnPausa || gestor.PartidaTerminada);
        if (EsLocal && !activa && !transformando && carga >= 100f && !pausa && !JugadorCenital.EntradaBloqueada)
        {
            if (!avisado) { avisado = true; GestorAudio.Reproducir(SndExtra.IraLista); }
            Keyboard k = Keyboard.current;
            bool tactil = ControlesTactiles.FuriaPulsada();
            if (tactil || (k != null && ((k[Key.K].isPressed && k[Key.L].wasPressedThisFrame) || (k[Key.L].isPressed && k[Key.K].wasPressedThisFrame))))
                Activar();
        }
        else if (EsLocal) ControlesTactiles.FuriaPulsada();   // un toque cuando no está lista no se guarda para después

        if (espejo) { ActualizarInterfaz(); return; }

        if (activa)
        {
            carga = Mathf.Clamp01((finIra - Time.time) / duracion) * 100f;
            if (Time.time >= finIra) Terminar();
        }
        ActualizarVisual();
        ActualizarInterfaz();
    }

    /// <summary>También se activa tocando la barra (celular).</summary>
    public void Activar()
    {
        if (activa || transformando || carga < 100f) return;
        if (espejo) { RedJuego.EnviarIra(); avisado = false; return; }   // el anfitrión hace la transformación
        StartCoroutine(Transformacion());
    }

    /// <summary>El jugador remoto pidió su furia (K + L en el otro equipo).</summary>
    public void ActivarRemoto()
    {
        if (activa || transformando || carga < 100f) return;
        StartCoroutine(Transformacion());
    }

    private IEnumerator Transformacion()
    {
        transformando = true;
        avisado = false;
        Progreso.Sumar("furias");
        JugadorCenital.EntradaBloqueada = true;
        GestorAudio.Reproducir(SndExtra.IraActivar);
        CamaraPisoCenital.Temblar(0.15f, 1.2f);
        if (titulo != null) { titulo.text = nombreFuria; titulo.color = color; }

        float inicio = Time.unscaledTime;
        const float dur = 1.5f;
        if (Time.timeScale > 0f) Time.timeScale = 0.25f;   // cámara lenta mientras se transforma
        while (Time.unscaledTime - inicio < dur)
        {
            float k = (Time.unscaledTime - inicio) / dur;
            if (EsLocal) CamaraPisoCenital.Enfocar(jugador.Centro + Vector2.up * 0.3f, Mathf.Lerp(0.8f, 0.5f, Mathf.Clamp01(k * 3f)));
            if (destello != null) destello.color = new Color(1f, 1f, 1f, k < 0.6f ? 0f : Mathf.Clamp01(1f - (k - 0.6f) * 2.5f) * 0.9f);
            if (titulo != null)
            {
                titulo.enabled = true;
                float e = k < 0.15f ? Mathf.Lerp(2.2f, 1f, k / 0.15f) : 1f + 0.03f * Mathf.Sin(Time.unscaledTime * 20f);
                titulo.rectTransform.localScale = new Vector3(e, e, 1f);
            }
            // El aura crece con pulsos durante la transformación.
            aura.enabled = true;
            float crecer = Mathf.Clamp01(k * 1.6f) * (1f + 0.15f * Mathf.Sin(Time.unscaledTime * 30f));
            ColocarAura(crecer * 1.25f, 0.9f);
            yield return null;
        }
        if (destello != null) destello.color = new Color(1f, 1f, 1f, 0f);
        if (titulo != null) titulo.enabled = false;
        if (gestor == null || !gestor.EstaEnPausa) Time.timeScale = 1f;
        if (EsLocal) CamaraPisoCenital.SoltarEnfoque();
        JugadorCenital.EntradaBloqueada = false;

        transformando = false;
        activa = true;
        finIra = Time.time + duracion;
        GestorAudio.Bucle("ira", SndExtra.IraBucle, true);
        Onda(jugador.Centro, 3.2f, true);
        ComentariosJugador.Evento(ComentariosJugador.Tipo.Poder, jugador);
    }

    private void Terminar()
    {
        activa = false;
        carga = 0f;
        GestorAudio.Bucle("ira", SndExtra.IraBucle, false);
        if (dibujoJugador != null) dibujoJugador.color = Color.white;
        aura.enabled = false;
        foreach (SpriteRenderer s in simbolos) s.enabled = false;
        rayo.enabled = false;
    }

    /// <summary>Onda de energía: vence a los mini villanos cercanos y aturde al demoledor.</summary>
    private void Onda(Vector2 centro, float radio, bool grande)
    {
        GestorAudio.Reproducir(SndExtra.IraOnda, grande ? 1f : 0.6f);
        CamaraPisoCenital.Temblar(grande ? 0.14f : 0.05f, grande ? 0.35f : 0.12f);
        SpriteRenderer o = EfectoSprite.Reproducir(ArteExtra.Serie("Efectos/onda_empujon_", 4), centro, 460, grande ? 3f : 1.8f, 14f);
        if (o != null) o.color = color;
        GestorMiniVillanos.VencerEnRadio(centro, radio);
        foreach (DemoledorCenital dem in DemoledorCenital.Todos)
            if (dem != null && Vector2.Distance(dem.transform.position, centro) <= radio + 0.6f) dem.Aturdir(grande ? 2.5f : 1.2f);
    }

    // ------------------------------------------------------------ visual
    private void ColocarAura(float escala, float alfa)
    {
        int orden = Provisional.OrdenY(transform.position.y);
        if (auraCuadros != null) aura.sprite = auraCuadros[(int)(Time.unscaledTime * 12f) % auraCuadros.Length];
        aura.transform.localPosition = new Vector3(0f, -0.15f, 0f);
        aura.transform.localScale = new Vector3(escala, escala, 1f);
        aura.sortingOrder = orden - 1;
        Color c = color;
        c.a = alfa * (0.55f + 0.2f * Mathf.Sin(Time.unscaledTime * 9f));
        aura.color = c;
    }

    private void ActualizarVisual()
    {
        if (!activa || espejo || aura == null) return;
        float resto = Mathf.Clamp01((finIra - Time.time) / duracion);
        bool parpadeoFinal = resto < 0.2f && (int)(Time.time * 10f) % 2 == 0;
        ColocarAura(1.15f + 0.05f * Mathf.Sin(Time.time * 6f), parpadeoFinal ? 0.3f : 0.9f);
        if (dibujoJugador != null) dibujoJugador.color = Color.Lerp(Color.white, color, 0.25f + 0.15f * Mathf.Sin(Time.time * 8f));

        int orden = Provisional.OrdenY(transform.position.y);
        for (int i = 0; i < simbolos.Count; i++)
        {
            SpriteRenderer s = simbolos[i];
            s.enabled = true;
            float ang = Time.time * 2.2f + i * Mathf.PI * 2f / simbolos.Count;
            float y = 0.75f + Mathf.Sin(ang) * 0.35f + Mathf.Sin(Time.time * 3f + i) * 0.08f;
            s.transform.localPosition = new Vector3(Mathf.Cos(ang) * 0.85f, y, 0f);
            float e = 0.55f + 0.1f * Mathf.Sin(Time.time * 5f + i);
            s.transform.localScale = new Vector3(e, e, 1f);
            s.sortingOrder = Mathf.Sin(ang) > 0f ? orden - 2 : orden + 2;   // pasan por detrás y por delante
            Color c = color; c.a = parpadeoFinal ? 0.4f : 1f;
            s.color = c;
        }
        // Chispas eléctricas de vez en cuando.
        rayo.enabled = rayos != null && Random.value < 0.25f;
        if (rayo.enabled)
        {
            rayo.sprite = rayos[Random.Range(0, rayos.Length)];
            rayo.transform.localPosition = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.3f, 1.3f), 0f);
            rayo.sortingOrder = orden + 3;
            rayo.color = Color.Lerp(Color.white, color, 0.4f);
        }
    }

    // ------------------------------------------------------------ interfaz
    private void CrearInterfaz()
    {
        GameObject g = new GameObject("BarraFuria", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        g.transform.SetParent(transform, false);
        canvas = g.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        CanvasScaler cs = g.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;
        Font fuente = DemoledorUnifranz.UI.Menus.FabricaUIArcade.Fuente;
        if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        fondoBarra = NuevaImagen("Barra", canvas.transform, ArteExtra.Uno("Tema/panel_serio"), Color.white);
        fondoBarra.type = Image.Type.Sliced;
        fondoBarra.pixelsPerUnitMultiplier = 0.25f;
        barra = fondoBarra.rectTransform;
        barra.anchorMin = barra.anchorMax = new Vector2(0.5f, 0f);
        barra.pivot = new Vector2(0.5f, 0f);
        barra.anchoredPosition = new Vector2(0f, 26f);
        barra.sizeDelta = new Vector2(560f, 46f);
        fondoBarra.raycastTarget = true;
        Button b = fondoBarra.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(Activar);

        relleno = NuevaImagen("Relleno", barra, null, color);
        RectTransform rr = relleno.rectTransform;
        rr.anchorMin = new Vector2(0f, 0f);
        rr.anchorMax = new Vector2(0f, 1f);
        rr.pivot = new Vector2(0f, 0.5f);
        rr.offsetMin = new Vector2(10f, 10f);
        rr.offsetMax = new Vector2(10f, -10f);

        // Símbolo de la carrera a la izquierda de la barra (late cuando está llena).
        if (figuras != null && figuras.Length > 0)
        {
            Image fondoIcono = NuevaImagen("FondoSimbolo", barra, ArteExtra.Uno("Controles/boton"), Color.white);
            fondoIcono.rectTransform.anchorMin = fondoIcono.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            fondoIcono.rectTransform.anchoredPosition = new Vector2(-38f, 4f);
            fondoIcono.rectTransform.sizeDelta = new Vector2(74f, 74f);
            fondoIcono.preserveAspect = true;
            iconoCarrera = NuevaImagen("Simbolo", fondoIcono.rectTransform, figuras[0], color);
            iconoCarrera.rectTransform.sizeDelta = new Vector2(40f, 40f);
            iconoCarrera.rectTransform.anchoredPosition = new Vector2(0f, 3f);
            iconoCarrera.preserveAspect = true;
        }

        etiqueta = NuevoTexto("Etiqueta", barra, fuente, 20, Color.white);
        etiqueta.rectTransform.anchorMin = etiqueta.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        etiqueta.rectTransform.anchoredPosition = new Vector2(0f, 16f);
        etiqueta.text = "FURIA DE " + (fichaUsada != null ? fichaUsada.carrera : "");

        aviso = NuevoTexto("Aviso", barra, fuente, 34, Color.white);
        aviso.rectTransform.anchorMin = aviso.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        aviso.rectTransform.anchoredPosition = new Vector2(0f, 58f);
        aviso.text = "¡FURIA LISTA!";
        aviso.enabled = false;

        CrearAvisoGrande(fuente);

        destello = NuevaImagen("Destello", canvas.transform, null, new Color(1f, 1f, 1f, 0f));
        destello.rectTransform.anchorMin = Vector2.zero;
        destello.rectTransform.anchorMax = Vector2.one;
        destello.rectTransform.offsetMin = destello.rectTransform.offsetMax = Vector2.zero;
        destello.raycastTarget = false;

        titulo = NuevoTexto("Titulo", canvas.transform, fuente, 84, color);
        titulo.rectTransform.anchorMin = titulo.rectTransform.anchorMax = new Vector2(0.5f, 0.72f);
        titulo.enabled = false;
    }

    private void CrearAvisoGrande(Font fuente)
    {
        spTecla = ArteExtra.Uno("Ira/tecla");
        spTeclaPresionada = ArteExtra.Uno("Ira/tecla_presionada");

        Image raiz = NuevaImagen("AvisoFuria", canvas.transform, null, new Color(0f, 0f, 0f, 0f));
        grupoAviso = raiz.rectTransform;
        grupoAviso.anchorMin = grupoAviso.anchorMax = new Vector2(0.5f, 0.6f);
        grupoAviso.sizeDelta = new Vector2(900f, 470f);
        raiz.raycastTarget = true;                     // se puede tocar/clic (celular)
        Button b = raiz.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(Activar);

        brilloAviso = NuevaImagen("Brillo", grupoAviso, ArteExtra.Uno("Ira/brillo_suave"), color);
        brilloAviso.rectTransform.sizeDelta = new Vector2(980f, 560f);

        textoAvisoTitulo = NuevoTexto("Titulo", grupoAviso, fuente, 72, color);
        textoAvisoTitulo.rectTransform.anchoredPosition = new Vector2(0f, 165f);
        textoAvisoTitulo.text = "¡DESATA TU FURIA!";
        textoAvisoTitulo.GetComponent<Outline>().effectDistance = new Vector2(5f, -5f);

        teclaK = CrearTecla("K", new Vector2(-165f, -5f), fuente, out caraK, out letraK);
        teclaL = CrearTecla("L", new Vector2(165f, -5f), fuente, out caraL, out letraL);

        mas = NuevoTexto("Mas", grupoAviso, fuente, 110, Color.white);
        mas.rectTransform.anchoredPosition = new Vector2(0f, 0f);
        mas.text = "+";
        mas.GetComponent<Outline>().effectDistance = new Vector2(5f, -5f);

        textoAvisoSub = NuevoTexto("Sub", grupoAviso, fuente, 44, Color.white);
        textoAvisoSub.rectTransform.anchoredPosition = new Vector2(0f, -170f);
        textoAvisoSub.text = "¡APRIÉTALAS JUNTAS!";

        // En el celular no hay teclado: en vez de K + L se muestra el rayo del botón FURIA.
        if (ControlesTactiles.DebeMostrarse)
        {
            teclaK.gameObject.SetActive(false);
            teclaL.gameObject.SetActive(false);
            mas.gameObject.SetActive(false);
            Sprite rayoBoton = ArteExtra.Uno("Controles/icono_furia");
            if (rayoBoton != null)
            {
                iconoFuria = NuevaImagen("Rayo", grupoAviso, rayoBoton, Color.white);
                iconoFuria.rectTransform.sizeDelta = new Vector2(230f, 230f);
                iconoFuria.preserveAspect = true;
            }
            textoAvisoSub.text = "¡TOCA EL BOTÓN DEL RAYO!";
        }

        grupoAviso.gameObject.SetActive(false);
    }

    private RectTransform CrearTecla(string letra, Vector2 pos, Font fuente, out Image cara, out Text texto)
    {
        cara = NuevaImagen("Tecla" + letra, grupoAviso, spTecla, Color.white);
        RectTransform rt = cara.rectTransform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(180f, 195f);
        if (spTecla == null) cara.color = new Color(0.92f, 0.93f, 0.97f);
        texto = NuevoTexto("Letra", rt, fuente, 112, new Color(0.08f, 0.09f, 0.16f));
        texto.text = letra;
        texto.GetComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.5f);
        texto.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);
        return rt;
    }

    private void AnimarTecla(RectTransform rt, Image cara, Text letra, bool presionada, float t)
    {
        if (spTecla != null) cara.sprite = presionada ? spTeclaPresionada : spTecla;
        // La cara de la tecla presionada está ~5 px de arte más abajo.
        letra.rectTransform.anchoredPosition = new Vector2(0f, presionada ? -2f : 15f);
        float s = presionada ? 0.94f : 1f + 0.04f * Mathf.Sin(t * 6f);
        rt.localScale = new Vector3(s, s, 1f);
        letra.color = presionada ? Color.Lerp(new Color(0.08f, 0.09f, 0.16f), color * 0.6f, 0.7f) : new Color(0.08f, 0.09f, 0.16f);
        cara.color = presionada ? Color.Lerp(Color.white, color, 0.35f) : Color.white;
    }

    private void AnimarAvisoGrande(bool mostrar)
    {
        if (grupoAviso == null) return;
        if (grupoAviso.gameObject.activeSelf != mostrar) grupoAviso.gameObject.SetActive(mostrar);
        if (!mostrar) { inicioAviso = -1f; return; }
        float t = Time.unscaledTime;
        if (inicioAviso < 0f) inicioAviso = t;
        float e = t - inicioAviso;

        // Entrada con rebote (pop) y luego un vaivén suave.
        float pop = e < 0.4f ? RebotarSalida(e / 0.4f) : 1f;
        float latido = 1f + 0.05f * Mathf.Sin(t * 7f);
        grupoAviso.localScale = Vector3.one * pop * latido;
        grupoAviso.anchoredPosition = new Vector2(0f, 10f * Mathf.Sin(t * 2.6f));
        grupoAviso.localRotation = Quaternion.Euler(0f, 0f, 2f * Mathf.Sin(t * 3.1f));

        // Brillo en el color de la carrera, palpitando.
        float p = 0.5f + 0.5f * Mathf.Sin(t * 8f);
        brilloAviso.color = new Color(color.r, color.g, color.b, 0.35f + 0.3f * p);
        brilloAviso.rectTransform.localScale = Vector3.one * (0.9f + 0.15f * p);

        textoAvisoTitulo.color = Color.Lerp(color, Color.white, p * 0.5f);
        textoAvisoTitulo.rectTransform.localScale = Vector3.one * (1f + 0.07f * p);
        textoAvisoSub.color = new Color(1f, 1f, 1f, 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 4.5f)));
        mas.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 8f * Mathf.Sin(t * 5f));
        if (iconoFuria != null)
        {
            float ei = 1f + 0.12f * Mathf.Abs(Mathf.Sin(t * 6f));
            iconoFuria.rectTransform.localScale = new Vector3(ei, ei, 1f);
            iconoFuria.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 6f * Mathf.Sin(t * 4f));
            iconoFuria.color = Color.Lerp(Color.white, color, 0.35f + 0.25f * p);
        }

        // Demostración: las dos teclas se "aprietan" juntas al ritmo; si el jugador
        // ya mantiene una, esa queda hundida (respuesta inmediata).
        bool golpe = (t * 1.6f) % 1f < 0.22f;
        Keyboard k = Keyboard.current;
        bool kReal = k != null && k[Key.K].isPressed;
        bool lReal = k != null && k[Key.L].isPressed;
        AnimarTecla(teclaK, caraK, letraK, golpe || kReal, t);
        AnimarTecla(teclaL, caraL, letraL, golpe || lReal, t + 0.5f);
    }

    private static float RebotarSalida(float x)
    {
        // easeOutBack: pasa un poco de 1 y vuelve.
        const float c1 = 1.9f, c3 = c1 + 1f;
        x -= 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    private static Image NuevaImagen(string nombre, Transform padre, Sprite sprite, Color c)
    {
        Image img = new GameObject(nombre, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(padre, false);
        img.sprite = sprite;
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    private static Text NuevoTexto(string nombre, Transform padre, Font fuente, int tam, Color c)
    {
        Text t = new GameObject(nombre, typeof(RectTransform), typeof(Text), typeof(Outline)).GetComponent<Text>();
        t.transform.SetParent(padre, false);
        t.font = fuente;
        t.fontSize = tam;
        t.color = c;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        Outline o = t.GetComponent<Outline>();
        o.effectColor = new Color(0.04f, 0.05f, 0.12f);
        o.effectDistance = new Vector2(3f, -3f);
        return t;
    }

    private void ActualizarInterfaz()
    {
        if (relleno == null) return;
        bool ocultar = gestor != null && gestor.PartidaTerminada;
        if (canvas.enabled == ocultar) canvas.enabled = !ocultar;
        float ancho = (barra.sizeDelta.x - 20f) * (carga / 100f);
        relleno.rectTransform.sizeDelta = new Vector2(ancho, relleno.rectTransform.sizeDelta.y);
        bool lleno = carga >= 100f && !activa && !transformando;
        float pulso = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f);
        relleno.color = lleno ? Color.Lerp(color, Color.white, pulso * 0.6f) : (activa ? Color.Lerp(color, Color.white, 0.2f) : color * new Color(0.85f, 0.85f, 0.85f, 1f));
        aviso.enabled = lleno;
        if (iconoCarrera != null)
        {
            float e = lleno ? 1f + 0.25f * pulso : activa ? 1.1f + 0.05f * Mathf.Sin(Time.unscaledTime * 12f) : 1f;
            iconoCarrera.rectTransform.localScale = new Vector3(e, e, 1f);
            iconoCarrera.rectTransform.localRotation = Quaternion.Euler(0f, 0f, activa ? Time.unscaledTime * 180f : 0f);
        }
        bool pausa = gestor != null && gestor.EstaEnPausa;
        AnimarAvisoGrande(lleno && !pausa && !JugadorCenital.EntradaBloqueada);
        if (lleno) aviso.rectTransform.localScale = Vector3.one * (1f + 0.08f * pulso);
        etiqueta.text = activa ? "¡" + nombreFuria.Trim('¡', '!') + "!" : "FURIA DE " + (fichaUsada != null ? fichaUsada.carrera : "");
    }
}
