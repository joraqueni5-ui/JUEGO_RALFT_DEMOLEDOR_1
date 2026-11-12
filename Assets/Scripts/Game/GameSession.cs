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

        // ---- Modo de 6 pisos (EdificioPisos). El modo original no usa estos datos.
        /// <summary>Piso en curso (0 = Piso 1). Al perder se continúa desde aquí.</summary>
        public static int PisoActual;
        /// <summary>Piso más alto alcanzado en la partida.</summary>
        public static int PisoMaximo;
        /// <summary>Puntos con los que empezó el piso en curso (los pisos ya completados).</summary>
        public static int PuntosAcumulados;
        /// <summary>Vidas con las que empezó el piso en curso (0 = todavía no se guardó ninguna).</summary>
        public static int VidasAlEmpezarPiso;

        /// <summary>Partida nueva del modo de pisos: vuelve al Piso 1 con todo en cero.</summary>
        public static void NuevaPartida()
        {
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
