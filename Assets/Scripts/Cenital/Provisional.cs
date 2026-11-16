using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Figuras de colores provisionales del modo cenital (cuadrado y círculo de 1 unidad) y
/// utilidades comunes. Se generan al vuelo: no hace falta ningún archivo de arte.
/// </summary>
public static class Provisional
{
    private static Sprite cuadrado;
    private static Sprite circulo;
    private static Sprite estrella;
    private static Sprite aro;
    private static Sprite visto;
    private static Sprite candado;
    private static readonly Sprite[] grietas = new Sprite[3];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        cuadrado = null;
        circulo = null;
        estrella = null;
        aro = null;
        visto = null;
        candado = null;
        for (int i = 0; i < grietas.Length; i++) grietas[i] = null;
    }

    /// <summary>Cuadrado blanco de 1 x 1 unidades (se tiñe con SpriteRenderer.color).</summary>
    public static Sprite Cuadrado
    {
        get
        {
            ArteNiveles guardado = ArteNiveles.Actual;
            if (guardado != null && guardado.cuadrado != null) return guardado.cuadrado;
            if (cuadrado == null)
            {
                Texture2D t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                Color[] px = new Color[16];
                for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                t.SetPixels(px);
                t.filterMode = FilterMode.Point;
                t.Apply();
                t.hideFlags = HideFlags.HideAndDontSave;
                cuadrado = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                cuadrado.hideFlags = HideFlags.HideAndDontSave;
            }
            return cuadrado;
        }
    }

    /// <summary>Círculo blanco de 1 unidad de diámetro, con borde suave.</summary>
    public static Sprite Circulo
    {
        get
        {
            ArteNiveles guardado = ArteNiveles.Actual;
            if (guardado != null && guardado.circulo != null) return guardado.circulo;
            if (circulo == null)
            {
                const int n = 64;
                Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                Color[] px = new Color[n * n];
                float c = (n - 1) * 0.5f;
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / (n * 0.5f);
                        px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * 12f));
                    }
                }
                t.SetPixels(px);
                t.filterMode = FilterMode.Bilinear;
                t.Apply();
                t.hideFlags = HideFlags.HideAndDontSave;
                circulo = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                circulo.hideFlags = HideFlags.HideAndDontSave;
            }
            return circulo;
        }
    }

    /// <summary>Estrella blanca de 5 puntas y 1 unidad de ancho (se tiñe con SpriteRenderer.color).</summary>
    public static Sprite Estrella
    {
        get
        {
            if (estrella == null)
            {
                const int n = 32;
                Vector2[] poligono = new Vector2[10];
                for (int i = 0; i < 10; i++)
                {
                    float ang = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                    float r = (i % 2 == 0 ? 0.48f : 0.2f) * n;
                    poligono[i] = new Vector2(n * 0.5f + Mathf.Cos(ang) * r, n * 0.5f + Mathf.Sin(ang) * r);
                }

                Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                Color[] px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                        px[y * n + x] = DentroDe(poligono, new Vector2(x + 0.5f, y + 0.5f)) ? Color.white : new Color(1f, 1f, 1f, 0f);
                t.SetPixels(px);
                t.filterMode = FilterMode.Point;
                t.Apply();
                t.hideFlags = HideFlags.HideAndDontSave;
                estrella = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                estrella.hideFlags = HideFlags.HideAndDontSave;
            }
            return estrella;
        }
    }

    /// <summary>Aro blanco de 1 unidad de diámetro y borde fino (se tiñe con SpriteRenderer.color).</summary>
    public static Sprite Aro
    {
        get
        {
            if (aro == null)
            {
                const int n = 64;
                Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                Color[] px = new Color[n * n];
                float c = (n - 1) * 0.5f;
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / (n * 0.5f);
                        float borde = Mathf.Clamp01(1f - Mathf.Abs(d - 0.88f) / 0.1f);
                        px[y * n + x] = new Color(1f, 1f, 1f, borde);
                    }
                }
                t.SetPixels(px);
                t.filterMode = FilterMode.Bilinear;
                t.Apply();
                t.hideFlags = HideFlags.HideAndDontSave;
                aro = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                aro.hideFlags = HideFlags.HideAndDontSave;
            }
            return aro;
        }
    }

    /// <summary>Marca de verificación blanca de 1 unidad de ancho (se tiñe con SpriteRenderer.color).</summary>
    public static Sprite Visto
    {
        get
        {
            if (visto == null)
            {
                const int n = 48;
                Vector2 a = new Vector2(0.2f, 0.5f) * n, b = new Vector2(0.42f, 0.27f) * n, c = new Vector2(0.82f, 0.76f) * n;
                float grosor = 0.075f * n;
                Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                Color[] px = new Color[n * n];
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                        float d = Mathf.Min(DistanciaASegmento(p, a, b), DistanciaASegmento(p, b, c));
                        px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((grosor - d) * 1.5f));
                    }
                }
                t.SetPixels(px);
                t.filterMode = FilterMode.Bilinear;
                t.Apply();
                t.hideFlags = HideFlags.HideAndDontSave;
                visto = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                visto.hideFlags = HideFlags.HideAndDontSave;
            }
            return visto;
        }
    }

    /// <summary>
    /// Grietas oscuras sobre fondo transparente, de 1 unidad de lado (se estiran al área del objeto). Nivel 0 =
    /// grietas chicas, 1 = medianas, 2 = grandes.
    /// </summary>
    public static Sprite Grietas(int nivel)
    {
        nivel = Mathf.Clamp(nivel, 0, 2);
        ArteNiveles guardado = ArteNiveles.Actual;
        if (guardado != null)
        {
            Sprite arte = nivel == 0 ? guardado.grietasChica : nivel == 1 ? guardado.grietasMediana : guardado.grietasGrande;
            if (arte != null) return arte;
        }
        if (grietas[nivel] != null) return grietas[nivel];

        const int n = 64;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        Color[] px = new Color[n * n];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(0f, 0f, 0f, 0f);

        System.Random azar = new System.Random(7 + nivel * 13);
        int lineas = nivel == 0 ? 2 : nivel == 1 ? 4 : 7;
        float largo = nivel == 0 ? 0.35f : nivel == 1 ? 0.5f : 0.75f;
        int grosor = nivel == 2 ? 2 : 1;
        Color tinta = new Color(0.08f, 0.04f, 0.04f, 0.9f);
        for (int l = 0; l < lineas; l++)
        {
            float x = (0.2f + 0.6f * (float)azar.NextDouble()) * n, y = (0.2f + 0.6f * (float)azar.NextDouble()) * n;
            float ang = (float)azar.NextDouble() * Mathf.PI * 2f;
            int pasos = (int)(largo * n);
            for (int k = 0; k < pasos; k++)
            {
                ang += ((float)azar.NextDouble() - 0.5f) * 0.9f;
                x += Mathf.Cos(ang); y += Mathf.Sin(ang);
                int ix = (int)x, iy = (int)y;
                for (int dx = 0; dx < grosor; dx++)
                    for (int dy = 0; dy < grosor; dy++)
                        if (ix + dx >= 0 && ix + dx < n && iy + dy >= 0 && iy + dy < n) px[(iy + dy) * n + ix + dx] = tinta;
            }
        }
        t.SetPixels(px);
        t.filterMode = FilterMode.Point;
        t.Apply();
        t.hideFlags = HideFlags.HideAndDontSave;
        grietas[nivel] = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        grietas[nivel].hideFlags = HideFlags.HideAndDontSave;
        return grietas[nivel];
    }

    /// <summary>Candado blanco de 1 unidad de lado (se tiñe con color).</summary>
    public static Sprite Candado
    {
        get
        {
            ArteNiveles guardado = ArteNiveles.Actual;
            if (guardado != null && guardado.candado != null) return guardado.candado;
            if (candado == null)
            {
                const int n = 32;
                Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                Color[] px = new Color[n * n];
                for (int i = 0; i < px.Length; i++) px[i] = new Color(1f, 1f, 1f, 0f);
                // Cuerpo.
                for (int y = 3; y < 17; y++) for (int x = 6; x < 26; x++) px[y * n + x] = Color.white;
                // Arco: anillo entre dos radios, solo la mitad de arriba, y dos patas hasta el cuerpo.
                for (int y = 15; y < 30; y++)
                {
                    for (int x = 8; x < 24; x++)
                    {
                        float dx = x + 0.5f - 16f, dy = y + 0.5f - 17f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d <= 8f && d >= 5f && y >= 17) px[y * n + x] = Color.white;
                        else if (y < 17 && (x == 9 || x == 10 || x == 21 || x == 22)) px[y * n + x] = Color.white;
                    }
                }
                // Ojo de la cerradura.
                for (int y = 7; y < 13; y++) for (int x = 15; x < 18; x++) px[y * n + x] = new Color(1f, 1f, 1f, 0f);
                t.SetPixels(px);
                t.filterMode = FilterMode.Point;
                t.Apply();
                t.hideFlags = HideFlags.HideAndDontSave;
                candado = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                candado.hideFlags = HideFlags.HideAndDontSave;
            }
            return candado;
        }
    }

    /// <summary>
    /// Pone en una imagen de la interfaz la estrella llena o vacía del arte (sin teñir). Sin arte usa la estrella
    /// provisional teñida de 'colorLlena' o 'colorVacia'.
    /// </summary>
    public static void PonerEstrella(Image imagen, bool llena, Color colorLlena, Color colorVacia, bool grande = true)
    {
        if (imagen == null) return;
        ArteNiveles a = ArteNiveles.Actual;
        Sprite arte = a == null ? null : llena ? (grande ? a.estrellaLlena32 : a.estrellaLlena16) : (grande ? a.estrellaVacia32 : a.estrellaVacia16);
        if (arte != null)
        {
            imagen.sprite = arte;
            imagen.color = Color.white;
            return;
        }
        imagen.sprite = Estrella;
        imagen.color = llena ? colorLlena : colorVacia;
    }

    /// <summary>Destruye un objeto: Destroy al jugar y DestroyImmediate en el editor (para armar mapas sin dar Play).</summary>
    public static void Eliminar(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
    }

    private static float DistanciaASegmento(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    private static bool DentroDe(Vector2[] poligono, Vector2 p)
    {
        bool dentro = false;
        for (int i = 0, j = poligono.Length - 1; i < poligono.Length; j = i++)
        {
            if ((poligono[i].y > p.y) != (poligono[j].y > p.y) &&
                p.x < (poligono[j].x - poligono[i].x) * (p.y - poligono[i].y) / (poligono[j].y - poligono[i].y) + poligono[i].x)
                dentro = !dentro;
        }
        return dentro;
    }

    /// <summary>Orden de dibujo según la altura (y): lo que está más abajo en pantalla se dibuja encima.</summary>
    public static int OrdenY(float y)
    {
        return Mathf.RoundToInt(100f - y * 10f);
    }

    /// <summary>
    /// Coloca un sprite dentro de un área del mundo. Se dibuja a su tamaño natural (1 baldosa = 32 px de arte)
    /// y solo se reduce si no cabe, así todos los objetos conservan la misma escala entre sí. La medida sale
    /// de 'medida' (el sprite intacto), para que el objeto no cambie de tamaño al romperse.
    /// </summary>
    public static void AjustarAlArea(SpriteRenderer dibujo, Sprite medida, Bounds area)
    {
        if (dibujo == null || medida == null) return;
        Bounds sb = medida.bounds;
        if (sb.size.x < 0.0001f || sb.size.y < 0.0001f) return;

        float natural = medida.pixelsPerUnit / 32f;
        float e = Mathf.Min(natural, Mathf.Min(area.size.x / sb.size.x, area.size.y / sb.size.y));
        Transform padre = dibujo.transform.parent;
        float escalaPadre = padre != null ? Mathf.Max(0.0001f, Mathf.Abs(padre.lossyScale.x)) : 1f;
        dibujo.transform.localScale = new Vector3(e / escalaPadre, e / escalaPadre, 1f);
        dibujo.transform.position = new Vector3(area.center.x - e * sb.center.x, area.center.y - e * sb.center.y, 0f);
    }

    /// <summary>Texto en el mundo (carteles y avisos). 'altura' es la altura de una línea en unidades.</summary>
    public static TextMesh CrearTexto(Transform padre, string nombre, string texto, float altura, Color color, int orden)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        TextMesh tm = g.AddComponent<TextMesh>();

        Font fuente = null;
        try { fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        catch { fuente = null; }
        if (fuente != null) tm.font = fuente;

        tm.fontSize = 64;
        tm.characterSize = altura * 10f / 64f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = color;
        tm.text = texto;

        MeshRenderer mr = g.GetComponent<MeshRenderer>();
        if (fuente != null) mr.sharedMaterial = fuente.material;
        mr.sortingOrder = orden;
        return tm;
    }

    /// <summary>Crea un hijo con un SpriteRenderer provisional.</summary>
    public static SpriteRenderer Crear(Transform padre, string nombre, Sprite sprite, Color color, int orden)
    {
        GameObject o = new GameObject(nombre);
        o.transform.SetParent(padre, false);
        SpriteRenderer sr = o.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = orden;
        sr.spriteSortPoint = SpriteSortPoint.Pivot;
        return sr;
    }
}
