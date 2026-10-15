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

        /// <summary>Reinicia los contadores al comenzar una partida nueva.</summary>
        public static void IniciarPartida(int metaLadrillos, string nivel)
        {
            Puntaje = 0;
            Vidas = VidasIniciales;
            Ladrillos = 0;
            MetaLadrillos = metaLadrillos;
            Victoria = false;
            NivelActual = nivel;
        }
    }
}
