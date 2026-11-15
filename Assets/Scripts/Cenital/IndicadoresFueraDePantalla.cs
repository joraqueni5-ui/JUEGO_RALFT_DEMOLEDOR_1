using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Flechas en el borde de la zona de juego que apuntan a lo que quedó fuera de la vista: el demoledor y el objeto que está por golpear
/// (rojas) y un poder recién aparecido (verde). Desaparecen en cuanto el objetivo entra en pantalla.
/// </summary>
public class IndicadoresFueraDePantalla : MonoBehaviour
{
    [Header("Qué avisar")]
    public bool avisarDemoledor = true;
    public bool avisarObjetoAmenazado = true;
    public bool avisarPoderes = true;
    [Tooltip("Segundos, desde que aparece, durante los que se avisa de un poder que esté fuera de la vista.")]
    [Min(1f)] public float segundosAvisoPoder = 8f;

    [Header("Aspecto")]
    public Color colorDemoledor = new Color(1f, 0.2f, 0.15f, 0.95f);
    public Color colorObjetoAmenazado = new Color(1f, 0.35f, 0.1f, 0.95f);
    public Color colorPoder = new Color(0.3f, 0.95f, 0.4f, 0.95f);
    [Tooltip("Tamaño de la flecha como fracción del alto de la pantalla.")]
    [Range(0.02f, 0.12f)] public float tamanoRelativo = 0.055f;
    [Tooltip("Distancia al borde de la zona de juego, como fracción del alto de la pantalla.")]
    [Range(0f, 0.1f)] public float margenBorde = 0.035f;

    private Camera camara;
    private DemoledorCenital demoledor;
    private RectTransform raiz;
    private readonly List<Image> flechas = new List<Image>();
    private static Sprite spriteFlecha;

    private void Awake()
    {
        GameObject lienzo = new GameObject("LienzoIndicadores", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        lienzo.transform.SetParent(transform, false);
        Canvas c = lienzo.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 90;
        CanvasScaler e = lienzo.GetComponent<CanvasScaler>();
        e.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;   // 1 unidad = 1 píxel de pantalla
        raiz = (RectTransform)lienzo.transform;
    }

    private static Sprite Flecha()
    {
        if (spriteFlecha != null) return spriteFlecha;

        const int n = 32;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        Color[] px = new Color[n * n];
        Vector2 a = new Vector2(3f, 3f), b = new Vector2(29f, 16f), d = new Vector2(3f, 29f);   // triángulo que apunta a la derecha
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                bool dentro = Dentro(p, a, b, d);
                bool interior = Dentro(p, a + new Vector2(3f, 1.5f), b - new Vector2(4f, 0f), d + new Vector2(3f, -1.5f));
                px[y * n + x] = !dentro ? new Color(0f, 0f, 0f, 0f) : interior ? Color.white : new Color(0.05f, 0.05f, 0.08f, 1f);
            }
        }
        t.SetPixels(px);
        t.filterMode = FilterMode.Point;
        t.Apply();
        t.hideFlags = HideFlags.HideAndDontSave;
        spriteFlecha = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        spriteFlecha.hideFlags = HideFlags.HideAndDontSave;
        return spriteFlecha;
    }

    private static bool Dentro(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Lado(p, a, b), d2 = Lado(p, b, c), d3 = Lado(p, c, a);
        bool neg = d1 < 0f || d2 < 0f || d3 < 0f, pos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(neg && pos);
    }

    private static float Lado(Vector2 p, Vector2 a, Vector2 b) { return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y); }

    private void LateUpdate()
    {
        if (camara == null)
        {
            CamaraPisoCenital cc = FindFirstObjectByType<CamaraPisoCenital>();
            camara = cc != null ? cc.GetComponent<Camera>() : Camera.main;
            if (camara == null) return;
        }
        if (demoledor == null) demoledor = FindFirstObjectByType<DemoledorCenital>();

        int usadas = 0;
        PisoCenital piso = PisoCenital.Actual;
        bool terminado = piso != null && (piso.Rendido || piso.Terminado);

        if (avisarDemoledor && demoledor != null && demoledor.gameObject.activeInHierarchy && !terminado)
            Mostrar(ref usadas, demoledor.transform.position, colorDemoledor);

        if (avisarObjetoAmenazado && demoledor != null && !terminado)
        {
            ObjetoReparable o = demoledor.ObjetivoAmenazado;
            if (o != null) Mostrar(ref usadas, o.Centro, colorObjetoAmenazado);
        }

        // La marca "¡SÍGUELO!" del último piso (verde).
        if (avisarPoderes && MarcaSigueloCenital.Actual != null)
            Mostrar(ref usadas, MarcaSigueloCenital.Actual.transform.position, colorPoder);

        if (avisarPoderes)
        {
            foreach (PoderRecogible p in PoderRecogible.Activos)
                if (p != null && p.Edad < segundosAvisoPoder) Mostrar(ref usadas, p.transform.position, colorPoder);
        }

        for (int i = usadas; i < flechas.Count; i++) flechas[i].gameObject.SetActive(false);
    }

    private void Mostrar(ref int usadas, Vector2 mundo, Color color)
    {
        Vector3 v = camara.WorldToViewportPoint(new Vector3(mundo.x, mundo.y, 0f));
        if (v.z > 0f && v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f) return;   // ya se ve

        Rect pr = camara.pixelRect;
        float lado = Screen.height * tamanoRelativo;
        float margen = Screen.height * margenBorde + lado * 0.5f;
        Vector2 centro = pr.center;
        Vector2 d = new Vector2((v.x - 0.5f) * pr.width, (v.y - 0.5f) * pr.height);
        if (v.z < 0f) d = -d;
        if (d.sqrMagnitude < 0.0001f) d = Vector2.up;

        float mitadX = Mathf.Max(1f, pr.width * 0.5f - margen), mitadY = Mathf.Max(1f, pr.height * 0.5f - margen);
        float t = Mathf.Min(mitadX / Mathf.Max(0.0001f, Mathf.Abs(d.x)), mitadY / Mathf.Max(0.0001f, Mathf.Abs(d.y)));
        Vector2 pos = centro + d * t;

        Image f = Flecha(usadas);
        usadas++;
        f.gameObject.SetActive(true);
        f.color = color;
        RectTransform rt = f.rectTransform;
        rt.sizeDelta = new Vector2(lado, lado);
        rt.anchoredPosition = pos;
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }

    private Image Flecha(int indice)
    {
        while (flechas.Count <= indice)
        {
            GameObject g = new GameObject("Flecha" + (flechas.Count + 1), typeof(RectTransform), typeof(Image));
            g.transform.SetParent(raiz, false);
            Image im = g.GetComponent<Image>();
            im.sprite = Flecha();
            im.raycastTarget = false;
            RectTransform rt = im.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;   // la posición es en píxeles desde la esquina inferior izquierda
            rt.pivot = new Vector2(0.5f, 0.5f);
            flechas.Add(im);
        }
        return flechas[indice];
    }
}
