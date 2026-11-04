using System;
using UnityEngine;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Juego de sprites pixel art que usan los menús. En el editor se llena con los
    /// PNG de Assets/Sprites/Menus (los guarda RecursosMenus en la escena) y, si no
    /// están disponibles (por ejemplo al dar Play directo en Universidad), se crea en memoria.
    /// </summary>
    [Serializable]
    public class KitSpritesArcade
    {
        [Header("Botones")]
        public Sprite botonAmarillo;
        public Sprite botonNaranja;
        public Sprite botonAzul;
        public Sprite botonRojo;
        public Sprite botonMorado;
        public Sprite marcoSeleccion;

        [Header("Paneles y decoración")]
        public Sprite panel;
        public Sprite panelRojo;
        public Sprite panelMorado;
        public Sprite cartel;
        public Sprite franja;
        public Sprite ladrillo;
        public Sprite muro;
        public Sprite hueco;
        public Sprite vineta;
        public Sprite sombraLateral;

        [Header("Controles")]
        public Sprite barra;
        public Sprite relleno;
        public Sprite perilla;
        public Sprite casilla;
        public Sprite marca;

        [Header("Iconos 7x7")]
        public Sprite iconoJugar;
        public Sprite iconoOpciones;
        public Sprite iconoAyuda;
        public Sprite iconoSalir;
        public Sprite iconoTrofeo;
        public Sprite iconoReiniciar;
        public Sprite iconoCasa;
        public Sprite iconoFlecha;
        public Sprite iconoEstrella;
        public Sprite iconoPausa;
        public Sprite iconoArriba;
        public Sprite iconoAbajo;

        [Header("Medallas")]
        public Sprite medallaBronce;
        public Sprite medallaPlata;
        public Sprite medallaOro;

        /// <summary>Multiplicador para Image.pixelsPerUnitMultiplier (bordes ×4).</summary>
        public const float MultiplicadorPixel = 1f / GeneradorPixelArt.Escala;

        private static KitSpritesArcade enMemoria;

        /// <summary>
        /// El juego de sprites disponible: el registrado por RecursosMenus (PNG del
        /// proyecto) o, si no hay, uno creado en memoria (una sola vez).
        /// </summary>
        public static KitSpritesArcade Actual()
        {
            if (RecursosMenus.Kit != null && RecursosMenus.Kit.botonAmarillo != null) return RecursosMenus.Kit;
            return EnMemoria();
        }

        /// <summary>Crea en memoria todos los sprites (mismos dibujos que los PNG).</summary>
        public static KitSpritesArcade EnMemoria()
        {
            if (enMemoria != null && enMemoria.botonAmarillo != null) return enMemoria;

            KitSpritesArcade k = new KitSpritesArcade();
            Vector4 bb = GeneradorPixelArt.BordeBoton, bp = GeneradorPixelArt.BordePanel;
            k.botonAmarillo = Crear(GeneradorPixelArt.Boton(PaletaArcade.Amarillo, "boton_px_amarillo"), bb);
            k.botonNaranja = Crear(GeneradorPixelArt.Boton(PaletaArcade.Naranja, "boton_px_naranja"), bb);
            k.botonAzul = Crear(GeneradorPixelArt.Boton(PaletaArcade.AzulClaro, "boton_px_azul"), bb);
            k.botonRojo = Crear(GeneradorPixelArt.Boton(PaletaArcade.Rojo, "boton_px_rojo"), bb);
            k.botonMorado = Crear(GeneradorPixelArt.Boton(PaletaArcade.Morado, "boton_px_morado"), bb);
            k.marcoSeleccion = Crear(GeneradorPixelArt.MarcoSeleccion(), bb);
            k.panel = Crear(GeneradorPixelArt.Panel(PaletaArcade.Amarillo, "panel_px"), bp);
            k.panelRojo = Crear(GeneradorPixelArt.Panel(PaletaArcade.Rojo, "panel_px_rojo"), bp);
            k.panelMorado = Crear(GeneradorPixelArt.Panel(PaletaArcade.Morado, "panel_px_morado"), bp);
            k.cartel = Crear(GeneradorPixelArt.Cartel(), GeneradorPixelArt.BordeCartel);
            k.franja = Crear(GeneradorPixelArt.Franja(), Vector4.zero);
            k.ladrillo = Crear(GeneradorPixelArt.Ladrillo(), Vector4.zero);
            k.barra = Crear(GeneradorPixelArt.Barra(), GeneradorPixelArt.BordeBarra);
            k.relleno = Crear(GeneradorPixelArt.Relleno(), GeneradorPixelArt.BordeBarra);
            k.perilla = Crear(GeneradorPixelArt.Perilla(), Vector4.zero);
            k.casilla = Crear(GeneradorPixelArt.Casilla(), GeneradorPixelArt.BordeCasilla);
            k.marca = Crear(GeneradorPixelArt.Marca(), Vector4.zero);
            k.iconoJugar = Icono(GeneradorPixelArt.IconoJugar, "icono_jugar");
            k.iconoOpciones = Icono(GeneradorPixelArt.IconoOpciones, "icono_opciones");
            k.iconoAyuda = Icono(GeneradorPixelArt.IconoAyuda, "icono_ayuda");
            k.iconoSalir = Icono(GeneradorPixelArt.IconoSalir, "icono_salir");
            k.iconoTrofeo = Icono(GeneradorPixelArt.IconoTrofeo, "icono_trofeo");
            k.iconoReiniciar = Icono(GeneradorPixelArt.IconoReiniciar, "icono_reiniciar");
            k.iconoCasa = Icono(GeneradorPixelArt.IconoCasa, "icono_casa");
            k.iconoFlecha = Icono(GeneradorPixelArt.IconoFlecha, "icono_flecha");
            k.iconoEstrella = Icono(GeneradorPixelArt.IconoEstrella, "icono_estrella");
            k.iconoPausa = Icono(GeneradorPixelArt.IconoPausa, "icono_pausa");
            k.iconoArriba = Icono(GeneradorPixelArt.IconoArriba, "icono_arriba");
            k.iconoAbajo = Icono(GeneradorPixelArt.IconoAbajo, "icono_abajo");
            // Los sprites grandes (muro, hueco, medallas, viñeta) solo se usan en escenas
            // de menú, donde siempre están los PNG; no se crean en memoria.
            enMemoria = k;
            return k;
        }

        private static Sprite Icono(string[] mapa, string nombre)
        {
            return Crear(GeneradorPixelArt.Icono(mapa, nombre), Vector4.zero);
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
