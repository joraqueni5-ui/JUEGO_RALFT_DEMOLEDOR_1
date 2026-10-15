using UnityEngine;
using UnityEngine.SceneManagement;
using RalftDemoledor.Game;

namespace RalftDemoledor.UI
{
    /// <summary>
    /// Menu de pausa del nivel. Se abre y se cierra con la tecla Escape
    /// o con los botones Reanudar / Reiniciar / Menu principal.
    /// Mientras esta abierto el tiempo del juego se detiene (timeScale = 0).
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Panel de pausa")]
        public GameObject panel;

        public bool EstaPausado { get; private set; }

        private void Awake()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void OnDisable()
        {
            // Seguridad: si la escena cambia no debe quedar el juego congelado.
            Time.timeScale = 1f;
        }

        /// <summary>Abre el menu si esta cerrado o lo cierra si esta abierto.</summary>
        public void AlternarPausa()
        {
            if (EstaPausado)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }
        }

        /// <summary>Detiene el juego y muestra el panel de pausa.</summary>
        public void Pausar()
        {
            EstaPausado = true;
            Time.timeScale = 0f;

            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        /// <summary>Continua la partida desde donde se encontraba.</summary>
        public void Reanudar()
        {
            EstaPausado = false;
            Time.timeScale = 1f;

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        /// <summary>Vuelve a empezar el nivel actual.</summary>
        public void ReiniciarNivel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(Escenas.Nivel1);
        }

        /// <summary>Abandona la partida y regresa al menu principal.</summary>
        public void VolverAlMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(Escenas.MenuPrincipal);
        }
    }
}
