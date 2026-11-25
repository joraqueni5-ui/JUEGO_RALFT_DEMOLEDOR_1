using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// Momentos de pelea del nivel final (los lanza GestorJuego):
///   KO        al reparar la última sección: cámara lenta, destello, "¡K.O.!" y "¡DEMOLEDOR DERROTADO!", el demoledor
///             queda mareado con estrellitas ("¡No... puede... ser...!"). Después siguen los estudiantes que lo lanzan.
///   Victoria  si pierdes en el nivel final: la cámara va al demoledor, que se ríe golpeando el techo ("¡JA, JA, JA!
///             ¡La U es MÍA!"), la pantalla se tiñe de rojo y tiembla todo antes de la pantalla de fin.
/// </summary>
public static class EscenasJefe
{
    // ------------------------------------------------------------ ¡K.O.!
    public static IEnumerator KO(DemoledorAutomatico dem)
    {
        if (dem == null) yield break;
        CamaraSeguimiento cam = Object.FindAnyObjectByType<CamaraSeguimiento>();
        if (cam != null) cam.objetivo = dem.transform;
        Canvas lienzo = Lienzo("EscenaKO");
        Image destello = Velo(lienzo, Color.white, 0.9f);
        Text grande = Texto(lienzo, "¡K.O.!", 190, new Color(1f, 0.85f, 0.25f), new Vector2(0f, 140f));
        Text sub = Texto(lienzo, "", 64, Color.white, new Vector2(0f, -10f));

        GestorAudio.Reproducir(SndExtra.JefeTransforma, 1f, 0.6f);
        GestorAudio.Reproducir(Snd.MenuConfirmar);
        CamaraSeguimiento.Temblar(0.3f, 0.6f);
        Time.timeScale = 0.25f;   // cámara lenta

        float t = 0f;
        while (t < 1.4f)
        {
            t += Time.unscaledDeltaTime;
            if (destello != null) destello.color = new Color(1f, 1f, 1f, Mathf.Max(0f, 0.9f - t * 1.8f));
            if (grande != null) grande.rectTransform.localScale = Vector3.one * (t < 0.2f ? Mathf.Lerp(3f, 1f, t / 0.2f) : 1f + 0.04f * Mathf.Sin(t * 14f));
            if (t > 0.6f && sub.text.Length == 0) { sub.text = "¡DEMOLEDOR DERROTADO!"; GestorAudio.Reproducir(SndExtra.Desinflar); }
            yield return null;
        }
        Time.timeScale = 1f;

        // Queda mareado: estrellitas girando y "no puede ser".
        Estrellas(dem.transform);
        Burbuja.Decir(dem.transform, "¡No... puede... ser...!", Burbuja.Voz.Villano, 1.7f);
        t = 0f;
        while (t < 1.2f)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01((1.2f - t) / 0.4f);
            if (grande != null) grande.color = new Color(1f, 0.85f, 0.25f, a);
            if (sub != null) sub.color = new Color(1f, 1f, 1f, a);
            dem.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 9f) * 8f);
            yield return null;
        }
        dem.transform.rotation = Quaternion.identity;
        Object.Destroy(lienzo.gameObject);
    }

    /// <summary>Estrellitas de mareo sobre la cabeza (giran solas; se van con el demoledor).</summary>
    private static void Estrellas(Transform dem)
    {
        Sprite[] arte = ArteNiveles.De(a => a.estrellasAturdido);
        if (arte == null) return;
        SpriteRenderer sr = Provisional.Crear(dem, "EstrellasMareo", arte[0], Color.white, 40);
        sr.transform.localPosition = new Vector3(0f, 1.45f, 0f);
        float tam = Mathf.Max(0.01f, arte[0].bounds.size.x);
        sr.transform.localScale = Vector3.one * (0.9f / tam);
        EfectoSprite.EnBucle(sr, arte, 8f);
    }

    // ------------------------------------------------------------ el demoledor gana
    public static IEnumerator Victoria(MonoBehaviour quien, DemoledorAutomatico dem)
    {
        if (dem == null) yield break;
        CamaraSeguimiento cam = Object.FindAnyObjectByType<CamaraSeguimiento>();
        if (cam != null) cam.objetivo = dem.transform;
        Canvas lienzo = Lienzo("EscenaDerrota");
        Image rojo = Velo(lienzo, new Color(0.7f, 0f, 0f), 0f);
        Text grande = Texto(lienzo, "¡EL DEMOLEDOR GANÓ!", 110, new Color(1f, 0.35f, 0.3f), new Vector2(0f, 160f));
        grande.color = new Color(1f, 0.35f, 0.3f, 0f);

        GestorAudio.Reproducir(SndExtra.RugidoDemoledor);
        Burbuja.Decir(dem.transform, "¡JA, JA, JA! ¡La U es MÍA!", Burbuja.Voz.Villano, 1.7f);
        quien.StartCoroutine(dem.GolpeDoble(2.6f));   // se ríe golpeando el techo
        float t = 0f, proximoTemblor = 0f;
        while (t < 2.8f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.6f);
            if (rojo != null) rojo.color = new Color(0.7f, 0f, 0f, 0.35f * k + 0.08f * Mathf.Sin(t * 8f));
            if (grande != null)
            {
                grande.color = new Color(1f, 0.35f, 0.3f, k);
                grande.rectTransform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(t * 10f));
            }
            if (t >= proximoTemblor)
            {
                proximoTemblor = t + 0.5f;
                CamaraSeguimiento.Temblar(0.12f, 0.3f);
                TrozoVolador.Lanzar((Vector2)dem.transform.position + Vector2.down * 0.2f, 3);
            }
            yield return null;
        }
    }

    // ------------------------------------------------------------ utilidades de interfaz
    private static Canvas Lienzo(string nombre)
    {
        GameObject g = new GameObject(nombre, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas c = g.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 250;
        CanvasScaler cs = g.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;
        return c;
    }

    private static Image Velo(Canvas c, Color color, float alfa)
    {
        Image im = new GameObject("Velo", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        im.transform.SetParent(c.transform, false);
        RectTransform rt = im.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        im.color = new Color(color.r, color.g, color.b, alfa);
        im.raycastTarget = false;
        return im;
    }

    private static Text Texto(Canvas c, string texto, int tam, Color color, Vector2 pos)
    {
        GameObject g = new GameObject("Texto", typeof(RectTransform), typeof(Text), typeof(Outline));
        g.transform.SetParent(c.transform, false);
        Text t = g.GetComponent<Text>();
        t.font = FabricaUIArcade.Fuente != null ? FabricaUIArcade.Fuente : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = texto;
        t.fontSize = tam;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        Outline o = g.GetComponent<Outline>();
        o.effectColor = Color.black;
        o.effectDistance = new Vector2(5f, -5f);
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1800f, tam * 1.4f);
        rt.anchoredPosition = pos;
        return t;
    }
}
