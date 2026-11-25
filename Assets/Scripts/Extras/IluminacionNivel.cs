using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Luces 2D de cada piso (URP): un tono propio por piso (la terraza con luz de tarde, informática más fría), luz de sol
/// que entra por las ventanas de arriba, lámparas que de vez en cuando parpadean, una alarma roja que gira cuando el
/// demoledor rompe el ventanal y el piso teñido de rojo en modo rabia (celeste si está congelado).
/// Corre en cada equipo por su cuenta (en multijugador las luces no viajan: se calculan con el estado del piso).
/// InstaladorExtras lo agrega a cada piso cenital.
/// </summary>
public class IluminacionNivel : MonoBehaviour
{
    private Light2D global;
    private Color colorBase = Color.white;
    private float intensidadBase = 1f;
    private readonly List<Light2D> lamparas = new List<Light2D>();
    private readonly List<float> fasesLampara = new List<float>();
    private readonly List<Light2D> soles = new List<Light2D>();
    private Light2D alarma, luzFuria, luzPropia;
    private PisoCenital piso;
    private bool ventanalAntes;

    /// <summary>Apagón del piso (0 = luz normal, 1 = a oscuras): lo maneja PeligrosPiso.</summary>
    public static float Apagon;

    /// <summary>Qué tan dañado está el edificio (0 a 1): las lámparas parpadean más. Lo maneja VidaEscenario.</summary>
    public static float Inestabilidad;

    /// <summary>El edificio pasó el 75 %: la alarma roja gira sin parar.</summary>
    public static bool AlarmaDerrumbe;
    private float finAlarma;

    private IEnumerator Start()
    {
        Apagon = 0f;
        Inestabilidad = 0f;
        AlarmaDerrumbe = false;
        yield return null;
        yield return null;
        piso = PisoCenital.Actual;
        if (piso == null || piso.Mapa == null) yield break;

        foreach (Light2D l in FindObjectsByType<Light2D>(FindObjectsInactive.Exclude))
            if (l.lightType == Light2D.LightType.Global) { global = l; break; }
        if (global == null) { enabled = false; yield break; }   // sin luz global los sprites no usan luces

        int numero = piso.Definicion != null ? piso.Definicion.numero : 0;
        switch (numero)
        {
            // La hora del día avanza al subir: mañana abajo, tarde en el medio y atardecer en la terraza.
            case 0: colorBase = new Color(0.96f, 0.98f, 1f); intensidadBase = 0.92f; break;      // mañana fresca
            case 1: colorBase = new Color(1f, 0.98f, 0.93f); intensidadBase = 0.92f; break;      // media mañana
            case 2: colorBase = new Color(0.92f, 0.95f, 1f); intensidadBase = 0.86f; break;      // mediodía (pantallas)
            case 3: colorBase = new Color(1f, 0.96f, 0.86f); intensidadBase = 0.9f; break;       // tarde temprano
            case 4: colorBase = new Color(1f, 0.92f, 0.78f); intensidadBase = 0.88f; break;      // tarde
            case 5: colorBase = new Color(1f, 0.85f, 0.66f); intensidadBase = 0.86f; break;      // tarde dorada
            case 6: colorBase = new Color(1f, 0.74f, 0.62f); intensidadBase = 0.84f; break;      // atardecer
            case 9: colorBase = new Color(0.72f, 0.78f, 1f); intensidadBase = 0.72f; break;      // supervivencia: de noche
            default: colorBase = new Color(0.98f, 0.97f, 0.95f); intensidadBase = 0.88f; break;
        }
        global.color = colorBase;
        global.intensity = intensidadBase;

        MapaCenital mapa = piso.Mapa;
        Rect lim = mapa.Limites;
        List<Vector2> celdas = mapa.CentrosTransitables;
        Random.State antes = Random.state;
        Random.InitState(4321 + numero * 31);

        // Sol por las ventanas: celdas libres junto al borde de arriba, separadas.
        List<Vector2> usadas = new List<Vector2>();
        foreach (Vector2 c in celdas)
        {
            if (c.y < lim.yMax - 2.2f) continue;
            bool lejos = true;
            foreach (Vector2 u in usadas) if (Vector2.Distance(u, c) < 4.5f) lejos = false;
            if (!lejos) continue;
            usadas.Add(c);
            Light2D l = NuevaLuz("Sol", c + new Vector2(0f, 0.6f), numero == 6 ? new Color(1f, 0.78f, 0.5f) : new Color(1f, 0.93f, 0.72f),
                numero == 2 ? 0.35f : 0.55f, 3.4f);
            soles.Add(l);
            if (soles.Count >= 7) break;
        }

        // Lámparas: algunas celdas al azar del interior (en informática, brillo azulado de pantallas).
        for (int i = 0; i < 4 && celdas.Count > 0; i++)
        {
            Vector2 c = celdas[Random.Range(0, celdas.Count)];
            Color col = numero == 2 ? new Color(0.55f, 0.8f, 1f) : new Color(1f, 0.95f, 0.85f);
            lamparas.Add(NuevaLuz("Lampara", c, col, 0.35f, 2.6f));
            fasesLampara.Add(Random.value * 10f);
        }
        Random.state = antes;

        // Alarma en el ventanal (apagada hasta que lo rompan).
        Vector2 pv = mapa.ventanal != null ? (Vector2)mapa.ventanal.transform.position : new Vector2(lim.center.x, lim.yMax - 1f);
        alarma = NuevaLuz("Alarma", pv, new Color(1f, 0.15f, 0.1f), 0f, 6f);
        // Luz que acompaña a tu personaje (suave siempre; fuerte y del color de tu carrera con la furia).
        luzPropia = NuevaLuz("LuzPropia", pv, new Color(1f, 0.96f, 0.88f), 0.18f, 2.2f);
        luzFuria = NuevaLuz("LuzFuria", pv, Color.white, 0f, 3.8f);
        ventanalAntes = mapa.ventanal != null && mapa.ventanal.Roto;
    }

    private Light2D NuevaLuz(string nombre, Vector2 pos, Color color, float intensidad, float radio)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(piso.Mapa.transform, true);
        g.transform.position = pos;
        Light2D l = g.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = color;
        l.intensity = intensidad;
        l.pointLightOuterRadius = radio;
        l.pointLightInnerRadius = radio * 0.15f;
        l.falloffIntensity = 0.6f;
        return l;
    }

    private void Update()
    {
        if (global == null || piso == null) return;

        // Estado de los demoledores (en el invitado llega por la red).
        bool rabia = false, furia = false, frio = false;
        if (DemoledorCenital.Todos.Count > 0 && !RedJuego.EsInvitadoEnPartida)
        {
            foreach (DemoledorCenital d in DemoledorCenital.Todos)
            {
                if (d == null || !d.isActiveAndEnabled || !d.EnPiso) continue;
                rabia |= d.EnRabia; furia |= d.EnFuria; frio |= d.Congelado;
            }
        }
        else { rabia = MultijugadorNivel.RabiaRed; furia = MultijugadorNivel.FuriaRed; frio = MultijugadorNivel.FrioRed; }
        if (piso.Rendido || piso.Terminado) rabia = furia = false;

        // Global: tono del piso → rojo pulsante en rabia, cálido en furia, frío si está congelado.
        Color objetivo = colorBase;
        float inten = intensidadBase;
        if (frio) objetivo = Color.Lerp(colorBase, new Color(0.7f, 0.85f, 1f), 0.6f);
        else if (rabia)
        {
            float p = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
            objetivo = Color.Lerp(colorBase, new Color(1f, 0.55f, 0.5f), 0.45f + 0.25f * p);
            inten = intensidadBase * (0.85f + 0.1f * p);
        }
        else if (furia) objetivo = Color.Lerp(colorBase, new Color(1f, 0.8f, 0.65f), 0.3f);
        inten *= Mathf.Lerp(1f, 0.1f, Apagon);
        float k = 1f - Mathf.Exp(-4f * Time.deltaTime);
        global.color = Color.Lerp(global.color, objetivo, k);
        global.intensity = Mathf.Lerp(global.intensity, inten, k);

        // Lámparas: parpadeo ocasional.
        for (int i = 0; i < lamparas.Count; i++)
        {
            Light2D l = lamparas[i];
            if (l == null) continue;
            float f = fasesLampara[i];
            bool parpadea = Mathf.Repeat(Time.time + f, 9f) < 0.5f || (rabia && Mathf.Repeat(Time.time * 2f + f, 3f) < 0.4f);
            l.intensity = (parpadea ? (Mathf.PerlinNoise(Time.time * 25f, f) > 0.5f ? 0.4f : 0.05f) : 0.35f) * (1f - Apagon);
            // Con el edificio dañado las lámparas tiemblan y se cortan.
            if (Inestabilidad > 0.3f && Mathf.PerlinNoise(Time.time * 9f, f + 3f) < (Inestabilidad - 0.3f) * 0.8f) l.intensity *= 0.15f;
        }

        // Sol: sube y baja muy despacio (nubes que pasan).
        for (int i = 0; i < soles.Count; i++)
            if (soles[i] != null) soles[i].intensity = (piso.Definicion != null && piso.Definicion.numero == 2 ? 0.35f : 0.55f) * (0.8f + 0.2f * Mathf.PerlinNoise(Time.time * 0.15f, i * 3.1f)) * (1f - 0.75f * Apagon);

        // Tu personaje: luz suave y, con la furia, un resplandor del color de tu carrera.
        Vector2 propia;
        if (CamaraPisoCenital.PosicionPropia(out propia))
        {
            if (luzPropia != null)
            {
                // A oscuras tu personaje alumbra como una linterna.
                luzPropia.transform.position = propia;
                luzPropia.intensity = 0.18f + 0.95f * Apagon;
                luzPropia.pointLightOuterRadius = 2.2f + 2.6f * Apagon;
            }
            if (luzFuria != null)
            {
                luzFuria.transform.position = propia;
                bool furiaPropia = IraEstudiante.ActivaLocal;
                luzFuria.color = IraEstudiante.ColorLocal;
                float objetivoF = furiaPropia ? 0.9f + 0.3f * Mathf.Sin(Time.time * 9f) : IraEstudiante.ListaLocal ? 0.25f + 0.2f * Mathf.Sin(Time.time * 6f) : 0f;
                luzFuria.intensity = Mathf.Lerp(luzFuria.intensity, objetivoF, 1f - Mathf.Exp(-8f * Time.deltaTime));
            }
        }

        // Alarma: gira unos segundos cuando rompen el ventanal y queda encendida en rabia.
        bool roto = piso.Mapa != null && piso.Mapa.ventanal != null && piso.Mapa.ventanal.Roto;
        if (roto && !ventanalAntes) finAlarma = Time.time + 5f;
        ventanalAntes = roto;
        if (alarma != null)
        {
            bool encendida = Time.time < finAlarma || rabia || AlarmaDerrumbe;
            float giro = Mathf.Abs(Mathf.Sin(Time.time * 6f));
            alarma.intensity = Mathf.Lerp(alarma.intensity, encendida ? 0.4f + 0.9f * giro : 0f, 1f - Mathf.Exp(-10f * Time.deltaTime));
            alarma.pointLightOuterRadius = 5f + 2f * giro;
        }
    }

    private void OnDestroy()
    {
        if (global != null) { global.color = colorBase; global.intensity = intensidadBase; }
    }
}
