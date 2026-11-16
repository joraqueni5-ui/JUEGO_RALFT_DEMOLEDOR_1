using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mensajes de ayuda de un piso (DefinicionPiso.pasosTutorial). Cada paso aparece una vez, cuando se
/// cumple su condición de aparición, y se va cuando el jugador hace lo que pide. Se muestra un solo mensaje
/// a la vez, abajo en la pantalla. PisoCenital lo agrega solo si la definición tiene pasos.
/// </summary>
public class TutorialPiso : MonoBehaviour
{
    /// <summary>Hay un mensaje del tutorial en pantalla.</summary>
    public static bool Visible { get; private set; }

    private PisoCenital piso;
    private DefinicionPiso def;
    private JugadorCenital jugador;
    private DemoledorCenital demoledor;
    private GestorJuego gestor;

    private bool[] cumplido, visto;
    private Vector2 inicioJugador;
    private bool tieneInicio;

    private GameObject panel;
    private Text texto;
    private int mostrado = -1;

    private const float DistanciaParaMoverse = 1.5f;

    private void Start()
    {
        piso = GetComponent<PisoCenital>();
        def = piso != null ? piso.Definicion : null;
        if (def == null || def.pasosTutorial == null || def.pasosTutorial.Length == 0) { enabled = false; return; }

        jugador = FindFirstObjectByType<JugadorCenital>();
        demoledor = FindFirstObjectByType<DemoledorCenital>();
        gestor = FindFirstObjectByType<GestorJuego>();
        cumplido = new bool[def.pasosTutorial.Length];
        visto = new bool[def.pasosTutorial.Length];
        CrearInterfaz();
    }

    private void OnDestroy()
    {
        Visible = false;
        if (panel != null) Destroy(panel.transform.root.gameObject);
    }

    private void CrearInterfaz()
    {
        GameObject lienzo = new GameObject("TutorialPiso", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        lienzo.transform.SetParent(transform, false);
        Canvas canvas = lienzo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        CanvasScaler escalador = lienzo.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        panel = new GameObject("Mensaje", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(lienzo.transform, false);
        RectTransform rp = (RectTransform)panel.transform;
        // Abajo a la izquierda y angosto: así no tapa los carteles de la derecha (RECEPCIÓN) ni los objetos del centro.
        rp.anchorMin = rp.anchorMax = new Vector2(0f, 0f);
        rp.pivot = new Vector2(0f, 0f);
        rp.anchoredPosition = new Vector2(24f, 20f);
        rp.sizeDelta = new Vector2(980f, 96f);
        Image fondo = panel.GetComponent<Image>();
        fondo.color = new Color(0.04f, 0.05f, 0.09f, 0.82f);
        fondo.raycastTarget = false;

        GameObject t = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        t.transform.SetParent(panel.transform, false);
        RectTransform rt = (RectTransform)t.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(20f, 6f);
        rt.offsetMax = new Vector2(-20f, -6f);
        texto = t.GetComponent<Text>();
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.fontSize = 30;
        texto.fontStyle = FontStyle.Bold;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = new Color(1f, 0.92f, 0.5f, 1f);
        texto.horizontalOverflow = HorizontalWrapMode.Wrap;
        texto.raycastTarget = false;
        Outline contorno = t.AddComponent<Outline>();
        contorno.effectColor = new Color(0f, 0f, 0f, 0.85f);
        contorno.effectDistance = new Vector2(2f, -2f);

        panel.SetActive(false);
    }

    private void Update()
    {
        if (def == null || piso == null) return;

        if (!tieneInicio && jugador != null) { inicioJugador = jugador.Centro; tieneInicio = true; }

        bool oculto = piso.Terminado || (gestor != null && (gestor.PartidaTerminada || gestor.EstaEnPausa));
        int elegido = -1;
        for (int i = 0; i < cumplido.Length; i++)
        {
            if (cumplido[i]) continue;
            if (Cumplido(def.pasosTutorial[i].cumple)) { cumplido[i] = true; continue; }
            bool aparece = Aparece(def.pasosTutorial[i].aparece);
            // Un aviso de golpe inminente se va solo cuando la amenaza pasa, aunque no se haya bloqueado.
            if (!aparece && visto[i] && def.pasosTutorial[i].aparece == AparicionTutorial.HayObjetoAmenazado) { cumplido[i] = true; continue; }
            if (elegido < 0 && aparece) elegido = i;
        }

        if (oculto) elegido = -1;
        if (elegido == mostrado) return;
        mostrado = elegido;
        if (elegido >= 0) visto[elegido] = true;
        if (panel == null) return;
        panel.SetActive(elegido >= 0);
        Visible = elegido >= 0;
        if (elegido >= 0) texto.text = def.pasosTutorial[elegido].texto;
    }

    private bool Aparece(AparicionTutorial cuando)
    {
        switch (cuando)
        {
            case AparicionTutorial.HayObjetoDanado:
                if (piso.Mapa == null) return false;
                foreach (ObjetoReparable o in piso.Mapa.objetos) if (o != null && o.Estado != EstadoObjeto.Intacto) return true;
                return false;
            case AparicionTutorial.HayObjetoAmenazado:
                return demoledor != null && demoledor.ObjetivoAmenazado != null;
            case AparicionTutorial.DemoledorSeFue:
                return piso.DemoledorSalio;
            default:
                return true;
        }
    }

    private bool Cumplido(CumplirTutorial que)
    {
        switch (que)
        {
            case CumplirTutorial.Moverse:
                return tieneInicio && jugador != null && Vector2.Distance(jugador.Centro, inicioJugador) >= DistanciaParaMoverse;
            case CumplirTutorial.Reparar:
                return piso.ReparacionesTotales > 0;
            case CumplirTutorial.Bloquear:
                return piso.BloqueosTotales > 0;
            case CumplirTutorial.LeerPuntoInfo:
                return piso.InfoLeidasCuenta > 0;
            default:
                return piso.Terminado;
        }
    }
}
