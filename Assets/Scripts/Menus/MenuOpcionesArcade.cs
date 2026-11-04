using UnityEngine;
using UnityEngine.UI;

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
        public Slider sliderVolumen;
        public Text textoVolumen;
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

        private void Awake()
        {
            if (sliderVolumen != null) sliderVolumen.onValueChanged.AddListener(CambiarVolumen);
            if (toggleSonido != null) toggleSonido.onValueChanged.AddListener(CambiarSonido);
            if (toggleVibracion != null) toggleVibracion.onValueChanged.AddListener(CambiarVibracion);
            if (togglePantallaCompleta != null) togglePantallaCompleta.onValueChanged.AddListener(CambiarPantallaCompleta);

            if (filaVibracion != null) filaVibracion.SetActive(PreferenciasMenus.EsCelular);
            if (filaPantallaCompleta != null) filaPantallaCompleta.SetActive(PreferenciasMenus.EsComputadora);
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
