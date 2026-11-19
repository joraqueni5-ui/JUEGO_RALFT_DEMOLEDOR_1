using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

/// <summary>
/// Botones en pantalla para jugar en el celular (Android sin teclado).
/// Se crean solos al cargar la escena del juego, así que la escena no cambia.
/// Se leen los toques directamente del Touchscreen del Input System: no hace falta
/// EventSystem ni cambiar la forma de entrada del proyecto.
/// Mantener el dedo en una flecha equivale a dejar la tecla apretada; saltar,
/// martillar y reiniciar son pulsaciones de un solo toque.
/// </summary>
public class ControlesTactiles : MonoBehaviour
{
    // Estado de los botones que se mantienen apretados.
    public static bool Izquierda { get; private set; }
    public static bool Derecha { get; private set; }
    public static bool Arriba { get; private set; }
    public static bool Abajo { get; private set; }

    // Pulsaciones de un solo toque: se guardan hasta que el juego las lee.
    private static bool arribaPendiente;
    private static bool saltarPendiente;
    private static bool repararPendiente;
    private static bool reiniciarPendiente;
    private static bool pausaPendiente;

    /// <summary>Se mantiene apretado el botón MAPA: la cámara muestra el piso entero.</summary>
    public static bool VerMapa { get; private set; }

    /// <summary>Lee (y borra) la pulsación del botón de pausa.</summary>
    public static bool PausaPulsada()
    {
        bool valor = pausaPendiente;
        pausaPendiente = false;
        return valor;
    }

    [Header("Tamaño de los botones (modo piso cenital)")]
    [Tooltip("Ancho mínimo real de cada botón, en centímetros (usa Screen.dpi).")]
    [Min(0.5f)] public float anchoMinimoCm = 1.5f;
    [Tooltip("Los botones también miden al menos esta fracción del alto de la pantalla.")]
    [Range(0.05f, 0.25f)] public float alturaRelativaMinima = 0.11f;
    [Tooltip("Transparencia de los botones cuando están sobre el mapa (no caben en las bandas laterales).")]
    [Range(0.1f, 1f)] public float alfaSobreElMapa = 0.45f;
    [Tooltip("Transparencia de los botones cuando están en las bandas laterales.")]
    [Range(0.1f, 1f)] public float alfaEnBandas = 0.92f;

    private GameObject lienzoActual;
    private int anchoPantalla, altoPantalla;
    private float bandaPrevia;

    private static readonly Color ColorBoton = new Color(0.09f, 0.11f, 0.16f, 0.55f);
    private static readonly Color ColorBotonApretado = new Color(0.98f, 0.75f, 0.15f, 0.75f);
    private static readonly Color ColorBorde = new Color(1f, 1f, 1f, 0.45f);

    private class Boton
    {
        public RectTransform area;
        public Image fondo;
        public bool conIcono;
        public float alfa = 0.92f;
        public System.Action<bool> accion;
        public int dedo = -1;
    }

    private readonly List<Boton> botones = new List<Boton>();

    /// <summary>Lee (y borra) la pulsación del botón de subir.</summary>
    public static bool ArribaPulsado()
    {
        bool valor = arribaPendiente;
        arribaPendiente = false;
        return valor;
    }

    /// <summary>Lee (y borra) la pulsación del botón de saltar.</summary>
    public static bool SaltarPulsado()
    {
        bool valor = saltarPendiente;
        saltarPendiente = false;
        return valor;
    }

    /// <summary>Lee (y borra) la pulsación del botón de martillar.</summary>
    public static bool RepararPulsado()
    {
        bool valor = repararPendiente;
        repararPendiente = false;
        return valor;
    }

    /// <summary>Lee (y borra) la pulsación del botón de reiniciar.</summary>
    public static bool ReiniciarPulsado()
    {
        bool valor = reiniciarPendiente;
        reiniciarPendiente = false;
        return valor;
    }

    /// <summary>Crea los controles solo si la escena tiene al muñeco jugable.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearAutomaticamente()
    {
        LimpiarEstado();

        if (Object.FindAnyObjectByType<Muneco>() == null && Object.FindAnyObjectByType<JugadorCenital>() == null) return;
        if (Object.FindAnyObjectByType<ControlesTactiles>() != null) return;

        GameObject objeto = new GameObject("ControlesTactiles");
        objeto.AddComponent<ControlesTactiles>();
    }

    private static void LimpiarEstado()
    {
        Izquierda = false;
        Derecha = false;
        Arriba = false;
        Abajo = false;
        arribaPendiente = false;
        saltarPendiente = false;
        repararPendiente = false;
        reiniciarPendiente = false;
        pausaPendiente = false;
        VerMapa = false;
    }

    /// <summary>
    /// Los botones solo se muestran en celular. En Windows y al dar Play en el editor
    /// el objeto queda desactivado y todos sus valores en false, así que el teclado
    /// funciona igual. (En el Device Simulator de Unity sí aparecen, para probarlos.)
    /// </summary>
    public static bool DebeMostrarse => Application.isMobilePlatform;

    private void Awake()
    {
        if (!DebeMostrarse)
        {
            enabled = false;
            return;
        }

        CrearLienzo();
    }

    private void OnDestroy()
    {
        LimpiarEstado();
    }

    private void Update()
    {
        // Si cambia el tamaño de la pantalla o las bandas laterales de la cámara, los botones se acomodan de nuevo.
        if (lienzoActual != null && (Screen.width != anchoPantalla || Screen.height != altoPantalla ||
                                     !Mathf.Approximately(CamaraPisoCenital.AnchoBandaPx(), bandaPrevia)))
        {
            Destroy(lienzoActual);
            botones.Clear();
            bool mapa = VerMapa;
            LimpiarEstado();
            if (mapa) VerMapa = false;
            CrearLienzo();
        }

        Touchscreen pantalla = Touchscreen.current;
        if (pantalla == null) return;

        var toques = pantalla.touches;
        for (int i = 0; i < toques.Count; i++)
        {
            TouchControl toque = toques[i];
            UnityEngine.InputSystem.TouchPhase fase = toque.phase.ReadValue();
            int id = toque.touchId.ReadValue();

            if (fase == UnityEngine.InputSystem.TouchPhase.Began ||
                fase == UnityEngine.InputSystem.TouchPhase.Moved ||
                fase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                if (BotonDeDedo(id) != null) continue;

                Boton boton = BotonEn(toque.position.ReadValue());
                if (boton == null || boton.dedo >= 0) continue;

                boton.dedo = id;
                Apretar(boton, true);
            }
            else
            {
                Boton boton = BotonDeDedo(id);
                if (boton == null) continue;

                boton.dedo = -1;
                Apretar(boton, false);
            }
        }
    }

    // ------------------------------------------------------------ toques
    private Boton BotonDeDedo(int id)
    {
        for (int i = 0; i < botones.Count; i++)
        {
            if (botones[i].dedo == id) return botones[i];
        }
        return null;
    }

    private Boton BotonEn(Vector2 punto)
    {
        for (int i = 0; i < botones.Count; i++)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(botones[i].area, punto, null))
                return botones[i];
        }
        return null;
    }

    private void Apretar(Boton boton, bool apretado)
    {
        if (boton.fondo != null)
        {
            if (boton.conIcono) boton.fondo.color = apretado ? new Color(0.72f, 0.72f, 0.72f, Mathf.Min(1f, boton.alfa + 0.3f)) : new Color(1f, 1f, 1f, boton.alfa);
            else boton.fondo.color = apretado ? ColorBotonApretado : ColorBoton;
        }
        if (boton.accion != null) boton.accion(apretado);
    }

    // ------------------------------------------------------------ construcción
    private void CrearLienzo()
    {
        GameObject lienzo = new GameObject("Controles", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        lienzo.transform.SetParent(transform, false);
        lienzoActual = lienzo;
        anchoPantalla = Screen.width;
        altoPantalla = Screen.height;
        bandaPrevia = CamaraPisoCenital.AnchoBandaPx();

        Canvas canvas = lienzo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;

        CanvasScaler escalador = lienzo.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        Transform raiz = lienzo.transform;

        // Piso visto desde arriba: flechas a la izquierda; martillo, pausa y mapa a la derecha.
        if (Object.FindAnyObjectByType<JugadorCenital>() != null)
        {
            CrearControlesCenital(escalador);
            return;
        }

        // Caminar: abajo a la izquierda.
        BotonNuevo(raiz, "<", EsquinaInferiorIzquierda, new Vector2(40f, 40f), new Vector2(150f, 150f), 54,
            apretado => Izquierda = apretado);
        BotonNuevo(raiz, ">", EsquinaInferiorIzquierda, new Vector2(210f, 40f), new Vector2(150f, 150f), 54,
            apretado => Derecha = apretado);

        // Trepar y soltarse: abajo a la derecha.
        BotonNuevo(raiz, "SUBIR", EsquinaInferiorDerecha, new Vector2(-210f, 180f), new Vector2(190f, 110f), 32,
            apretado => { Arriba = apretado; if (apretado) arribaPendiente = true; });
        BotonNuevo(raiz, "BAJAR", EsquinaInferiorDerecha, new Vector2(-410f, 180f), new Vector2(190f, 110f), 32,
            apretado => Abajo = apretado);
        BotonNuevo(raiz, "SALTAR", EsquinaInferiorDerecha, new Vector2(-210f, 40f), new Vector2(190f, 120f), 34,
            apretado => { if (apretado) saltarPendiente = true; });
        BotonNuevo(raiz, "MARTILLO", EsquinaInferiorDerecha, new Vector2(-20f, 40f), new Vector2(180f, 120f), 30,
            apretado => { if (apretado) repararPendiente = true; });

        // Reiniciar: arriba a la derecha, lejos de los botones de juego.
        BotonNuevo(raiz, "REINICIAR", EsquinaSuperiorDerecha, new Vector2(-20f, -20f), new Vector2(220f, 76f), 28,
            apretado => { if (apretado) reiniciarPendiente = true; });
    }

    private float alfaActual = 0.92f;

    /// <summary>
    /// Controles del piso cenital en píxeles reales: cada botón mide al menos 'anchoMinimoCm' (Screen.dpi). Si las bandas laterales
    /// de la cámara son lo bastante anchas, los botones van en ellas y no tapan el mapa; si no, quedan sobre el mapa semitransparentes.
    /// </summary>
    private void CrearControlesCenital(CanvasScaler escalador)
    {
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;   // 1 unidad = 1 píxel de pantalla
        escalador.scaleFactor = 1f;
        Transform raiz = lienzoActual.transform;

        float alto = Screen.height;
        float dpi = Screen.dpi > 1f ? Screen.dpi : 160f;
        float cm = anchoMinimoCm / 2.54f * dpi;
        float lado = Mathf.Max(cm, alto * alturaRelativaMinima);
        float margen = lado * 0.15f;
        float banda = CamaraPisoCenital.AnchoBandaPx();

        // Izquierda: cruz de 4 flechas. Cabe en la banda si mide 3 botones de ancho.
        float ladoCruz = Mathf.Max(cm, Mathf.Min(lado, (banda - 2f * margen) / 3f));
        bool cruzEnBanda = banda >= 3f * cm + 2f * margen;
        float l = cruzEnBanda ? ladoCruz : lado;
        float x0 = cruzEnBanda ? (banda - 3f * l) * 0.5f : margen;
        float y0 = margen * 2f;
        alfaActual = cruzEnBanda ? alfaEnBandas : alfaSobreElMapa;
        Vector2 tam = new Vector2(l, l);
        BotonNuevo(raiz, "^", EsquinaInferiorIzquierda, new Vector2(x0 + l, y0 + 2f * l), tam, 30, apretado => Arriba = apretado);
        BotonNuevo(raiz, "<", EsquinaInferiorIzquierda, new Vector2(x0, y0 + l), tam, 30, apretado => Izquierda = apretado);
        BotonNuevo(raiz, ">", EsquinaInferiorIzquierda, new Vector2(x0 + 2f * l, y0 + l), tam, 30, apretado => Derecha = apretado);
        BotonNuevo(raiz, "v", EsquinaInferiorIzquierda, new Vector2(x0 + l, y0), tam, 30, apretado => Abajo = apretado);

        // Derecha: martillo, pausa y mapa en columna.
        bool accionesEnBanda = banda >= cm + 2f * margen;
        float r = accionesEnBanda ? Mathf.Max(cm, Mathf.Min(lado, banda - 2f * margen)) : lado;
        float xr = accionesEnBanda ? -(banda - r) * 0.5f : -margen;
        alfaActual = accionesEnBanda ? alfaEnBandas : alfaSobreElMapa;
        float y = y0;
        BotonNuevo(raiz, "MARTILLO", EsquinaInferiorDerecha, new Vector2(xr, y), new Vector2(r, r), 24,
            apretado => { if (apretado) repararPendiente = true; });
        y += r + margen;
        BotonNuevo(raiz, "PAUSA", EsquinaInferiorDerecha, new Vector2(xr, y), new Vector2(r, r), 24,
            apretado => { if (apretado) pausaPendiente = true; });
        y += r + margen;
        BotonNuevo(raiz, "MAPA", EsquinaInferiorDerecha, new Vector2(xr, y), new Vector2(r, r * 0.7f), 24,
            apretado => VerMapa = apretado);
        y += r * 0.7f + margen;
        BotonNuevo(raiz, "REINICIAR", EsquinaInferiorDerecha, new Vector2(xr, y), new Vector2(r, r * 0.6f), 18,
            apretado => { if (apretado) reiniciarPendiente = true; });
        alfaActual = 0.92f;
    }

    private static readonly Vector2 EsquinaInferiorIzquierda = Vector2.zero;
    private static readonly Vector2 EsquinaInferiorDerecha = new Vector2(1f, 0f);
    private static readonly Vector2 EsquinaSuperiorDerecha = new Vector2(1f, 1f);

    private void BotonNuevo(Transform padre, string etiqueta, Vector2 ancla, Vector2 posicion, Vector2 tamano,
        int tamanioFuente, System.Action<bool> accion)
    {
        GameObject objeto = new GameObject("Boton_" + etiqueta, typeof(RectTransform), typeof(Image));
        objeto.transform.SetParent(padre, false);

        RectTransform rt = (RectTransform)objeto.transform;
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = ancla;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        Image imagen = objeto.GetComponent<Image>();
        imagen.color = ColorBoton;
        imagen.raycastTarget = false;

        // Con arte, el botón es el icono (flechas, martillo, saltar); sin él, el rectángulo con texto de siempre.
        Sprite icono = IconoDe(etiqueta);
        if (icono != null)
        {
            imagen.sprite = icono;
            imagen.preserveAspect = true;
            imagen.color = new Color(1f, 1f, 1f, alfaActual);
            botones.Add(new Boton { area = rt, fondo = imagen, accion = accion, conIcono = true, alfa = alfaActual });
            return;
        }

        Outline borde = objeto.AddComponent<Outline>();
        borde.effectColor = ColorBorde;
        borde.effectDistance = new Vector2(2f, 2f);

        GameObject textoObjeto = new GameObject("Texto", typeof(RectTransform));
        textoObjeto.transform.SetParent(objeto.transform, false);

        RectTransform rtTexto = (RectTransform)textoObjeto.transform;
        rtTexto.anchorMin = Vector2.zero;
        rtTexto.anchorMax = Vector2.one;
        rtTexto.offsetMin = Vector2.zero;
        rtTexto.offsetMax = Vector2.zero;

        Text texto = textoObjeto.AddComponent<Text>();
        texto.font = FuentePredeterminada();
        texto.text = etiqueta;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.fontSize = tamanioFuente;
        texto.fontStyle = FontStyle.Bold;
        texto.color = new Color(1f, 1f, 1f, 0.92f);
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        texto.raycastTarget = false;

        botones.Add(new Boton { area = rt, fondo = imagen, accion = accion });
    }

    /// <summary>Icono del arte para un botón (null si no hay arte o el botón no tiene icono, como REINICIAR).</summary>
    private static Sprite IconoDe(string etiqueta)
    {
        ArteNiveles a = ArteNiveles.Actual;
        if (a == null) return null;
        switch (etiqueta)
        {
            case "^": case "SUBIR": return a.botonArriba;
            case "v": case "BAJAR": return a.botonAbajo;
            case "<": return a.botonIzquierda;
            case ">": return a.botonDerecha;
            case "MARTILLO": return a.botonReparar;
            case "SALTAR": return a.botonSaltar;
            case "PAUSA": return a.botonPausa;
        }
        return null;
    }

    private static Font FuentePredeterminada()
    {
        Font fuente = null;
        try
        {
            fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
            fuente = null;
        }

        if (fuente == null)
        {
            try
            {
                fuente = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch
            {
                fuente = null;
            }
        }
        return fuente;
    }
}
