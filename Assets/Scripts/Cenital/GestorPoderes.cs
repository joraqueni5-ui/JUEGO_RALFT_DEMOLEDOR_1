using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hace aparecer los poderes del piso (DefinicionPiso.poderes) cada cierto tiempo, con un máximo de
/// poderes en el suelo a la vez. PisoCenital lo agrega solo cuando la definición tiene poderes.
/// Los valores (intervalo, máximo, duración en el suelo) salen de la DefinicionPiso.
/// </summary>
public class GestorPoderes : MonoBehaviour
{
    private PisoCenital piso;
    private DefinicionPiso def;
    private JugadorCenital jugador;
    private GestorJuego gestor;
    private float proximo;

    private void Start()
    {
        piso = GetComponent<PisoCenital>();
        def = piso != null ? piso.Definicion : null;
        jugador = FindAnyObjectByType<JugadorCenital>();
        gestor = FindAnyObjectByType<GestorJuego>();
        Reiniciar();
    }

    /// <summary>Vuelve a esperar el primer intervalo (al reiniciar el piso).</summary>
    public void Reiniciar()
    {
        proximo = Time.time + SiguienteIntervalo();
    }

    private float SiguienteIntervalo()
    {
        if (def == null) return 8f;
        // Ritmo impredecible: además de la variación del piso, a veces sale mucho antes o bastante después.
        float v = Mathf.Max(def.variacionIntervaloPoderes, 0.45f);
        float t = def.intervaloPoderes * Random.Range(1f - v, 1f + v) * Personajes.FactorIntervaloPotenciadores;
        // Con el demoledor débil (menos de 30 % de energía) aparecen más seguido.
        if (piso != null && piso.Energia < 0.3f) t *= 0.6f;
        return Mathf.Max(1f, t);
    }

    private void Update()
    {
        if (JugadorCenital.Todos.Count > 1) jugador = JugadorCenital.Todos[Random.Range(0, JugadorCenital.Todos.Count)];
        if (def == null || piso == null || piso.Mapa == null || def.poderes == null || def.poderes.Length == 0) return;
        if (piso.Terminado || piso.Rendido || (gestor != null && (gestor.PartidaTerminada || gestor.EstaEnPausa))) return;
        if (Time.time < proximo) return;

        proximo = Time.time + SiguienteIntervalo();
        if (PoderRecogible.Cantidad >= def.maxPoderesEnPiso) return;
        Aparecer();
    }

    private void Aparecer()
    {
        // Poderes que se pueden crear ahora (no hay otro igual en el suelo y se puede ubicar).
        List<PoderPiso> candidatos = new List<PoderPiso>();
        List<Vector2> posiciones = new List<Vector2>();
        float total = 0f;
        foreach (PoderPiso p in def.poderes)
        {
            if (p == null || p.peso <= 0f || PoderRecogible.Hay(p)) continue;
            Vector2 pos;
            if (!BuscarPosicion(p, out pos)) continue;
            candidatos.Add(p);
            posiciones.Add(pos);
            total += p.peso;
        }
        if (candidatos.Count == 0) return;

        float azar = Random.value * total;
        int elegido = candidatos.Count - 1;
        for (int i = 0; i < candidatos.Count; i++)
        {
            azar -= candidatos[i].peso;
            if (azar <= 0f) { elegido = i; break; }
        }

        PoderPiso datos = candidatos[elegido];
        Vector2 donde = posiciones[elegido];
        PoderRecogible.Crear(datos, donde, def.duracionPoderEnSuelo, def.parpadeoFinalPoder, def.radioRecogidaPoder);
        TextoFlotante.Mostrar(donde + Vector2.up * 1.3f, datos.mensajeAparece, datos.colorAro);
    }

    /// <summary>Celda libre donde debe aparecer el poder, o false si ahora no hay dónde ponerlo.</summary>
    private bool BuscarPosicion(PoderPiso p, out Vector2 posicion)
    {
        posicion = Vector2.zero;
        MapaCenital mapa = piso.Mapa;
        Vector2 deseada;

        // La mayoría de las veces aparece en un lugar al azar del piso (no siempre en el mismo punto).
        if (Random.value < 0.65f && PosicionAlAzar(mapa, out posicion)) return true;

        switch (p.origen)
        {
            case OrigenPoder.JuntoAlChef:
                if (EstudianteNPC.Chef == null) return false;
                deseada = (Vector2)EstudianteNPC.Chef.transform.position + p.desplazamiento;
                break;
            case OrigenPoder.FrenteAlObjeto:
                ObjetoReparable objetivo = null;
                foreach (ObjetoReparable o in mapa.objetos)
                {
                    if (o == null) continue;
                    bool coincide;
                    if (!string.IsNullOrEmpty(p.objetoReferencia))
                    {
                        ObjetoCenital ficha = def.ObjetoCenitalDe(o.indice);
                        coincide = ficha != null && ficha.nombre == p.objetoReferencia;
                    }
                    else coincide = o.indice == p.indiceObjeto;
                    if (coincide) { objetivo = o; break; }
                }
                if (objetivo == null) return false;
                deseada = objetivo.Centro + p.desplazamiento;
                break;
            case OrigenPoder.PuntoP:
            case OrigenPoder.PuntoQ:
                Vector2[] puntos = p.origen == OrigenPoder.PuntoP ? mapa.puntosPoderP : mapa.puntosPoderQ;
                if (puntos == null || puntos.Length == 0) return false;
                deseada = puntos[Random.Range(0, puntos.Length)] + p.desplazamiento;
                break;
            default:
                deseada = p.desplazamiento;
                break;
        }

        // La celda libre más cercana a la posición deseada que no esté sobre el jugador ni sobre otro poder.
        float mejor = float.MaxValue;
        bool hallada = false;
        foreach (Vector2 celda in mapa.CentrosTransitables)
        {
            if (jugador != null && Vector2.Distance(celda, jugador.Centro) < 1f) continue;
            if (OcupadaPorPoder(celda)) continue;
            float d = (celda - deseada).sqrMagnitude;
            if (d < mejor) { mejor = d; posicion = celda; hallada = true; }
        }
        return hallada;
    }

    /// <summary>Celda libre al azar, a más de 2,5 baldosas del jugador y lejos del demoledor y de otros poderes.</summary>
    private bool PosicionAlAzar(MapaCenital mapa, out Vector2 posicion)
    {
        posicion = Vector2.zero;
        List<Vector2> celdas = mapa.CentrosTransitables;
        if (celdas == null || celdas.Count == 0) return false;
        DemoledorCenital dem = DemoledorCenital.MasCercano(jugador != null ? jugador.Centro : Vector2.zero);
        for (int intento = 0; intento < 25; intento++)
        {
            Vector2 c = celdas[Random.Range(0, celdas.Count)];
            if (jugador != null && Vector2.Distance(c, jugador.Centro) < 2.5f) continue;
            if (dem != null && Vector2.Distance(c, dem.transform.position) < 2f) continue;
            if (OcupadaPorPoder(c)) continue;
            posicion = c;
            return true;
        }
        return false;
    }

    private static bool OcupadaPorPoder(Vector2 celda)
    {
        foreach (Vector2 q in PoderRecogible.Posiciones()) if (Vector2.Distance(q, celda) < 1f) return true;
        return false;
    }
}
