using UnityEngine;

/// <summary>
/// Figuras de colores provisionales del modo cenital (cuadrado y círculo de 1 unidad) y
/// utilidades comunes. Se generan al vuelo: no hace falta ningún archivo de arte.
/// </summary>
public static class Provisional
{
    private static Sprite cuadrado;
    private static Sprite circulo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        cuadrado = null;
        circulo = null;
    }

    /// <summary>Cuadrado blanco de 1 x 1 unidades (se tiñe con SpriteRenderer.color).</summary>
    public static Sprite Cuadrado
    {
        get
        {
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

    /// <summary>Orden de dibujo según la altura (y): lo que está más abajo en pantalla se dibuja encima.</summary>
    public static int OrdenY(float y)
    {
        return Mathf.RoundToInt(100f - y * 10f);
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
        return sr;
    }
}
