using UnityEngine;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Sprites pixel art de los menús, dibujados por código a baja resolución
    /// (filtro Point). Se muestran ampliados ×4 para que combinen con el arte del
    /// demoledor y del muñeco. Son dibujos originales hechos con formas simples.
    ///
    /// Los bordes 9-slice están en pixeles del sprite; las Image usan
    /// pixelsPerUnitMultiplier = 1 / Escala para que el borde también se amplíe.
    /// </summary>
    public static class GeneradorPixelArt
    {
        /// <summary>Cuántas unidades de UI ocupa cada pixel del sprite.</summary>
        public const float Escala = 4f;

        private static readonly Color Transparente = new Color(0f, 0f, 0f, 0f);

        // ------------------------------------------------------------ lienzo
        private sealed class Lienzo
        {
            public readonly int ancho, alto;
            public readonly Color[] px;

            public Lienzo(int ancho, int alto)
            {
                this.ancho = ancho;
                this.alto = alto;
                px = new Color[ancho * alto];
                for (int i = 0; i < px.Length; i++) px[i] = Transparente;
            }

            /// <summary>Pinta un pixel. (0,0) es la esquina SUPERIOR izquierda, como en un editor de pixel art.</summary>
            public void P(int x, int y, Color c)
            {
                if (x < 0 || y < 0 || x >= ancho || y >= alto) return;
                px[(alto - 1 - y) * ancho + x] = c;
            }

            public void Rect(int x0, int y0, int x1, int y1, Color c)
            {
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                        P(x, y, c);
            }

            public void Mapa(string[] filas, int x0, int y0, Color c)
            {
                for (int j = 0; j < filas.Length; j++)
                    for (int i = 0; i < filas[j].Length; i++)
                        if (filas[j][i] == 'X') P(x0 + i, y0 + j, c);
            }

            public Texture2D ATextura(string nombre, bool repetir)
            {
                Texture2D tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
                tex.name = nombre;
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = repetir ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                tex.SetPixels(px);
                tex.Apply(false, false);
                return tex;
            }
        }

        private static Color Osc(Color c, float k) { return new Color(c.r * (1f - k), c.g * (1f - k), c.b * (1f - k), c.a); }
        private static Color Acl(Color c, float k) { return Color.Lerp(c, Color.white, k); }

        // ------------------------------------------------------------ botones y marcos
        /// <summary>Borde 9-slice del botón pixel (izq, abajo, der, arriba).</summary>
        public static readonly Vector4 BordeBoton = new Vector4(3f, 5f, 4f, 3f);

        /// <summary>Botón 24×16: contorno, brillo arriba, canto oscuro abajo y sombra.</summary>
        public static Texture2D Boton(Color cuerpo, string nombre)
        {
            Lienzo l = new Lienzo(24, 16);
            Color n = PaletaArcade.Contorno;
            l.Rect(1, 1, 23, 15, new Color(0f, 0f, 0f, 0.45f));   // sombra
            l.Rect(0, 0, 22, 14, n);                                // contorno
            l.Rect(1, 1, 21, 13, cuerpo);                           // cara
            l.Rect(1, 11, 21, 13, Osc(cuerpo, 0.35f));              // canto inferior
            l.Rect(2, 2, 20, 2, Acl(cuerpo, 0.45f));                // brillo
            l.P(1, 1, n); l.P(21, 1, n); l.P(1, 13, n); l.P(21, 13, n);   // esquinas recortadas
            return l.ATextura(nombre, false);
        }

        /// <summary>Marco blanco del botón seleccionado (24×16, mismo borde que el botón).</summary>
        public static Texture2D MarcoSeleccion()
        {
            Lienzo l = new Lienzo(24, 16);
            Color b = Color.white;
            l.Rect(0, 0, 23, 0, b); l.Rect(0, 15, 23, 15, b);
            l.Rect(0, 0, 0, 15, b); l.Rect(23, 0, 23, 15, b);
            return l.ATextura("marco_seleccion", false);
        }

        /// <summary>Borde 9-slice de los paneles.</summary>
        public static readonly Vector4 BordePanel = new Vector4(6f, 6f, 6f, 6f);

        /// <summary>Panel 16×16: contorno, marco de color de 2 px y fondo azul noche.</summary>
        public static Texture2D Panel(Color marco, string nombre)
        {
            Lienzo l = new Lienzo(16, 16);
            Color n = PaletaArcade.Contorno;
            l.Rect(0, 0, 15, 15, n);
            l.Rect(1, 1, 14, 14, marco);
            l.Rect(1, 1, 14, 1, Acl(marco, 0.35f));
            l.Rect(1, 14, 14, 14, Osc(marco, 0.3f));
            l.Rect(3, 3, 12, 12, n);
            l.Rect(4, 4, 11, 11, PaletaArcade.Hex(0x15214A));
            l.P(2, 2, n); l.P(13, 2, n); l.P(2, 13, n); l.P(13, 13, n);   // remaches
            return l.ATextura(nombre, false);
        }

        /// <summary>Borde 9-slice del cartel de obra.</summary>
        public static readonly Vector4 BordeCartel = new Vector4(5f, 5f, 5f, 5f);

        /// <summary>Cartel amarillo de obra 16×16 con remaches.</summary>
        public static Texture2D Cartel()
        {
            Lienzo l = new Lienzo(16, 16);
            Color n = PaletaArcade.Contorno;
            l.Rect(1, 1, 15, 15, new Color(0f, 0f, 0f, 0.45f));
            l.Rect(0, 0, 14, 14, n);
            l.Rect(1, 1, 13, 13, PaletaArcade.Amarillo);
            l.Rect(1, 1, 13, 1, PaletaArcade.AmarilloClaro);
            l.Rect(1, 12, 13, 13, Osc(PaletaArcade.Amarillo, 0.2f));
            l.P(2, 2, n); l.P(12, 2, n);
            return l.ATextura("cartel_px", false);
        }

        /// <summary>Franja de peligro 8×8 que se repite (Image Tiled).</summary>
        public static Texture2D Franja()
        {
            Lienzo l = new Lienzo(8, 8);
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    l.P(x, y, ((x + y) / 4) % 2 == 0 ? PaletaArcade.Amarillo : PaletaArcade.Contorno);
            return l.ATextura("franja_px", true);
        }

        /// <summary>Muro de ladrillo 64×32 que se repite sin cortes (ladrillos de 16×8).</summary>
        public static Texture2D Muro()
        {
            Lienzo l = new Lienzo(64, 32);
            Color junta = PaletaArcade.Hex(0x3C1C0E);
            Color baseL = PaletaArcade.Hex(0xB2542C);
            for (int y = 0; y < 32; y++)
            {
                int fila = y / 8, ly = y % 8;
                for (int x = 0; x < 64; x++)
                {
                    int xx = (x + (fila % 2) * 8) % 64;
                    int col = xx / 16, lx = xx % 16;
                    Color c;
                    if (ly == 7 || lx == 15) c = junta;
                    else
                    {
                        int semilla = ((col * 73856093) ^ (fila * 19349663)) & 1023;
                        float k = 0.86f + (semilla / 1023f) * 0.26f;
                        c = new Color(baseL.r * k, baseL.g * k, baseL.b * k, 1f);
                        if (ly == 0) c = Acl(c, 0.18f);
                        else if (ly == 6) c = Osc(c, 0.15f);
                    }
                    l.P(x, y, c);
                }
            }
            return l.ATextura("muro_px", true);
        }

        /// <summary>Un ladrillo suelto 8×4 (escombros y transición).</summary>
        public static Texture2D Ladrillo()
        {
            Lienzo l = new Lienzo(8, 4);
            l.Rect(0, 0, 7, 3, PaletaArcade.Hex(0x3C1C0E));
            l.Rect(1, 0, 6, 2, PaletaArcade.Ladrillo);
            l.Rect(1, 0, 6, 0, Acl(PaletaArcade.Ladrillo, 0.2f));
            return l.ATextura("ladrillo_px", false);
        }

        /// <summary>Hueco roto en el muro 64×72 (interior azul con borde de mortero).</summary>
        public static Texture2D Hueco()
        {
            const int W = 64, H = 72;
            Lienzo l = new Lienzo(W, H);
            System.Random azar = new System.Random(5);
            const int Puntos = 18;
            float[] radios = new float[Puntos];
            for (int i = 0; i < Puntos; i++) radios[i] = 0.80f + (float)azar.NextDouble() * 0.20f;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = (x + 0.5f - W / 2f) / (W / 2f - 1f), dy = (y + 0.5f - H / 2f) / (H / 2f - 1f);
                    float u = (Mathf.Atan2(dy, dx) / (2f * Mathf.PI) + 1f) % 1f * Puntos;
                    int i0 = Mathf.FloorToInt(u) % Puntos, i1 = (i0 + 1) % Puntos;
                    float limite = Mathf.Lerp(radios[i0], radios[i1], u - Mathf.Floor(u));
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r > limite) continue;
                    if (r > limite - 0.07f) l.P(x, y, PaletaArcade.Hex(0x2A130A));
                    else l.P(x, y, Color.Lerp(PaletaArcade.Hex(0x18264F), PaletaArcade.Hex(0x070B1E), y / (float)H));
                }
            return l.ATextura("hueco_px", false);
        }

        // ------------------------------------------------------------ iconos 7×7 (blancos, se tiñen con Image.color)
        public static readonly string[] IconoJugar = { "X......", "XXX....", "XXXXX..", "XXXXXXX", "XXXXX..", "XXX....", "X......" };
        public static readonly string[] IconoOpciones = { "..X.X..", ".XXXXX.", "XXX.XXX", "XX...XX", "XXX.XXX", ".XXXXX.", "..X.X.." };
        public static readonly string[] IconoAyuda = { ".XXXXX.", "XX...XX", ".....XX", "...XXX.", "...XX..", ".......", "...XX.." };
        public static readonly string[] IconoSalir = { "XXXX...", "X..X...", "X..X.X.", "X..XXXX", "X..X.X.", "X..X...", "XXXX..." };
        public static readonly string[] IconoTrofeo = { "XXXXXXX", "XXXXXXX", ".XXXXX.", "..XXX..", "...X...", "..XXX..", ".XXXXX." };
        public static readonly string[] IconoReiniciar = { "..XXX..", ".X...X.", "X......", "X...XXX", "X....XX", ".X...X.", "..XXX.." };
        public static readonly string[] IconoCasa = { "...X...", "..XXX..", ".XXXXX.", "XXXXXXX", ".XX.XX.", ".XX.XX.", ".XXXXX." };
        public static readonly string[] IconoFlecha = { "X......", "XX.....", "XXX....", "XXXX...", "XXX....", "XX.....", "X......" };
        public static readonly string[] IconoEstrella = { "...X...", "...X...", "XXXXXXX", ".XXXXX.", "..XXX..", ".XX.XX.", "X.....X" };
        public static readonly string[] IconoPausa = { ".......", ".XX.XX.", ".XX.XX.", ".XX.XX.", ".XX.XX.", ".XX.XX.", "......." };
        public static readonly string[] IconoArriba = { "...X...", "..XXX..", ".XXXXX.", "XXXXXXX", ".......", ".......", "......." };
        public static readonly string[] IconoAbajo = { ".......", ".......", ".......", "XXXXXXX", ".XXXXX.", "..XXX..", "...X..." };
        public static readonly string[] IconoCorazon = { ".XX.XX.", "XXXXXXX", "XXXXXXX", "XXXXXXX", ".XXXXX.", "..XXX..", "...X..." };

        public static Texture2D Icono(string[] mapa, string nombre)
        {
            Lienzo l = new Lienzo(7, 7);
            l.Mapa(mapa, 0, 0, Color.white);
            return l.ATextura(nombre, false);
        }

        // ------------------------------------------------------------ medallas 16×20
        public static Texture2D Medalla(Color metal, string nombre)
        {
            Lienzo l = new Lienzo(16, 20);
            Color n = PaletaArcade.Contorno;
            // Cinta.
            l.Mapa(new[] { "XXX......XXX", ".XXX....XXX.", "..XXX..XXX..", "...XXXXXX..." }, 2, 0, n);
            l.Mapa(new[] { ".X........X.", "..XX....XX..", "...XX..XX..." }, 2, 0, PaletaArcade.AzulClaro);
            l.Mapa(new[] { "..X......X..", "...X....X...", "....X..X...." }, 2, 0, PaletaArcade.Rojo);
            // Disco.
            for (int y = 0; y < 14; y++)
                for (int x = 0; x < 14; x++)
                {
                    float dx = x - 6.5f, dy = y - 6.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r > 7f) continue;
                    Color c = r > 6f ? n : (r > 4.6f ? Osc(metal, 0.18f) : metal);
                    if (r <= 6f && dx < -1f && dy < -1f && r > 2f) c = Acl(c, 0.35f);
                    l.P(1 + x, 6 + y, c);
                }
            l.Mapa(IconoEstrella, 5, 9, Osc(metal, 0.45f));
            return l.ATextura(nombre, false);
        }

        // ------------------------------------------------------------ slider y casilla
        public static readonly Vector4 BordeBarra = new Vector4(2f, 2f, 2f, 2f);

        public static Texture2D Barra()
        {
            Lienzo l = new Lienzo(8, 6);
            l.Rect(0, 0, 7, 5, PaletaArcade.Contorno);
            l.Rect(1, 1, 6, 4, PaletaArcade.Hex(0x070B1E));
            return l.ATextura("barra_px", false);
        }

        public static Texture2D Relleno()
        {
            Lienzo l = new Lienzo(8, 6);
            l.Rect(1, 1, 6, 4, PaletaArcade.Naranja);
            l.Rect(1, 1, 6, 1, PaletaArcade.AmarilloClaro);
            return l.ATextura("relleno_px", false);
        }

        public static Texture2D Perilla()
        {
            Lienzo l = new Lienzo(6, 9);
            l.Rect(0, 0, 5, 8, PaletaArcade.Contorno);
            l.Rect(1, 1, 4, 7, PaletaArcade.Amarillo);
            l.Rect(1, 1, 4, 1, PaletaArcade.AmarilloClaro);
            l.Rect(1, 7, 4, 7, PaletaArcade.NaranjaOscuro);
            return l.ATextura("perilla_px", false);
        }

        public static readonly Vector4 BordeCasilla = new Vector4(3f, 3f, 3f, 3f);

        public static Texture2D Casilla()
        {
            Lienzo l = new Lienzo(9, 9);
            l.Rect(0, 0, 8, 8, PaletaArcade.Contorno);
            l.Rect(1, 1, 7, 7, PaletaArcade.Amarillo);
            l.Rect(2, 2, 6, 6, PaletaArcade.Hex(0x070B1E));
            return l.ATextura("casilla_px", false);
        }

        public static Texture2D Marca()
        {
            Lienzo l = new Lienzo(7, 7);
            l.Mapa(new[] { "......X", ".....XX", "X...XX.", "XX.XX..", ".XXX...", "..X....", "......." }, 0, 0, PaletaArcade.Naranja);
            return l.ATextura("marca_px", false);
        }

        // ------------------------------------------------------------ fondos suaves
        /// <summary>Degradado horizontal (oscuro a la izquierda, transparente a la derecha) para leer los botones sobre el video.</summary>
        public static Texture2D SombraLateral()
        {
            Lienzo l = new Lienzo(64, 4);
            for (int x = 0; x < 64; x++)
            {
                float a = Mathf.Clamp01(1f - x / 63f);
                a = a * a * (3f - 2f * a) * 0.85f;
                for (int y = 0; y < 4; y++) l.P(x, y, new Color(0.03f, 0.05f, 0.12f, a));
            }
            Texture2D t = l.ATextura("sombra_lateral", false);
            t.filterMode = FilterMode.Bilinear;
            return t;
        }
    }
}
