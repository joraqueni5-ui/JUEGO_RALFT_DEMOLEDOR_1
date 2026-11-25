using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ambiente visual de los niveles: motas de polvo que flotan delante de la cámara y una vineta suave en los bordes de la
/// pantalla. La vineta late en rojo cuando el demoledor está en furia o cuando te queda una sola vida, y en celeste
/// mientras está congelado. InstaladorExtras lo crea solo en cada piso y en el nivel final.
/// </summary>
public class AmbienteNivel : MonoBehaviour
{
    [Tooltip("Cantidad de motas de polvo.")]
    public int motas = 28;
    [Tooltip("Opacidad máxima de la vineta en calma.")]
    [Range(0f, 1f)] public float vinetaCalma = 0.32f;

    private Transform[] particulas;
    private SpriteRenderer[] dibujos;
    private Vector2[] velocidades;
    private float[] fases;
    private Image vineta;
    private Camera cam;
    private DemoledorCenital demoledor;
    private GestorJuego gestor;

    private void Start()
    {
        cam = Camera.main;
        demoledor = FindAnyObjectByType<DemoledorCenital>();
        gestor = FindAnyObjectByType<GestorJuego>();
        CrearMotas();
        CrearVineta();
    }

    private void CrearMotas()
    {
        Sprite mota = ArteExtra.Uno("Tema/mota");
        if (mota == null) mota = Provisional.Circulo;
        particulas = new Transform[motas];
        dibujos = new SpriteRenderer[motas];
        velocidades = new Vector2[motas];
        fases = new float[motas];
        for (int i = 0; i < motas; i++)
        {
            SpriteRenderer sr = Provisional.Crear(transform, "Mota" + i, mota, new Color(1f, 0.97f, 0.85f, 0f), 900);
            float t = Random.Range(0.6f, 1.4f);
            sr.transform.localScale = new Vector3(t * (mota == Provisional.Circulo ? 0.05f : 3f), t * (mota == Provisional.Circulo ? 0.05f : 3f), 1f);
            particulas[i] = sr.transform;
            dibujos[i] = sr;
            velocidades[i] = new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(0.05f, 0.22f));
            fases[i] = Random.value * 10f;
            Reubicar(i, true);
        }
    }

    private void Reubicar(int i, bool enCualquierLado)
    {
        if (cam == null) return;
        float alto = cam.orthographicSize, ancho = alto * cam.aspect;
        Vector3 c = cam.transform.position;
        float y = enCualquierLado ? Random.Range(-alto, alto) : -alto - 0.3f;
        particulas[i].position = new Vector3(c.x + Random.Range(-ancho, ancho), c.y + y, 0f);
    }

    private void CrearVineta()
    {
        GameObject g = new GameObject("ViñetaAmbiente", typeof(RectTransform), typeof(Canvas));
        g.transform.SetParent(transform, false);
        Canvas canvas = g.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -200;   // debajo del HUD y de los menús

        GameObject img = new GameObject("Viñeta", typeof(RectTransform), typeof(Image));
        img.transform.SetParent(g.transform, false);
        RectTransform rt = (RectTransform)img.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        vineta = img.GetComponent<Image>();
        vineta.raycastTarget = false;
        vineta.sprite = CrearSpriteVineta();
        vineta.color = new Color(0f, 0f, 0f, vinetaCalma);
    }

    /// <summary>Degradado radial: transparente al centro y opaco en las esquinas.</summary>
    private static Sprite CrearSpriteVineta()
    {
        const int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.3f, d));
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    private void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || particulas == null) return;

        float alto = cam.orthographicSize, ancho = alto * cam.aspect;
        bool rabia = demoledor != null ? demoledor.EnRabia && demoledor.EnPiso : MultijugadorNivel.RabiaRed;
        Vector3 c = cam.transform.position;
        for (int i = 0; i < particulas.Length; i++)
        {
            Transform p = particulas[i];
            Vector2 v = velocidades[i] + new Vector2(Mathf.Sin(Time.time * 0.7f + fases[i]) * 0.08f, 0f);
            if (rabia) v += Vector2.up * 0.35f;   // en rabia las motas suben como brasas
            p.position += (Vector3)(v * Time.deltaTime);
            Vector3 rel = p.position - c;
            if (rel.y > alto + 0.3f || Mathf.Abs(rel.x) > ancho + 0.5f || rel.y < -alto - 1f) Reubicar(i, false);
            float brillo = 0.12f + 0.12f * Mathf.Sin(Time.time * 1.3f + fases[i]);
            dibujos[i].color = rabia ? new Color(1f, 0.45f + 0.2f * Mathf.Sin(Time.time * 4f + fases[i]), 0.15f, Mathf.Max(0f, brillo * 2.2f))
                                     : new Color(1f, 0.97f, 0.85f, Mathf.Max(0f, brillo));
        }

        if (vineta == null) return;
        bool furia = demoledor != null ? demoledor.EnFuria && demoledor.EnPiso : MultijugadorNivel.FuriaRed;
        bool frio = demoledor != null ? demoledor.Congelado : MultijugadorNivel.FrioRed;
        bool ultima = gestor != null && gestor.Vidas == 1 && !gestor.PartidaTerminada;
        Color objetivo = new Color(0f, 0f, 0f, vinetaCalma);
        if (frio) objetivo = new Color(0.3f, 0.7f, 1f, 0.35f);
        else if (rabia)
        {
            float pulso = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
            objetivo = new Color(0.85f, 0.04f, 0.02f, 0.38f + 0.22f * pulso);
        }
        else if (furia || ultima)
        {
            float pulso = 0.5f + 0.5f * Mathf.Sin(Time.time * (ultima ? 5f : 3f));
            objetivo = new Color(0.75f, 0.05f, 0.03f, 0.3f + 0.2f * pulso);
        }
        vineta.color = Color.Lerp(vineta.color, objetivo, 1f - Mathf.Exp(-6f * Time.deltaTime));
    }
}
