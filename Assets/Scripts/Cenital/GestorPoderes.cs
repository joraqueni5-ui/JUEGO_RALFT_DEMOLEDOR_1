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
        jugador = FindFirstObjectByType<JugadorCenital>();
        gestor = FindFirstObjectByType<GestorJuego>();
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
        float v = def.variacionIntervaloPoderes;
        return Mathf.Max(1f, def.intervaloPoderes * Random.Range(1f - v, 1f + v));
    }

    private void Update()
    {
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

    private static bool OcupadaPorPoder(Vector2 celda)
    {
        foreach (Vector2 q in PoderRecogible.Posiciones()) if (Vector2.Distance(q, celda) < 1f) return true;
        return false;
    }
}
