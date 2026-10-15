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
        public const string Nivel1 = "Nivel1";
        public const string FinJuego = "FinJuego";
    }
}
