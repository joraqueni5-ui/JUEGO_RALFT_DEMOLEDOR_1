using UnityEngine;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Colores de los menús de DEMOLEDOR UNIFRANZ.
    /// Azul oscuro de fondo, amarillo y naranja de obra, rojo para salir.
    /// </summary>
    public static class PaletaArcade
    {
        public static readonly Color AzulNoche = Hex(0x0B1330);
        public static readonly Color AzulOscuro = Hex(0x15214A);
        public static readonly Color AzulMedio = Hex(0x24407A);
        public static readonly Color AzulClaro = Hex(0x3C6FD1);
        public static readonly Color Amarillo = Hex(0xFFC21A);
        public static readonly Color AmarilloClaro = Hex(0xFFE27A);
        public static readonly Color Naranja = Hex(0xFF7A1A);
        public static readonly Color NaranjaOscuro = Hex(0xC2450C);
        public static readonly Color Rojo = Hex(0xE23B2E);
        public static readonly Color Ladrillo = Hex(0xB4532A);
        public static readonly Color Contorno = Hex(0x0A0F24);
        public static readonly Color Crema = Hex(0xFFF4DC);
        public static readonly Color Velo = new Color(0.02f, 0.04f, 0.10f, 0.72f);

        /// <summary>Convierte 0xRRGGBB en Color.</summary>
        public static Color Hex(int rgb, float alfa = 1f)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alfa);
        }
    }
}
