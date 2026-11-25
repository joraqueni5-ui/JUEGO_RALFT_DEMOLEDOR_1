using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DemoledorUnifranz.Game;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Ventana de opciones:
    /// - Volumen general (slider) y sonido activado/desactivado (ConfiguracionAudio).
    /// - Vibración: solo se muestra en el celular.
    /// - Pantalla completa: solo se muestra en computadora.
    /// Todo se guarda al momento; no hay opciones que no funcionen en la plataforma actual.
    /// </summary>
    public class MenuOpcionesArcade : MonoBehaviour
    {
        [Header("Audio")]
        [Tooltip("Volumen de la música (0 a 100 %).")]
        public Slider sliderVolumen;
        public Text textoVolumen;
        [Tooltip("Volumen de los efectos del juego y de la interfaz (0 a 100 %).")]
        public Slider sliderEfectos;
        public Text textoEfectos;
        public Toggle toggleSonido;
        public Text textoSonido;

        [Header("Celular")]
        public GameObject filaVibracion;
        public Toggle toggleVibracion;
        public Text textoVibracion;

        [Header("Computadora")]
        public GameObject filaPantallaCompleta;
        public Toggle togglePantallaCompleta;
        public Text textoPantallaCompleta;

        private bool sincronizando;
        private GameObject confirmacion;
        private Button botonBorrar, botonSi, botonNo;

        private void Awake()
        {
            if (sliderVolumen != null) sliderVolumen.onValueChanged.AddListener(CambiarVolumen);
            if (sliderEfectos != null) sliderEfectos.onValueChanged.AddListener(CambiarEfectos);
            if (toggleSonido != null) toggleSonido.onValueChanged.AddListener(CambiarSonido);
            if (toggleVibracion != null) toggleVibracion.onValueChanged.AddListener(CambiarVibracion);
            if (togglePantallaCompleta != null) togglePantallaCompleta.onValueChanged.AddListener(CambiarPantallaCompleta);

            if (filaVibracion != null) filaVibracion.SetActive(PreferenciasMenus.EsCelular);
            if (filaPantallaCompleta != null) filaPantallaCompleta.SetActive(PreferenciasMenus.EsComputadora);
            CrearBorrarProgreso();
        }

        // ------------------------------------------------------------ borrar progreso
        /// <summary>Agrega el botón BORRAR PROGRESO sobre VOLVER, con su ventana de confirmación.</summary>
        private void CrearBorrarProgreso()
        {
            Transform volver = null;
            foreach (Button b in GetComponentsInChildren<Button>(true)) if (b.name == "BotonVolver") { volver = b.transform; break; }
            if (volver == null) return;
            RectTransform caja = (RectTransform)volver.parent;
            KitSpritesArcade kit = KitSpritesArcade.Actual();

            botonBorrar = FabricaUIArcade.CrearBoton(caja, "BotonBorrarProgreso", "BORRAR PROGRESO", kit.botonRojo, new Vector2(430f, 80f), 28, null, kit);
            FabricaUIArcade.Colocar((RectTransform)botonBorrar.transform, new Vector2(0.5f, 0f), new Vector2(-230f, 216f), new Vector2(430f, 80f));
            botonBorrar.onClick.AddListener(PedirConfirmacion);
            CrearControles(caja, kit, volver);

            // Ventana de confirmación sobre todo el menú.
            Canvas lienzo = GetComponentInParent<Canvas>();
            Transform raiz = lienzo != null ? lienzo.transform : transform;
            RectTransform velo = FabricaUIArcade.CrearImagen(raiz, "ConfirmarBorrado", null, new Color(0f, 0f, 0f, 0.75f), true).rectTransform;
            FabricaUIArcade.Estirar(velo);
            confirmacion = velo.gameObject;

            Image cuadro = FabricaUIArcade.CrearImagen(velo, "Cuadro", null, new Color(0.08f, 0.09f, 0.16f, 1f));
            FabricaUIArcade.Centrar(cuadro.rectTransform, Vector2.zero, new Vector2(900f, 420f));
            Text pregunta = FabricaUIArcade.CrearTexto(cuadro.transform, "Pregunta", "¿BORRAR TODO EL PROGRESO?\nSe pierden las estrellas, los logros y los premios.", 34,
                PaletaArcade.Crema, TextAnchor.MiddleCenter, 4f, 0f, true);
            pregunta.horizontalOverflow = HorizontalWrapMode.Wrap;
            FabricaUIArcade.Colocar(pregunta.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(820f, 180f));

            botonSi = FabricaUIArcade.CrearBoton(cuadro.transform, "BotonSi", "SÍ, BORRAR", kit.botonRojo, new Vector2(360f, 90f), 34, null, kit);
            FabricaUIArcade.Colocar((RectTransform)botonSi.transform, new Vector2(0.5f, 0.5f), new Vector2(-200f, -120f), new Vector2(360f, 90f));
            botonNo = FabricaUIArcade.CrearBoton(cuadro.transform, "BotonNo", "NO", kit.botonAmarillo, new Vector2(360f, 90f), 34, null, kit);
            FabricaUIArcade.Colocar((RectTransform)botonNo.transform, new Vector2(0.5f, 0.5f), new Vector2(200f, -120f), new Vector2(360f, 90f));
            botonSi.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = botonNo, selectOnLeft = botonNo };
            botonNo.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = botonSi, selectOnLeft = botonSi };
            botonSi.onClick.AddListener(ConfirmarBorrado);
            botonNo.onClick.AddListener(CancelarBorrado);
            confirmacion.SetActive(false);
        }

        // ------------------------------------------------------------ controles (celular)
        private GameObject ventanaControles;
        private Button botonControles, botonListo;
        private Slider sliderTamano, sliderSensibilidad;
        private Text valorTamano, valorSensibilidad, estadoVibrarGolpes, estadoFps;
        private Toggle toggleVibrarGolpes, toggleFps;

        /// <summary>Botón CONTROLES (al lado de BORRAR PROGRESO) con su ventana: tamaño de los botones táctiles, sensibilidad de
        /// la palanca y vibración al recibir golpes.</summary>
        private void CrearControles(RectTransform caja, KitSpritesArcade kit, Transform volver)
        {
            botonControles = FabricaUIArcade.CrearBoton(caja, "BotonControles", "CONTROLES", kit.botonAzul, new Vector2(430f, 80f), 28, null, kit);
            FabricaUIArcade.Colocar((RectTransform)botonControles.transform, new Vector2(0.5f, 0f), new Vector2(230f, 216f), new Vector2(430f, 80f));
            botonControles.onClick.AddListener(AbrirControles);
            Button bv = volver != null ? volver.GetComponent<Button>() : null;
            if (bv != null)
            {
                botonBorrar.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = botonControles, selectOnDown = bv, selectOnUp = toggleSonido };
                botonControles.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = botonBorrar, selectOnDown = bv, selectOnUp = toggleSonido };
            }

            Canvas lienzo = GetComponentInParent<Canvas>();
            Transform raiz = lienzo != null ? lienzo.transform : transform;
            RectTransform velo = FabricaUIArcade.CrearImagen(raiz, "VentanaControles", null, new Color(0f, 0f, 0f, 0.8f), true).rectTransform;
            FabricaUIArcade.Estirar(velo);
            ventanaControles = velo.gameObject;
            Image cuadro = FabricaUIArcade.CrearImagen(velo, "Cuadro", kit.panel, Color.white);
            FabricaUIArcade.Centrar(cuadro.rectTransform, Vector2.zero, new Vector2(1060f, 720f));
            Text titulo = FabricaUIArcade.CrearTexto(cuadro.transform, "Titulo", "CONTROLES", 52, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, 4f, 4f);
            FabricaUIArcade.Colocar(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 70f));

            sliderTamano = FilaSlider(cuadro.transform, kit, "TAMAÑO DE LOS BOTONES", 170f, 0.7f, 1.4f, out valorTamano);
            sliderSensibilidad = FilaSlider(cuadro.transform, kit, "SENSIBILIDAD DE LA PALANCA", 20f, 0.6f, 1.6f, out valorSensibilidad);
            Text et = FabricaUIArcade.CrearTexto(cuadro.transform, "EtVibrar", "VIBRAR CON LOS GOLPES", 32, PaletaArcade.Crema, TextAnchor.MiddleLeft, 3f, 0f);
            FabricaUIArcade.Centrar(et.rectTransform, new Vector2(-150f, -110f), new Vector2(600f, 60f));
            toggleVibrarGolpes = FabricaUIArcade.CrearToggle(cuadro.transform, "ToggleVibrar", kit, 56f, out estadoVibrarGolpes);
            FabricaUIArcade.Centrar((RectTransform)toggleVibrarGolpes.transform, new Vector2(330f, -110f), new Vector2(356f, 56f));
            Text etFps = FabricaUIArcade.CrearTexto(cuadro.transform, "EtFps", "MOSTRAR FPS", 32, PaletaArcade.Crema, TextAnchor.MiddleLeft, 3f, 0f);
            FabricaUIArcade.Centrar(etFps.rectTransform, new Vector2(-150f, -165f), new Vector2(600f, 56f));
            toggleFps = FabricaUIArcade.CrearToggle(cuadro.transform, "ToggleFps", kit, 56f, out estadoFps);
            FabricaUIArcade.Centrar((RectTransform)toggleFps.transform, new Vector2(330f, -165f), new Vector2(356f, 52f));
            Text nota = FabricaUIArcade.CrearTexto(cuadro.transform, "Nota", "Botones y palanca: para el celular, se aplican al empezar el próximo piso.", 22,
                new Color(0.8f, 0.84f, 0.95f), TextAnchor.MiddleCenter, 2f, 0f, true);
            FabricaUIArcade.Centrar(nota.rectTransform, new Vector2(0f, -214f), new Vector2(950f, 30f));

            botonListo = FabricaUIArcade.CrearBoton(cuadro.transform, "BotonListo", "LISTO", kit.botonAmarillo, new Vector2(360f, 90f), 36, null, kit);
            FabricaUIArcade.Colocar((RectTransform)botonListo.transform, new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(360f, 90f));
            botonListo.onClick.AddListener(CerrarControles);

            sliderTamano.onValueChanged.AddListener(v => { if (sincronizando) return; PreferenciasMenus.TamanoBotones = v; RefrescarControles(); ServiciosMenus.Instancia.Tic(); });
            sliderSensibilidad.onValueChanged.AddListener(v => { if (sincronizando) return; PreferenciasMenus.SensibilidadJoystick = v; RefrescarControles(); ServiciosMenus.Instancia.Tic(); });
            toggleVibrarGolpes.onValueChanged.AddListener(v => { if (sincronizando) return; PreferenciasMenus.VibracionJuego = v; RefrescarControles(); PreferenciasMenus.Vibrar(90); });
            toggleFps.onValueChanged.AddListener(v => { if (sincronizando) return; Rendimiento.MostrarFps = v; RefrescarControles(); ServiciosMenus.Instancia.Tic(); });
            FabricaUIArcade.NavegacionVertical(sliderTamano, sliderSensibilidad, toggleVibrarGolpes, toggleFps, botonListo);
            ventanaControles.SetActive(false);
        }

        private Slider FilaSlider(Transform padre, KitSpritesArcade kit, string etiqueta, float y, float min, float max, out Text valor)
        {
            Text et = FabricaUIArcade.CrearTexto(padre, "Et" + etiqueta, etiqueta, 32, PaletaArcade.Crema, TextAnchor.MiddleLeft, 3f, 0f);
            FabricaUIArcade.Centrar(et.rectTransform, new Vector2(-60f, y + 50f), new Vector2(780f, 50f));
            Slider s = FabricaUIArcade.CrearSlider(padre, "Slider" + etiqueta, kit, new Vector2(640f, 56f));
            FabricaUIArcade.Centrar((RectTransform)s.transform, new Vector2(-80f, y - 14f), new Vector2(640f, 56f));
            s.minValue = min;
            s.maxValue = max;
            valor = FabricaUIArcade.CrearTexto(padre, "Valor" + etiqueta, "100%", 32, PaletaArcade.Crema, TextAnchor.MiddleRight, 3f, 0f);
            FabricaUIArcade.Centrar(valor.rectTransform, new Vector2(350f, y - 14f), new Vector2(160f, 56f));
            return s;
        }

        private void AbrirControles()
        {
            if (ventanaControles == null) return;
            sincronizando = true;
            sliderTamano.value = PreferenciasMenus.TamanoBotones;
            sliderSensibilidad.value = PreferenciasMenus.SensibilidadJoystick;
            toggleVibrarGolpes.isOn = PreferenciasMenus.VibracionJuego;
            if (toggleFps != null) toggleFps.isOn = Rendimiento.MostrarFps;
            sincronizando = false;
            RefrescarControles();
            ventanaControles.SetActive(true);
            ventanaControles.transform.SetAsLastSibling();
            ServiciosMenus.Instancia.Clic();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(sliderTamano.gameObject);
        }

        public void CerrarControles()
        {
            if (ventanaControles != null) ventanaControles.SetActive(false);
            if (botonControles != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(botonControles.gameObject);
        }

        /// <summary>True si la ventana de controles está abierta (ESC la cierra primero).</summary>
        public bool ControlesAbiertos { get { return ventanaControles != null && ventanaControles.activeSelf; } }

        private void RefrescarControles()
        {
            if (valorTamano != null) valorTamano.text = Mathf.RoundToInt(PreferenciasMenus.TamanoBotones * 100f) + "%";
            if (valorSensibilidad != null) valorSensibilidad.text = Mathf.RoundToInt(PreferenciasMenus.SensibilidadJoystick * 100f) + "%";
            PonerEstado(estadoVibrarGolpes, PreferenciasMenus.VibracionJuego);
            if (estadoFps != null) PonerEstado(estadoFps, Rendimiento.MostrarFps);
        }

        private void PedirConfirmacion()
        {
            if (confirmacion == null) return;
            confirmacion.SetActive(true);
            confirmacion.transform.SetAsLastSibling();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(botonNo.gameObject);
        }

        private void ConfirmarBorrado()
        {
            GameSession.BorrarProgreso();
            Progreso.Borrar();
            CancelarBorrado();
            ServiciosMenus.Instancia.Clic();
        }

        private void CancelarBorrado()
        {
            if (confirmacion != null) confirmacion.SetActive(false);
            if (botonBorrar != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(botonBorrar.gameObject);
        }

        private void OnEnable()
        {
            Sincronizar();
        }

        /// <summary>Muestra en los controles los valores guardados.</summary>
        public void Sincronizar()
        {
            sincronizando = true;
            float volumen = ConfiguracionAudio.Volumen;
            bool sonido = ConfiguracionAudio.SonidoActivado;

            if (sliderVolumen != null) sliderVolumen.value = volumen;
            if (sliderEfectos != null) sliderEfectos.value = ConfiguracionAudio.VolumenEfectos;
            if (toggleSonido != null) toggleSonido.isOn = sonido;
            if (toggleVibracion != null) toggleVibracion.isOn = PreferenciasMenus.Vibracion;
            if (togglePantallaCompleta != null) togglePantallaCompleta.isOn = PreferenciasMenus.PantallaCompleta;
            Refrescar();
            sincronizando = false;
        }

        private void CambiarVolumen(float valor)
        {
            if (sincronizando) return;
            ConfiguracionAudio.Volumen = valor;
            Refrescar();
            ServiciosMenus.Instancia.Tic();
        }

        private void CambiarEfectos(float valor)
        {
            if (sincronizando) return;
            ConfiguracionAudio.VolumenEfectos = valor;
            Refrescar();
            ServiciosMenus.Instancia.Tic();
        }

        private void CambiarSonido(bool activado)
        {
            if (sincronizando) return;
            ConfiguracionAudio.SonidoActivado = activado;
            Refrescar();
            if (activado) ServiciosMenus.Instancia.Clic();
        }

        private void CambiarVibracion(bool activada)
        {
            if (sincronizando) return;
            PreferenciasMenus.Vibracion = activada;
            Refrescar();
            PreferenciasMenus.VibrarCorto();
        }

        private void CambiarPantallaCompleta(bool activada)
        {
            if (sincronizando) return;
            PreferenciasMenus.PantallaCompleta = activada;
            Refrescar();
            ServiciosMenus.Instancia.Clic();
        }

        private void Refrescar()
        {
            float volumen = ConfiguracionAudio.Volumen;
            bool sonido = ConfiguracionAudio.SonidoActivado;

            if (textoVolumen != null) textoVolumen.text = Mathf.RoundToInt(volumen * 100f) + "%";
            if (sliderVolumen != null) sliderVolumen.interactable = sonido;
            if (textoEfectos != null) textoEfectos.text = Mathf.RoundToInt(ConfiguracionAudio.VolumenEfectos * 100f) + "%";
            if (sliderEfectos != null) sliderEfectos.interactable = sonido;
            PonerEstado(textoSonido, sonido);
            if (toggleVibracion != null) PonerEstado(textoVibracion, toggleVibracion.isOn);
            if (togglePantallaCompleta != null) PonerEstado(textoPantallaCompleta, togglePantallaCompleta.isOn);
        }

        private static void PonerEstado(Text texto, bool activo)
        {
            if (texto == null) return;
            texto.text = activo ? "SÍ" : "NO";
            texto.color = activo ? PaletaArcade.Amarillo : new Color(0.75f, 0.78f, 0.88f);
        }
    }
}
