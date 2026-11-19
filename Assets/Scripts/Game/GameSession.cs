using UnityEngine;

namespace DemoledorUnifranz.Game
{
    /// <summary>
    /// Datos de la partida en curso (puntaje, vidas y objetivos).
    /// Al ser una clase estatica conserva la informacion cuando se
    /// cambia de escena, por ejemplo del nivel a la pantalla de fin de juego.
    /// </summary>
    public static class GameSession
    {
        public const int VidasIniciales = 3;

        /// <summary>Nivel que se carga cuando el jugador pulsa "Reintentar".</summary>
        public static string NivelActual = Escenas.Nivel1;

        // ---- Nivel final (la fachada del edificio alto).
        /// <summary>Se está jugando (o se va a repetir) el nivel final.</summary>
        public static bool EnNivelFinal;
        /// <summary>Tras el nivel final: mostrar los créditos y luego los récords.</summary>
        public static bool FlujoFinal;
        private const string ClaveNivelFinal = "DemoledorNivelFinalDesbloqueado";

        /// <summary>El nivel final se desbloquea al superar el piso 6 y se guarda con el resto del progreso.</summary>
        public static bool NivelFinalDesbloqueado
        {
            get { return PlayerPrefs.GetInt(ClaveNivelFinal, 0) == 1; }
            set { PlayerPrefs.SetInt(ClaveNivelFinal, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static int Puntaje;
        public static int Vidas;
        public static int Ladrillos;
        public static int MetaLadrillos = 5;

        /// <summary>True cuando el jugador gano la partida, false cuando la perdio.</summary>
        public static bool Victoria;

        // ---- Datos de la escena Universidad (el edificio que se derrumba).
        /// <summary>True si la última partida terminó por derrumbe en Universidad.</summary>
        public static bool HayDatosDerrumbe;
        public static int Reparaciones;
        /// <summary>Estructura destruida al terminar (0 a 1).</summary>
        public static float PorcentajeDestruido;

        /// <summary>
        /// Guarda los datos reales de la partida de Universidad antes de ir a FinJuego.
        /// Solo guarda datos: no cambia las reglas del juego.
        /// </summary>
        public static void RegistrarDerrumbe(int puntos, int reparaciones, float destruido, string nivel)
        {
            Puntaje = puntos;
            Reparaciones = reparaciones;
            PorcentajeDestruido = destruido;
            Victoria = false;
            HayDatosDerrumbe = true;
            NivelActual = nivel;
        }

        /// <summary>
        /// El edificio alto quedó salvado (se completaron todas las secciones).
        /// FinJuego muestra "¡VICTORIA!" con el puntaje.
        /// </summary>
        public static void RegistrarVictoria(int puntos, int reparaciones, string nivel)
        {
            Puntaje = puntos;
            Reparaciones = reparaciones;
            PorcentajeDestruido = 0f;
            Victoria = true;
            HayDatosDerrumbe = false;
            NivelActual = nivel;
        }

        /// <summary>
        /// El muñeco se quedó sin vidas (el edificio sigue en pie).
        /// FinJuego muestra "GAME OVER" con el puntaje.
        /// </summary>
        public static void RegistrarSinVidas(int puntos, int reparaciones, string nivel)
        {
            Puntaje = puntos;
            Reparaciones = reparaciones;
            PorcentajeDestruido = 0f;
            Victoria = false;
            HayDatosDerrumbe = false;
            NivelActual = nivel;
        }

        // ---- Estrellas: la mejor cantidad (0 a 3) lograda en cada piso. Se guarda también entre sesiones.
        public const int TotalPisosEstrellas = 7;   // 0 = planta baja, 1 a 6
        private const string ClaveEstrellas = "DemoledorEstrellasP";

        /// <summary>Borra todo el progreso guardado (estrellas de cada piso) y la partida en curso. No toca los récords.</summary>
        public static void BorrarProgreso()
        {
            for (int i = 0; i < TotalPisosEstrellas; i++) PlayerPrefs.DeleteKey(ClaveEstrellas + i);
            PlayerPrefs.DeleteKey(ClaveNivelFinal);
            PlayerPrefs.Save();
            NuevaPartida();
        }

        /// <summary>Suma de las mejores estrellas de todos los pisos.</summary>
        public static int EstrellasTotales()
        {
            int n = 0;
            for (int i = 0; i < TotalPisosEstrellas; i++) n += EstrellasDe(i);
            return n;
        }

        /// <summary>Mejor cantidad de estrellas del piso (0 = planta baja).</summary>
        public static int EstrellasDe(int piso)
        {
            if (piso < 0 || piso >= TotalPisosEstrellas) return 0;
            return Mathf.Clamp(PlayerPrefs.GetInt(ClaveEstrellas + piso, 0), 0, 3);
        }

        /// <summary>Guarda las estrellas del piso solo si superan a la mejor marca. Devuelve true si es un récord.</summary>
        public static bool GuardarEstrellas(int piso, int estrellas)
        {
            if (piso < 0 || piso >= TotalPisosEstrellas) return false;
            estrellas = Mathf.Clamp(estrellas, 0, 3);
            if (estrellas <= EstrellasDe(piso)) return false;
            PlayerPrefs.SetInt(ClaveEstrellas + piso, estrellas);
            PlayerPrefs.Save();
            return true;
        }

        // ---- Modo de 6 pisos (EdificioPisos). El modo original no usa estos datos.
        /// <summary>Piso en curso (0 = planta baja, 1 = Piso 1...). Al perder se continúa desde aquí.</summary>
        public static int PisoActual;
        /// <summary>Piso más alto alcanzado en la partida.</summary>
        public static int PisoMaximo;
        /// <summary>Puntos con los que empezó el piso en curso (los pisos ya completados).</summary>
        public static int PuntosAcumulados;
        /// <summary>Vidas con las que empezó el piso en curso (0 = todavía no se guardó ninguna).</summary>
        public static int VidasAlEmpezarPiso;

        /// <summary>Modo elegido en el menú NIVELES (SegunEscena = el que marca SelectorModoJuego en la escena).</summary>
        public enum ModoElegido { SegunEscena, Original, Pisos }
        public static ModoElegido ModoSeleccionado = ModoElegido.SegunEscena;

        /// <summary>
        /// Elige un nivel desde el menú NIVELES: el edificio original completo o un piso concreto
        /// (0 = planta baja). Al perder, el jugador sigue en ese modo y ese piso.
        /// </summary>
        public static void SeleccionarNivel(bool modoPisos, int piso)
        {
            NuevaPartida();
            ModoSeleccionado = modoPisos ? ModoElegido.Pisos : ModoElegido.Original;
            if (!modoPisos) return;
            PisoActual = Mathf.Max(0, piso);
            PisoMaximo = PisoActual;
        }

        /// <summary>Partida nueva del modo de pisos: vuelve a la planta baja con todo en cero.</summary>
        public static void NuevaPartida()
        {
            ModoSeleccionado = ModoElegido.SegunEscena;
            EnNivelFinal = false;
            PisoActual = 0;
            PisoMaximo = 0;
            PuntosAcumulados = 0;
            VidasAlEmpezarPiso = 0;
        }

        /// <summary>Guarda el estado con el que empieza un piso: a él se vuelve al perder o con R.</summary>
        public static void GuardarInicioPiso(int piso, int puntos, int vidas)
        {
            PisoActual = piso;
            if (piso > PisoMaximo) PisoMaximo = piso;
            PuntosAcumulados = puntos;
            VidasAlEmpezarPiso = vidas;
        }

        /// <summary>Reinicia los contadores al comenzar una partida nueva.</summary>
        public static void IniciarPartida(int metaLadrillos, string nivel)
        {
            Puntaje = 0;
            Vidas = VidasIniciales;
            Ladrillos = 0;
            MetaLadrillos = metaLadrillos;
            Victoria = false;
            NivelActual = nivel;
            HayDatosDerrumbe = false;
            Reparaciones = 0;
            PorcentajeDestruido = 0f;
        }
    }
}
