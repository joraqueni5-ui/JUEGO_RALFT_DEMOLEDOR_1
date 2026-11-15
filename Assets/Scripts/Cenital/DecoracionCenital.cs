using UnityEngine;

/// <summary>
/// Una pieza de decoración del piso (banco, planta, alfombrilla, ventana, cartel...). No se rompe y
/// no cuenta para el destrozo. Si su tipo tiene colisión Simple, el prefab le pone un collider y el
/// jugador y el A* la rodean. El arte sale de la ficha 'tipo' de DefinicionPiso.decoracion; si no hay
/// sprite se dibuja una figura del color provisional de la ficha.
/// </summary>
public class DecoracionCenital : MonoBehaviour
{
    [Tooltip("Posición del tipo en la lista 'Decoración' de la DefinicionPiso.")]
    public int tipo;
    [Tooltip("Tamaño en baldosas. (0, 0) = el de la ficha o el del collider.")]
    public Vector2Int tamano;
    [Tooltip("Texto que se escribe sobre la pieza (carteles).")]
    public string texto = "";
    [Tooltip("Va sobre un muro (ventana, cartel): se dibuja encima de las paredes.")]
    public bool enPared;
    [Tooltip("Va encima de un mueble (por ejemplo un microscopio sobre una mesa).")]
    public bool encima;

    private const int OrdenSuelo = -190;
    private const int OrdenPared = -140;

    private SpriteRenderer dibujo;
    private TextMesh textoMalla;
    private float anchoMaximoTexto;
    private bool ajustarTexto;

    /// <summary>Se llama una vez al empezar el piso: crea el dibujo, la sombra y el texto.</summary>
    public void Preparar(DefinicionPiso definicion)
    {
        FichaDecoracion ficha = definicion != null ? definicion.DecoracionDe(tipo) : null;
        BorrarHijo("Dibujo");
        BorrarHijo("Sombra");
        BorrarHijo("Texto");

        Bounds area = Area(ficha);
        bool alSuelo = !enPared && !encima && (ficha == null || (ficha.colision == ColisionDecoracion.Ninguna && !ficha.ordenarPorAltura));
        int orden = enPared ? OrdenPared : alSuelo ? OrdenSuelo : Provisional.OrdenY(area.min.y) + (encima ? 3 : 0);

        bool conSombra = ficha != null && ficha.sombra && !enPared && !alSuelo && !encima;
        if (conSombra)
        {
            SpriteRenderer sombra = Provisional.Crear(transform, "Sombra", Provisional.Circulo, new Color(0f, 0f, 0f, 0.26f), orden - 1);
            sombra.transform.position = new Vector3(area.center.x, area.min.y + 0.1f, 0f);
            sombra.transform.localScale = new Vector3(Mathf.Max(0.5f, area.size.x * 0.95f), 0.4f, 1f);
        }

        Sprite sprite = ficha != null ? ficha.sprite : null;
        if (ficha != null && DibujarPiezaLarga(ficha, area, orden))
        {
            // Armada con varias piezas o repitiendo una baldosa (ya dibujada).
        }
        else if (sprite != null)
        {
            dibujo = Provisional.Crear(transform, "Dibujo", sprite, Color.white, orden);
            Provisional.AjustarAlArea(dibujo, sprite, area);
        }
        else
        {
            Color color = ficha != null ? ficha.colorProvisional : new Color(0.7f, 0.7f, 0.7f);
            bool circular = ficha != null && ficha.provisionalCircular;
            float k = ficha != null ? Mathf.Clamp(ficha.escalaProvisional, 0.1f, 1f) : 1f;
            dibujo = Provisional.Crear(transform, "Dibujo", circular ? Provisional.Circulo : Provisional.Cuadrado, color, orden);
            dibujo.transform.position = area.center;
            dibujo.transform.localScale = new Vector3(Mathf.Max(0.2f, area.size.x - 0.1f) * k, Mathf.Max(0.2f, area.size.y - 0.1f) * k, 1f);
        }

        if (!string.IsNullOrEmpty(texto))
        {
            TextMesh t = Provisional.CrearTexto(transform, "Texto", texto, Mathf.Min(0.5f, area.size.y * 0.55f), Color.white, orden + 1);
            t.transform.position = area.center;
            textoMalla = t;
            anchoMaximoTexto = Mathf.Max(0.5f, area.size.x - 0.3f);
            ajustarTexto = true;
        }
    }

    /// <summary>Medida de un sprite en baldosas (1 baldosa = 32 px de arte).</summary>
    private static Vector2Int Baldosas(Sprite s)
    {
        float ppu = Mathf.Max(1f, s.pixelsPerUnit);
        return new Vector2Int(Mathf.RoundToInt(s.rect.width / ppu), Mathf.RoundToInt(s.rect.height / ppu));
    }

    /// <summary>
    /// Dibuja la pieza con arte que no cabe en un solo sprite: la variante que mide exactamente lo mismo, una pieza
    /// larga armada con extremos y centros (carteles, pizarras) o una baldosa repetida (bancas largas). Devuelve false
    /// si la ficha no necesita nada de eso y se usa el sprite normal.
    /// </summary>
    private bool DibujarPiezaLarga(FichaDecoracion ficha, Bounds area, int orden)
    {
        int w = Mathf.Max(1, Mathf.RoundToInt(area.size.x)), h = Mathf.Max(1, Mathf.RoundToInt(area.size.y));

        // 1) Un arte hecho justo a esa medida (banca de 1 x 3, cartel de 4 x 1).
        if (ficha.spritesPorTamano != null)
        {
            foreach (Sprite s in ficha.spritesPorTamano)
            {
                if (s == null || Baldosas(s) != new Vector2Int(w, h)) continue;
                dibujo = Provisional.Crear(transform, "Dibujo", s, Color.white, orden);
                Provisional.AjustarAlArea(dibujo, s, area);
                return true;
            }
        }

        // 2) Extremo izquierdo + centros + extremo derecho.
        if (h == 1 && w >= 2 && ficha.spriteIzquierda != null && ficha.spriteDerecha != null)
        {
            Sprite medio = ficha.spriteCentro != null ? ficha.spriteCentro : ficha.spriteIzquierda;
            Transform contenedor = new GameObject("Dibujo").transform;
            contenedor.SetParent(transform, false);
            for (int i = 0; i < w; i++)
            {
                Sprite s = i == 0 ? ficha.spriteIzquierda : i == w - 1 ? ficha.spriteDerecha : medio;
                SpriteRenderer parte = Provisional.Crear(contenedor, "Parte " + (i + 1), s, Color.white, orden);
                Provisional.AjustarAlArea(parte, s, new Bounds(new Vector3(area.min.x + i + 0.5f, area.center.y, 0f), new Vector3(1f, 1f, 0f)));
            }
            return true;
        }

        // 3) Una baldosa repetida para llenar el área.
        Sprite uno = ficha.sprite;
        if (uno != null && (w > 1 || h > 1) && Baldosas(uno) == Vector2Int.one)
        {
            Transform contenedor = new GameObject("Dibujo").transform;
            contenedor.SetParent(transform, false);
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    // Cada baldosa se ordena por su propia altura (la de abajo se dibuja delante), salvo suelo y muro.
                    int ordenBaldosa = orden == OrdenSuelo || orden == OrdenPared ? orden : Provisional.OrdenY(area.min.y + j) + (encima ? 3 : 0);
                    SpriteRenderer parte = Provisional.Crear(contenedor, "Baldosa " + (i + 1) + "," + (j + 1), uno, Color.white, ordenBaldosa);
                    Provisional.AjustarAlArea(parte, uno, new Bounds(new Vector3(area.min.x + i + 0.5f, area.min.y + j + 0.5f, 0f), new Vector3(1f, 1f, 0f)));
                }
            }
            return true;
        }
        return false;
    }

    // El texto se achica si es más ancho que la pieza (se mide una vez, cuando ya tiene su malla).
    private void LateUpdate()
    {
        if (!ajustarTexto || textoMalla == null) return;
        ajustarTexto = false;
        MeshRenderer mr = textoMalla.GetComponent<MeshRenderer>();
        if (mr == null) return;
        float ancho = mr.bounds.size.x;
        if (ancho > anchoMaximoTexto && ancho > 0.01f)
            textoMalla.transform.localScale *= anchoMaximoTexto / ancho;
    }

    private Bounds Area(FichaDecoracion ficha)
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c != null && c.enabled && !c.isTrigger) return c.bounds;

        // Sobre un muro delgado: del grosor de la franja de la pared, no de la baldosa entera.
        MapaCenital mapa = GetComponentInParent<MapaCenital>();
        if (enPared && ficha != null && ficha.ajustarAFranja && mapa != null)
        {
            Vector2Int celda = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
            Rect franja = mapa.FranjaDePared(celda);
            return new Bounds(new Vector3(franja.center.x, franja.center.y, 0f), new Vector3(franja.width, franja.height, 0f));
        }

        Vector2Int t = tamano;
        if (t.x <= 0 || t.y <= 0) t = ficha != null ? ficha.tamanoBaldosas : new Vector2Int(1, 1);
        return new Bounds(transform.position, new Vector3(Mathf.Max(1, t.x), Mathf.Max(1, t.y), 0f));
    }

    private void BorrarHijo(string nombre)
    {
        Transform t = transform.Find(nombre);
        if (t == null) return;
        if (Application.isPlaying) Destroy(t.gameObject); else DestroyImmediate(t.gameObject);
    }
}
