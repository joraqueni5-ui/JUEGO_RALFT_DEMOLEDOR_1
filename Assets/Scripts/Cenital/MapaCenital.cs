using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// El mapa de un piso visto desde arriba (prefab Mapa_PisoN). Lo edita el equipo a mano:
/// - Tilemaps SIN collider = suelo.  Tilemaps CON TilemapCollider2D = paredes (sólido).
/// - Cualquier otro collider dentro del prefab (objetos reparables, ventanal, ascensor,
///   muebles que agreguen) también bloquea el paso.
/// - Puntos de aparición: hijos "SpawnJugador", "SpawnDemoledor" y "EntradaDemoledor".
///
/// Al empezar el piso lee el mapa TAL COMO QUEDÓ y arma la cuadrícula de celdas transitables
/// que usa el A* del demoledor. No hay ninguna distribución fija en el código.
/// </summary>
public class MapaCenital : MonoBehaviour
{
    [Header("Puntos (si se dejan vacíos se buscan por nombre)")]
    public Transform spawnJugador;
    [Tooltip("Fuera del edificio, sobre el ventanal.")]
    public Transform spawnDemoledor;
    [Tooltip("Dentro del piso, justo bajo el ventanal.")]
    public Transform entradaDemoledor;
    public VentanalCenital ventanal;

    public readonly List<ObjetoReparable> objetos = new List<ObjetoReparable>();

    private Tilemap[] tilemaps;
    private Grid grid;
    private Vector3Int origen;
    private int ancho, alto;
    private bool[,] transitable;
    private readonly List<Vector2> centrosTransitables = new List<Vector2>();
    private Rect limites;

    /// <summary>Zona del mapa en coordenadas de mundo (todas las celdas con baldosa).</summary>
    public Rect Limites { get { return limites; } }
    public List<Vector2> CentrosTransitables { get { return centrosTransitables; } }

    public void Preparar(DefinicionPiso definicion)
    {
        tilemaps = GetComponentsInChildren<Tilemap>();
        grid = GetComponentInChildren<Grid>();
        if (grid == null && tilemaps.Length > 0) grid = tilemaps[0].layoutGrid;

        if (spawnJugador == null) spawnJugador = BuscarHijo("SpawnJugador");
        if (spawnDemoledor == null) spawnDemoledor = BuscarHijo("SpawnDemoledor");
        if (entradaDemoledor == null) entradaDemoledor = BuscarHijo("EntradaDemoledor");
        if (ventanal == null) ventanal = GetComponentInChildren<VentanalCenital>();

        objetos.Clear();
        objetos.AddRange(GetComponentsInChildren<ObjetoReparable>());
        foreach (ObjetoReparable o in objetos) o.Preparar(definicion);
        if (ventanal != null) ventanal.Preparar(definicion);

        Rehacer();
    }

    private Transform BuscarHijo(string nombre)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }

    private static bool EsSolido(Tilemap tm)
    {
        return tm.GetComponent<TilemapCollider2D>() != null || tm.GetComponent<CompositeCollider2D>() != null;
    }

    /// <summary>Vuelve a leer el mapa (por si se movieron objetos).</summary>
    public void Rehacer()
    {
        Physics2D.SyncTransforms();
        centrosTransitables.Clear();
        if (tilemaps == null || tilemaps.Length == 0 || grid == null) { transitable = new bool[0, 0]; return; }

        BoundsInt total = new BoundsInt();
        bool primero = true;
        foreach (Tilemap tm in tilemaps)
        {
            tm.CompressBounds();
            BoundsInt b = tm.cellBounds;
            if (b.size.x == 0 || b.size.y == 0) continue;
            if (primero) { total = b; primero = false; }
            else
            {
                Vector3Int min = Vector3Int.Min(total.min, b.min);
                Vector3Int max = Vector3Int.Max(total.max, b.max);
                total.SetMinMax(min, max);
            }
        }
        if (primero) { transitable = new bool[0, 0]; return; }

        origen = new Vector3Int(total.xMin, total.yMin, 0);
        ancho = total.size.x;
        alto = total.size.y;
        transitable = new bool[ancho, alto];

        // Colliders que bloquean además de las paredes: objetos, ventanal, ascensor...
        List<Rect> bloqueos = new List<Rect>();
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
        {
            if (c == null || !c.enabled || c.isTrigger) continue;
            if (c is TilemapCollider2D || c is CompositeCollider2D) continue;
            Bounds bb = c.bounds;
            bloqueos.Add(new Rect(bb.min.x, bb.min.y, bb.size.x, bb.size.y));
        }

        Vector3 tam = grid.cellSize;
        float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
        for (int x = 0; x < ancho; x++)
        {
            for (int y = 0; y < alto; y++)
            {
                Vector3Int celda = new Vector3Int(origen.x + x, origen.y + y, 0);
                bool suelo = false, solido = false, hayBaldosa = false;
                foreach (Tilemap tm in tilemaps)
                {
                    if (!tm.HasTile(celda)) continue;
                    hayBaldosa = true;
                    if (EsSolido(tm)) solido = true; else suelo = true;
                }

                Vector3 centro3 = tm0().GetCellCenterWorld(celda);
                if (hayBaldosa)
                {
                    xMin = Mathf.Min(xMin, centro3.x - tam.x * 0.5f);
                    yMin = Mathf.Min(yMin, centro3.y - tam.y * 0.5f);
                    xMax = Mathf.Max(xMax, centro3.x + tam.x * 0.5f);
                    yMax = Mathf.Max(yMax, centro3.y + tam.y * 0.5f);
                }
                if (!suelo || solido) continue;

                Rect celdaRect = new Rect(centro3.x - tam.x * 0.4f, centro3.y - tam.y * 0.4f, tam.x * 0.8f, tam.y * 0.8f);
                bool bloqueada = false;
                for (int i = 0; i < bloqueos.Count; i++)
                {
                    if (bloqueos[i].Overlaps(celdaRect)) { bloqueada = true; break; }
                }
                if (bloqueada) continue;

                transitable[x, y] = true;
                centrosTransitables.Add(new Vector2(centro3.x, centro3.y));
            }
        }
        limites = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    private Tilemap tm0() { return tilemaps[0]; }

    [Header("Diagnóstico")]
    [Tooltip("Dibuja con Gizmos las celdas que el A* considera libres (verde) y las que no (rojo).")]
    public bool dibujarCuadricula;

    private void OnDrawGizmos()
    {
        if (!dibujarCuadricula || !Application.isPlaying || transitable == null || grid == null) return;
        for (int x = 0; x < ancho; x++)
        {
            for (int y = 0; y < alto; y++)
            {
                Vector2 c = CentroDe(new Vector2Int(origen.x + x, origen.y + y));
                Gizmos.color = transitable[x, y] ? new Color(0f, 1f, 0f, 0.12f) : new Color(1f, 0f, 0f, 0.25f);
                Gizmos.DrawCube(c, new Vector3(0.9f, 0.9f, 0f));
            }
        }
    }

    // ------------------------------------------------------------ consultas de la cuadrícula
    public int Ancho { get { return ancho; } }
    public int Alto { get { return alto; } }

    public bool EsTransitable(Vector2Int c)
    {
        int x = c.x - origen.x, y = c.y - origen.y;
        return x >= 0 && y >= 0 && x < ancho && y < alto && transitable[x, y];
    }

    public Vector2Int CeldaDe(Vector2 mundo)
    {
        Vector3Int c = grid.WorldToCell(new Vector3(mundo.x, mundo.y, 0f));
        return new Vector2Int(c.x, c.y);
    }

    public Vector2 CentroDe(Vector2Int celda)
    {
        Vector3 p = tm0().GetCellCenterWorld(new Vector3Int(celda.x, celda.y, 0));
        return new Vector2(p.x, p.y);
    }

    /// <summary>La celda transitable más cercana a un punto del mundo (la misma si ya lo es).</summary>
    public bool CeldaTransitableCercana(Vector2 mundo, out Vector2Int resultado)
    {
        Vector2Int c = CeldaDe(mundo);
        if (EsTransitable(c)) { resultado = c; return true; }

        float mejor = float.MaxValue;
        resultado = c;
        bool hallada = false;
        foreach (Vector2 centro in centrosTransitables)
        {
            float d = (centro - mundo).sqrMagnitude;
            if (d < mejor) { mejor = d; resultado = CeldaDe(centro); hallada = true; }
        }
        return hallada;
    }

    /// <summary>Hay paso libre en línea recta (con margen a los lados) entre dos puntos.</summary>
    public bool LineaLibre(Vector2 a, Vector2 b, float margen)
    {
        Vector2 d = b - a;
        float largo = d.magnitude;
        if (largo < 0.001f) return true;
        Vector2 dir = d / largo;
        Vector2 lado = new Vector2(-dir.y, dir.x) * margen;
        for (float t = 0f; t <= largo; t += 0.2f)
        {
            Vector2 p = a + dir * t;
            if (!EsTransitable(CeldaDe(p)) || !EsTransitable(CeldaDe(p + lado)) || !EsTransitable(CeldaDe(p - lado)))
                return false;
        }
        return EsTransitable(CeldaDe(b));
    }
}
