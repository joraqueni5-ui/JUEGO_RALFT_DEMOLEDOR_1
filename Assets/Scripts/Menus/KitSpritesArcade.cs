using UnityEngine;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Juego de sprites que usan los menús. En el editor se llena con los PNG de
    /// Assets/Sprites/Menus; en tiempo de juego (menú de pausa) se crea en memoria.
    /// </summary>
    public class KitSpritesArcade
    {
        public Sprite botonAmarillo;
        public Sprite botonNaranja;
        public Sprite botonAzul;
        public Sprite botonRojo;
        public Sprite panel;
        public Sprite franja;
        public Sprite ladrillo;
        public Sprite barra;
        public Sprite relleno;
        public Sprite perilla;
        public Sprite casilla;
        public Sprite marca;

        // Menú principal.
        public Sprite muro;
        public Sprite cartel;
        public Sprite hueco;
        public Sprite vineta;

        // Bordes 9-slice (izquierda, abajo, derecha, arriba) en pixeles del sprite.
        public static readonly Vector4 BordeBoton = new Vector4(30f, 30f, 30f, 30f);
        public static readonly Vector4 BordePanel = new Vector4(40f, 40f, 40f, 40f);
        public static readonly Vector4 BordeBarra = new Vector4(14f, 13f, 14f, 13f);
        public static readonly Vector4 BordeCasilla = new Vector4(18f, 18f, 18f, 18f);
        public static readonly Vector4 BordeCartel = new Vector4(48f, 48f, 48f, 48f);

        private static KitSpritesArcade enMemoria;

        /// <summary>
        /// Crea (una sola vez) en memoria los sprites que necesita el menú de pausa.
        /// </summary>
        public static KitSpritesArcade EnMemoria()
        {
            if (enMemoria != null && enMemoria.botonAmarillo != null) return enMemoria;

            KitSpritesArcade kit = new KitSpritesArcade();
            kit.botonAmarillo = Crear(GeneradorSpritesArcade.Boton(PaletaArcade.Amarillo, "boton_amarillo"), BordeBoton);
            kit.botonNaranja = Crear(GeneradorSpritesArcade.Boton(PaletaArcade.Naranja, "boton_naranja"), BordeBoton);
            kit.botonAzul = Crear(GeneradorSpritesArcade.Boton(PaletaArcade.AzulClaro, "boton_azul"), BordeBoton);
            kit.botonRojo = Crear(GeneradorSpritesArcade.Boton(PaletaArcade.Rojo, "boton_rojo"), BordeBoton);
            kit.panel = Crear(GeneradorSpritesArcade.Panel(), BordePanel);
            kit.franja = Crear(GeneradorSpritesArcade.FranjaPeligro(), Vector4.zero);
            kit.ladrillo = Crear(GeneradorSpritesArcade.Ladrillo(), Vector4.zero);
            enMemoria = kit;
            return kit;
        }

        private static Sprite Crear(Texture2D tex, Vector4 borde)
        {
            Sprite s = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, borde);
            s.name = tex.name;
            return s;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEstatico()
        {
            enMemoria = null;
        }
    }
}
