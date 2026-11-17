using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "¡TERREMOTO!" del nivel final. Cuando la energía del demoledor baja al umbral (40 % por defecto) se activa
/// una sola vez: aviso de unos segundos (el demoledor deja de atacar y golpea el techo con los dos puños, aparece
/// el texto en grande, la cámara tiembla y la vereda se agrieta y parpadea) y después la vereda se derrumba. Si el
/// muñeco estaba parado en ella, cae al vacío y pierde una vida. El demoledor sigue atacando normalmente.
/// Al reiniciar el nivel la vereda vuelve a estar firme y el terremoto se puede activar otra vez.
/// </summary>
[RequireComponent(typeof(EdificioAlto))]
public class TerremotoFachada : MonoBehaviour
{
    [Tooltip("Energía del demoledor (%) a la que se activa el terremoto.")]
    [Range(1f, 99f)] public float umbralTerremoto = 40f;
    [Tooltip("Segundos del aviso antes de que la vereda se derrumbe.")]
    [Min(0.5f)] public float duracionAviso = 2f;
    [Tooltip("Golpes al techo con los dos puños durante el aviso (uno cada duracionAviso / golpesAviso segundos: 4 en 2 s = uno cada 0,5 s).")]
    [Min(1)] public int golpesAviso = 4;
    [Tooltip("Fuerza del temblor de pantalla durante el aviso.")]
    public float fuerzaTemblor = 0.14f;
    public string textoAviso = "¡TERREMOTO!";
    [Tooltip("Tamaño de las letras del aviso.")]
    public int tamanoLetra = 130;

    private EdificioAlto edificio;
    private EnergiaDemoledorFachada energia;
    private DemoledorAutomatico demoledor;
    private Muneco muneco;
    private GestorJuego gestor;
    private VeredaFachada vereda;
    private Coroutine secuencia;
    private bool activado;
    private GameObject lienzo;

    /// <summary>El terremoto está ocurriendo ahora (aviso y derrumbe).</summary>
    public bool EnCurso { get { return secuencia != null; } }

    /// <summary>El terremoto ya se activó en esta partida.</summary>
    public bool Activado { get { return activado; } }

    private void Start()
    {
        edificio = GetComponent<EdificioAlto>();
        energia = GetComponent<EnergiaDemoledorFachada>();
        demoledor = FindFirstObjectByType<DemoledorAutomatico>();
        muneco = FindFirstObjectByType<Muneco>();
        gestor = FindFirstObjectByType<GestorJuego>();
        vereda = GetComponentInChildren<VeredaFachada>();
    }

    private void Update()
    {
        if (activado || energia == null || edificio == null || edificio.Terminado) return;
        if (gestor != null && (gestor.PartidaTerminada || gestor.EstaEnPausa)) return;
        if (energia.Porcentaje <= umbralTerremoto) secuencia = StartCoroutine(Secuencia());
    }

    /// <summary>Vuelve todo a su estado inicial (al reiniciar con R o reintentar).</summary>
    public void Reiniciar()
    {
        if (secuencia != null) { StopCoroutine(secuencia); secuencia = null; }
        activado = false;
        QuitarTexto();
        if (vereda == null) vereda = GetComponentInChildren<VeredaFachada>();
        if (vereda != null) vereda.Restablecer();
    }

    private IEnumerator Secuencia()
    {
        activado = true;
        GestorAudio.Reproducir(Snd.Terremoto);
        if (vereda == null) vereda = GetComponentInChildren<VeredaFachada>();

        // 1) Aviso: el demoledor golpea el techo con los dos puños, la cámara tiembla y la vereda se agrieta.
        if (demoledor != null) demoledor.Pausar();
        if (vereda != null) vereda.Agrietar();
        MostrarTexto();

        // 'golpesAviso' golpes al techo con los dos puños, repartidos en 'duracionAviso' segundos.
        float intervalo = duracionAviso / Mathf.Max(1, golpesAviso);
        int golpes = 0;
        float t = 0f, proximoTemblor = 0f;
        while (t < duracionAviso)
        {
            if (gestor == null || !gestor.EstaEnPausa)
            {
                t += Time.deltaTime;
                if (golpes < golpesAviso && t >= golpes * intervalo)
                {
                    if (demoledor != null) demoledor.GolpeAlTecho();
                    golpes++;
                }
                if (t >= proximoTemblor)
                {
                    CamaraSeguimiento.Temblar(fuerzaTemblor, 0.3f);
                    proximoTemblor = t + 0.25f;
                }
                PulsarTexto(t);
            }
            yield return null;
        }
        if (demoledor != null) demoledor.TerminarGolpes();

        // 2) La vereda se derrumba; si el muñeco estaba parado en ella, cae al vacío.
        bool enLaVereda = muneco != null && vereda != null && vereda.Firme &&
                          muneco.transform.position.y <= vereda.SuperficieY + 0.6f && muneco.EnSuelo;
        if (vereda != null) vereda.Derrumbar();
        if (enLaVereda) muneco.CaerAlVacioForzado();
        QuitarTexto();

        // 3) El demoledor sigue atacando normalmente.
        if (demoledor != null) demoledor.Reanudar();
        secuencia = null;
    }

    // ------------------------------------------------------------ texto en grande
    private void MostrarTexto()
    {
        QuitarTexto();
        lienzo = new GameObject("TerremotoTexto", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas c = lienzo.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 120;
        CanvasScaler esc = lienzo.GetComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 0.5f;

        GameObject g = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        g.transform.SetParent(lienzo.transform, false);
        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.55f);
        rt.sizeDelta = new Vector2(1700f, 220f);
        Text texto = g.GetComponent<Text>();
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.fontSize = tamanoLetra;
        texto.fontStyle = FontStyle.Bold;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = new Color(1f, 0.85f, 0.2f, 1f);
        texto.text = textoAviso;
        texto.raycastTarget = false;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        Outline o = g.AddComponent<Outline>();
        o.effectColor = new Color(0.55f, 0.05f, 0.02f, 1f);
        o.effectDistance = new Vector2(6f, -6f);
    }

    private void PulsarTexto(float t)
    {
        if (lienzo == null) return;
        Transform g = lienzo.transform.GetChild(0);
        float k = 1f + 0.08f * Mathf.Sin(t * 22f);
        g.localScale = new Vector3(k, k, 1f);
        g.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 30f) * 2f);
    }

    private void QuitarTexto()
    {
        if (lienzo != null) Destroy(lienzo);
        lienzo = null;
    }

    private void OnDestroy()
    {
        QuitarTexto();
    }
}
