using UnityEngine;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Dibuja por código los gráficos de los menús (botones, paneles, ladrillo, franja
    /// de peligro, slider y casilla). Son dibujos originales hechos con formas simples:
    /// no usan recursos de otros juegos.
    ///
    /// La herramienta del editor los guarda como PNG en Assets/Sprites/Menus
    /// (Tools → Demoledor Unifranz) y el menú de pausa los crea en memoria.
    /// </summary>
    public static class GeneradorSpritesArcade
    {
        // ------------------------------------------------------------ lienzo
        private sealed class Lienzo
        {
            public readonly int ancho;
            public readonly int alto;
            public readonly Color[] px;

            public Lienzo(int ancho, int alto)
            {
                this.ancho = ancho;
                this.alto = alto;
                px = new Color[ancho * alto];
            }

            /// <summary>Mezcla un color encima del pixel (alfa "over").</summary>
            public void Pintar(int x, int y, Color c, float cobertura = 1f)
            {
                if (x < 0 || y < 0 || x >= ancho || y >= alto) return;
                float a = Mathf.Clamp01(c.a * cobertura);
                if (a <= 0f) return;

                int i = y * ancho + x;
                Color d = px[i];
                float salida = a + d.a * (1f - a);
                if (salida <= 0f)
                {
                    px[i] = new Color(0f, 0f, 0f, 0f);
                    return;
                }

                px[i] = new Color(
                    (c.r * a + d.r * d.a * (1f - a)) / salida,
                    (c.g * a + d.g * d.a * (1f - a)) / salida,
                    (c.b * a + d.b * d.a * (1f - a)) / salida,
                    salida);
            }

            public void Rect(int x0, int y0, int x1, int y1, Color c)
            {
                for (int y = Mathf.Max(0, y0); y < Mathf.Min(alto, y1); y++)
                    for (int x = Mathf.Max(0, x0); x < Mathf.Min(ancho, x1); x++)
                        Pintar(x, y, c);
            }

            public Texture2D ATextura(string nombre, bool repetir)
            {
                Texture2D tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
                tex.name = nombre;
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = repetir ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                tex.SetPixels(px);
                tex.Apply(false, false);
                return tex;
            }
        }

        // ------------------------------------------------------------ formas
        /// <summary>Distancia con signo a un rectángulo redondeado (negativo = dentro).</summary>
        private static float DistRect(float x, float y, float cx, float cy, float medioAncho, float medioAlto, float radio)
        {
            float qx = Mathf.Abs(x - cx) - (medioAncho - radio);
            float qy = Mathf.Abs(y - cy) - (medioAlto - radio);
            float fuera = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            float dentro = Mathf.Min(Mathf.Max(qx, qy), 0f);
            return fuera + dentro - radio;
        }

        private static float DistSegmento(float x, float y, Vector2 a, Vector2 b)
        {
            Vector2 p = new Vector2(x, y);
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            return (p - (a + ab * t)).magnitude;
        }

        private static float Cobertura(float distancia)
        {
            return Mathf.Clamp01(0.5f - distancia);
        }

        private static Color Aclarar(Color c, float t)
        {
            return Color.Lerp(c, Color.white, t);
        }

        private static Color Oscurecer(Color c, float t)
        {
            Color r = Color.Lerp(c, Color.black, t);
            r.a = c.a;
            return r;
        }

        /// <summary>Rectángulo redondeado con contorno y relleno en degradado vertical.</summary>
        private static void RectRedondeado(Lienzo l, float x0, float y0, float x1, float y1, float radio,
            Color abajo, Color arriba)
        {
            float cx = (x0 + x1) * 0.5f;
            float cy = (y0 + y1) * 0.5f;
            float mx = (x1 - x0) * 0.5f;
            float my = (y1 - y0) * 0.5f;

            for (int y = 0; y < l.alto; y++)
            {
                float t = Mathf.InverseLerp(y0, y1, y + 0.5f);
                Color c = Color.Lerp(abajo, arriba, t);
                for (int x = 0; x < l.ancho; x++)
                {
                    float d = DistRect(x + 0.5f, y + 0.5f, cx, cy, mx, my, radio);
                    if (d < 0.5f) l.Pintar(x, y, c, Cobertura(d));
                }
            }
        }

        private static void Circulo(Lienzo l, float cx, float cy, float radio, Color c)
        {
            int x0 = Mathf.FloorToInt(cx - radio - 1f), x1 = Mathf.CeilToInt(cx + radio + 1f);
            int y0 = Mathf.FloorToInt(cy - radio - 1f), y1 = Mathf.CeilToInt(cy + radio + 1f);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = new Vector2(x + 0.5f - cx, y + 0.5f - cy).magnitude - radio;
                    if (d < 0.5f) l.Pintar(x, y, c, Cobertura(d));
                }
        }

        private static void Linea(Lienzo l, Vector2 a, Vector2 b, float grosor, Color c)
        {
            int x0 = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - grosor - 1f), x1 = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + grosor + 1f);
            int y0 = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - grosor - 1f), y1 = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + grosor + 1f);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = DistSegmento(x + 0.5f, y + 0.5f, a, b) - grosor * 0.5f;
                    if (d < 0.5f) l.Pintar(x, y, c, Cobertura(d));
                }
        }

        // ------------------------------------------------------------ botones y paneles
        /// <summary>Botón de 192×80 con contorno grueso, bisel y brillo. Borde 9-slice: 30.</summary>
        public static Texture2D Boton(Color cuerpo, string nombre)
        {
            Lienzo l = new Lienzo(192, 80);
            Color contorno = PaletaArcade.Contorno;

            // Sombra inferior (da sensación de botón físico).
            RectRedondeado(l, 2, 0, 190, 74, 24, new Color(0f, 0f, 0f, 0.45f), new Color(0f, 0f, 0f, 0.45f));
            // Contorno.
            RectRedondeado(l, 2, 4, 190, 78, 24, contorno, contorno);
            // Base oscura (canto del bisel).
            RectRedondeado(l, 7, 9, 185, 73, 19, Oscurecer(cuerpo, 0.38f), Oscurecer(cuerpo, 0.30f));
            // Cara del botón.
            RectRedondeado(l, 7, 17, 185, 73, 19, Oscurecer(cuerpo, 0.06f), Aclarar(cuerpo, 0.22f));
            // Brillo superior.
            RectRedondeado(l, 18, 58, 174, 66, 4, new Color(1f, 1f, 1f, 0.38f), new Color(1f, 1f, 1f, 0.38f));

            return l.ATextura(nombre, false);
        }

        /// <summary>Panel de 128×128: azul noche con marco amarillo y remaches. Borde 9-slice: 40.</summary>
        public static Texture2D Panel()
        {
            Lienzo l = new Lienzo(128, 128);
            RectRedondeado(l, 1, 1, 127, 127, 26, PaletaArcade.Contorno, PaletaArcade.Contorno);
            RectRedondeado(l, 6, 6, 122, 122, 21, PaletaArcade.NaranjaOscuro, PaletaArcade.Amarillo);
            RectRedondeado(l, 14, 14, 114, 114, 14, PaletaArcade.Contorno, PaletaArcade.Contorno);
            RectRedondeado(l, 17, 17, 111, 111, 12, PaletaArcade.AzulNoche, PaletaArcade.AzulOscuro);

            // Remaches en las cuatro esquinas del marco.
            float[] pos = { 10f, 118f };
            foreach (float x in pos)
                foreach (float y in pos)
                {
                    Circulo(l, x, y, 3.2f, PaletaArcade.Contorno);
                    Circulo(l, x - 0.6f, y + 0.6f, 1.6f, new Color(1f, 1f, 1f, 0.7f));
                }

            return l.ATextura("panel_arcade", false);
        }

        /// <summary>Franja de peligro amarilla y negra de 64×32 (se repite en horizontal).</summary>
        public static Texture2D FranjaPeligro()
        {
            Lienzo l = new Lienzo(64, 32);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 64; x++)
                {
                    bool amarillo = ((x + y) % 32) < 16;
                    l.Pintar(x, y, amarillo ? PaletaArcade.Amarillo : PaletaArcade.Contorno);
                }
            // Cantos oscuros arriba y abajo.
            l.Rect(0, 0, 64, 3, new Color(0f, 0f, 0f, 0.55f));
            l.Rect(0, 29, 64, 32, new Color(0f, 0f, 0f, 0.55f));
            return l.ATextura("franja_peligro", true);
        }

        /// <summary>Ladrillo de 48×24 para los escombros decorativos.</summary>
        public static Texture2D Ladrillo()
        {
            Lienzo l = new Lienzo(48, 24);
            RectRedondeado(l, 0, 0, 48, 24, 3, PaletaArcade.Contorno, PaletaArcade.Contorno);
            RectRedondeado(l, 2, 2, 46, 22, 2, Oscurecer(PaletaArcade.Ladrillo, 0.25f), Aclarar(PaletaArcade.Ladrillo, 0.12f));
            l.Rect(4, 17, 44, 19, new Color(1f, 1f, 1f, 0.25f));
            l.Rect(10, 8, 13, 10, new Color(0f, 0f, 0f, 0.25f));
            l.Rect(30, 6, 32, 8, new Color(0f, 0f, 0f, 0.25f));
            return l.ATextura("ladrillo_menu", false);
        }

        // ------------------------------------------------------------ controles de opciones
        /// <summary>Riel del slider (64×28, borde 14).</summary>
        public static Texture2D Barra()
        {
            Lienzo l = new Lienzo(64, 28);
            RectRedondeado(l, 0, 0, 64, 28, 14, PaletaArcade.Contorno, PaletaArcade.Contorno);
            RectRedondeado(l, 4, 4, 60, 24, 10, PaletaArcade.AzulNoche, Oscurecer(PaletaArcade.AzulNoche, 0.3f));
            return l.ATextura("barra", false);
        }

        /// <summary>Relleno del slider (64×28, borde 14).</summary>
        public static Texture2D Relleno()
        {
            Lienzo l = new Lienzo(64, 28);
            RectRedondeado(l, 4, 4, 60, 24, 10, PaletaArcade.Naranja, PaletaArcade.Amarillo);
            RectRedondeado(l, 10, 17, 54, 20, 1.5f, new Color(1f, 1f, 1f, 0.45f), new Color(1f, 1f, 1f, 0.45f));
            return l.ATextura("barra_relleno", false);
        }

        /// <summary>Perilla del slider (56×56).</summary>
        public static Texture2D Perilla()
        {
            Lienzo l = new Lienzo(56, 56);
            Circulo(l, 28, 26, 26f, new Color(0f, 0f, 0f, 0.4f));
            Circulo(l, 28, 29, 26f, PaletaArcade.Contorno);
            Circulo(l, 28, 29, 21f, PaletaArcade.NaranjaOscuro);
            Circulo(l, 28, 31, 19f, PaletaArcade.Amarillo);
            Circulo(l, 22, 37, 5f, new Color(1f, 1f, 1f, 0.6f));
            return l.ATextura("perilla", false);
        }

        /// <summary>Casilla de verificación vacía (56×56, borde 18).</summary>
        public static Texture2D Casilla()
        {
            Lienzo l = new Lienzo(56, 56);
            RectRedondeado(l, 0, 0, 56, 56, 12, PaletaArcade.Contorno, PaletaArcade.Contorno);
            RectRedondeado(l, 4, 4, 52, 52, 9, PaletaArcade.Amarillo, PaletaArcade.AmarilloClaro);
            RectRedondeado(l, 9, 9, 47, 47, 6, PaletaArcade.AzulNoche, PaletaArcade.AzulOscuro);
            return l.ATextura("casilla", false);
        }

        /// <summary>Marca de verificación (56×56).</summary>
        public static Texture2D Marca()
        {
            Lienzo l = new Lienzo(56, 56);
            Vector2 a = new Vector2(14, 29), b = new Vector2(24, 17), c = new Vector2(43, 41);
            Linea(l, a, b, 11, PaletaArcade.Contorno);
            Linea(l, b, c, 11, PaletaArcade.Contorno);
            Linea(l, a, b, 6, PaletaArcade.Naranja);
            Linea(l, b, c, 6, PaletaArcade.Naranja);
            return l.ATextura("marca", false);
        }
    }
}
