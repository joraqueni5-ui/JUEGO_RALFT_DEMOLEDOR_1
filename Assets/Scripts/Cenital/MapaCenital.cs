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
    [Tooltip("Ascensor de abajo: se abre cuando el demoledor se rinde y se va.")]
    public AscensorCenital ascensor;

    [Header("Datos del mapa de texto (los escribe el constructor)")]
    [Tooltip("Celdas de puerta (huecos en los muros).")]
    public Vector2Int[] puertas = new Vector2Int[0];
    [Tooltip("Centros de las celdas 'P': puntos de aparición de poderes 1.")]
    public Vector2[] puntosPoderP = new Vector2[0];
    [Tooltip("Centros de las celdas 'Q': puntos de aparición de poderes 2.")]
    public Vector2[] puntosPoderQ = new Vector2[0];
    [Tooltip("Celdas de pared que se dibujan como baranda (borde exterior de una terraza).")]
    public Vector2Int[] barandas = new Vector2Int[0];
    [Tooltip("Punto '!' desde donde salta el demoledor al rendirse en un piso sin ascensor.")]
    public Transform puntoSalto;
    [Tooltip("Hacia dónde salta (hacia fuera del edificio).")]
    public Vector2 direccionSalto = Vector2.right;

    [Header("Paredes dibujadas en la escena")]
    [Tooltip("Las paredes delgadas ya están dibujadas y guardadas en el objeto 'ParedesDelgadas' (las deja el menú de escenas). " +
             "Desmárcalo para que se vuelvan a dibujar al jugar (por ejemplo si pintaste el mapa a mano).")]
    public bool paredesGuardadas;

    public readonly List<ObjetoReparable> objetos = new List<ObjetoReparable>();

    private DibujoParedes dibujoParedes;

    /// <summary>La celda es una puerta del mapa.</summary>
    public bool EsPuerta(Vector2Int celda)
    {
        if (puertas == null) return false;
        foreach (Vector2Int p in puertas) if (p == celda) return true;
        return false;
    }

    /// <summary>Zona del mundo que cubre la franja de la pared de una celda (el resto de la baldosa es suelo).</summary>
    public Rect FranjaDePared(Vector2Int celda)
    {
        return dibujoParedes != null ? dibujoParedes.FranjaDe(celda) : new Rect(celda.x, celda.y, 1f, 1f);
    }

    private void OnDestroy()
    {
        if (dibujoParedes != null) dibujoParedes.Limpiar();
    }

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

        if (ascensor == null)
        {
            Transform t = BuscarHijo("Ascensor");
            if (t != null) ascensor = t.GetComponent<AscensorCenital>() ?? t.gameObject.AddComponent<AscensorCenital>();
        }

        AsegurarColisionDeParedes();
        DibujarParedes(definicion);

        objetos.Clear();
        objetos.AddRange(GetComponentsInChildren<ObjetoReparable>());
        foreach (ObjetoReparable o in objetos) o.Preparar(definicion);
        foreach (DecoracionCenital d in GetComponentsInChildren<DecoracionCenital>()) d.Preparar(definicion);
        if (ventanal != null) ventanal.Preparar(definicion);
        if (ascensor != null) ascensor.Preparar(definicion);

        Rehacer();
    }

    /// <summary>
    /// Las paredes bloquean con la baldosa ENTERA (el dibujo delgado es solo visual). Se asegura que cada Tilemap de paredes tenga su
    /// cuerpo estático, TilemapCollider2D y CompositeCollider2D activos, se genera su geometría y se avisa si queda vacía.
    /// </summary>
    private void AsegurarColisionDeParedes()
    {
        Physics2D.SyncTransforms();
        foreach (Tilemap tm in tilemaps)
        {
            if (!EsSolido(tm)) continue;

            Rigidbody2D cuerpo = tm.GetComponent<Rigidbody2D>();
            if (cuerpo == null) cuerpo = tm.gameObject.AddComponent<Rigidbody2D>();
            cuerpo.bodyType = RigidbodyType2D.Static;

            TilemapCollider2D tc = tm.GetComponent<TilemapCollider2D>();
            if (tc == null) tc = tm.gameObject.AddComponent<TilemapCollider2D>();
            tc.enabled = true;
            tc.isTrigger = false;
            tc.compositeOperation = Collider2D.CompositeOperation.Merge;

            CompositeCollider2D cc = tm.GetComponent<CompositeCollider2D>();
            if (cc == null) cc = tm.gameObject.AddComponent<CompositeCollider2D>();
            cc.enabled = true;
            cc.isTrigger = false;

            tc.ProcessTilemapChanges();
            cc.GenerateGeometry();
            tm.CompressBounds();
            int baldosas = 0;
            foreach (Vector3Int c in tm.cellBounds.allPositionsWithin) if (tm.HasTile(c)) baldosas++;
            if (baldosas > 0 && cc.pointCount == 0)
                Debug.LogError("[Mapa] Las paredes de '" + name + "' tienen " + baldosas + " baldosas pero el collider quedó vacío: el jugador las atravesaría. Revisa el Tilemap '" + tm.name + "'.");
        }
    }

    /// <summary>Prepara el dibujo de las paredes a partir de los Tilemaps (null si no hay suelo).</summary>
    private DibujoParedes CrearDibujoParedes(DefinicionPiso definicion)
    {
        Tilemap suelo = null, muros = null;
        foreach (Tilemap tm in tilemaps)
        {
            if (EsSolido(tm)) { if (muros == null) muros = tm; }
            else if (suelo == null || tm.name == "Suelo") suelo = tm;
        }
        if (suelo == null) return null;

        Physics2D.SyncTransforms();
        List<Vector2Int> huecos = new List<Vector2Int>();
        AgregarCeldas(huecos, ventanal != null ? ventanal.GetComponent<Collider2D>() : null);
        AgregarCeldas(huecos, ascensor != null ? ascensor.GetComponent<Collider2D>() : null);
        return new DibujoParedes(suelo, muros, huecos, puertas, definicion, barandas);
    }

    /// <summary>Pinta las paredes como franjas delgadas (las baldosas de pared quedan solo para la colisión).</summary>
    private void DibujarParedes(DefinicionPiso definicion)
    {
        Transform guardado = transform.Find("ParedesDelgadas");
        if (dibujoParedes != null) dibujoParedes.Limpiar();

        if (paredesGuardadas && guardado != null)
        {
            // Ya están dibujadas en la escena: solo se ocultan las baldosas de pared (quedan para el collider).
            foreach (Tilemap tm in tilemaps)
            {
                if (!EsSolido(tm)) continue;
                TilemapRenderer tr = tm.GetComponent<TilemapRenderer>();
                if (tr != null) tr.enabled = false;
            }
            dibujoParedes = CrearDibujoParedes(definicion);   // para FranjaDePared
            return;
        }

        if (guardado != null) Provisional.Eliminar(guardado.gameObject);
        dibujoParedes = CrearDibujoParedes(definicion);
        if (dibujoParedes != null) dibujoParedes.Dibujar(transform);
    }

    /// <summary>
    /// Compone las paredes y puertas en una sola textura para guardarlas en la escena (la usa el editor).
    /// 'origen' es la celda de la esquina inferior izquierda de la textura.
    /// </summary>
    public Texture2D ComponerParedes(DefinicionPiso definicion, out Vector2Int origen)
    {
        origen = Vector2Int.zero;
        tilemaps = GetComponentsInChildren<Tilemap>();
        grid = GetComponentInChildren<Grid>();
        if (ventanal == null) ventanal = GetComponentInChildren<VentanalCenital>();
        if (ascensor == null) ascensor = GetComponentInChildren<AscensorCenital>();
        DibujoParedes d = CrearDibujoParedes(definicion);
        return d == null ? null : d.Componer(out origen);
    }

    private static void AgregarCeldas(List<Vector2Int> lista, Collider2D c)
    {
        if (c == null) return;
        Bounds b = c.bounds;
        for (int x = Mathf.FloorToInt(b.min.x + 0.01f); x < Mathf.CeilToInt(b.max.x - 0.01f); x++)
            for (int y = Mathf.FloorToInt(b.min.y + 0.01f); y < Mathf.CeilToInt(b.max.y - 0.01f); y++)
                lista.Add(new Vector2Int(x, y));
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
    [Tooltip("Dibuja en el editor las celdas de pared que bloquean (rojo) y los colliders de objetos, ventanal y ascensor (cian).")]
    public bool mostrarColisiones = true;
    [Tooltip("Dibuja con Gizmos las celdas que el A* considera libres (verde) y las que no (rojo).")]
    public bool dibujarCuadricula;

    private void OnDrawGizmos()
    {
        // Marcas del mapa para el editor: inicio del jugador (verde), entrada del demoledor (rojo), poderes P y Q (azul y violeta).
        if (spawnJugador != null) { Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.9f); Gizmos.DrawWireSphere(spawnJugador.position, 0.4f); }
        if (spawnDemoledor != null) { Gizmos.color = new Color(0.95f, 0.25f, 0.2f, 0.9f); Gizmos.DrawWireSphere(spawnDemoledor.position, 0.4f); }
        if (entradaDemoledor != null) { Gizmos.color = new Color(0.95f, 0.5f, 0.2f, 0.9f); Gizmos.DrawWireSphere(entradaDemoledor.position, 0.3f); }
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.9f);
        if (puntosPoderP != null) foreach (Vector2 p in puntosPoderP) Gizmos.DrawWireCube(p, new Vector3(0.5f, 0.5f, 0f));
        Gizmos.color = new Color(0.75f, 0.4f, 0.95f, 0.9f);
        if (puntosPoderQ != null) foreach (Vector2 p in puntosPoderQ) Gizmos.DrawWireCube(p, new Vector3(0.5f, 0.5f, 0f));

        if (mostrarColisiones) DibujarColisiones();

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

    private void DibujarColisiones()
    {
        Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.9f);
        foreach (Tilemap tm in GetComponentsInChildren<Tilemap>())
        {
            if (!EsSolido(tm)) continue;
            BoundsInt b = tm.cellBounds;
            foreach (Vector3Int c in b.allPositionsWithin)
                if (tm.HasTile(c)) Gizmos.DrawWireCube(tm.GetCellCenterWorld(c), new Vector3(0.96f, 0.96f, 0f));
        }
        Gizmos.color = new Color(0.1f, 0.9f, 0.95f, 0.9f);
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            if (col == null || col.isTrigger || col is TilemapCollider2D || col is CompositeCollider2D) continue;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
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
