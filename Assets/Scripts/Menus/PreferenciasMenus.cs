using UnityEngine;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Preferencias de los menús que no son de audio: vibración (solo celular) y
    /// pantalla completa (solo computadora). Se guardan con PlayerPrefs.
    /// </summary>
    public static class PreferenciasMenus
    {
        private const string ClaveVibracion = "demoledor.vibracion";

        /// <summary>True en celulares (Android/iOS), donde tiene sentido la vibración.</summary>
        public static bool EsCelular { get { return Application.isMobilePlatform; } }

        /// <summary>True en computadora (Windows/Mac/Linux), donde tiene sentido la pantalla completa.</summary>
        public static bool EsComputadora { get { return !Application.isMobilePlatform && !Application.isConsolePlatform; } }

        public static bool Vibracion
        {
            get { return PlayerPrefs.GetInt(ClaveVibracion, 1) == 1; }
            set
            {
                PlayerPrefs.SetInt(ClaveVibracion, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool PantallaCompleta
        {
            get { return Screen.fullScreen; }
            set { Screen.fullScreen = value; }   // Unity recuerda esta elección entre sesiones.
        }

        /// <summary>Vibración corta (25 ms) al tocar un botón, solo en Android y si está activada.</summary>
        public static void VibrarCorto() { Vibrar(25); }

        private const string ClaveVibracionJuego = "demoledor.vibracionJuego";

        /// <summary>Vibrar cuando te golpean o te da un chispazo (además de la vibración de los botones).</summary>
        public static bool VibracionJuego
        {
            get { return PlayerPrefs.GetInt(ClaveVibracionJuego, 1) == 1; }
            set { PlayerPrefs.SetInt(ClaveVibracionJuego, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Vibración del juego (golpes): respeta las dos opciones de vibración.</summary>
        public static void Vibrar(int milisegundos)
        {
            if (milisegundos > 30 && !VibracionJuego) return;
            VibrarAhora(milisegundos);
        }

        // ------------------------------------------------------------ controles táctiles
        private const string ClaveTamanoBotones = "demoledor.tamanoBotones";
        private const string ClaveSensibilidad = "demoledor.sensibilidadJoystick";

        /// <summary>Tamaño de los botones táctiles (0,7 a 1,4; 1 = normal).</summary>
        public static float TamanoBotones
        {
            get { return Mathf.Clamp(PlayerPrefs.GetFloat(ClaveTamanoBotones, 1f), 0.7f, 1.4f); }
            set { PlayerPrefs.SetFloat(ClaveTamanoBotones, Mathf.Clamp(value, 0.7f, 1.4f)); PlayerPrefs.Save(); }
        }

        /// <summary>Sensibilidad de la palanca (0,6 a 1,6; más alta = responde con menos recorrido del dedo).</summary>
        public static float SensibilidadJoystick
        {
            get { return Mathf.Clamp(PlayerPrefs.GetFloat(ClaveSensibilidad, 1f), 0.6f, 1.6f); }
            set { PlayerPrefs.SetFloat(ClaveSensibilidad, Mathf.Clamp(value, 0.6f, 1.6f)); PlayerPrefs.Save(); }
        }

        private static void VibrarAhora(long milisegundos)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Vibracion) return;
            try
            {
                using (AndroidJavaClass jugador = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject actividad = jugador.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrador = actividad.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrador != null) vibrador.Call("vibrate", milisegundos);
                }
            }
            catch (System.Exception)
            {
                // Si el celular no permite vibrar, simplemente no vibra.
            }
#endif
        }

#if UNITY_ANDROID
        // Unity agrega el permiso VIBRATE al APK solo si encuentra Handheld.Vibrate en el código.
        // Este método nunca se llama: solo sirve para que el permiso quede incluido.
        private static void PermisoVibracion()
        {
            Handheld.Vibrate();
        }
#endif
    }
}
