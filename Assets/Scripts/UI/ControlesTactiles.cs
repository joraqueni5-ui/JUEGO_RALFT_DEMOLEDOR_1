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
    private static bool furiaPendiente;

    /// <summary>Lee (y borra) la pulsación del botón FURIA (en el celular reemplaza a K + L).</summary>
    public static bool FuriaPulsada()
    {
        bool valor = furiaPendiente;
        furiaPendiente = false;
        return valor;
    }

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
        public Image fondo, icono;
        public Sprite normal, apretado;
        public bool repetir;
        public float proximaRepeticion;
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

    /// <summary>Lee (y borra) la pulsación de reiniciar (ya no hay botón: reiniciar está en el menú de pausa).</summary>
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
        furiaPendiente = false;
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
            palanca = null;
            bool mapa = VerMapa;
            LimpiarEstado();
            if (mapa) VerMapa = false;
            CrearLienzo();
        }

        // El martillo repite solo mientras se mantiene apretado.
        for (int i = 0; i < botones.Count; i++)
        {
            Boton b = botones[i];
            if (b.repetir && b.dedo >= 0 && Time.unscaledTime >= b.proximaRepeticion)
            {
                b.proximaRepeticion = Time.unscaledTime + 0.3f;
                if (b.accion != null) b.accion(true);
            }
            // Pulso suave al soltar (vuelve a su tamaño).
            if (b.area != null && (b != botonFuria || b.dedo >= 0))
            {
                float objetivo = b.dedo >= 0 ? 0.9f : 1f;
                // Solo si cambia: tocar el lienzo cada cuadro obliga a rehacerlo (caro en celulares).
                if (b.area.localScale.x != objetivo)
                {
                    float e = Mathf.MoveTowards(b.area.localScale.x, objetivo, Time.unscaledDeltaTime * 3f);
                    b.area.localScale = new Vector3(e, e, 1f);
                }
            }
        }
        ActualizarPalancaVisual();
        ActualizarBotonFuria();

        Touchscreen pantalla = Touchscreen.current;
        if (pantalla == null) { SoltarTodo(); return; }

        dedosVivos.Clear();
        var toques = pantalla.touches;
        for (int i = 0; i < toques.Count; i++)
        {
            TouchControl toque = toques[i];
            UnityEngine.InputSystem.TouchPhase fase = toque.phase.ReadValue();
            int id = toque.touchId.ReadValue();
            Vector2 pos = toque.position.ReadValue();

            if ((fase == UnityEngine.InputSystem.TouchPhase.Began ||
                 fase == UnityEngine.InputSystem.TouchPhase.Moved ||
                 fase == UnityEngine.InputSystem.TouchPhase.Stationary) && toque.press.isPressed)
            {
                dedosVivos.Add(id);
                if (palanca != null && palanca.dedo == id) { MoverPalanca(pos); continue; }
                if (BotonDeDedo(id) != null) continue;

                Boton boton = BotonEn(pos);
                if (boton != null)
                {
                    if (boton.dedo >= 0) continue;
                    boton.dedo = id;
                    Apretar(boton, true);
                    continue;
                }
                // Toque nuevo en la zona de la palanca: la palanca aparece bajo el pulgar.
                if (palanca != null && palanca.dedo < 0 && fase == UnityEngine.InputSystem.TouchPhase.Began && EnZonaPalanca(pos))
                {
                    palanca.dedo = id;
                    palanca.centro = AcotarCentro(pos);
                    MoverPalanca(pos);
                }
            }
            else
            {
                if (palanca != null && palanca.dedo == id) { SoltarPalanca(); continue; }
                Boton boton = BotonDeDedo(id);
                if (boton == null) continue;

                boton.dedo = -1;
                Apretar(boton, false);
            }
        }

        // Seguro: un dedo que ya no está en la pantalla (su "soltar" se perdió en un cuadro lento, o el sistema reusó el
        // número del toque) suelta su botón. Antes el MARTILLO podía quedar apretado y martillaba sin parar.
        for (int i = 0; i < botones.Count; i++)
        {
            Boton b = botones[i];
            if (b.dedo >= 0 && !dedosVivos.Contains(b.dedo)) { b.dedo = -1; Apretar(b, false); }
        }
        if (palanca != null && palanca.dedo >= 0 && !dedosVivos.Contains(palanca.dedo)) SoltarPalanca();
    }

    private readonly HashSet<int> dedosVivos = new HashSet<int>();

    /// <summary>Suelta todos los botones y la palanca (sin pantalla táctil o al salir de la aplicación).</summary>
    private void SoltarTodo()
    {
        for (int i = 0; i < botones.Count; i++)
        {
            Boton b = botones[i];
            if (b.dedo >= 0) { b.dedo = -1; Apretar(b, false); }
        }
        if (palanca != null && palanca.dedo >= 0) SoltarPalanca();
    }

    private void OnApplicationFocus(bool foco) { if (!foco) SoltarTodo(); }
    private void OnApplicationPause(bool pausa) { if (pausa) SoltarTodo(); }

    // ------------------------------------------------------------ palanca analógica
    private class Palanca
    {
        public RectTransform baseRt, perillaRt;
        public Image baseImg, perillaImg;
        public Vector2 reposo, centro;
        public float radio, diametro;
        public int dedo = -1;
        public Rect zona;
        public bool soloHorizontal;
        /// <summary>Fachada: la palanca también sube y baja (trepar y bajar de repisa); arriba no salta.</summary>
        public bool fachada;
    }

    private Palanca palanca;

    private bool EnZonaPalanca(Vector2 p) { return palanca.zona.Contains(p); }

    private Vector2 AcotarCentro(Vector2 p)
    {
        float m = palanca.diametro * 0.5f + 8f;
        return new Vector2(Mathf.Clamp(p.x, m, Screen.width - m), Mathf.Clamp(p.y, m, Screen.height - m));
    }

    private void MoverPalanca(Vector2 p)
    {
        Vector2 v = (p - palanca.centro) / Mathf.Max(1f, palanca.radio);
        if (v.sqrMagnitude > 1f) v.Normalize();
        palanca.perillaRt.anchoredPosition = palanca.centro + v * palanca.radio;
        palanca.baseRt.anchoredPosition = palanca.centro;

        bool activo = v.magnitude > 0.25f;
        Vector2 n = activo ? v.normalized : Vector2.zero;
        Izquierda = activo && n.x < -0.38f;
        Derecha = activo && n.x > 0.38f;
        if (palanca.fachada)
        {
            // En la fachada hace falta inclinar más para trepar o bajar: caminar en diagonal no baja de la repisa sin querer.
            Arriba = activo && n.y > 0.55f;
            Abajo = activo && n.y < -0.62f;
            return;
        }
        bool arriba = !palanca.soloHorizontal && activo && n.y > 0.38f;
        if (arriba && !Arriba) arribaPendiente = true;
        Arriba = arriba;
        Abajo = !palanca.soloHorizontal && activo && n.y < -0.38f;
    }

    private void SoltarPalanca()
    {
        palanca.dedo = -1;
        palanca.centro = palanca.reposo;
        Izquierda = Derecha = Arriba = Abajo = false;
    }

    private void ActualizarPalancaVisual()
    {
        if (palanca == null) return;
        bool usando = palanca.dedo >= 0;
        if (!usando && (palanca.baseRt.anchoredPosition != palanca.reposo || palanca.perillaRt.anchoredPosition != palanca.reposo))
        {
            Vector2 b = Vector2.Lerp(palanca.baseRt.anchoredPosition, palanca.reposo, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            Vector2 pe = Vector2.Lerp(palanca.perillaRt.anchoredPosition, b, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
            if ((b - palanca.reposo).sqrMagnitude < 0.25f) b = palanca.reposo;            // ya llegó: se deja quieta
            if ((pe - b).sqrMagnitude < 0.25f && b == palanca.reposo) pe = palanca.reposo;
            palanca.baseRt.anchoredPosition = b;
            palanca.perillaRt.anchoredPosition = pe;
        }
        float a = usando ? 0.95f : 0.6f;
        palanca.baseImg.color = new Color(1f, 1f, 1f, Mathf.MoveTowards(palanca.baseImg.color.a, a, Time.unscaledDeltaTime * 4f));
        palanca.perillaImg.color = new Color(1f, 1f, 1f, Mathf.MoveTowards(palanca.perillaImg.color.a, usando ? 1f : 0.8f, Time.unscaledDeltaTime * 4f));
    }

    private void CrearPalanca(Transform raiz, float diametro, Vector2 reposo, Rect zona, bool soloHorizontal)
    {
        // Sensibilidad: con más sensibilidad basta un recorrido más corto del dedo.
        float sens = DemoledorUnifranz.UI.Menus.PreferenciasMenus.SensibilidadJoystick;
        palanca = new Palanca { diametro = diametro, radio = diametro * 0.36f / sens, reposo = reposo, centro = reposo, zona = zona, soloHorizontal = soloHorizontal };
        palanca.baseImg = ImagenUI(raiz, "PalancaBase", Arte("base_joystick"), new Vector2(diametro, diametro), reposo);
        palanca.perillaImg = ImagenUI(raiz, "PalancaPerilla", Arte("palanca"), new Vector2(diametro * 0.46f, diametro * 0.46f), reposo);
        palanca.baseRt = palanca.baseImg.rectTransform;
        palanca.perillaRt = palanca.perillaImg.rectTransform;
        palanca.baseImg.color = new Color(1f, 1f, 1f, 0.6f);
        if (palanca.baseImg.sprite == null) palanca.baseImg.color = new Color(0.09f, 0.11f, 0.16f, 0.5f);
        if (palanca.perillaImg.sprite == null) palanca.perillaImg.color = new Color(1f, 0.76f, 0.1f, 0.9f);
    }

    private static Image ImagenUI(Transform padre, string nombre, Sprite sprite, Vector2 tam, Vector2 posPantalla)
    {
        GameObject o = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        o.transform.SetParent(padre, false);
        RectTransform rt = (RectTransform)o.transform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tam;
        rt.anchoredPosition = posPantalla;
        Image img = o.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    private static Sprite Arte(string nombre) { return ArteExtra.Uno("Controles/" + nombre); }

    // ------------------------------------------------------------ botón FURIA
    private Boton botonFuria;
    private Image cargaFuria;

    private void ActualizarBotonFuria()
    {
        if (botonFuria == null || botonFuria.area == null) return;
        bool enFachada = FuriaFachada.Instancia != null;
        float carga = enFachada ? FuriaFachada.Instancia.Carga : IraEstudiante.CargaLocal;
        bool lista = enFachada ? FuriaFachada.Instancia.Lista : IraEstudiante.ListaLocal;
        bool activa = enFachada ? FuriaFachada.Activa : IraEstudiante.ActivaLocal;
        if (cargaFuria != null)
        {
            cargaFuria.fillAmount = Mathf.Clamp01(carga / 100f);
            Color c = enFachada ? FuriaFachada.Instancia.ColorFuria : IraEstudiante.ColorLocal;
            float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f);
            cargaFuria.color = lista ? Color.Lerp(c, Color.white, p * 0.5f) : new Color(c.r, c.g, c.b, activa ? 0.9f : 0.55f);
        }
        if (botonFuria.icono != null)
        {
            float a = lista || activa ? 1f : 0.45f;
            botonFuria.icono.color = new Color(1f, 1f, 1f, a);
        }
        if (botonFuria.dedo < 0)
        {
            float e = lista ? 1f + 0.12f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) : 1f;
            if (botonFuria.area.localScale.x != e) botonFuria.area.localScale = new Vector3(e, e, 1f);
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
            if (boton.normal != null) boton.fondo.sprite = apretado && boton.apretado != null ? boton.apretado : boton.normal;
            else boton.fondo.color = apretado ? ColorBotonApretado : ColorBoton;
        }
        if (boton.icono != null)
        {
            RectTransform ri = boton.icono.rectTransform;
            ri.anchoredPosition = new Vector2(0f, apretado ? -boton.area.sizeDelta.y * 0.03f : boton.area.sizeDelta.y * 0.02f);
        }
        if (apretado) boton.proximaRepeticion = Time.unscaledTime + 0.35f;
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

        // Todo en píxeles reales de pantalla (1 unidad = 1 píxel): los tamaños salen de los cm y del alto de la pantalla.
        CanvasScaler escalador = lienzo.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        escalador.scaleFactor = 1f;

        bool cenital = Object.FindAnyObjectByType<JugadorCenital>() != null || RedJuego.EsInvitadoEnPartida;
        CrearControles(cenital);
    }

    /// <summary>
    /// Controles con el estilo del juego: a la izquierda una palanca que aparece bajo el pulgar; a la derecha el MARTILLO grande
    /// (amarillo, se puede mantener) y botones redondos para pausa y mapa (en el piso) o saltar, subir y bajar (en la fachada).
    /// Sin botón de reiniciar: eso está en el menú de pausa.
    /// </summary>
    private void CrearControles(bool cenital)
    {
        Transform raiz = lienzoActual.transform;
        float ancho = Screen.width, alto = Screen.height;
        float dpi = Screen.dpi > 1f ? Screen.dpi : 160f;
        float cm = dpi / 2.54f;
        float margen = Mathf.Max(0.35f * cm, alto * 0.035f);

        // Tamaño elegido en OPCIONES → CONTROLES (0,7 a 1,4).
        float tam = DemoledorUnifranz.UI.Menus.PreferenciasMenus.TamanoBotones;
        float diametro = Mathf.Clamp(Mathf.Max(2.6f * cm, alto * 0.32f), 160f, alto * 0.45f) * tam;
        Vector2 reposo = new Vector2(margen + diametro * 0.5f, margen + diametro * 0.5f);
        Rect zona = new Rect(0f, 0f, ancho * 0.45f, alto * 0.78f);
        CrearPalanca(raiz, diametro, reposo, zona, false);
        palanca.fachada = !cenital;

        float grande = Mathf.Clamp(Mathf.Max(1.8f * cm, alto * 0.24f), 120f, alto * 0.34f) * tam;
        float chico = Mathf.Max(1.1f * cm, grande * 0.55f);
        Vector2 abajoDer = new Vector2(1f, 0f);

        // FURIA: al lado del martillo (en la fachada, encima); se va llenando y, llena, late (en el celular reemplaza a K + L).
        {
            float tamFuria = grande * 0.72f;
            Vector2 posFuria = cenital ? new Vector2(-margen * 2f - grande, margen)
                                       : new Vector2(-margen - (grande - tamFuria) * 0.5f, margen * 1.8f + grande);
            BotonNuevo(raiz, "FURIA", abajoDer, posFuria, new Vector2(tamFuria, tamFuria), false,
                Arte("icono_furia"), apretado => { if (apretado) furiaPendiente = true; }, false);
            botonFuria = botones[botones.Count - 1];
            // Carga en círculo detrás del icono.
            Image disco = ImagenUI(botonFuria.area, "Carga", Arte("disco_relleno"), new Vector2(tamFuria, tamFuria), Vector2.zero);
            RectTransform rd = disco.rectTransform;
            rd.anchorMin = rd.anchorMax = new Vector2(0.5f, 0.5f);
            rd.anchoredPosition = Vector2.zero;
            disco.type = Image.Type.Filled;
            disco.fillMethod = Image.FillMethod.Radial360;
            disco.fillOrigin = (int)Image.Origin360.Top;
            disco.fillClockwise = true;
            disco.preserveAspect = true;
            disco.fillAmount = 0f;
            rd.SetSiblingIndex(0);
            cargaFuria = disco;
        }

        // MARTILLO: la acción principal, grande y amarilla.
        Vector2 posMartillo = new Vector2(-margen, margen);
        BotonNuevo(raiz, "MARTILLO", abajoDer, posMartillo, new Vector2(grande, grande), true, Arte("icono_martillo"),
            apretado => { if (apretado) repararPendiente = true; }, true);

        if (cenital)
        {
            // Pausa y mapa: chicos, en columna sobre el martillo.
            float y = margen + grande + margen * 0.8f;
            BotonNuevo(raiz, "MAPA", abajoDer, new Vector2(-margen - (grande - chico) * 0.5f, y), new Vector2(chico, chico), false,
                Arte("icono_mapa"), apretado => VerMapa = apretado, false);
            BotonNuevo(raiz, "PAUSA", new Vector2(1f, 1f), new Vector2(-margen, -alto * 0.12f), new Vector2(chico * 0.85f, chico * 0.85f), false,
                Arte("icono_pausa"), apretado => { if (apretado) pausaPendiente = true; }, false);
            return;
        }

        // Fachada (vista lateral): la palanca camina, trepa y baja; a la derecha solo SALTAR, el MARTILLO y la FURIA.
        float medio = grande * 0.72f;
        BotonNuevo(raiz, "SALTAR", abajoDer, new Vector2(-margen * 2f - grande, margen), new Vector2(medio, medio), false,
            Arte("icono_saltar"), apretado => { if (apretado) saltarPendiente = true; }, false);
        BotonNuevo(raiz, "PAUSA", new Vector2(1f, 1f), new Vector2(-margen, -alto * 0.12f), new Vector2(chico * 0.85f, chico * 0.85f), false,
            Arte("icono_pausa"), apretado => { if (apretado) pausaPendiente = true; }, false);
    }

    private void BotonNuevo(Transform padre, string etiqueta, Vector2 ancla, Vector2 posicion, Vector2 tamano, bool accion,
        Sprite icono, System.Action<bool> alApretar, bool repetir, float giroIcono = 0f)
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
        imagen.raycastTarget = false;
        imagen.preserveAspect = true;
        Sprite normal = Arte(accion ? "boton_accion" : "boton");
        Sprite apretado = Arte(accion ? "boton_accion_apretado" : "boton_apretado");
        Boton b = new Boton { area = rt, fondo = imagen, accion = alApretar, repetir = repetir, normal = normal, apretado = apretado };
        if (normal != null)
        {
            imagen.sprite = normal;
            imagen.color = Color.white;
        }
        else
        {
            imagen.color = ColorBoton;
            Outline borde = objeto.AddComponent<Outline>();
            borde.effectColor = ColorBorde;
            borde.effectDistance = new Vector2(2f, 2f);
        }

        if (icono != null)
        {
            Image ico = ImagenUI(objeto.transform, "Icono", icono, tamano * 0.62f, Vector2.zero);
            RectTransform ri = ico.rectTransform;
            ri.anchorMin = ri.anchorMax = new Vector2(0.5f, 0.5f);
            ri.anchoredPosition = new Vector2(0f, tamano.y * 0.02f);
            ri.localRotation = Quaternion.Euler(0f, 0f, giroIcono);
            b.icono = ico;
        }
        else
        {
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
            texto.fontSize = Mathf.RoundToInt(tamano.y * 0.2f);
            texto.fontStyle = FontStyle.Bold;
            texto.color = new Color(1f, 1f, 1f, 0.95f);
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.raycastTarget = false;
        }
        botones.Add(b);
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
