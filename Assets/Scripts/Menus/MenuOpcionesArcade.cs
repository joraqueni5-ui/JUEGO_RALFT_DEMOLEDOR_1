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

            botonBorrar = FabricaUIArcade.CrearBoton(caja, "BotonBorrarProgreso", "BORRAR PROGRESO", kit.botonRojo, new Vector2(520f, 80f), 30, null, kit);
            FabricaUIArcade.Colocar((RectTransform)botonBorrar.transform, new Vector2(0.5f, 0f), new Vector2(0f, 216f), new Vector2(520f, 80f));
            botonBorrar.onClick.AddListener(PedirConfirmacion);

            // Ventana de confirmación sobre todo el menú.
            Canvas lienzo = GetComponentInParent<Canvas>();
            Transform raiz = lienzo != null ? lienzo.transform : transform;
            RectTransform velo = FabricaUIArcade.CrearImagen(raiz, "ConfirmarBorrado", null, new Color(0f, 0f, 0f, 0.75f), true).rectTransform;
            FabricaUIArcade.Estirar(velo);
            confirmacion = velo.gameObject;

            Image cuadro = FabricaUIArcade.CrearImagen(velo, "Cuadro", null, new Color(0.08f, 0.09f, 0.16f, 1f));
            FabricaUIArcade.Centrar(cuadro.rectTransform, Vector2.zero, new Vector2(900f, 420f));
            Text pregunta = FabricaUIArcade.CrearTexto(cuadro.transform, "Pregunta", "¿BORRAR TODO EL PROGRESO?\nSe pierden las estrellas de cada piso.", 34,
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
