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

    private static readonly Color ColorBoton = new Color(0.09f, 0.11f, 0.16f, 0.55f);
    private static readonly Color ColorBotonApretado = new Color(0.98f, 0.75f, 0.15f, 0.75f);
    private static readonly Color ColorBorde = new Color(1f, 1f, 1f, 0.45f);

    private class Boton
    {
        public RectTransform area;
        public Image fondo;
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

        if (Object.FindAnyObjectByType<Muneco>() == null) return;
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
    }

    private void Awake()
    {
        CrearLienzo();
    }

    private void OnDestroy()
    {
        LimpiarEstado();
    }

    private void Update()
    {
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
        if (boton.fondo != null) boton.fondo.color = apretado ? ColorBotonApretado : ColorBoton;
        if (boton.accion != null) boton.accion(apretado);
    }

    // ------------------------------------------------------------ construcción
    private void CrearLienzo()
    {
        GameObject lienzo = new GameObject("Controles", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        lienzo.transform.SetParent(transform, false);

        Canvas canvas = lienzo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;

        CanvasScaler escalador = lienzo.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        Transform raiz = lienzo.transform;

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
