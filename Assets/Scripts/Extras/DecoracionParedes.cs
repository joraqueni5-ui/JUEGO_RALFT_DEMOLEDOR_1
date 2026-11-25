using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Paredes con vida: cuadros, pizarras, letreros de salida, extintores, relojes, tableros de anuncios y ventanas
/// colgados a lo largo de las paredes, según el piso (las aulas con pizarras, la terraza con ventanas...).
/// El reparto es siempre el mismo en cada piso y queda dentro del mapa (en multijugador cada equipo lo arma igual).
/// InstaladorExtras lo agrega a cada piso cenital.
/// </summary>
public class DecoracionParedes : MonoBehaviour
{
    [Tooltip("Baldosas mínimas entre dos cosas colgadas en la pared.")]
    public float separacion = 3.5f;
    public int maximo = 22;

    private static string[] DelPiso(int numero)
    {
        switch (numero)
        {
            case 0: return new[] { "letrero_salida", "tablero", "cuadro", "reloj", "extintor" };
            case 1: return new[] { "extintor", "cuadro", "tablero", "reloj", "letrero_salida" };
            case 2: return new[] { "reloj", "tablero", "extintor", "letrero_salida", "cuadro" };
            case 6: return new[] { "ventana", "ventana", "cuadro", "reloj", "extintor" };
            default: return new[] { "pizarra", "pizarra", "reloj", "extintor", "tablero", "letrero_salida" };
        }
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return null;
        PisoCenital piso = PisoCenital.Actual;
        if (piso == null || piso.Mapa == null) yield break;
        MapaCenital mapa = piso.Mapa;
        int numero = piso.Definicion != null ? piso.Definicion.numero : 0;
        string[] lista = DelPiso(numero);

        Random.State antes = Random.state;
        Random.InitState(777 + numero * 13);
        GameObject raiz = new GameObject("DecoracionParedes");
        raiz.transform.SetParent(mapa.transform, false);

        // Celdas libres con pared justo arriba (la pared del fondo de cada sala).
        List<Vector2> candidatas = new List<Vector2>();
        foreach (Vector2 c in mapa.CentrosTransitables)
            if (EsPared(c + Vector2.up)) candidatas.Add(c);
        // Orden al azar (siempre el mismo para el piso).
        for (int i = candidatas.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); Vector2 t = candidatas[i]; candidatas[i] = candidatas[j]; candidatas[j] = t; }

        List<Vector2> puestas = new List<Vector2>();
        foreach (Vector2 c in candidatas)
        {
            if (puestas.Count >= maximo) break;
            bool lejos = true;
            foreach (Vector2 p in puestas) if (Vector2.Distance(p, c) < separacion) lejos = false;
            if (!lejos) continue;
            if (mapa.spawnJugador != null && Vector2.Distance(c, mapa.spawnJugador.position) < 1.5f) continue;
            string nombre = lista[Random.Range(0, lista.Length)];
            Sprite s = ArteExtra.Uno("Pared/" + nombre);
            if (s == null) continue;
            puestas.Add(c);
            SpriteRenderer sr = Provisional.Crear(raiz.transform, nombre, s, Color.white, -140);
            // Apoyada en la cara de la pared que da a la sala (el sprite tiene el pivote abajo).
            sr.transform.position = new Vector3(c.x + Random.Range(-0.15f, 0.15f), c.y + 0.32f, 0f);
            float e = nombre == "pizarra" || nombre == "ventana" ? 0.95f : 0.8f;
            sr.transform.localScale = new Vector3(e, e, 1f);
        }
        DetallesDelSuelo(mapa, numero, raiz.transform);
        Random.state = antes;
    }

    /// <summary>
    /// Alfombras en algunas salas, el número del piso pintado frente al ascensor y franjas de peligro amarillas y negras
    /// bajo el ventanal por donde entra el demoledor.
    /// </summary>
    private static void DetallesDelSuelo(MapaCenital mapa, int numero, Transform raiz)
    {
        HashSet<Vector2Int> libres = new HashSet<Vector2Int>();
        foreach (Vector2 c in mapa.CentrosTransitables) libres.Add(new Vector2Int(Mathf.FloorToInt(c.x), Mathf.FloorToInt(c.y)));

        // Alfombras: zonas libres de 3 x 2 baldosas.
        Sprite[] alfombras = { ArteExtra.Uno("Mejoras/alfombra_roja"), ArteExtra.Uno("Mejoras/alfombra_azul") };
        List<Vector2> celdas = new List<Vector2>(mapa.CentrosTransitables);
        int puestas = 0;
        List<Vector2> usadas = new List<Vector2>();
        for (int intento = 0; intento < 60 && puestas < 3 && celdas.Count > 0 && numero != 6; intento++)
        {
            Vector2 c = celdas[Random.Range(0, celdas.Count)];
            Vector2Int b = new Vector2Int(Mathf.FloorToInt(c.x), Mathf.FloorToInt(c.y));
            bool cabe = true;
            for (int dx = -1; dx <= 1 && cabe; dx++)
                for (int dy = 0; dy <= 1 && cabe; dy++)
                    if (!libres.Contains(b + new Vector2Int(dx, dy))) cabe = false;
            if (!cabe) continue;
            bool lejos = true;
            foreach (Vector2 u in usadas) if (Vector2.Distance(u, c) < 6f) lejos = false;
            if (!lejos || (mapa.spawnJugador != null && Vector2.Distance(c, mapa.spawnJugador.position) < 2f)) continue;
            Sprite s = alfombras[puestas % 2];
            if (s == null) break;
            usadas.Add(c);
            puestas++;
            SpriteRenderer sr = Provisional.Crear(raiz, "Alfombra", s, Color.white, -189);
            sr.transform.position = new Vector3(c.x, c.y + 0.5f, 0f);
            float e = 3f / Mathf.Max(0.01f, s.bounds.size.x);
            sr.transform.localScale = new Vector3(e, e, 1f);
        }

        // Número del piso frente al ascensor.
        if (mapa.ascensor != null)
        {
            TextMesh tm = Provisional.CrearTexto(raiz, "NumeroPiso", numero == 0 ? "PB" : "P" + numero, 0.9f,
                new Color(1f, 1f, 1f, 0.28f), -186);
            tm.transform.position = (Vector2)mapa.ascensor.transform.position + new Vector2(0f, -1.3f);
        }

        // Franjas de peligro bajo el ventanal.
        Sprite franjas = ArteExtra.Uno("Mejoras/franjas_peligro");
        if (franjas != null && mapa.ventanal != null)
        {
            SpriteRenderer sr = Provisional.Crear(raiz, "FranjasPeligro", franjas, Color.white, -187);
            sr.transform.position = (Vector2)mapa.ventanal.transform.position + new Vector2(0f, -0.9f);
            float e = 3f / Mathf.Max(0.01f, franjas.bounds.size.x);
            sr.transform.localScale = new Vector3(e, e, 1f);
        }
    }

    /// <summary>¿En ese punto hay pared (el collider del mapa) y no un objeto reparable?</summary>
    private static bool EsPared(Vector2 p)
    {
        foreach (Collider2D c in Physics2D.OverlapPointAll(p))
        {
            if (c == null || c.isTrigger) continue;
            if (c.GetComponentInParent<ObjetoReparable>() != null || c.GetComponentInParent<JugadorCenital>() != null ||
                c.GetComponentInParent<DemoledorCenital>() != null || c.GetComponentInParent<EstudianteNPC>() != null ||
                c.GetComponentInParent<AscensorCenital>() != null || c.GetComponentInParent<VentanalCenital>() != null) continue;
            if (c is CompositeCollider2D || c.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null) return true;
        }
        return false;
    }
}
