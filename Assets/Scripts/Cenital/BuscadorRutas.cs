using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Búsqueda de caminos A* sobre la cuadrícula del mapa (8 direcciones, sin cortar esquinas).
/// Sin paquetes externos. Devuelve puntos del mundo (centros de celda) ya suavizados.
/// </summary>
public static class BuscadorRutas
{
    private class Nodo
    {
        public Vector2Int celda;
        public float g, f;
        public Nodo padre;
    }

    private static readonly Vector2Int[] Vecinos =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
    };

    private static float Octil(Vector2Int a, Vector2Int b)
    {
        float dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        return (dx + dy) + (1.41421356f - 2f) * Mathf.Min(dx, dy);
    }

    /// <summary>Camino de 'desde' a 'hasta', o null si no hay.</summary>
    public static List<Vector2> Buscar(MapaCenital mapa, Vector2 desde, Vector2 hasta)
    {
        Vector2Int inicio, meta;
        if (!mapa.CeldaTransitableCercana(desde, out inicio)) return null;
        if (!mapa.CeldaTransitableCercana(hasta, out meta)) return null;

        List<Nodo> abiertos = new List<Nodo>();
        Dictionary<Vector2Int, Nodo> vistos = new Dictionary<Vector2Int, Nodo>();
        HashSet<Vector2Int> cerrados = new HashSet<Vector2Int>();

        Nodo n0 = new Nodo { celda = inicio, g = 0f, f = Octil(inicio, meta) };
        abiertos.Add(n0);
        vistos[inicio] = n0;

        Nodo final = null;
        while (abiertos.Count > 0)
        {
            int mejor = 0;
            for (int i = 1; i < abiertos.Count; i++) if (abiertos[i].f < abiertos[mejor].f) mejor = i;
            Nodo actual = abiertos[mejor];
            abiertos.RemoveAt(mejor);

            if (actual.celda == meta) { final = actual; break; }
            cerrados.Add(actual.celda);

            for (int k = 0; k < Vecinos.Length; k++)
            {
                Vector2Int d = Vecinos[k];
                Vector2Int vecina = actual.celda + d;
                if (cerrados.Contains(vecina) || !mapa.EsTransitable(vecina)) continue;

                bool diagonal = d.x != 0 && d.y != 0;
                if (diagonal && (!mapa.EsTransitable(actual.celda + new Vector2Int(d.x, 0)) ||
                                 !mapa.EsTransitable(actual.celda + new Vector2Int(0, d.y)))) continue;

                float g = actual.g + (diagonal ? 1.41421356f : 1f);
                Nodo existente;
                if (vistos.TryGetValue(vecina, out existente))
                {
                    if (g >= existente.g) continue;
                    existente.g = g;
                    existente.f = g + Octil(vecina, meta);
                    existente.padre = actual;
                }
                else
                {
                    Nodo nuevo = new Nodo { celda = vecina, g = g, f = g + Octil(vecina, meta), padre = actual };
                    vistos[vecina] = nuevo;
                    abiertos.Add(nuevo);
                }
            }
        }
        if (final == null) return null;

        List<Vector2> puntos = new List<Vector2>();
        for (Nodo n = final; n != null; n = n.padre) puntos.Add(mapa.CentroDe(n.celda));
        puntos.Reverse();
        return Suavizar(mapa, puntos);
    }

    /// <summary>Quita puntos intermedios cuando hay línea recta libre.</summary>
    private static List<Vector2> Suavizar(MapaCenital mapa, List<Vector2> ruta)
    {
        if (ruta.Count <= 2) return ruta;
        List<Vector2> res = new List<Vector2> { ruta[0] };
        int ancla = 0;
        while (ancla < ruta.Count - 1)
        {
            int lejos = ancla + 1;
            for (int i = ruta.Count - 1; i > ancla + 1; i--)
            {
                if (mapa.LineaLibre(ruta[ancla], ruta[i], 0.3f)) { lejos = i; break; }
            }
            res.Add(ruta[lejos]);
            ancla = lejos;
        }
        return res;
    }
}
