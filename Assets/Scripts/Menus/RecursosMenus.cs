using UnityEngine;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Guarda en la escena (MenuPrincipal y FinJuego) las fuentes y los sprites de
    /// los menús, y los deja disponibles para todo el juego. Así el menú de pausa,
    /// que se crea por código en Universidad, usa la misma fuente pixel y los mismos
    /// sprites sin tener que modificar esa escena ni usar una carpeta Resources.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class RecursosMenus : MonoBehaviour
    {
        [Tooltip("Fuente de títulos y botones (Silkscreen Bold).")]
        public Font fuenteTitulos;
        [Tooltip("Fuente de textos largos (Silkscreen Regular).")]
        public Font fuenteCuerpo;
        public KitSpritesArcade kit = new KitSpritesArcade();

        public static Font FuenteTitulos { get; private set; }
        public static Font FuenteCuerpo { get; private set; }
        public static KitSpritesArcade Kit { get; private set; }

        private void Awake()
        {
            if (fuenteTitulos != null) FuenteTitulos = fuenteTitulos;
            if (fuenteCuerpo != null) FuenteCuerpo = fuenteCuerpo;
            if (kit != null && kit.botonAmarillo != null) Kit = kit;
            FabricaUIArcade.Fuente = FuenteTitulos;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEstatico()
        {
            FuenteTitulos = null;
            FuenteCuerpo = null;
            Kit = null;
        }
    }
}
