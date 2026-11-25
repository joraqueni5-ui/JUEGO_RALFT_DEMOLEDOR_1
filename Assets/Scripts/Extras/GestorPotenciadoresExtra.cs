using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reparte los potenciadores nuevos por el piso cenital, además de los poderes propios de cada piso (DefinicionPiso.poderes):
///   - ZAPATILLAS TURBO: corres un 60 % más rápido (con estela).
///   - MARTILLO DORADO: martillas el doble de rápido y llegas más lejos (también contra los mini villanos).
///   - RELOJ DE HIELO: congela a los mini villanos y frena un rato al demoledor.
///   - CASCO DE OBRA: aguanta el próximo golpe sin perder vida.
/// Elige con sinergia según lo que pasa: con muchos mini villanos sale más el reloj; con una vida, el casco; con mucho
/// destrozo, el martillo; con el jugador lejos de lo roto, las zapatillas. InstaladorExtras lo agrega solo a cada piso.
/// </summary>
public class GestorPotenciadoresExtra : MonoBehaviour
{
    [Tooltip("Segundos hasta el primer potenciador extra.")]
    public float esperaInicial = 8f;
    [Tooltip("Segundos entre potenciadores (mínimo y máximo).")]
    public Vector2 intervalo = new Vector2(15f, 22f);
    [Tooltip("Segundos que espera en el suelo antes de desaparecer.")]
    public float vidaEnSuelo = 10f;

    private static GestorPotenciadoresExtra instancia;
    private static PoderPiso turbo, martillo, reloj, casco;

    private PisoCenital piso;
    private JugadorCenital jugador;
    private DemoledorCenital demoledor;
    private GestorJuego gestor;
    private float proximo = -1f;

    /// <summary>Los cuatro potenciadores (se crean una vez, con su sprite de Resources/Extras/Poderes).</summary>
    public static PoderPiso[] Todos
    {
        get
        {
            if (turbo == null)
            {
                turbo = Nuevo("Zapatillas turbo", TipoPoder.Velocidad, "TURBO", 7f, "¡ZAPATILLAS TURBO!", "¡A TODA VELOCIDAD!", ">>",
                    new Color(0.3f, 0.8f, 1f), new Color(0.45f, 0.85f, 1f), "Poderes/poder_turbo");
                martillo = Nuevo("Martillo dorado", TipoPoder.ReparacionRapida, "MARTILLO DORADO", 8f, "¡MARTILLO DORADO!", "¡REPARAS AL DOBLE!", "M",
                    new Color(1f, 0.78f, 0.2f), new Color(1f, 0.72f, 0.2f), "Poderes/poder_martillo");
                reloj = Nuevo("Reloj de hielo", TipoPoder.CongelarEnemigos, "CONGELADOS", 6f, "¡RELOJ DE HIELO!", "¡TODOS CONGELADOS!", "*",
                    new Color(0.6f, 0.9f, 1f), new Color(0.6f, 0.9f, 1f), "Poderes/poder_reloj");
                casco = Nuevo("Casco de obra", TipoPoder.Casco, "CASCO", 0f, "¡CASCO DE OBRA!", "¡AGUANTAS UN GOLPE!", "C",
                    new Color(1f, 0.82f, 0.1f), new Color(1f, 0.85f, 0.25f), "Poderes/poder_casco");
            }
            return new[] { turbo, martillo, reloj, casco };
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstatico()
    {
        instancia = null;
        turbo = martillo = reloj = casco = null;
    }

    private static PoderPiso Nuevo(string nombre, TipoPoder tipo, string hud, float duracion, string aparece, string recogido,
                                   string simbolo, Color color, Color aro, string sprite)
    {
        return new PoderPiso
        {
            nombre = nombre,
            tipo = tipo,
            etiquetaHud = hud,
            duracion = duracion,
            valor = 1,
            peso = 1f,
            origen = OrigenPoder.PuntoFijo,
            mensajeAparece = aparece,
            mensajeRecogido = recogido,
            simbolo = simbolo,
            color = color,
            colorAro = aro,
            sprite = ArteExtra.Uno(sprite)
        };
    }

    private static bool EsExtra(PoderPiso p)
    {
        return p != null && (p == turbo || p == martillo || p == reloj || p == casco);
    }

    /// <summary>Deja un potenciador elegido con sinergia en 'posicion' (lo usan los mini villanos al caer).</summary>
    public static void SoltarEn(Vector2 posicion)
    {
        if (instancia == null) return;
        instancia.Aparecer(posicion, true);
    }

    // ------------------------------------------------------------ ciclo
    private void Awake() { instancia = this; }

    private void OnDestroy() { if (instancia == this) instancia = null; }

    private void Start()
    {
        piso = GetComponent<PisoCenital>();
        if (piso == null) piso = PisoCenital.Actual;
        jugador = FindAnyObjectByType<JugadorCenital>();
        demoledor = FindAnyObjectByType<DemoledorCenital>();
        gestor = FindAnyObjectByType<GestorJuego>();
    }

    private void Update()
    {
        // Con dos jugadores, cada potenciador se piensa para uno de ellos al azar.
        if (JugadorCenital.Todos.Count > 1) jugador = JugadorCenital.Todos[Random.Range(0, JugadorCenital.Todos.Count)];
        if (JugadorCenital.Todos.Count > 1) demoledor = DemoledorCenital.MasCercano(jugador != null ? jugador.Centro : Vector2.zero);
        if (piso == null || piso.Mapa == null || jugador == null) return;
        if (piso.Rendido || piso.Terminado || (gestor != null && (gestor.PartidaTerminada || gestor.EstaEnPausa))) return;
        if (demoledor != null && !demoledor.EnPiso) { proximo = -1f; return; }
        if (proximo < 0f) { proximo = Time.time + esperaInicial; return; }
        if (Time.time < proximo) return;

        bool debil = piso.Energia < 0.3f;
        float espera = Random.Range(intervalo.x, Mathf.Max(intervalo.x, intervalo.y)) * Personajes.FactorIntervaloPotenciadores;
        if (debil) espera *= 0.5f;   // con el demoledor débil (y sus oleadas) aparecen más potenciadores
        proximo = Time.time + espera;
        int enSuelo = 0;
        foreach (PoderRecogible p in PoderRecogible.Activos) if (p != null && EsExtra(p.datos)) enSuelo++;
        if (enSuelo >= (debil ? 2 : 1)) return;

        Vector2 donde;
        if (BuscarCelda(out donde)) Aparecer(donde, false);
    }

    private void Aparecer(Vector2 donde, bool soltado)
    {
        PoderPiso datos = Elegir();
        if (datos == null) return;
        PoderRecogible.Crear(datos, donde, vidaEnSuelo, 2.5f, 0.6f);
#if UNITY_EDITOR
        Debug.Log("[Extras] Potenciador: " + datos.nombre + " en " + donde);
#endif
        TextoFlotante.Mostrar(donde + Vector2.up * 1.3f, soltado ? "¡SOLTÓ ALGO!" : datos.mensajeAparece, datos.colorAro);
    }

    /// <summary>Sorteo con pesos que dependen de la partida (sinergia).</summary>
    private PoderPiso Elegir()
    {
        PoderPiso[] todos = Todos;
        float[] pesos = new float[todos.Length];

        int minis = MiniVillano.Activos.Count;
        float destrozo = piso.DestrozoSeccionActual;
        bool unaVida = gestor != null && gestor.Vidas <= 1;

        pesos[0] = 1f + (jugador != null && LejosDeLoRoto() ? 1.5f : 0f);                       // turbo
        pesos[1] = 1f + destrozo * 4f;                                                              // martillo
        pesos[2] = 0.4f + minis * 1.2f + (demoledor != null && demoledor.EnFuria ? 1f : 0f);      // reloj
        pesos[3] = jugador != null && jugador.TieneCasco ? 0f : (unaVida ? 3f : 0.8f);              // casco

        float total = 0f;
        foreach (float p in pesos) total += p;
        float azar = Random.value * total;
        for (int i = 0; i < todos.Length; i++)
        {
            azar -= pesos[i];
            if (azar <= 0f) return todos[i];
        }
        return todos[0];
    }

    private bool LejosDeLoRoto()
    {
        float mejor = float.MaxValue;
        foreach (ObjetoReparable o in piso.Mapa.objetos)
            if (o != null && o.Estado != EstadoObjeto.Intacto) mejor = Mathf.Min(mejor, Vector2.Distance(o.Centro, jugador.Centro));
        return mejor < float.MaxValue && mejor > 6f;
    }

    /// <summary>Celda libre a 3 a 8 baldosas del jugador, lejos del demoledor y de otros poderes.</summary>
    private bool BuscarCelda(out Vector2 celda)
    {
        celda = Vector2.zero;
        List<Vector2> candidatas = new List<Vector2>();
        foreach (Vector2 c in piso.Mapa.CentrosTransitables)
        {
            float d = Vector2.Distance(c, jugador.Centro);
            if (d < 2.5f || d > 14f) continue;
            if (demoledor != null && Vector2.Distance(c, demoledor.transform.position) < 2.5f) continue;
            bool ocupada = false;
            foreach (Vector2 q in PoderRecogible.Posiciones()) if (Vector2.Distance(q, c) < 1.2f) ocupada = true;
            if (!ocupada) candidatas.Add(c);
        }
        if (candidatas.Count == 0) return false;
        celda = candidatas[Random.Range(0, candidatas.Count)];
        return true;
    }
}
