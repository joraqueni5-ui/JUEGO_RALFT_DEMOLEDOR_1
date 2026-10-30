using UnityEngine;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Cambio de escena para los menús: revisa que la escena esté en Build Settings,
    /// restaura Time.timeScale y hace un fundido. Los nombres de escena salen de
    /// DemoledorUnifranz.Game.Escenas (no se escriben a mano).
    /// </summary>
    public static class NavegadorEscenas
    {
        /// <summary>True si la escena está en Build Settings y se puede cargar.</summary>
        public static bool Disponible(string escena)
        {
            return !string.IsNullOrEmpty(escena) && Application.CanStreamedLevelBeLoaded(escena);
        }

        /// <summary>Carga la escena con fundido. Devuelve false si no está en Build Settings.</summary>
        public static bool Cargar(string escena)
        {
            Time.timeScale = 1f;

            if (!Disponible(escena))
            {
                Debug.LogError("[Menus] La escena '" + escena + "' no está en File → Build Profiles (Build Settings). " +
                               "Agrégala a la lista de escenas para poder cargarla.");
                return false;
            }

            ServiciosMenus.Instancia.CargarEscena(escena);
            return true;
        }

        /// <summary>Cierra la aplicación. Dentro del editor detiene el Play Mode.</summary>
        public static void Salir()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
