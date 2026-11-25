using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 60 cuadros por segundo estables, en PC y en Android, sin quitar nada del juego:
///   - Android venía limitado a 30 fps (lo que Unity pone por defecto): ahora pide 60, sin vsync, y la pantalla no se apaga.
///   - PC: vsync en monitores de 60 Hz; en los de más Hz, tope de 60 (no gasta de más ni se calienta).
///   - En celulares el búfer de color no usa HDR (en este juego no se nota y pesa mucho en GPUs como la del Galaxy A20).
///   - Física: si un cuadro sale lento, no intenta ponerse al día con muchos pasos seguidos (eso es lo que "traba").
///   - Luces 2D: las que están en cero (alarma apagada, furia sin activar, pantallas rotas...) se desactivan para no
///     dibujarlas en vano, y se vuelven a prender solas cuando suben.
///   - Contador de FPS opcional (OPCIONES → CONTROLES → MOSTRAR FPS) para comprobarlo en cada equipo.
/// </summary>
public class Rendimiento : MonoBehaviour
{
    private const string ClaveFps = "opciones.mostrarFps";
    private static Rendimiento inst;

    public static bool MostrarFps
    {
        get { return PlayerPrefs.GetInt(ClaveFps, 0) == 1; }
        set { PlayerPrefs.SetInt(ClaveFps, value ? 1 : 0); PlayerPrefs.Save(); if (inst != null) inst.ActualizarContador(); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Iniciar()
    {
        Configurar();
        GameObject g = new GameObject("[Rendimiento]");
        DontDestroyOnLoad(g);
        inst = g.AddComponent<Rendimiento>();
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        Configurar();
        if (inst != null) { inst.proximoEscaneo = 0f; inst.apagadas.Clear(); }
    }

    private static void Configurar()
    {
        if (Application.isMobilePlatform)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#if !UNITY_EDITOR
            UniversalRenderPipelineAsset urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.supportsHDR = false;
                urp.msaaSampleCount = 1;
            }
#endif
        }
        else
        {
            double hz = Screen.currentResolution.refreshRateRatio.value;
            if (hz > 1.0 && hz < 75.0) { QualitySettings.vSyncCount = 1; Application.targetFrameRate = -1; }
            else { QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60; }
        }
        // Un cuadro lento no dispara 10 pasos de física seguidos (espiral que congela en equipos lentos).
        Time.maximumDeltaTime = 0.1f;
    }

    // ------------------------------------------------------------ luces apagadas
    private readonly List<Light2D> luces = new List<Light2D>();
    private readonly HashSet<Light2D> apagadas = new HashSet<Light2D>();
    private float proximoEscaneo, proximoAjuste;

    private void Update()
    {
        float t = Time.unscaledTime;
        if (t >= proximoEscaneo)
        {
            proximoEscaneo = t + 1.5f;
            luces.Clear();
            luces.AddRange(FindObjectsByType<Light2D>(FindObjectsInactive.Include));
        }
        if (t >= proximoAjuste)
        {
            proximoAjuste = t + 0.1f;
            for (int i = 0; i < luces.Count; i++)
            {
                Light2D l = luces[i];
                if (l == null || l.lightType == Light2D.LightType.Global) continue;
                bool nula = l.intensity <= 0.004f;
                if (nula && l.enabled) { l.enabled = false; apagadas.Add(l); }
                else if (!nula && !l.enabled && apagadas.Contains(l)) { l.enabled = true; apagadas.Remove(l); }
            }
        }
        if (contador != null) ContarFps();
    }

    // ------------------------------------------------------------ contador de FPS
    private Text contador;
    private int cuadros;
    private float desde, peor;

    private void Start() { ActualizarContador(); }

    private void ActualizarContador()
    {
        bool ver = MostrarFps;
        if (ver && contador == null)
        {
            GameObject c = new GameObject("ContadorFps", typeof(Canvas), typeof(CanvasScaler));
            c.transform.SetParent(transform, false);
            Canvas canvas = c.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            CanvasScaler cs = c.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.matchWidthOrHeight = 0.5f;
            GameObject tg = new GameObject("Texto", typeof(RectTransform), typeof(Text), typeof(Outline));
            tg.transform.SetParent(c.transform, false);
            contador = tg.GetComponent<Text>();
            contador.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            contador.fontSize = 30;
            contador.color = new Color(0.5f, 1f, 0.5f);
            contador.raycastTarget = false;
            contador.alignment = TextAnchor.UpperLeft;
            contador.horizontalOverflow = HorizontalWrapMode.Overflow;
            tg.GetComponent<Outline>().effectColor = Color.black;
            RectTransform rt = contador.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(12f, -112f);
            rt.sizeDelta = new Vector2(300f, 40f);
            desde = Time.unscaledTime;
            cuadros = 0;
            peor = 0f;
        }
        if (contador != null) contador.transform.parent.gameObject.SetActive(ver);
    }

    private void ContarFps()
    {
        if (!contador.isActiveAndEnabled) return;
        cuadros++;
        peor = Mathf.Max(peor, Time.unscaledDeltaTime);
        float t = Time.unscaledTime - desde;
        if (t < 0.5f) return;
        int fps = Mathf.RoundToInt(cuadros / t);
        int minimo = peor > 0.0001f ? Mathf.RoundToInt(1f / peor) : fps;
        contador.text = fps + " FPS  (mín " + minimo + ")";
        contador.color = fps >= 57 ? new Color(0.5f, 1f, 0.5f) : fps >= 45 ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.4f, 0.35f);
        cuadros = 0;
        peor = 0f;
        desde = Time.unscaledTime;
    }
}
