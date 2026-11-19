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
        public const string Nivel1 = "Piso0_PlantaBaja"; // Los scripts del modo anterior que pedían Nivel1 ahora cargan la planta baja
        public const string Universidad = "Piso0_PlantaBaja";
        public const string FinJuego = "FinJuego";
        /// <summary>Escena de prueba anterior con los 7 pisos en una sola escena (ya no está en el build).</summary>
        public const string PisoCenital = "PisoCenital";
        /// <summary>El nivel final: la fachada de Unifranz vista de costado.</summary>
        public const string NivelFinal = "NivelFinal_Fachada";

        /// <summary>Escena de un piso (0 a 6): cada piso tiene la suya en Assets/Scenes/Niveles.</summary>
        public static string ParaPiso(int piso)
        {
            return DefinicionPiso.NombreEscenaDe(piso);
        }

        /// <summary>Escena de juego para volver a intentar: la del piso en curso (o el nivel final).</summary>
        public static string EscenaDeJuego()
        {
            if (GameSession.EnNivelFinal) return NivelFinal;
            return ParaPiso(GameSession.PisoActual);
        }
    }
}