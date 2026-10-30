using UnityEngine;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Volumen general y sonido activado/desactivado, guardados con PlayerPrefs.
    /// Se aplica sobre AudioListener.volume, así que afecta a todo el audio del juego
    /// (los sonidos de los menús y cualquier sonido que se agregue después).
    /// </summary>
    public static class ConfiguracionAudio
    {
        private const string ClaveVolumen = "ralft.volumen";
        private const string ClaveSonido = "ralft.sonido";

        public static float Volumen
        {
            get { return PlayerPrefs.GetFloat(ClaveVolumen, 0.8f); }
            set
            {
                PlayerPrefs.SetFloat(ClaveVolumen, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                Aplicar();
            }
        }

        public static bool SonidoActivado
        {
            get { return PlayerPrefs.GetInt(ClaveSonido, 1) == 1; }
            set
            {
                PlayerPrefs.SetInt(ClaveSonido, value ? 1 : 0);
                PlayerPrefs.Save();
                Aplicar();
            }
        }

        /// <summary>Pone el volumen guardado en el AudioListener.</summary>
        public static void Aplicar()
        {
            AudioListener.volume = SonidoActivado ? Volumen : 0f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AlIniciar()
        {
            Aplicar();
        }
    }
}
