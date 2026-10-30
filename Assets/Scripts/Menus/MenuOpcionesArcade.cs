using UnityEngine;
using UnityEngine.UI;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Ventana de opciones: volumen general (slider) y sonido activado/desactivado.
    /// Los valores se guardan con PlayerPrefs (ConfiguracionAudio) y se aplican al
    /// momento sobre AudioListener.volume.
    /// </summary>
    public class MenuOpcionesArcade : MonoBehaviour
    {
        public Slider sliderVolumen;
        public Text textoVolumen;
        public Toggle toggleSonido;
        public Text textoSonido;

        private bool sincronizando;

        private void Awake()
        {
            if (sliderVolumen != null) sliderVolumen.onValueChanged.AddListener(CambiarVolumen);
            if (toggleSonido != null) toggleSonido.onValueChanged.AddListener(CambiarSonido);
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
            Refrescar(volumen, sonido);
            sincronizando = false;
        }

        private void CambiarVolumen(float valor)
        {
            if (sincronizando) return;
            ConfiguracionAudio.Volumen = valor;
            Refrescar(valor, ConfiguracionAudio.SonidoActivado);
            ServiciosMenus.Instancia.Tic();
        }

        private void CambiarSonido(bool activado)
        {
            if (sincronizando) return;
            ConfiguracionAudio.SonidoActivado = activado;
            Refrescar(ConfiguracionAudio.Volumen, activado);
            if (activado) ServiciosMenus.Instancia.Clic();
        }

        private void Refrescar(float volumen, bool sonido)
        {
            if (textoVolumen != null) textoVolumen.text = Mathf.RoundToInt(volumen * 100f) + "%";
            if (textoSonido != null)
            {
                textoSonido.text = sonido ? "ACTIVADO" : "DESACTIVADO";
                textoSonido.color = sonido ? PaletaArcade.Amarillo : new Color(0.75f, 0.78f, 0.88f);
            }
            if (sliderVolumen != null) sliderVolumen.interactable = sonido;
        }
    }
}
