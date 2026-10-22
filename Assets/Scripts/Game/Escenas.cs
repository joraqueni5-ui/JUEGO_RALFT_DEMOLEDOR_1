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
    }
}