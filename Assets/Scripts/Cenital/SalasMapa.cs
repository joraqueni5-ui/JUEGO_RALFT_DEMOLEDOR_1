using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Divide el mapa en salas: zonas de celdas transitables separadas por paredes y puertas (las puertas no son de ninguna
/// sala). Lo usan los demoledores para no ir dos a la misma sala y para recorrer el piso sala por sala.
/// </summary>
public static class SalasMapa
{
    private static MapaCenital mapaCache;
    private static readonly Dictionary<Vector2Int, int> salas = new Dictionary<Vector2Int, int>();
    private static readonly Dictionary<ObjetoReparable, int> salaObjeto = new Dictionary<ObjetoReparable, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        mapaCache = null;
        salas.Clear();
        salaObjeto.Clear();
    }

    /// <summary>Sala del punto (la de la celda transitable más cercana), o -1.</summary>
    public static int De(MapaCenital mapa, Vector2 punto)
    {
        if (mapa == null) return -1;
        Preparar(mapa);
        Vector2Int c = mapa.CeldaDe(punto);
        int s;
        if (salas.TryGetValue(c, out s)) return s;
        if (!mapa.CeldaTransitableCercana(punto, out c)) return -1;
        return salas.TryGetValue(c, out s) ? s : -1;
    }

    /// <summary>Sala de un objeto reparable (la de la celda libre más cercana a su centro).</summary>
    public static int DeObjeto(MapaCenital mapa, ObjetoReparable o)
    {
        if (mapa == null || o == null) return -1;
        Preparar(mapa);
        int s;
        if (salaObjeto.TryGetValue(o, out s)) return s;
        s = -1;
        float mejor = float.MaxValue;
        foreach (Vector2 c in mapa.CentrosTransitables)
        {
            float d = Vector2.Distance(c, o.PuntoMasCercano(c));
            if (d > 1.2f || d >= mejor) continue;
            int sc;
            if (!salas.TryGetValue(mapa.CeldaDe(c), out sc)) continue;
            mejor = d;
            s = sc;
        }
        salaObjeto[o] = s;
        return s;
    }

    private static void Preparar(MapaCenital mapa)
    {
        if (mapa == mapaCache && salas.Count > 0) return;
        mapaCache = mapa;
        salas.Clear();
        salaObjeto.Clear();
        HashSet<Vector2Int> libres = new HashSet<Vector2Int>();
        foreach (Vector2 c in mapa.CentrosTransitables)
        {
            Vector2Int k = mapa.CeldaDe(c);
            if (!mapa.EsPuerta(k)) libres.Add(k);
        }
        int id = 0;
        Queue<Vector2Int> cola = new Queue<Vector2Int>();
        foreach (Vector2Int inicio in libres)
        {
            if (salas.ContainsKey(inicio)) continue;
            salas[inicio] = id;
            cola.Enqueue(inicio);
            while (cola.Count > 0)
            {
                Vector2Int c = cola.Dequeue();
                foreach (Vector2Int d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    Vector2Int n = c + d;
                    if (!libres.Contains(n) || salas.ContainsKey(n)) continue;
                    salas[n] = id;
                    cola.Enqueue(n);
                }
            }
            id++;
        }
    }
}
