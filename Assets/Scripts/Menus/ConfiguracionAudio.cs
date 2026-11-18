using UnityEngine;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Volúmenes de Música y de Efectos (0 a 1) y sonido activado/desactivado, guardados con PlayerPrefs.
    /// 'Volumen' es el de la música; los efectos (juego e interfaz) usan 'VolumenEfectos'. GestorAudio los aplica.
    /// El interruptor SONIDO silencia todo mediante AudioListener.volume.
    /// </summary>
    public static class ConfiguracionAudio
    {
        private const string ClaveVolumen = "ralft.volumen";
        private const string ClaveSonido = "ralft.sonido";
        private const string ClaveEfectos = "ralft.volumenEfectos";

        /// <summary>Volumen de los efectos del juego y de la interfaz (0 a 1).</summary>
        public static float VolumenEfectos
        {
            get { return PlayerPrefs.GetFloat(ClaveEfectos, 0.8f); }
            set
            {
                PlayerPrefs.SetFloat(ClaveEfectos, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

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

        /// <summary>Aplica el interruptor de sonido (los volúmenes los aplica GestorAudio).</summary>
        public static void Aplicar()
        {
            AudioListener.volume = SonidoActivado ? 1f : 0f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AlIniciar()
        {
            Aplicar();
        }
    }
}
