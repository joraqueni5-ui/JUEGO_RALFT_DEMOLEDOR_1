using UnityEngine;
using UnityEngine.UI;

namespace DemoledorUnifranz.UI
{
    /// <summary>
    /// Interfaz que se muestra durante el nivel: puntaje, vidas
    /// y progreso de ladrillos recolectados.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Textos del HUD")]
        public Text textoPuntaje;
        public Text textoVidas;
        public Text textoObjetivo;

        /// <summary>Actualiza los tres textos con los datos de la partida.</summary>
        public void Refrescar(int puntaje, int vidas, int ladrillos, int meta)
        {
            if (textoPuntaje != null)
            {
                textoPuntaje.text = "PUNTAJE: " + puntaje;
            }

            if (textoVidas != null)
            {
                textoVidas.text = "VIDAS: " + vidas;
            }

            if (textoObjetivo != null)
            {
                textoObjetivo.text = "LADRILLOS: " + ladrillos + " / " + meta;
            }
        }
    }
}
