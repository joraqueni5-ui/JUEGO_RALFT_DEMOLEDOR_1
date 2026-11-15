using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Dibuja las paredes como franjas delgadas. Cada pared sigue ocupando su baldosa entera para las colisiones y
/// el A*, pero se pinta como una franja de unos 10 px (de 32) centrada en la baldosa y siguiendo la dirección
/// del muro; el resto de la baldosa muestra el suelo de cada lado (se copia del suelo vecino). En el borde
/// exterior del mapa la franja va pegada al lado interior y lo de afuera es oscuro. Las puertas son huecos con
/// un marco fino. Se calcula al empezar el piso a partir de los Tilemaps, así que sigue valiendo si el equipo
/// repinta el mapa a mano.
/// </summary>
public class DibujoParedes
{
    private enum Vecino { Fuera, Suelo, Pared, Puerta }

    private const int Lado = 32;

    private Tilemap suelo, paredes;
    private HashSet<Vector2Int> huecos = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> puertas = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> barandas = new HashSet<Vector2Int>();
    private Color colorPared = new Color(0.34f, 0.35f, 0.42f);
    private Color colorMarco = new Color(0.45f, 0.30f, 0.18f);
    private Color colorExterior = new Color(0.07f, 0.07f, 0.1f);
    private int grosor = 10;
    private Sprite texturaPared, marcoPuerta;
    private readonly List<Object> creados = new List<Object>();
    private GameObject raiz;

    /// <param name="huecos">Celdas del muro ocupadas por la entrada o el ascensor (cuentan como pared para continuar la franja).</param>
    public DibujoParedes(Tilemap suelo, Tilemap paredes, IEnumerable<Vector2Int> huecos, IEnumerable<Vector2Int> puertas, DefinicionPiso def, IEnumerable<Vector2Int> barandas = null)
    {
        this.suelo = suelo;
        this.paredes = paredes;
        if (huecos != null) this.huecos = new HashSet<Vector2Int>(huecos);
        if (puertas != null) this.puertas = new HashSet<Vector2Int>(puertas);
        if (barandas != null) this.barandas = new HashSet<Vector2Int>(barandas);
        if (def != null)
        {
            colorPared = def.colorPared;
            colorMarco = def.colorMarcoPuerta;
            colorExterior = def.colorExterior;
            grosor = Mathf.Clamp(def.grosorPared, 4, 20);
            if (Legible(def.texturaPared)) texturaPared = def.texturaPared;
            if (Legible(def.spriteMarcoPuerta)) marcoPuerta = def.spriteMarcoPuerta;
        }
    }

    /// <summary>El sprite se puede muestrear (su textura está marcada como legible).</summary>
    private static bool Legible(Sprite s)
    {
        if (s == null || s.texture == null) return false;
        if (!s.texture.isReadable)
        {
            Debug.LogWarning("[Paredes] El sprite '" + s.name + "' no es legible (Read/Write): se usa el color liso.");
            return false;
        }
        return true;
    }

    /// <summary>Píxel (0 a 31) de un sprite de 32 x 32 px.</summary>
    private static Color Muestra(Sprite s, int px, int py)
    {
        Rect r = s.textureRect;
        int tx = (int)r.x + Mathf.Clamp(px, 0, (int)r.width - 1);
        int ty = (int)r.y + Mathf.Clamp(py, 0, (int)r.height - 1);
        return s.texture.GetPixel(tx, ty);
    }

    // ------------------------------------------------------------ clasificación de celdas
    private Vecino Tipo(int x, int y)
    {
        Vector2Int c = new Vector2Int(x, y);
        if (puertas.Contains(c)) return Vecino.Puerta;
        if (huecos.Contains(c)) return Vecino.Pared;
        Vector3Int c3 = new Vector3Int(x, y, 0);
        if (paredes != null && paredes.HasTile(c3)) return Vecino.Pared;
        if (suelo != null && suelo.HasTile(c3)) return Vecino.Suelo;
        return Vecino.Fuera;
    }

    private static bool Continua(Vecino v) { return v == Vecino.Pared || v == Vecino.Puerta; }

    /// <summary>Rango de columnas y filas (de la baldosa de 32 x 32) que ocupa la franja de una pared.</summary>
    private void Geometria(int x, int y, out int c0, out int c1, out int r0, out int r1)
    {
        bool nFuera = Tipo(x, y + 1) == Vecino.Fuera, sFuera = Tipo(x, y - 1) == Vecino.Fuera;
        bool wFuera = Tipo(x - 1, y) == Vecino.Fuera, eFuera = Tipo(x + 1, y) == Vecino.Fuera;
        int centro = (Lado - grosor) / 2;

        if (nFuera && !sFuera) { r0 = 0; r1 = grosor - 1; }
        else if (sFuera && !nFuera) { r0 = Lado - grosor; r1 = Lado - 1; }
        else { r0 = centro; r1 = centro + grosor - 1; }

        if (wFuera && !eFuera) { c0 = Lado - grosor; c1 = Lado - 1; }
        else if (eFuera && !wFuera) { c0 = 0; c1 = grosor - 1; }
        else { c0 = centro; c1 = centro + grosor - 1; }
    }

    /// <summary>Zona del mundo que cubre la franja de una pared (para piezas pegadas al muro, como una ventana).</summary>
    public Rect FranjaDe(Vector2Int celda)
    {
        int c0, c1, r0, r1;
        Geometria(celda.x, celda.y, out c0, out c1, out r0, out r1);
        bool horizontal = Continua(Tipo(celda.x - 1, celda.y)) || Continua(Tipo(celda.x + 1, celda.y));
        bool vertical = Continua(Tipo(celda.x, celda.y - 1)) || Continua(Tipo(celda.x, celda.y + 1));
        float u = 1f / Lado;
        if (horizontal || !vertical) return new Rect(celda.x, celda.y + r0 * u, 1f, (r1 - r0 + 1) * u);
        return new Rect(celda.x + c0 * u, celda.y, (c1 - c0 + 1) * u, 1f);
    }

    // ------------------------------------------------------------ dibujo
    /// <summary>Textura de cada pared y puerta del mapa (y el rango de celdas que se recorrió).</summary>
    private List<KeyValuePair<Vector2Int, Texture2D>> GenerarCeldas(out BoundsInt area)
    {
        List<KeyValuePair<Vector2Int, Texture2D>> lista = new List<KeyValuePair<Vector2Int, Texture2D>>();
        area = suelo != null ? suelo.cellBounds : new BoundsInt();
        if (suelo == null) return lista;
        if (paredes != null) { paredes.CompressBounds(); area = Union(area, paredes.cellBounds); }

        for (int y = area.yMin - 1; y <= area.yMax; y++)
        {
            for (int x = area.xMin - 1; x <= area.xMax; x++)
            {
                Vector2Int c = new Vector2Int(x, y);
                Texture2D t = null;
                if (puertas.Contains(c)) t = TexturaPuerta(x, y);
                else if (paredes != null && paredes.HasTile(new Vector3Int(x, y, 0)) && !huecos.Contains(c)) t = TexturaPared(x, y);
                if (t == null) continue;
                t.filterMode = FilterMode.Point;
                t.Apply();
                lista.Add(new KeyValuePair<Vector2Int, Texture2D>(c, t));
            }
        }
        return lista;
    }

    public void Dibujar(Transform padre)
    {
        Limpiar();
        raiz = new GameObject("ParedesDelgadas");
        raiz.transform.SetParent(padre, false);

        if (paredes != null)
        {
            TilemapRenderer tr = paredes.GetComponent<TilemapRenderer>();
            if (tr != null) tr.enabled = false;   // las baldosas quedan solo para el collider
        }
        if (suelo == null) return;

        BoundsInt area;
        foreach (KeyValuePair<Vector2Int, Texture2D> par in GenerarCeldas(out area))
        {
            Vector2Int c = par.Key;
            Sprite s = Sprite.Create(par.Value, new Rect(0, 0, Lado, Lado), new Vector2(0.5f, 0.5f), Lado);
            creados.Add(par.Value);
            creados.Add(s);

            GameObject g = new GameObject(puertas.Contains(c) ? "Puerta" : "Pared");
            g.transform.SetParent(raiz.transform, false);
            g.transform.position = new Vector3(c.x + 0.5f, c.y + 0.5f, 0f);
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = puertas.Contains(c) ? -149 : -150;
        }
    }

    /// <summary>
    /// Compone todas las paredes y puertas en UNA sola textura (transparente donde no hay nada). La usa el editor para
    /// guardar las paredes ya dibujadas en la escena. 'origen' es la celda de la esquina inferior izquierda.
    /// </summary>
    public Texture2D Componer(out Vector2Int origen)
    {
        BoundsInt area;
        List<KeyValuePair<Vector2Int, Texture2D>> celdas = GenerarCeldas(out area);
        origen = new Vector2Int(area.xMin - 1, area.yMin - 1);
        int columnas = area.xMax - area.xMin + 2, filas = area.yMax - area.yMin + 2;
        if (celdas.Count == 0 || columnas <= 0 || filas <= 0) return null;

        Texture2D total = new Texture2D(columnas * Lado, filas * Lado, TextureFormat.RGBA32, false);
        Color[] vacio = new Color[total.width * total.height];
        total.SetPixels(vacio);
        foreach (KeyValuePair<Vector2Int, Texture2D> par in celdas)
        {
            total.SetPixels((par.Key.x - origen.x) * Lado, (par.Key.y - origen.y) * Lado, Lado, Lado, par.Value.GetPixels());
            Eliminar(par.Value);
        }
        total.filterMode = FilterMode.Point;
        total.Apply();
        return total;
    }

    private static BoundsInt Union(BoundsInt a, BoundsInt b)
    {
        if (a.size.x == 0 || a.size.y == 0) return b;
        if (b.size.x == 0 || b.size.y == 0) return a;
        BoundsInt r = new BoundsInt();
        r.SetMinMax(Vector3Int.Min(a.min, b.min), Vector3Int.Max(a.max, b.max));
        return r;
    }

    public void Limpiar()
    {
        foreach (Object o in creados) Eliminar(o);
        creados.Clear();
        Eliminar(raiz);
        raiz = null;
    }

    private static void Eliminar(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
    }

    // ------------------------------------------------------------ texturas
    private Texture2D TexturaPared(int x, int y)
    {
        int c0, c1, r0, r1;
        Geometria(x, y, out c0, out c1, out r0, out r1);
        bool armE = Continua(Tipo(x + 1, y)), armW = Continua(Tipo(x - 1, y));
        bool armN = Continua(Tipo(x, y + 1)), armS = Continua(Tipo(x, y - 1));

        Texture2D t = new Texture2D(Lado, Lado, TextureFormat.RGBA32, false);
        Color[] px = new Color[Lado * Lado];
        bool[] esPared = new bool[Lado * Lado];
        bool baranda = barandas.Contains(new Vector2Int(x, y));
        bool corrida = armE || armW;

        for (int py = 0; py < Lado; py++)
        {
            for (int pxl = 0; pxl < Lado; pxl++)
            {
                bool enCols = pxl >= c0 && pxl <= c1, enFilas = py >= r0 && py <= r1;
                bool pared = (enCols && enFilas)
                          || (enFilas && pxl > c1 && armE) || (enFilas && pxl < c0 && armW)
                          || (enCols && py > r1 && armN) || (enCols && py < r0 && armS);
                esPared[py * Lado + pxl] = pared;
                if (pared)
                {
                    px[py * Lado + pxl] = baranda ? ColorBaranda(pxl, py, corrida, c0, c1, r0, r1)
                                        : texturaPared != null ? Opaco(Muestra(texturaPared, pxl, py)) : colorPared;
                    continue;
                }

                int hx = pxl < c0 ? -1 : pxl > c1 ? 1 : 0;
                int vy = py < r0 ? -1 : py > r1 ? 1 : 0;
                px[py * Lado + pxl] = ColorDeFondo(x, y, hx, vy, pxl, py);
            }
        }

        // Borde un poco más oscuro donde la franja toca el suelo (la baranda lleva sus propios postes).
        Color borde = new Color(colorPared.r * 0.72f, colorPared.g * 0.72f, colorPared.b * 0.72f, 1f);
        for (int py = 0; py < Lado; py++)
        {
            for (int pxl = 0; pxl < Lado; pxl++)
            {
                if (!esPared[py * Lado + pxl]) continue;
                bool toca = (pxl > 0 && !esPared[py * Lado + pxl - 1]) || (pxl < Lado - 1 && !esPared[py * Lado + pxl + 1]) ||
                            (py > 0 && !esPared[(py - 1) * Lado + pxl]) || (py < Lado - 1 && !esPared[(py + 1) * Lado + pxl]);
                if (toca && !baranda)
                    px[py * Lado + pxl] = texturaPared == null ? borde : new Color(px[py * Lado + pxl].r * 0.45f, px[py * Lado + pxl].g * 0.45f, px[py * Lado + pxl].b * 0.5f, 1f);
            }
        }

        // Con textura (paredes claras): una sombra de 2 px sobre el suelo junto a la pared para que se distinga.
        if (texturaPared != null && !baranda)
        {
            for (int py = 0; py < Lado; py++)
            {
                for (int pxl = 0; pxl < Lado; pxl++)
                {
                    if (esPared[py * Lado + pxl]) continue;
                    bool cerca = false;
                    for (int dy = -2; dy <= 2 && !cerca; dy++)
                    {
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            int nx = pxl + dx, ny = py + dy;
                            if (nx < 0 || ny < 0 || nx >= Lado || ny >= Lado) continue;
                            if (esPared[ny * Lado + nx]) { cerca = true; break; }
                        }
                    }
                    if (cerca) { Color o = px[py * Lado + pxl]; px[py * Lado + pxl] = new Color(o.r * 0.72f, o.g * 0.72f, o.b * 0.76f, 1f); }
                }
            }
        }
        t.SetPixels(px);
        return t;
    }

    private static Color Opaco(Color c) { c.a = 1f; return c; }

    /// <summary>Baranda: pasamanos claro con postes oscuros cada 8 px.</summary>
    private static Color ColorBaranda(int pxl, int py, bool corrida, int c0, int c1, int r0, int r1)
    {
        Color poste = new Color(0.30f, 0.33f, 0.38f), pasamanos = new Color(0.80f, 0.84f, 0.88f);
        int a = corrida ? pxl : py;
        bool borde = corrida ? (py == r0 || py == r1) : (pxl == c0 || pxl == c1);
        return (a % 8 < 2 || borde) ? poste : pasamanos;
    }

    /// <summary>Color de un píxel que no es pared: el suelo del lado al que pertenece, o el exterior oscuro.</summary>
    private Color ColorDeFondo(int x, int y, int hx, int vy, int pxl, int py)
    {
        // Vecinos candidatos en orden: el vertical, el horizontal y el diagonal.
        List<Vector2Int> candidatos = new List<Vector2Int>(3);
        if (vy != 0) candidatos.Add(new Vector2Int(x, y + vy));
        if (hx != 0) candidatos.Add(new Vector2Int(x + hx, y));
        if (hx != 0 && vy != 0) candidatos.Add(new Vector2Int(x + hx, y + vy));

        // Primero un suelo de verdad; una puerta solo si no hay otro (así el borde del hueco no toma el suelo del lado equivocado).
        foreach (Vector2Int v in candidatos)
            if (Tipo(v.x, v.y) == Vecino.Suelo) return MuestraSuelo(v, pxl, py);
        foreach (Vector2Int v in candidatos)
            if (Tipo(v.x, v.y) == Vecino.Puerta) return MuestraSuelo(v, pxl, py);
        return colorExterior;
    }

    private Color MuestraSuelo(Vector2Int celda, int pxl, int py)
    {
        if (suelo == null) return colorExterior;
        Sprite s = suelo.GetSprite(new Vector3Int(celda.x, celda.y, 0));
        if (s == null || s.texture == null || !s.texture.isReadable) return new Color(0.8f, 0.8f, 0.8f, 1f);
        Rect r = s.textureRect;
        int tx = Mathf.Clamp((int)r.x + pxl, 0, s.texture.width - 1);
        int ty = Mathf.Clamp((int)r.y + py, 0, s.texture.height - 1);
        Color c = s.texture.GetPixel(tx, ty);
        c.a = 1f;
        return c;
    }

    /// <summary>Hueco del muro con un marco fino a cada lado y el suelo de ambos lados.</summary>
    private Texture2D TexturaPuerta(int x, int y)
    {
        bool entreMurosHorizontal = Continua(Tipo(x - 1, y)) && Continua(Tipo(x + 1, y));
        bool entreMurosVertical = Continua(Tipo(x, y - 1)) && Continua(Tipo(x, y + 1));
        bool pasoVertical = entreMurosHorizontal || !entreMurosVertical;   // se cruza de norte a sur

        int centro = (Lado - grosor) / 2;
        int b0 = centro - 1, b1 = centro + grosor;
        Texture2D t = new Texture2D(Lado, Lado, TextureFormat.RGBA32, false);
        Color[] px = new Color[Lado * Lado];
        for (int py = 0; py < Lado; py++)
        {
            for (int pxl = 0; pxl < Lado; pxl++)
            {
                Vector2Int lado;
                if (pasoVertical) lado = new Vector2Int(x, py >= Lado / 2 ? y + 1 : y - 1);
                else lado = new Vector2Int(pxl >= Lado / 2 ? x + 1 : x - 1, y);

                Vecino tipo = Tipo(lado.x, lado.y);
                Color c = tipo == Vecino.Suelo || tipo == Vecino.Puerta ? MuestraSuelo(lado, pxl, py) : MuestraSuelo(new Vector2Int(x, y), pxl, py);

                if (marcoPuerta != null)
                {
                    // El arte del marco está dibujado para un paso norte-sur; en un muro vertical se gira un cuarto de vuelta.
                    Color m = pasoVertical ? Muestra(marcoPuerta, pxl, py) : Muestra(marcoPuerta, py, Lado - 1 - pxl);
                    px[py * Lado + pxl] = Color.Lerp(c, new Color(m.r, m.g, m.b, 1f), m.a);
                    continue;
                }

                bool marco = pasoVertical
                    ? (pxl <= 2 || pxl >= Lado - 3) && py >= b0 && py <= b1
                    : (py <= 2 || py >= Lado - 3) && pxl >= b0 && pxl <= b1;
                px[py * Lado + pxl] = marco ? colorMarco : c;
            }
        }
        t.SetPixels(px);
        return t;
    }
}
