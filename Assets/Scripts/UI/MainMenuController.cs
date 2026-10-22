using UnityEngine;
using UnityEngine.SceneManagement;
using DemoledorUnifranz.Game;

namespace DemoledorUnifranz.UI
{
    /// <summary>
    /// Menu principal del juego.
    /// El boton Jugar carga el primer nivel, el boton Salir cierra la
    /// aplicacion y el boton Controles muestra una ayuda breve.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Paneles del menu")]
        public GameObject panelControles;

        private void Start()
        {
            Time.timeScale = 1f;

            if (panelControles != null)
            {
                panelControles.SetActive(false);
            }

            if (Autoprueba.Activa)
            {
                Debug.Log("[AUTOPRUEBA] menu principal: se pulsa Jugar");
                Autoprueba.Programar(this, 2f, Jugar);
            }
        }

        /// <summary>Carga la escena del primer nivel.</summary>
        public void Jugar()
        {
            SceneManager.LoadScene(Escenas.Nivel1);
        }

        /// <summary>Cierra el juego. Dentro del editor detiene la reproduccion.</summary>
        public void Salir()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Muestra u oculta el panel de controles.</summary>
        public void AlternarControles()
        {
            if (panelControles == null)
            {
                return;
            }

            panelControles.SetActive(!panelControles.activeSelf);
        }
    }
}
