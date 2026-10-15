using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RalftDemoledor.Game;

namespace RalftDemoledor.UI
{
    /// <summary>
    /// Pantalla final del juego. Muestra "VICTORIA" o "GAME OVER"
    /// segun el resultado guardado en GameSession y permite
    /// reintentar el nivel o volver al menu principal.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        [Header("Textos de la pantalla final")]
        public Text textoTitulo;
        public Text textoPuntaje;
        public Text textoDetalle;

        private void Start()
        {
            Time.timeScale = 1f;

            bool gano = GameSession.Victoria;

            if (textoTitulo != null)
            {
                textoTitulo.text = gano ? "VICTORIA" : "GAME OVER";
                textoTitulo.color = gano
                    ? new Color(0.55f, 0.95f, 0.45f)
                    : new Color(0.95f, 0.35f, 0.30f);
            }

            if (textoPuntaje != null)
            {
                textoPuntaje.text = "PUNTAJE FINAL: " + GameSession.Puntaje;
            }

            if (textoDetalle != null)
            {
                textoDetalle.text = gano
                    ? "Ralph completo la demolicion: " + GameSession.Ladrillos + " / " + GameSession.MetaLadrillos + " ladrillos"
                    : "Ralph se quedo sin vidas: " + GameSession.Ladrillos + " / " + GameSession.MetaLadrillos + " ladrillos";
            }
        }

        /// <summary>Vuelve a cargar el nivel desde el inicio.</summary>
        public void Reintentar()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(Escenas.Nivel1);
        }

        /// <summary>Regresa al menu principal.</summary>
        public void VolverAlMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(Escenas.MenuPrincipal);
        }
    }
}
