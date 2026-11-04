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
        public static void VibrarCorto()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Vibracion) return;
            try
            {
                using (AndroidJavaClass jugador = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject actividad = jugador.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrador = actividad.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrador != null) vibrador.Call("vibrate", 25L);
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
