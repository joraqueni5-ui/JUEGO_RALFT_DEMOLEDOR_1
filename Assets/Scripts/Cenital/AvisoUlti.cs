using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aviso en pantalla de la ulti del demoledor: los bordes de la pantalla se tiñen de rojo (palpitando) y arriba aparece el cartel
/// aviso_lluvia_escombros con el texto. Sirve también para mensajes sin bordes rojos (por ejemplo "¡ESTÁ AGOTADO!").
/// Se destruye solo al acabar su tiempo (con el tiempo del juego: la pausa lo detiene).
/// </summary>
public class AvisoUlti : MonoBehaviour
{
    private static AvisoUlti actual;

    /// <summary>Hay un aviso grande de la ulti en pantalla.</summary>
    public static bool Activo { get { return actual != null; } }

    private float fin, duracion;
    private bool bordes;
    private Image[] franjas;
    private Image cartel;
    private Text texto;
    private Sprite[] cuadros;
    private float inicio;

    /// <summary>Muestra el aviso 'segundos'. Reemplaza al que hubiera.</summary>
    public static AvisoUlti Mostrar(string mensaje, float segundos, bool bordesRojos, Color colorTexto)
    {
        Ocultar();

        GameObject g = new GameObject("AvisoUlti", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        AvisoUlti a = g.AddComponent<AvisoUlti>();
        actual = a;
        a.duracion = Mathf.Max(0.3f, segundos);
        a.inicio = Time.time;
        a.fin = Time.time + a.duracion;
        a.bordes = bordesRojos;

        Canvas c = g.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 118;
        CanvasScaler e = g.GetComponent<CanvasScaler>();
        e.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        e.referenceResolution = new Vector2(1920f, 1080f);
        e.matchWidthOrHeight = 0.5f;

        if (bordesRojos)
        {
            a.franjas = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                Image f = Crear(g.transform, "Borde" + i);
                f.color = new Color(1f, 0.05f, 0.03f, 0f);
                RectTransform rt = f.rectTransform;
                const float grosor = 70f;
                if (i == 0) { rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f); rt.sizeDelta = new Vector2(0f, grosor); }
                else if (i == 1) { rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f); rt.pivot = new Vector2(0.5f, 0f); rt.sizeDelta = new Vector2(0f, grosor); }
                else if (i == 2) { rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 0.5f); rt.sizeDelta = new Vector2(grosor, 0f); }
                else { rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(1f, 0.5f); rt.sizeDelta = new Vector2(grosor, 0f); }
                a.franjas[i] = f;
            }
        }

        // Cartel arriba (debajo del HUD) con el texto encima.
        a.cuadros = ArteNiveles.De(x => x.avisoLluviaEscombros);
        a.cartel = Crear(g.transform, "Cartel");
        RectTransform rc = a.cartel.rectTransform;
        rc.anchorMin = rc.anchorMax = new Vector2(0.5f, 1f);
        rc.pivot = new Vector2(0.5f, 1f);
        rc.anchoredPosition = new Vector2(0f, -170f);
        rc.sizeDelta = new Vector2(768f, 192f);
        if (a.cuadros != null) { a.cartel.sprite = a.cuadros[0]; a.cartel.color = Color.white; }
        else a.cartel.color = new Color(0.1f, 0.05f, 0.05f, 0.85f);

        GameObject t = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        t.transform.SetParent(g.transform, false);
        RectTransform rt2 = (RectTransform)t.transform;
        rt2.anchorMin = rt2.anchorMax = new Vector2(0.5f, 1f);
        rt2.pivot = new Vector2(0.5f, 1f);
        rt2.anchoredPosition = new Vector2(0f, -170f);
        rt2.sizeDelta = new Vector2(760f, 192f);
        a.texto = t.GetComponent<Text>();
        a.texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        a.texto.text = mensaje;
        a.texto.fontSize = 52;
        a.texto.fontStyle = FontStyle.Bold;
        a.texto.alignment = TextAnchor.MiddleCenter;
        a.texto.color = colorTexto;
        a.texto.horizontalOverflow = HorizontalWrapMode.Wrap;
        a.texto.raycastTarget = false;
        Outline o = t.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.9f);
        o.effectDistance = new Vector2(3f, -3f);
        return a;
    }

    private static Image Crear(Transform padre, string nombre)
    {
        GameObject g = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(padre, false);
        Image i = g.GetComponent<Image>();
        i.raycastTarget = false;
        return i;
    }

    public static void Ocultar()
    {
        if (actual != null) Destroy(actual.gameObject);
        actual = null;
    }

    private void Update()
    {
        if (Time.time >= fin) { Destroy(gameObject); return; }

        float t = Time.time - inicio;
        if (bordes && franjas != null)
        {
            float pulso = 0.35f + 0.25f * Mathf.Abs(Mathf.Sin(t * 7f));
            float entrada = Mathf.Clamp01(t / 0.25f) * Mathf.Clamp01((fin - Time.time) / 0.3f);
            foreach (Image f in franjas) f.color = new Color(1f, 0.05f, 0.03f, pulso * entrada);
        }
        if (cuadros != null && cartel != null) cartel.sprite = cuadros[(int)(t * 6f) % cuadros.Length];
        if (texto != null) texto.transform.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(t * 10f));
    }

    private void OnDestroy() { if (actual == this) actual = null; }
}
