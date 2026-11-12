using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// El demoledor visto desde arriba. Entra rompiendo el ventanal y recorre el piso eligiendo
/// objetos para destruir, rodeando obstáculos con A* sobre la cuadrícula del mapa.
///
/// Ataque 1, puñetazo: junto a un objeto se prepara (0.5 s, con un círculo rojo visible) y lo
/// daña un estado; si el jugador está en el radio del golpe pierde una vida.
/// Ataque 2, lanzamiento: se detiene, se prepara y lanza escombros en arco; una sombra marca el
/// punto de caída. Al caer daña los objetos del radio y quita una vida si el jugador está ahí.
///
/// Cuándo usa cada uno y con qué frecuencia sale de la DefinicionPiso. Choca con el jugador
/// (no se atraviesan).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class DemoledorCenital : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Baldosas por segundo (se multiplica por el ritmo del piso).")]
    public float velocidad = 2.0f;
    [Tooltip("Radio del collider (baldosas). Debe ser menor que media baldosa (0.5) para pasar por puertas y esquinas.")]
    [Range(0.1f, 0.45f)] public float radioCuerpo = 0.35f;
    [Tooltip("Distancia del cuerpo al borde de un objeto para poder golpearlo.")]
    public float alcancePunetazo = 0.95f;

    [Header("Pausas")]
    public float esperaInicial = 1.2f;
    public float pausaTrasGolpe = 0.5f;
    public float pausaTrasLanzamiento = 0.7f;

    [Header("Diagnóstico (Gizmos y registro)")]
    [Tooltip("Dibuja la ruta A*, el destino y el objetivo en la vista Scene (y en Game con Gizmos activado).")]
    public bool dibujarDiagnostico = true;
    [Tooltip("Escribe en la consola cuándo se atasca, con qué choca y qué hace para salir.")]
    public bool registrarAtascos = true;

    [Header("Dibujo")]
    public float alturaDibujo = 2f;
    public float cuadrosCaminar = 8f;
    public float cuadrosPorSegundoGolpe = 14f;
    public AnimacionesCenital animaciones = new AnimacionesCenital();

    private enum Estado { Esperando, Entrando, Decidiendo, Caminando, PrepPunetazo, PrepLanzamiento, Recuperando, Detenido }

    private Rigidbody2D cuerpo;
    private Collider2D colision;
    private SpriteRenderer dibujo;
    private SpriteRenderer telegrafo;
    private AnimadorSprites animador;
    private PisoCenital piso;
    private DefinicionPiso def;
    private JugadorCenital jugador;

    private Estado estado = Estado.Detenido;
    private float finEstado;
    private Vector2 velDeseada;
    private Vector2 direccion = Vector2.right;
    private bool mirandoIzquierda;
    private List<Vector2> ruta;
    private int indiceRuta;
    private ObjetoReparable objetivo, ultimoObjetivo;
    private float ultimoLanzamiento;
    private float proximoChequeo;
    private Vector2 posChequeo;
    private int atascos;
    private bool golpeando;
    private float finGolpe;

    /// <summary>Centro del cuerpo. El collider está centrado en el transform, igual que los puntos del A*.</summary>
    private Vector2 Centro { get { return transform.position; } }

    private Vector2 destinoAproximacion;
    private int recalculos;
    private float proximoRecalculo;

    // Parámetros con valores de la definición (o por defecto).
    private float Ritmo { get { return piso != null ? piso.RitmoDemoledor : 1f; } }
    private float PrepPunetazo_ { get { return def != null ? def.preparacionPunetazo : 0.5f; } }
    private float RadioPunetazo { get { return def != null ? def.radioPunetazo : 1f; } }
    private float PrepLanzamiento_ { get { return def != null ? def.preparacionLanzamiento : 0.8f; } }
    private float Vuelo { get { return def != null ? def.vueloLanzamiento : 0.9f; } }
    private float RadioImpacto { get { return def != null ? def.radioImpacto : 1f; } }
    private float Intervalo { get { return def != null ? def.intervaloLanzamiento : 8f; } }
    private float DistLejos { get { return def != null ? def.distanciaJugadorLejos : 7f; } }
    private float Proporcion { get { return def != null ? def.proporcionLanzamiento : 0.3f; } }
    private float ProbAlJugador { get { return def != null ? def.lanzarAlJugador : 0.5f; } }

    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        cuerpo.gravityScale = 0f;
        cuerpo.freezeRotation = true;
        cuerpo.mass = 100f;
        cuerpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;
        colision = GetComponent<Collider2D>();

        // Cuerpo centrado y más chico que una baldosa: el A* mueve este mismo punto por el centro de las
        // celdas, y así el collider no roza paredes ni objetos de las celdas vecinas.
        CircleCollider2D circulo = colision as CircleCollider2D;
        if (circulo != null)
        {
            circulo.offset = Vector2.zero;
            circulo.radius = radioCuerpo;
        }

        dibujo = GetComponentInChildren<SpriteRenderer>();
        if (dibujo == null) dibujo = Provisional.Crear(transform, "Dibujo", null, Color.white, 0);
        animador = new AnimadorSprites(dibujo);

        // Círculo rojo en el suelo que muestra el radio del puñetazo mientras se prepara.
        telegrafo = Provisional.Crear(transform, "TelegrafoPunetazo", Provisional.Circulo, new Color(1f, 0.15f, 0.1f, 0.4f), -120);
        telegrafo.enabled = false;
    }

    private void Start()
    {
        piso = PisoCenital.Actual;
        def = piso != null ? piso.Definicion : null;
        jugador = FindFirstObjectByType<JugadorCenital>();
        if (def != null) animaciones = Combinar(animaciones, def.demoledor);
        animaciones.QuitarInvalidos();
        Reiniciar();
    }

    private static Sprite[] Si(Sprite[] nuevo, Sprite[] actual)
    {
        return nuevo != null && nuevo.Length > 0 ? nuevo : actual;
    }

    private static AnimacionesCenital Combinar(AnimacionesCenital a, AnimacionesCenital d)
    {
        if (d == null) return a;
        return new AnimacionesCenital
        {
            caminarAbajo = Si(d.caminarAbajo, a.caminarAbajo),
            caminarArriba = Si(d.caminarArriba, a.caminarArriba),
            caminarDerecha = Si(d.caminarDerecha, a.caminarDerecha),
            golpeAbajo = Si(d.golpeAbajo, a.golpeAbajo),
            golpeArriba = Si(d.golpeArriba, a.golpeArriba),
            golpeDerecha = Si(d.golpeDerecha, a.golpeDerecha),
            especial = Si(d.especial, a.especial),
        };
    }

    // ------------------------------------------------------------ control desde GestorJuego
    /// <summary>Se queda quieto (fin de partida).</summary>
    public void Detener()
    {
        estado = Estado.Detenido;
        velDeseada = Vector2.zero;
        if (cuerpo != null) cuerpo.linearVelocity = Vector2.zero;
        if (telegrafo != null) telegrafo.enabled = false;
    }

    /// <summary>Vuelve fuera del ventanal y repite la entrada.</summary>
    public void Reiniciar()
    {
        if (piso == null || piso.Mapa == null) return;
        EscombroCenital.EliminarTodos();

        MapaCenital mapa = piso.Mapa;
        if (mapa.ventanal != null) mapa.ventanal.Restaurar();
        Vector2 inicio = mapa.spawnDemoledor != null ? (Vector2)mapa.spawnDemoledor.position
                       : new Vector2(mapa.Limites.center.x, mapa.Limites.yMax + 1.5f);
        transform.position = inicio;
        cuerpo.position = inicio;
        cuerpo.linearVelocity = Vector2.zero;
        colision.enabled = false;

        velDeseada = Vector2.zero;
        ruta = null;
        objetivo = null;
        ultimoObjetivo = null;
        golpeando = false;
        direccion = Vector2.down;
        telegrafo.enabled = false;
        ultimoLanzamiento = Time.time;
        estado = Estado.Esperando;
        finEstado = Time.time + esperaInicial;
        atascos = 0;
    }

    // ------------------------------------------------------------ ciclo
    private void FixedUpdate()
    {
        cuerpo.linearVelocity = velDeseada;
    }

    private void Update()
    {
        if (piso == null || piso.Mapa == null) return;

        if (piso.Terminado && estado != Estado.Detenido) Detener();

        switch (estado)
        {
            case Estado.Esperando: UpdateEsperando(); break;
            case Estado.Entrando: UpdateEntrando(); break;
            case Estado.Decidiendo: if (Time.time >= finEstado) Decidir(); break;
            case Estado.Caminando: UpdateCaminando(); break;
            case Estado.PrepPunetazo: UpdatePrepPunetazo(); break;
            case Estado.PrepLanzamiento: UpdatePrepLanzamiento(); break;
            case Estado.Recuperando: if (Time.time >= finEstado) { estado = Estado.Decidiendo; finEstado = 0f; } break;
        }

        Animar();
        AjustarDibujo();
    }

    // ------------------------------------------------------------ entrada
    private void UpdateEsperando()
    {
        velDeseada = Vector2.zero;
        if (Time.time < finEstado) return;
        if (piso.Mapa.ventanal != null) piso.Mapa.ventanal.Romper();
        estado = Estado.Entrando;
    }

    private void UpdateEntrando()
    {
        Vector2 meta = piso.Mapa.entradaDemoledor != null ? (Vector2)piso.Mapa.entradaDemoledor.position
                     : new Vector2(transform.position.x, piso.Mapa.Limites.yMax - 1.5f);
        Vector2 d = meta - (Vector2)transform.position;
        if (d.magnitude < 0.15f)
        {
            velDeseada = Vector2.zero;
            colision.enabled = true;
            estado = Estado.Decidiendo;
            finEstado = Time.time + 0.4f;
            return;
        }
        direccion = d.normalized;
        velDeseada = direccion * velocidad * 1.6f * Ritmo;
    }

    // ------------------------------------------------------------ decidir
    private void Decidir()
    {
        velDeseada = Vector2.zero;

        // Lanzamiento: cada cierto tiempo, o (con la proporción del piso) si el jugador está lejos.
        float sinLanzar = Time.time - ultimoLanzamiento;
        bool listo = sinLanzar >= Intervalo;
        bool lejos = jugador != null && Vector2.Distance(Centro, jugador.Centro) > DistLejos;
        if (listo || (lejos && sinLanzar >= Intervalo * 0.4f && Random.value < Proporcion))
        {
            IniciarLanzamiento();
            return;
        }

        // Puñetazo: si ya está junto a un objeto sano o dañado.
        ObjetoReparable junto = ObjetoAlAlcance();
        if (junto != null && !(junto == ultimoObjetivo && Random.value < 0.6f))
        {
            IniciarPunetazo(junto);
            return;
        }

        ElegirObjetivoYCaminar();
    }

    private ObjetoReparable ObjetoAlAlcance()
    {
        ObjetoReparable mejor = null;
        float mejorD = alcancePunetazo;
        foreach (ObjetoReparable o in piso.Mapa.objetos)
        {
            if (o == null || o.Estado == EstadoObjeto.Roto) continue;
            float d = Vector2.Distance(Centro, o.PuntoMasCercano(Centro));
            if (d < mejorD) { mejorD = d; mejor = o; }
        }
        return mejor;
    }

    private void ElegirObjetivoYCaminar()
    {
        List<ObjetoReparable> candidatos = new List<ObjetoReparable>();
        foreach (ObjetoReparable o in piso.Mapa.objetos)
            if (o != null && o.Estado != EstadoObjeto.Roto && o != ultimoObjetivo) candidatos.Add(o);

        if (candidatos.Count == 0)
        {
            // Todo roto (o solo queda el último): espera un momento.
            ultimoObjetivo = null;
            finEstado = Time.time + 0.8f;
            return;
        }

        // Más probable: los intactos (3) que los dañados (2), en orden aleatorio ponderado.
        List<ObjetoReparable> orden = new List<ObjetoReparable>();
        while (candidatos.Count > 0)
        {
            float total = 0f;
            foreach (ObjetoReparable o in candidatos) total += o.Estado == EstadoObjeto.Intacto ? 3f : 2f;
            float azar = Random.value * total;
            int elegido = candidatos.Count - 1;
            for (int i = 0; i < candidatos.Count; i++)
            {
                azar -= candidatos[i].Estado == EstadoObjeto.Intacto ? 3f : 2f;
                if (azar <= 0f) { elegido = i; break; }
            }
            orden.Add(candidatos[elegido]);
            candidatos.RemoveAt(elegido);
        }

        // Para cada objeto prueba sus celdas libres de al lado; si ninguna tiene camino, pasa al siguiente.
        foreach (ObjetoReparable o in orden)
        {
            foreach (Vector2 celda in CeldasDeAproximacion(o, 4))
            {
                List<Vector2> r = BuscadorRutas.Buscar(piso.Mapa, transform.position, celda);
                if (r == null) continue;

                objetivo = o;
                destinoAproximacion = celda;
                ruta = r;
                indiceRuta = 0;
                atascos = 0;
                recalculos = 0;
                posChequeo = transform.position;
                proximoChequeo = Time.time + 0.5f;
                estado = Estado.Caminando;
                return;
            }
            if (registrarAtascos) Debug.Log("[Demoledor] '" + o.name + "' no tiene un lado accesible: elige otro objeto.");
        }
        finEstado = Time.time + 0.5f;   // ninguno es alcanzable por ahora
    }

    /// <summary>
    /// Celdas LIBRES pegadas al objeto (nunca la ocupada), las más cercanas al demoledor primero.
    /// Las que comparten borde con el objeto van antes que las de esquina.
    /// </summary>
    private List<Vector2> CeldasDeAproximacion(ObjetoReparable o, int maximo)
    {
        List<KeyValuePair<float, Vector2>> lista = new List<KeyValuePair<float, Vector2>>();
        foreach (Vector2 c in piso.Mapa.CentrosTransitables)
        {
            float alBorde = Vector2.Distance(c, o.PuntoMasCercano(c));
            if (alBorde > 0.8f) continue;
            float puntaje = (c - (Vector2)transform.position).sqrMagnitude + (alBorde > 0.55f ? 1000f : 0f);
            lista.Add(new KeyValuePair<float, Vector2>(puntaje, c));
        }
        lista.Sort((a, b) => a.Key.CompareTo(b.Key));

        List<Vector2> res = new List<Vector2>();
        for (int i = 0; i < lista.Count && i < maximo; i++) res.Add(lista[i].Value);
        return res;
    }

    // ------------------------------------------------------------ caminar
    private void UpdateCaminando()
    {
        if (ruta == null || indiceRuta >= ruta.Count) { LlegoAlDestino(); return; }

        Vector2 d = ruta[indiceRuta] - (Vector2)transform.position;
        if (d.magnitude < 0.12f) { indiceRuta++; return; }

        direccion = d.normalized;
        velDeseada = direccion * velocidad * Ritmo;

        // Sin avanzar durante ~1 s (se mide cada 0.5 s, dos veces seguidas): primero recalcula la ruta
        // una vez; si sigue atascado, cambia de objetivo. Nunca se recalcula cuadro a cuadro.
        if (Time.time >= proximoChequeo)
        {
            if (Vector2.Distance(transform.position, posChequeo) < 0.1f) atascos++;
            else { atascos = 0; recalculos = 0; }
            posChequeo = transform.position;
            proximoChequeo = Time.time + 0.5f;

            if (atascos >= 2)
            {
                atascos = 0;
                RegistrarAtasco();
                if (recalculos < 1 && Time.time >= proximoRecalculo && Replanificar()) recalculos++;
                else CambiarObjetivo();
            }
        }
    }

    /// <summary>Nueva ruta al mismo destino. Devuelve false si ya no hay camino.</summary>
    private bool Replanificar()
    {
        proximoRecalculo = Time.time + 1f;
        List<Vector2> r = BuscadorRutas.Buscar(piso.Mapa, transform.position, destinoAproximacion);
        if (r == null) return false;
        ruta = r;
        indiceRuta = 0;
        if (registrarAtascos) Debug.Log("[Demoledor] Ruta recalculada hacia '" + (objetivo != null ? objetivo.name : "?") + "'.");
        return true;
    }

    private void CambiarObjetivo()
    {
        if (registrarAtascos) Debug.Log("[Demoledor] Sigue atascado: cambia de objetivo (era '" + (objetivo != null ? objetivo.name : "?") + "').");
        velDeseada = Vector2.zero;
        ultimoObjetivo = objetivo;
        recalculos = 0;
        estado = Estado.Decidiendo;
        finEstado = Time.time + 0.2f;
    }

    /// <summary>Escribe en consola con qué está chocando el demoledor.</summary>
    private void RegistrarAtasco()
    {
        if (!registrarAtascos) return;
        ContactPoint2D[] contactos = new ContactPoint2D[8];
        int n = colision.GetContacts(contactos);
        string con = "";
        for (int i = 0; i < n; i++)
            con += (contactos[i].collider != null ? contactos[i].collider.name : "?") + (i < n - 1 ? ", " : "");
        Vector2 celda = piso.Mapa.CeldaDe(transform.position);
        Debug.LogWarning("[Demoledor] ATASCADO en " + transform.position + " (celda " + celda + ", transitable: " +
                         piso.Mapa.EsTransitable(new Vector2Int((int)celda.x, (int)celda.y)) + "). Waypoint " + indiceRuta + "/" +
                         (ruta != null ? ruta.Count : 0) + ". Choca con: " + (n == 0 ? "nada" : con));
    }

    private void LlegoAlDestino()
    {
        velDeseada = Vector2.zero;
        if (objetivo != null && objetivo.Estado != EstadoObjeto.Roto &&
            Vector2.Distance(Centro, objetivo.PuntoMasCercano(Centro)) <= alcancePunetazo + 0.35f)
        {
            IniciarPunetazo(objetivo);
            return;
        }
        estado = Estado.Decidiendo;
        finEstado = Time.time + 0.2f;
    }

    // ------------------------------------------------------------ puñetazo
    private void IniciarPunetazo(ObjetoReparable o)
    {
        velDeseada = Vector2.zero;
        objetivo = o;
        Vector2 hacia = o.PuntoMasCercano(Centro) - Centro;
        if (hacia.sqrMagnitude > 0.0001f) direccion = hacia.normalized;
        estado = Estado.PrepPunetazo;
        finEstado = Time.time + PrepPunetazo_ / Mathf.Max(0.5f, Ritmo);
        telegrafo.enabled = true;
    }

    private Vector2 PuntoGolpe { get { return Centro + direccion * 0.6f; } }

    private void UpdatePrepPunetazo()
    {
        velDeseada = Vector2.zero;
        telegrafo.transform.position = PuntoGolpe;
        telegrafo.transform.localScale = new Vector3(RadioPunetazo * 2f, RadioPunetazo * 2f, 1f);
        float pulso = 0.25f + 0.25f * Mathf.Abs(Mathf.Sin(Time.time * 14f));
        telegrafo.color = new Color(1f, 0.15f, 0.1f, pulso);
        if (Time.time < finEstado) return;
        Golpear();
    }

    private void Golpear()
    {
        telegrafo.enabled = false;
        Vector2 punto = PuntoGolpe;

        if (objetivo != null && objetivo.Danar()) piso.RegistrarGolpe();
        CamaraPisoCenital.Temblar(0.06f, 0.15f);
        if (jugador != null && Vector2.Distance(jugador.Centro, punto) <= RadioPunetazo) jugador.RecibirGolpe();

        EmpezarAnimacionGolpe();
        ultimoObjetivo = objetivo;
        estado = Estado.Recuperando;
        finEstado = Time.time + pausaTrasGolpe / Mathf.Max(0.5f, Ritmo);
    }

    // ------------------------------------------------------------ lanzamiento
    private void IniciarLanzamiento()
    {
        velDeseada = Vector2.zero;
        ultimoLanzamiento = Time.time;
        estado = Estado.PrepLanzamiento;
        finEstado = Time.time + PrepLanzamiento_ / Mathf.Max(0.5f, Ritmo);
    }

    private void UpdatePrepLanzamiento()
    {
        velDeseada = Vector2.zero;
        if (Time.time < finEstado) return;

        Vector2 destino = ElegirDestinoLanzamiento();
        EscombroCenital.Lanzar(Centro + Vector2.up * 1.2f, destino, Vuelo, RadioImpacto, def, AlImpactar);
        EmpezarAnimacionGolpe();
        estado = Estado.Recuperando;
        finEstado = Time.time + pausaTrasLanzamiento / Mathf.Max(0.5f, Ritmo);
    }

    private Vector2 ElegirDestinoLanzamiento()
    {
        Vector2 p;
        if (jugador != null && Random.value < ProbAlJugador)
        {
            p = jugador.Centro + Random.insideUnitCircle * 0.5f;
        }
        else
        {
            List<ObjetoReparable> lejanos = new List<ObjetoReparable>();
            List<ObjetoReparable> todos = new List<ObjetoReparable>();
            foreach (ObjetoReparable o in piso.Mapa.objetos)
            {
                if (o == null || o.Estado == EstadoObjeto.Roto) continue;
                todos.Add(o);
                if (Vector2.Distance(Centro, o.Centro) > 4f) lejanos.Add(o);
            }
            List<ObjetoReparable> lista = lejanos.Count > 0 ? lejanos : todos;
            if (lista.Count > 0) p = lista[Random.Range(0, lista.Count)].Centro;
            else p = jugador != null ? jugador.Centro : piso.Mapa.Limites.center;
        }

        Rect lim = piso.Mapa.Limites;
        p.x = Mathf.Clamp(p.x, lim.xMin + 0.6f, lim.xMax - 0.6f);
        p.y = Mathf.Clamp(p.y, lim.yMin + 0.6f, lim.yMax - 0.6f);
        return p;
    }

    private void AlImpactar(Vector2 punto)
    {
        if (piso == null || piso.Mapa == null) return;
        bool dano = false;
        foreach (ObjetoReparable o in piso.Mapa.objetos)
        {
            if (o == null) continue;
            if (Vector2.Distance(punto, o.PuntoMasCercano(punto)) <= RadioImpacto && o.Danar()) dano = true;
        }
        if (dano) piso.RegistrarGolpe();
        CamaraPisoCenital.Temblar(0.1f, 0.2f);
        if (jugador != null && Vector2.Distance(jugador.Centro, punto) <= RadioImpacto) jugador.RecibirGolpe();
    }

    // ------------------------------------------------------------ dibujo
    private void EmpezarAnimacionGolpe()
    {
        Sprite[] serie = SerieGolpe();
        if (serie == null || serie.Length == 0) return;
        animador.Reproducir(serie, cuadrosPorSegundoGolpe, false, true);
        golpeando = true;
        finGolpe = Time.time + serie.Length / Mathf.Max(1f, cuadrosPorSegundoGolpe);
    }

    private Sprite[] SerieGolpe()
    {
        Sprite[] s;
        if (Mathf.Abs(direccion.x) > Mathf.Abs(direccion.y)) s = animaciones.golpeDerecha;
        else s = direccion.y > 0f ? animaciones.golpeArriba : animaciones.golpeAbajo;
        if (s == null || s.Length == 0) s = animaciones.golpeDerecha;
        return s;
    }

    private Sprite[] SerieCaminar()
    {
        Sprite[] s;
        if (Mathf.Abs(direccion.x) > Mathf.Abs(direccion.y)) s = animaciones.caminarDerecha;
        else s = direccion.y > 0f ? animaciones.caminarArriba : animaciones.caminarAbajo;
        if (s == null || s.Length == 0) s = animaciones.caminarDerecha;
        return s;
    }

    private bool Provisional_ { get { return dibujo.sprite == null || dibujo.sprite == Provisional.Cuadrado; } }

    private void Animar()
    {
        if (Mathf.Abs(direccion.x) > 0.3f) mirandoIzquierda = direccion.x < 0f;
        dibujo.flipX = mirandoIzquierda;

        if (golpeando && Time.time >= finGolpe) golpeando = false;
        if (!golpeando)
        {
            Sprite[] serie = SerieCaminar();
            if (serie != null && serie.Length > 0)
            {
                if (velDeseada.sqrMagnitude > 0.01f) animador.Reproducir(serie, cuadrosCaminar, true, false);
                else animador.MostrarCuadroFijo(serie, 0);
            }
        }
        animador.Actualizar(Time.deltaTime);

        if (dibujo.sprite == null) dibujo.sprite = Provisional.Cuadrado;

        // Avisos visibles de preparación (también con arte real: parpadeo rojo / amarillo).
        Color baseColor = Provisional_ ? new Color(0.78f, 0.28f, 0.22f) : Color.white;
        if (estado == Estado.PrepPunetazo)
            dibujo.color = ((int)(Time.time * 16f) % 2 == 0) ? new Color(1f, 0.4f, 0.35f) : baseColor;
        else if (estado == Estado.PrepLanzamiento)
            dibujo.color = ((int)(Time.time * 10f) % 2 == 0) ? new Color(1f, 0.9f, 0.3f) : baseColor;
        else dibujo.color = baseColor;
    }

    private void AjustarDibujo()
    {
        Sprite s = dibujo.sprite;
        if (s == null) return;
        Bounds b = s.bounds;
        if (b.size.y <= 0.0001f) return;

        Vector3 tam;
        if (Provisional_) tam = new Vector3(1.1f, alturaDibujo * 0.9f, 1f);
        else { float e = alturaDibujo / b.size.y; tam = new Vector3(e, e, 1f); }

        // En el lanzamiento se estira hacia arriba (brazos en alto).
        if (estado == Estado.PrepLanzamiento) tam.y *= 1.1f;
        dibujo.transform.localScale = tam;
        float signo = dibujo.flipX ? -1f : 1f;
        // El origen del objeto es el centro del cuerpo: los pies del sprite van en la base del collider.
        dibujo.transform.localPosition = new Vector3(-b.center.x * tam.x * signo, -b.min.y * tam.y - radioCuerpo, 0f);
        dibujo.sortingOrder = Provisional.OrdenY(transform.position.y - radioCuerpo);
    }

    // ------------------------------------------------------------ diagnóstico
    private void OnDrawGizmos()
    {
        if (!dibujarDiagnostico) return;

        // Cuerpo (collider real) y alcance del puñetazo.
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, radioCuerpo);
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, alcancePunetazo);

        // Objetivo (rojo) y celda de destino a su lado (verde).
        if (objetivo != null && estado != Estado.Detenido)
        {
            Gizmos.color = Color.red;
            Bounds b = objetivo.Area;
            Gizmos.DrawWireCube(b.center, b.size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(destinoAproximacion, new Vector3(0.9f, 0.9f, 0f));
            Gizmos.DrawSphere(destinoAproximacion, 0.12f);
        }

        // Ruta A* actual: tramos celestes, puntos intermedios y el waypoint que sigue en amarillo.
        if (ruta != null && estado == Estado.Caminando)
        {
            Vector2 anterior = transform.position;
            for (int i = indiceRuta; i < ruta.Count; i++)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(anterior, ruta[i]);
                Gizmos.color = i == indiceRuta ? Color.yellow : Color.cyan;
                Gizmos.DrawSphere(ruta[i], i == indiceRuta ? 0.15f : 0.09f);
                anterior = ruta[i];
            }
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.6f,
            estado + (estado == Estado.Caminando ? "  atascos " + atascos + "  recalc " + recalculos : ""));
#endif
    }
}
