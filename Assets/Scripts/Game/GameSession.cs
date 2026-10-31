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
