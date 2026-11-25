using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Nivel final: si el estudiante se cae por debajo de la sección donde está el demoledor, aparece arriba de la pantalla
/// una flecha que late con "¡TE CAÍSTE! ¡VUELVE A SUBIR!" y cuántos pisos le faltan. Se esconde sola al volver.
/// InstaladorExtras lo agrega al nivel final.
/// </summary>
public class IndicadorSubir : MonoBehaviour
{
    private Muneco muneco;
    private GestorJuego gestor;
    private RectTransform grupo, flecha;
    private Text texto;
    private int pisosMostrados = -1;

    private void Start()
    {
        muneco = FindAnyObjectByType<Muneco>();
        gestor = FindAnyObjectByType<GestorJuego>();
        Crear();
    }

    private void Crear()
    {
        GameObject g = new GameObject("IndicadorSubir", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        g.transform.SetParent(transform, false);
        Canvas c = g.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 70;
        CanvasScaler cs = g.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;

        grupo = new GameObject("Grupo", typeof(RectTransform)).GetComponent<RectTransform>();
        grupo.SetParent(c.transform, false);
        grupo.anchorMin = grupo.anchorMax = new Vector2(0.5f, 1f);
        grupo.anchoredPosition = new Vector2(0f, -250f);

        Image im = new GameObject("Flecha", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        im.transform.SetParent(grupo, false);
        Sprite s = ArteExtra.Uno("Controles/icono_flecha");
        im.sprite = s;
        im.preserveAspect = true;
        im.raycastTarget = false;
        im.color = new Color(1f, 0.85f, 0.25f);
        if (s == null) im.color = new Color(1f, 0.85f, 0.25f, 0f);
        flecha = im.rectTransform;
        flecha.sizeDelta = new Vector2(120f, 120f);

        texto = new GameObject("Texto", typeof(RectTransform), typeof(Text), typeof(Outline)).GetComponent<Text>();
        texto.transform.SetParent(grupo, false);
        texto.font = DemoledorUnifranz.UI.Menus.FabricaUIArcade.Fuente != null ? DemoledorUnifranz.UI.Menus.FabricaUIArcade.Fuente
                                                                              : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.fontSize = 44;
        texto.color = new Color(1f, 0.85f, 0.25f);
        texto.alignment = TextAnchor.MiddleCenter;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.raycastTarget = false;
        texto.GetComponent<Outline>().effectColor = Color.black;
        texto.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);
        texto.rectTransform.sizeDelta = new Vector2(1400f, 120f);
        texto.rectTransform.anchoredPosition = new Vector2(0f, -110f);
        grupo.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (grupo == null) return;
        EdificioAlto ed = EdificioAlto.Instancia;
        bool ver = muneco != null && muneco.isActiveAndEnabled && ed != null && !ed.Terminado && !IntroFinalJefe.EnCurso &&
                   (gestor == null || !gestor.PartidaTerminada);
        int pisos = 0;
        if (ver)
        {
            float pisoSeccion = ed.PisoSeccion(ed.SeccionActual);
            float falta = pisoSeccion - muneco.transform.position.y;
            ver = falta > 0.8f;
            float altoPiso = ed.PisosDeSeccion(ed.SeccionActual) > 0
                ? Mathf.Max(0.5f, (ed.AlturaCornisa(ed.SeccionActual) - pisoSeccion) / ed.PisosDeSeccion(ed.SeccionActual))
                : 1.3f;
            pisos = Mathf.Max(1, Mathf.CeilToInt(falta / altoPiso));
        }
        if (grupo.gameObject.activeSelf != ver) grupo.gameObject.SetActive(ver);
        if (!ver) { pisosMostrados = -1; return; }
        if (pisos != pisosMostrados)
        {
            pisosMostrados = pisos;
            texto.text = "¡TE CAÍSTE! ¡VUELVE A SUBIR!\n" + (pisos == 1 ? "1 PISO" : pisos + " PISOS") + " MÁS ARRIBA";
        }
        float t = Time.unscaledTime;
        flecha.anchoredPosition = new Vector2(0f, Mathf.Abs(Mathf.Sin(t * 5f)) * 22f);
        float e = 1f + 0.08f * Mathf.Sin(t * 10f);
        flecha.localScale = new Vector3(e, e, 1f);
    }
}
