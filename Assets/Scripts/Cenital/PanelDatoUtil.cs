using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel "DATO ÚTIL" del HUD (Screen Space): marco dorado abajo al centro con un foco a la izquierda, una pestaña con el nombre del área
/// (del color del piso) sobre el borde superior, el texto completo, una línea "Consejo:" en dorado y, arriba a la
/// derecha, cuántos "?" lleva leídos el jugador. Entra y sale deslizándose desde abajo. No pausa el juego.
/// Se cierra al acabar el tiempo de lectura (según el largo del texto), al alejarse del "?" o ante un aviso grande o el tutorial.
/// Los valores salen de la DefinicionPiso (sección "Panel DATO ÚTIL").
/// </summary>
public class PanelDatoUtil : MonoBehaviour
{
    private static PanelDatoUtil instancia;

    /// <summary>Hay un dato en pantalla.</summary>
    public static bool Visible { get { return instancia != null && instancia.estado != Estado.Oculto; } }

    private enum Estado { Oculto, Entrando, Visible, Saliendo }

    private const float TamanoCaja = 3f;          // los sprites de 1 px se dibujan a 3 px de lienzo (marco de 8 px = 24)
    private const float Margen = 24f;
    private const float DuracionDesliz = 0.2f;
    private static readonly Color Dorado = new Color(1f, 0.878f, 0.541f, 1f);   // #FFE08A

    private Canvas canvas;
    private CanvasGroup grupo;
    private float altoPantallaPx;
    private RectTransform caja, pestana, contador, icono;
    private Image fondo, imgPestana, imgIcono, destello;
    private Image[] libros = new Image[0];
    private Text textoArea, textoPrincipal, textoConsejo, textoContador;

    private Estado estado = Estado.Oculto;
    private float tEstado, restante, esperaAlejado, opacidad = 1f;
    private float altoCaja, anchoCaja, baseY;
    private string principal = "", consejo = "";
    private int indiceActual = -1;
    private Transform origen;
    private float tDestello = 10f;
    private int libroDestello = -1;

    private PisoCenital piso;
    private DefinicionPiso def;
    private JugadorCenital jugador;
    private DemoledorCenital demoledor;
    private GestorJuego gestor;

    // ------------------------------------------------------------ API
    /// <summary>
    /// Muestra el dato del "?" 'indice'. Si ese mismo dato ya está en pantalla, lo completa y reinicia su tiempo.
    /// 'primeraVez' hace parpadear el librito que acaba de leerse.
    /// </summary>
    public static void Mostrar(Transform origen, int indice, PuntoInfoPiso ficha, string textoAlternativo, bool primeraVez)
    {
        if (instancia == null)
        {
            GameObject g = new GameObject("PanelDatoUtil", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            instancia = g.AddComponent<PanelDatoUtil>();
            instancia.Construir();
        }
        instancia.Abrir(origen, indice, ficha, textoAlternativo, primeraVez);
    }

    /// <summary>Oculta el panel de inmediato (por ejemplo al reiniciar el piso).</summary>
    public static void Ocultar()
    {
        if (instancia != null) instancia.Cerrar(true);
    }

    // ------------------------------------------------------------ construcción
    private static Font Fuente { get { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } }

    private static float EscalaLienzo()
    {
        float w = Screen.width / 1920f, h = Screen.height / 1080f;
        return Mathf.Exp(Mathf.Lerp(Mathf.Log(w), Mathf.Log(h), 0.5f));
    }

    private Image Crear(string nombre, Transform padre, Sprite sprite, bool cortado)
    {
        GameObject g = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(padre, false);
        Image i = g.GetComponent<Image>();
        i.sprite = sprite;
        i.raycastTarget = false;
        if (sprite != null)
        {
            i.type = cortado ? Image.Type.Sliced : Image.Type.Simple;
            // Las esquinas del marco miden TamanoCaja píxeles de lienzo por cada píxel del dibujo.
            if (cortado) i.pixelsPerUnitMultiplier = 100f / sprite.pixelsPerUnit / TamanoCaja;
        }
        else i.color = new Color(0.07f, 0.1f, 0.2f, 0.96f);
        return i;
    }

    private Text CrearTexto(string nombre, Transform padre, int tamano, FontStyle estilo, Color color, TextAnchor ancla)
    {
        GameObject g = new GameObject(nombre, typeof(RectTransform), typeof(Text));
        g.transform.SetParent(padre, false);
        Text t = g.GetComponent<Text>();
        t.font = Fuente;
        t.fontSize = tamano;
        t.fontStyle = estilo;
        t.color = color;
        t.alignment = ancla;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.supportRichText = false;
        return t;
    }

    private void Construir()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;
        CanvasScaler esc = GetComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 0.5f;
        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        ArteNiveles arte = ArteNiveles.Actual;
        Sprite sPanel = arte != null ? arte.panelDato : null;

        fondo = Crear("Caja", transform, sPanel, true);
        caja = fondo.rectTransform;
        caja.anchorMin = caja.anchorMax = new Vector2(0.5f, 0f);
        caja.pivot = new Vector2(0.5f, 0f);

        // Foco a la izquierda (16 px a 3x).
        imgIcono = Crear("Foco", caja, arte != null ? arte.iconoFoco : null, false);
        icono = imgIcono.rectTransform;
        icono.anchorMin = icono.anchorMax = new Vector2(0f, 0.5f);
        icono.pivot = new Vector2(0.5f, 0.5f);
        icono.sizeDelta = new Vector2(48f, 48f);
        icono.anchoredPosition = new Vector2(Margen + 24f + 12f, 0f);
        if (imgIcono.sprite == null) imgIcono.color = new Color(1f, 0.9f, 0.4f);

        textoPrincipal = CrearTexto("Texto", caja, 34, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
        textoConsejo = CrearTexto("Consejo", caja, 28, FontStyle.Normal, Dorado, TextAnchor.UpperLeft);
        foreach (Text t in new[] { textoPrincipal, textoConsejo })
        {
            Outline borde = t.gameObject.AddComponent<Outline>();   // contorno oscuro de 1 px para leer bien en pantallas pequeñas
            borde.effectColor = new Color(0.02f, 0.03f, 0.08f, 0.95f);
            borde.effectDistance = new Vector2(1f, -1f);
        }

        // Pestaña con el nombre del área, sobre el borde superior a la izquierda.
        imgPestana = Crear("Pestana", caja, arte != null ? arte.etiquetaArea : null, true);
        pestana = imgPestana.rectTransform;
        pestana.anchorMin = pestana.anchorMax = new Vector2(0f, 1f);
        pestana.pivot = new Vector2(0f, 0f);
        textoArea = CrearTexto("Area", pestana, 22, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        textoArea.horizontalOverflow = HorizontalWrapMode.Overflow;
        Outline contorno = textoArea.gameObject.AddComponent<Outline>();
        contorno.effectColor = new Color(0f, 0f, 0f, 0.7f);
        contorno.effectDistance = new Vector2(2f, -2f);
        RectTransform ra = textoArea.rectTransform;
        ra.anchorMin = Vector2.zero; ra.anchorMax = Vector2.one; ra.offsetMin = ra.offsetMax = Vector2.zero;

        // Contador arriba a la derecha, sobre el borde.
        Image fondoContador = Crear("Contador", caja, sPanel, true);
        contador = fondoContador.rectTransform;
        contador.anchorMin = contador.anchorMax = new Vector2(1f, 1f);
        contador.pivot = new Vector2(1f, 0.5f);
        textoContador = CrearTexto("Cuenta", contador, 22, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        textoContador.horizontalOverflow = HorizontalWrapMode.Overflow;

        destello = Crear("Destello", contador, null, false);
        destello.color = new Color(1f, 1f, 1f, 0f);
        destello.rectTransform.sizeDelta = new Vector2(24f, 24f);
        destello.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }

    // ------------------------------------------------------------ mostrar / cerrar
    private void Abrir(Transform origen, int indice, PuntoInfoPiso ficha, string textoAlternativo, bool primeraVez)
    {
        piso = PisoCenital.Actual;
        def = piso != null ? piso.Definicion : null;
        if (jugador == null) jugador = FindFirstObjectByType<JugadorCenital>();
        if (gestor == null) gestor = FindFirstObjectByType<GestorJuego>();

        if (HayAvisoGrande()) return;

        // Contenido: nunca se muestra un panel vacío. Sin texto propio se usa el dato general del piso; sin nada, se avisa y no se abre.
        string area = ficha != null && !string.IsNullOrEmpty(ficha.area) ? ficha.area : (def != null ? def.NombreArea : "DATO ÚTIL");
        string texto = ficha != null && !string.IsNullOrEmpty(ficha.texto) ? ficha.texto : textoAlternativo;
        if (string.IsNullOrWhiteSpace(texto) && def != null) texto = def.fichaDatoUtil;
        if (string.IsNullOrWhiteSpace(texto))
        {
            Debug.LogError("[DatoÚtil] El \"?\" " + (indice + 1) + " del piso " + (def != null ? def.numero.ToString() : "?") + " (posición " +
                           (origen != null ? (Vector2)origen.position : Vector2.zero) + ") no tiene dato: no se muestra el panel.");
            return;
        }

        // El mismo "?" otra vez (incluso si se estaba cerrando): el panel se queda y el tiempo se reinicia.
        if (estado != Estado.Oculto && indice == indiceActual && indice >= 0)
        {
            ReiniciarTiempo();
            if (estado == Estado.Saliendo) { estado = Estado.Visible; tEstado = DuracionDesliz; PosicionarCaja(1f); }
            DesplazarCamara(true);
            return;
        }

        this.origen = origen;
        indiceActual = indice;
        principal = texto.Replace("\n", " ").Trim();
        consejo = ficha != null && !string.IsNullOrEmpty(ficha.consejo) ? ficha.consejo.Replace("\n", " ").Trim() : "";
        textoArea.text = area.ToUpperInvariant();

        gameObject.SetActive(true);
        Disponer();
        ActualizarContador(primeraVez);

        tEstado = 0f;
        ReiniciarTiempo();
        estado = Estado.Entrando;
        icono.localScale = Vector3.one * 0.6f;
        PosicionarCaja(0f);
        DesplazarCamara(true);
    }

    /// <summary>Tiempo de lectura: base + segundos por carácter (texto y consejo), entre el mínimo y el máximo.</summary>
    private void ReiniciarTiempo()
    {
        float b = def != null ? def.segundosBaseDato : 4f, c = def != null ? def.segundosPorCaracterDato : 0.06f;
        float min = def != null ? def.minimoSegundosDato : 8f, max = def != null ? def.maximoSegundosDato : 14f;
        restante = Mathf.Clamp(b + c * (principal.Length + consejo.Length), min, Mathf.Max(min, max));
        esperaAlejado = 0f;
    }

    private void Cerrar(bool ya)
    {
        if (estado == Estado.Oculto) return;
        DesplazarCamara(false);
        if (ya) { estado = Estado.Oculto; indiceActual = -1; gameObject.SetActive(false); return; }
        if (estado == Estado.Saliendo) return;
        estado = Estado.Saliendo;
        tEstado = 0f;
    }

    /// <summary>Mientras el panel está abierto, la cámara baja su punto de seguimiento la altura del panel (el jugador queda por encima).</summary>
    private void DesplazarCamara(bool abierto)
    {
        if (!abierto) { CamaraPisoCenital.DesplazarSeguimiento(0f); return; }
        float alturaPx = (baseY + altoCaja) * EscalaLienzo();
        CamaraPisoCenital.DesplazarSeguimiento(alturaPx * CamaraPisoCenital.UnidadesPorPixel);
    }

    private bool HayAvisoGrande()
    {
        if (AvisoUlti.Activo || TutorialPiso.Visible) return true;
        if (piso != null && (piso.Rendido || piso.Terminado)) return true;
        if (gestor != null && gestor.AvisoGrandeActivo) return true;
        return false;
    }

    // ------------------------------------------------------------ disposición
    private void Disponer()
    {
        float escala = EscalaLienzo();
        float anchoLienzo = Screen.width / escala;
        float banda = CamaraPisoCenital.AnchoBandaPx() / escala;     // los botones táctiles ocupan las bandas de los costados
        Rect segura = Screen.safeArea;
        float izq = Mathf.Max(banda, segura.xMin / escala), der = Mathf.Max(banda, (Screen.width - segura.xMax) / escala);
        float disponible = anchoLienzo - izq - der - 32f;
        anchoCaja = Mathf.Max(360f, Mathf.Min(760f, anchoLienzo * 0.6f, disponible));
        baseY = Margen + Mathf.Max(0f, segura.yMin / escala);

        float xTexto = Margen + 48f + 24f;                 // marco + foco + espacio
        float anchoTexto = anchoCaja - xTexto - Margen - 8f;
        textoPrincipal.rectTransform.sizeDelta = new Vector2(anchoTexto, 10f);
        textoConsejo.rectTransform.sizeDelta = new Vector2(anchoTexto, 10f);

        // El alto sale del texto completo, así nada queda cortado y no salta mientras se escribe.
        textoPrincipal.text = principal;
        textoConsejo.text = consejo;
        float hP = textoPrincipal.preferredHeight, hC = string.IsNullOrEmpty(consejo) ? 0f : textoConsejo.preferredHeight;
        float separacion = hC > 0f ? 8f : 0f;
        altoCaja = Mathf.Max(120f, Margen * 2f + hP + separacion + hC);

        caja.sizeDelta = new Vector2(anchoCaja, altoCaja);
        float yTop = altoCaja * 0.5f;
        RectTransform rp = textoPrincipal.rectTransform, rc = textoConsejo.rectTransform;
        foreach (RectTransform r in new[] { rp, rc })
        {
            r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0f, 1f);
        }
        float arriba = (hP + separacion + hC) * 0.5f;
        rp.sizeDelta = new Vector2(anchoTexto, hP);
        rp.anchoredPosition = new Vector2(xTexto, arriba);
        rc.sizeDelta = new Vector2(anchoTexto, Mathf.Max(hC, 1f));
        rc.anchoredPosition = new Vector2(xTexto, arriba - hP - separacion);

        // Pestaña: ancho según el nombre del área, alto 16 px del dibujo a 3x.
        textoArea.rectTransform.sizeDelta = Vector2.zero;
        float anchoPestana = Mathf.Max(150f, textoArea.preferredWidth + 48f);
        pestana.sizeDelta = new Vector2(anchoPestana, 16f * TamanoCaja);
        pestana.anchoredPosition = new Vector2(Margen + 8f, -12f);
        imgPestana.color = def != null ? def.ColorPestana : Color.white;
        if (imgPestana.sprite == null) imgPestana.color = def != null ? def.ColorPestana : Color.white;

        contador.anchoredPosition = new Vector2(-(Margen + 8f), 0f);
    }

    private void ActualizarContador(bool primeraVez)
    {
        int total = piso != null ? piso.InfoTotal : 0;
        int leidos = piso != null ? piso.InfoLeidasCuenta : 0;
        ArteNiveles arte = ArteNiveles.Actual;
        Sprite sLeido = arte != null ? arte.libroLeido : null, sPendiente = arte != null ? arte.libroPendiente : null;

        for (int i = 0; i < libros.Length; i++) if (libros[i] != null) Destroy(libros[i].gameObject);
        libros = new Image[total];
        const float lado = 24f, hueco = 6f;
        textoContador.text = leidos + "/" + total;
        textoContador.rectTransform.anchorMin = textoContador.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        textoContador.rectTransform.pivot = new Vector2(0f, 0.5f);
        textoContador.rectTransform.sizeDelta = new Vector2(70f, 30f);
        textoContador.rectTransform.anchoredPosition = new Vector2(Margen * 0.5f + 4f, 0f);
        float x = Margen * 0.5f + 4f + 62f;
        for (int i = 0; i < total; i++)
        {
            bool leido = i < leidos;
            Image l = Crear("Libro" + i, contador, leido ? sLeido : sPendiente, false);
            if (l.sprite == null) l.color = leido ? new Color(1f, 0.85f, 0.3f) : new Color(0.4f, 0.45f, 0.6f);
            RectTransform rl = l.rectTransform;
            rl.anchorMin = rl.anchorMax = new Vector2(0f, 0.5f);
            rl.pivot = new Vector2(0f, 0.5f);
            rl.sizeDelta = new Vector2(lado, lado);
            rl.anchoredPosition = new Vector2(x, 0f);
            libros[i] = l;
            x += lado + hueco;
        }
        contador.sizeDelta = new Vector2(x + Margen * 0.5f, 56f);

        // Destello sobre el librito que acaba de leerse.
        libroDestello = primeraVez && leidos > 0 && leidos <= total ? leidos - 1 : -1;
        tDestello = libroDestello >= 0 ? 0f : 10f;
        destello.gameObject.SetActive(libroDestello >= 0);
        if (libroDestello >= 0)
        {
            destello.transform.SetParent(libros[libroDestello].transform, false);
            RectTransform rd = destello.rectTransform;
            rd.anchorMin = Vector2.zero; rd.anchorMax = Vector2.one; rd.offsetMin = rd.offsetMax = Vector2.zero;
            destello.transform.SetAsLastSibling();
        }
    }

    private void PosicionarCaja(float k)
    {
        // Desliza desde abajo: k = 0 fuera de pantalla, 1 en su sitio.
        float y = Mathf.Lerp(-altoCaja - 40f, baseY, k);
        caja.anchoredPosition = new Vector2(0f, y);
    }

    // ------------------------------------------------------------ vida
    private void Update()
    {
        if (estado == Estado.Oculto) return;
        float dt = Time.deltaTime;
        tEstado += dt;

        if (estado == Estado.Entrando)
        {
            float k = Mathf.Clamp01(tEstado / DuracionDesliz);
            PosicionarCaja(1f - (1f - k) * (1f - k));
            if (k >= 1f) estado = Estado.Visible;
        }
        else if (estado == Estado.Saliendo)
        {
            float k = Mathf.Clamp01(tEstado / DuracionDesliz);
            PosicionarCaja(1f - k * k);
            if (k >= 1f) { Cerrar(true); return; }
        }
        else PosicionarCaja(1f);

        ActualizarOpacidad(dt);
        if (estado == Estado.Saliendo) return;

        // Rebote del foco: 0,6 -> 1,1 -> 1 en 0,25 s.
        float r = Mathf.Clamp01(tEstado / 0.25f);
        float esc = r < 0.6f ? Mathf.Lerp(0.6f, 1.1f, r / 0.6f) : Mathf.Lerp(1.1f, 1f, (r - 0.6f) / 0.4f);
        icono.localScale = Vector3.one * esc;

        // Destello del librito nuevo.
        if (libroDestello >= 0 && tDestello < 0.5f)
        {
            tDestello += dt;
            float k = Mathf.Clamp01(tDestello / 0.5f);
            destello.color = new Color(1f, 1f, 1f, (1f - k) * 0.9f);
            libros[libroDestello].rectTransform.localScale = Vector3.one * Mathf.Lerp(1.5f, 1f, k);
            if (tDestello >= 0.5f) destello.gameObject.SetActive(false);
        }

        if (HayAvisoGrande()) { Cerrar(false); return; }

        // Tiempo: no corre mientras el jugador sigue junto al "?"; al alejarse del todo espera un momento y se cierra.
        float mantener = def != null ? def.distanciaMantenerPanelDato : 1.5f;
        float cerrar = def != null ? def.distanciaCerrarPanelDato : 2f;
        float espera = def != null ? def.esperaCerrarPanelDato : 1.5f;
        float d = jugador != null && origen != null ? Vector2.Distance(jugador.Centro, origen.position) : 0f;
        if (d <= mantener) { esperaAlejado = 0f; return; }

        restante -= dt;
        if (d > cerrar) esperaAlejado += dt; else esperaAlejado = 0f;
        if (restante <= 0f || esperaAlejado >= espera) Cerrar(false);
    }

    /// <summary>Si el jugador o el demoledor quedan detrás del panel, este baja su opacidad (45 % por defecto).</summary>
    private void ActualizarOpacidad(float dt)
    {
        float objetivo = 1f;
        Camera cam = Camera.main;
        float u = CamaraPisoCenital.UnidadesPorPixel;
        if (cam != null && u > 0f && estado != Estado.Saliendo)
        {
            float esc = EscalaLienzo();
            float mitad = anchoCaja * esc * 0.5f, cx = Screen.width * 0.5f;
            Rect pantalla = new Rect(cx - mitad, baseY * esc, mitad * 2f, altoCaja * esc + 48f * esc);   // incluye la pestaña
            float margenPx = 0.5f / u;                                                                    // medio mundo de radio del personaje
            pantalla.xMin -= margenPx; pantalla.xMax += margenPx; pantalla.yMin -= margenPx; pantalla.yMax += margenPx;

            if (demoledor == null) demoledor = FindFirstObjectByType<DemoledorCenital>();
            if ((jugador != null && Detras(cam, jugador.Centro, pantalla)) ||
                (demoledor != null && Detras(cam, demoledor.transform.position, pantalla)))
                objetivo = def != null ? def.opacidadPanelTapando : 0.45f;
        }
        opacidad = Mathf.MoveTowards(opacidad, objetivo, dt * 6f);
        grupo.alpha = opacidad;
    }

    private static bool Detras(Camera cam, Vector2 mundo, Rect pantalla)
    {
        return pantalla.Contains(cam.WorldToScreenPoint(mundo));
    }

    private void OnDestroy()
    {
        if (instancia == this) { instancia = null; CamaraPisoCenital.DesplazarSeguimiento(0f); }
    }
}
