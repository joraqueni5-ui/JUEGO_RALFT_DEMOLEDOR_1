namespace DemoledorUnifranz.Game
{
    /// <summary>
    /// Nombres de las escenas del juego.
    /// Se centralizan en una sola clase para evitar errores de escritura
    /// al momento de cambiar de escena con SceneManager.LoadScene().
    /// </summary>
    public static class Escenas
    {
        public const string MenuPrincipal = "MenuPrincipal";
        public const string Nivel1 = "Universidad"; // Redirige todas las llamadas de Nivel1 a tu escena Universidad
        public const string Universidad = "Universidad";
        public const string FinJuego = "FinJuego";
        /// <summary>Piso 1 visto desde arriba (Gastronomía y Medicina).</summary>
        public const string PisoCenital = "PisoCenital";

        /// <summary>Escena que se carga para jugar el piso elegido (el piso 1 es el cenital).</summary>
        public static string ParaPiso(int piso)
        {
            return piso == 0 ? PisoCenital : Nivel1;
        }

        /// <summary>Escena de juego según la selección actual (para volver a intentar).</summary>
        public static string EscenaDeJuego()
        {
            bool cenital = GameSession.ModoSeleccionado == GameSession.ModoElegido.Pisos && GameSession.PisoActual == 0;
            return cenital ? PisoCenital : Nivel1;
        }
    }
}