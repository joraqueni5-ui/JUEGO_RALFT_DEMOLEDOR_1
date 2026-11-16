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

    private enum Estado { Esperando, Entrando, Decidiendo, Caminando, PrepPunetazo, PrepLanzamiento, Recuperando, Aturdido, Rindiendo, Saliendo, Saltando, Ido, Detenido, UltiAviso, UltiLluvia, Agotado }

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
    private bool furia;
    private TextMesh zz;

    [Header("Lanzamiento de bloque")]
    [Tooltip("Altura (baldosas, desde el centro del cuerpo) a la que sostiene el bloque sobre su cabeza.")]
    public float alturaBloqueManos = 2.1f;

    private SpriteRenderer bloqueManos;
    private bool usandoBloque;
    private float inicioPrep, duracionPrep;

    // Ulti.
    private bool ultiUsada;
    private int rocasPorLanzar, rocasTotales, golpesHechos, golpesLluvia;
    private float inicioLluvia, proximoGolpeUlti, inicioAplastamiento = -10f;

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
    private float Intervalo
    {
        get
        {
            if (def == null) return furia ? 3.5f : 6f;
            return furia ? def.intervaloLanzamientoFuria : def.intervaloLanzamiento;
        }
    }
    private float UmbralFuria { get { return def != null ? def.umbralFuria : 0.5f; } }
    private float VelocidadFuria { get { return furia ? (def != null ? def.velocidadFuria : 1.4f) : 1f; } }

    /// <summary>El objeto que el demoledor está a punto de golpear (solo durante la preparación), o null.</summary>
    public ObjetoReparable ObjetivoAmenazado { get { return estado == Estado.PrepPunetazo ? objetivo : null; } }
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

        zz = Provisional.CrearTexto(transform, "zZ", "zZ", 0.55f, new Color(0.7f, 0.85f, 1f, 1f), 600);

        // Con arte: las "zZ" son 3 cuadros en bucle (el texto queda vacío y el dibujo cuelga del mismo objeto).
        Sprite[] arteDormido = ArteNiveles.De(a => a.zzDormido);
        if (arteDormido != null)
        {
            zz.text = "";
            SpriteRenderer dormido = Provisional.Crear(zz.transform, "ZzDormido", arteDormido[0], Color.white, 600);
            EfectoSprite.EnBucle(dormido, arteDormido, 4f);
        }
        zz.gameObject.SetActive(false);
    }

    private void Start()
    {
        piso = PisoCenital.Actual;
        def = piso != null ? piso.Definicion : null;
        jugador = FindFirstObjectByType<JugadorCenital>();
        if (def != null) animaciones = AnimacionesCenital.Combinar(animaciones, def.demoledor);
        animaciones.QuitarInvalidos();
        Reiniciar();
    }

    // ------------------------------------------------------------ control desde GestorJuego
    /// <summary>Se queda quieto (fin de partida).</summary>
    public void Detener()
    {
        estado = Estado.Detenido;
        velDeseada = Vector2.zero;
        if (cuerpo != null) cuerpo.linearVelocity = Vector2.zero;
        if (telegrafo != null) telegrafo.enabled = false;
        LimpiarAmenaza();
        AvisoUlti.Ocultar();
        if (bloqueManos != null) bloqueManos.enabled = false;
        if (zz != null) zz.gameObject.SetActive(false);
    }

    private void LimpiarAmenaza()
    {
        if (objetivo != null) objetivo.MarcarAmenaza(false);
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

        LimpiarAmenaza();
        velDeseada = Vector2.zero;
        ruta = null;
        objetivo = null;
        ultimoObjetivo = null;
        golpeando = false;
        furia = false;
        if (zz != null) zz.gameObject.SetActive(false);
        if (dibujo != null) { dibujo.enabled = true; dibujo.color = Color.white; }
        elevacion = 0f;
        alphaSalto = 1f;
        ultiUsada = false;
        GestorAudio.Tension("furia", false);
        GestorAudio.Tension("ulti", false);
        AvisoUlti.Ocultar();
        if (bloqueManos != null) bloqueManos.enabled = false;
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
        // Seguro: mientras patrulla dentro del piso no puede salir del rectángulo del mapa (al entrar y al irse sí cruza el borde).
        if (piso != null && piso.Mapa != null &&
            (estado == Estado.Decidiendo || estado == Estado.Caminando || estado == Estado.PrepPunetazo ||
             estado == Estado.PrepLanzamiento || estado == Estado.Recuperando || estado == Estado.Aturdido))
        {
            Rect lim = piso.Mapa.Limites;
            if (lim.width > 1f && lim.height > 1f)
            {
                Vector2 p = cuerpo.position;
                Vector2 c = new Vector2(Mathf.Clamp(p.x, lim.xMin + 0.4f, lim.xMax - 0.4f), Mathf.Clamp(p.y, lim.yMin + 0.4f, lim.yMax - 0.4f));
                if (c != p) cuerpo.position = c;
            }
        }
        cuerpo.linearVelocity = velDeseada;
    }

    private void Update()
    {
        if (piso == null || piso.Mapa == null) return;

        if (piso.Terminado && estado != Estado.Detenido) Detener();
        if (piso.Rendido && PuedeRendirse) IniciarRendicion();
        if (!piso.Rendido) ActualizarFuria();
        ComprobarUlti();

        switch (estado)
        {
            case Estado.UltiAviso: UpdateUltiAviso(); break;
            case Estado.UltiLluvia: UpdateUltiLluvia(); break;
            case Estado.Agotado: UpdateAgotado(); break;
            case Estado.Esperando: UpdateEsperando(); break;
            case Estado.Entrando: UpdateEntrando(); break;
            case Estado.Decidiendo: if (Time.time >= finEstado) Decidir(); break;
            case Estado.Caminando: UpdateCaminando(); break;
            case Estado.PrepPunetazo: UpdatePrepPunetazo(); break;
            case Estado.PrepLanzamiento: UpdatePrepLanzamiento(); break;
            case Estado.Recuperando: if (Time.time >= finEstado) { estado = Estado.Decidiendo; finEstado = 0f; } break;
            case Estado.Aturdido: UpdateAturdido(); break;
            case Estado.Rindiendo: UpdateRindiendo(); break;
            case Estado.Saliendo: UpdateSaliendo(); break;
            case Estado.Saltando: UpdateSalto(); break;
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
        velDeseada = direccion * velocidad * Ritmo * VelocidadFuria;

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
        o.MarcarAmenaza(true);
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
        LimpiarAmenaza();
        Vector2 punto = PuntoGolpe;

        GestorAudio.Reproducir(Snd.Punetazo);
        if (objetivo != null && objetivo.Danar()) { piso.RegistrarGolpe(); GestorAudio.Reproducir(Snd.GolpeObjeto); }
        CamaraPisoCenital.Temblar(0.06f, 0.15f);
        EfectoSprite.Reproducir(ArteNiveles.De(a => a.golpeEstallido), punto, 400, 1f, 12f);
        EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), punto, 399, 0.8f, 12f);
        if (jugador != null && Vector2.Distance(jugador.Centro, punto) <= RadioPunetazo) jugador.RecibirGolpe();

        EmpezarAnimacionGolpe();
        ultimoObjetivo = objetivo;
        estado = Estado.Recuperando;
        finEstado = Time.time + pausaTrasGolpe / Mathf.Max(0.5f, Ritmo);
    }

    // ------------------------------------------------------------ bloqueo y aturdimiento
    /// <summary>
    /// El jugador reparó 'o'. Si el demoledor se preparaba para golpear justo ese objeto, el golpe falla y
    /// queda aturdido. Devuelve true si hubo bloqueo.
    /// </summary>
    public bool IntentarBloqueo(ObjetoReparable o)
    {
        if (estado != Estado.PrepPunetazo || objetivo == null || objetivo != o) return false;

        LimpiarAmenaza();
        telegrafo.enabled = false;
        velDeseada = Vector2.zero;
        golpeando = false;
        ultimoObjetivo = objetivo;
        estado = Estado.Aturdido;
        finEstado = Time.time + (def != null ? def.aturdimientoBloqueo : 2f);
        CamaraPisoCenital.Temblar(0.05f, 0.12f);
        return true;
    }

    private void UpdateAturdido()
    {
        velDeseada = Vector2.zero;
        if (zz != null)
        {
            if (!zz.gameObject.activeSelf) zz.gameObject.SetActive(true);
            zz.transform.localPosition = new Vector3(0.35f, alturaDibujo - radioCuerpo + 0.2f + Mathf.Sin(Time.time * 4f) * 0.08f, 0f);
        }
        if (Time.time < finEstado) return;

        if (zz != null) zz.gameObject.SetActive(false);
        estado = Estado.Decidiendo;
        finEstado = 0f;
    }

    // ------------------------------------------------------------ rendición
    private float inicioSalida;
    private bool salidaDirecta;
    private bool saltar;
    private float inicioSalto;
    private Vector2 origenSalto;
    private bool gritoDado;
    private Vector2 metaSalida;

    private bool PuedeRendirse
    {
        get
        {
            return estado != Estado.Esperando && estado != Estado.Entrando && estado != Estado.Rindiendo &&
                   estado != Estado.Saliendo && estado != Estado.Saltando && estado != Estado.Ido && estado != Estado.Detenido;
        }
    }

    /// <summary>Energía en 0: deja de atacar, avisa que se rinde y se prepara para irse.</summary>
    private void IniciarRendicion()
    {
        LimpiarAmenaza();
        telegrafo.enabled = false;
        if (zz != null) zz.gameObject.SetActive(false);
        EscombroCenital.EliminarTodos();
        AvisoUlti.Ocultar();
        if (bloqueManos != null) bloqueManos.enabled = false;
        golpeando = false;
        furia = false;
        velDeseada = Vector2.zero;
        ruta = null;
        objetivo = null;
        colision.enabled = false;   // ya no empuja ni choca con el jugador
        TextoFlotante.Mostrar(Centro + Vector2.up * 2.4f, "¡ME RINDO!", new Color(1f, 0.95f, 0.5f), 2.2f);
        GestorAudio.Reproducir(Snd.MeRindo);
        GestorAudio.Tension("furia", false);
        GestorAudio.Tension("ulti", false);
        CamaraPisoCenital.Temblar(0.05f, 0.15f);
        estado = Estado.Rindiendo;
        finEstado = Time.time + (def != null ? def.pausaRendicion : 1.5f);
    }

    private void UpdateRindiendo()
    {
        velDeseada = Vector2.zero;
        if (Time.time < finEstado) return;

        // Ruta por el piso hasta el punto bajo el ventanal; desde ahí sube y sale.
        MapaCenital mapa = piso.Mapa;
        Vector2 entrada = mapa.entradaDemoledor != null ? (Vector2)mapa.entradaDemoledor.position
                        : new Vector2(mapa.Limites.center.x, mapa.Limites.yMax - 1.5f);
        metaSalida = mapa.spawnDemoledor != null ? (Vector2)mapa.spawnDemoledor.position
                   : new Vector2(entrada.x, mapa.Limites.yMax + 1.5f);

        // Sin ascensor, el demoledor corre hasta el punto '!' de la baranda y salta.
        saltar = mapa.ascensor == null && mapa.puntoSalto != null;
        if (saltar) { entrada = mapa.puntoSalto.position; metaSalida = entrada; }

        Vector2Int celda;
        ruta = null;
        if (mapa.CeldaTransitableCercana(entrada, out celda)) ruta = BuscadorRutas.Buscar(mapa, transform.position, mapa.CentroDe(celda));
        indiceRuta = 0;
        salidaDirecta = ruta == null;
        inicioSalida = Time.time;
        estado = Estado.Saliendo;
    }

    private void UpdateSaliendo()
    {
        float rapidez = velocidad * Ritmo * (def != null ? def.velocidadSalida : 1.3f) * (saltar ? 1.5f : 1f);
        float limite = def != null ? def.tiempoMaximoSalida : 12f;
        if (!salidaDirecta && Time.time - inicioSalida > limite) salidaDirecta = true;

        Vector2 meta;
        if (!salidaDirecta && ruta != null && indiceRuta < ruta.Count)
        {
            meta = ruta[indiceRuta];
            if (((Vector2)transform.position - meta).magnitude < 0.15f) { indiceRuta++; return; }
        }
        else
        {
            meta = metaSalida;
            Vector2 resto = meta - (Vector2)transform.position;
            if (resto.magnitude < 0.25f || Time.time - inicioSalida > limite * 2f)
            {
                velDeseada = Vector2.zero;
                if (saltar) { IniciarSalto(); return; }
                if (dibujo != null) dibujo.enabled = false;
                estado = Estado.Ido;
                piso.DemoledorSeFue();
                return;
            }
        }

        Vector2 d = meta - (Vector2)transform.position;
        direccion = d.normalized;
        velDeseada = direccion * rapidez;
    }

    // ------------------------------------------------------------ salto desde la baranda
    private const float DuracionSubida = 0.4f, DuracionCaida = 0.6f;

    private void IniciarSalto()
    {
        estado = Estado.Saltando;
        inicioSalto = Time.time;
        origenSalto = transform.position;
        gritoDado = false;
        GestorAudio.Reproducir(Snd.SaltoTerraza);
        Vector2 hacia = piso.Mapa.direccionSalto;
        direccion = hacia.sqrMagnitude > 0.01f ? hacia.normalized : Vector2.right;
    }

    /// <summary>Se eleva un poco sobre la baranda, cae hacia fuera gritando y desaparece.</summary>
    private void UpdateSalto()
    {
        velDeseada = Vector2.zero;
        float t = Time.time - inicioSalto;
        Vector2 hacia = direccion;

        if (t < DuracionSubida)
        {
            float k = t / DuracionSubida;
            elevacion = 0.9f * Mathf.Sin(k * Mathf.PI * 0.5f);
            Vector2 pos = origenSalto + hacia * (0.8f * k);
            transform.position = pos;
            if (cuerpo != null) cuerpo.position = pos;
            return;
        }

        if (!gritoDado)
        {
            gritoDado = true;
            TextoFlotante.Mostrar(Centro + Vector2.up * 2.2f, "¡AAAH!", new Color(1f, 0.9f, 0.4f), 1.4f);
        }
        float c = Mathf.Clamp01((t - DuracionSubida) / DuracionCaida);
        elevacion = 0.9f - 2.6f * c * c;
        Vector2 caida = origenSalto + hacia * (0.8f + 1.4f * c);
        transform.position = caida;
        if (cuerpo != null) cuerpo.position = caida;
        alphaSalto = 1f - c;

        if (c >= 1f)
        {
            if (dibujo != null) dibujo.enabled = false;
            estado = Estado.Ido;
            piso.DemoledorSeFue();
        }
    }

    // ------------------------------------------------------------ ulti: ¡LLUVIA DE ESCOMBROS!
    /// <summary>Una vez por nivel, cuando la energía baja del umbral y no está atacando.</summary>
    private void ComprobarUlti()
    {
        if (def == null || !def.ultiActiva || ultiUsada || piso.Rendido || piso.Terminado) return;
        if (piso.Energia >= def.umbralUlti) return;
        // Si está atacando (preparando un golpe o un lanzamiento) espera a que termine.
        if (estado != Estado.Decidiendo && estado != Estado.Caminando && estado != Estado.Recuperando && estado != Estado.Aturdido) return;
        if (golpeando) return;
        IniciarUlti();
    }

    private void IniciarUlti()
    {
        ultiUsada = true;
        LimpiarAmenaza();
        telegrafo.enabled = false;
        if (zz != null) zz.gameObject.SetActive(false);
        velDeseada = Vector2.zero;
        ruta = null;
        objetivo = null;
        golpeando = false;
        estado = Estado.UltiAviso;
        finEstado = Time.time + def.SegundosAvisoUlti;
        proximoGolpeUlti = Time.time;
        golpesHechos = 0;
        AvisoUlti.Mostrar("¡LLUVIA DE ESCOMBROS!", def.SegundosAvisoUlti, true, new Color(1f, 0.92f, 0.45f));
        GestorAudio.Reproducir(Snd.AvisoLluvia);
        GestorAudio.Tension("ulti", true);
    }

    private void UpdateUltiAviso()
    {
        velDeseada = Vector2.zero;
        // Aviso: golpea el suelo con los dos puños, uno cada 'intervaloGolpeUlti' (4 golpes = 2 s).
        if (golpesHechos < def.golpesAvisoUlti && Time.time >= proximoGolpeUlti)
        {
            GolpeAlSuelo();
            golpesHechos++;
            proximoGolpeUlti += def.intervaloGolpeUlti;
        }
        if (Time.time < finEstado) return;

        estado = Estado.UltiLluvia;
        inicioLluvia = Time.time;
        rocasPorLanzar = def.RocasUlti;
        rocasTotales = rocasPorLanzar;
        golpesLluvia = def.GolpesLluvia;
        golpesHechos = 0;
        proximoGolpeUlti = Time.time;   // el primer golpe de la lluvia sale al instante
    }

    private void UpdateUltiLluvia()
    {
        velDeseada = Vector2.zero;

        // Cada golpe al suelo suelta la siguiente tanda; las rocas se reparten parejo entre todos los golpes.
        if (golpesHechos < golpesLluvia && Time.time >= proximoGolpeUlti)
        {
            GolpeAlSuelo();
            golpesHechos++;
            proximoGolpeUlti += def.intervaloGolpeUlti;

            int cuantas = golpesHechos >= golpesLluvia ? rocasPorLanzar
                : Mathf.Min(rocasPorLanzar, rocasTotales * golpesHechos / golpesLluvia - (rocasTotales - rocasPorLanzar));
            List<Vector2> tanda = new List<Vector2>();
            for (int i = 0; i < cuantas; i++)
            {
                // La mitad cerca del jugador y la otra mitad sobre reparables.
                bool cerca = (rocasPorLanzar - i) % 2 == 0;
                Vector2 p = ObjetivoRoca(cerca, tanda);
                tanda.Add(p);
                SoltarRoca(p);
            }
            rocasPorLanzar -= cuantas;
        }

        bool acabo = golpesHechos >= golpesLluvia && rocasPorLanzar <= 0 && RocaLluvia.Cantidad == 0;
        if (acabo || Time.time > inicioLluvia + def.duracionLluvia + def.avisoCaidaRoca + 3f) IniciarAgotado();
    }

    /// <summary>Golpe de la ulti: baja los dos puños contra el suelo (animación de golpe hacia abajo y aplastamiento), temblor corto, polvo a los pies y sonido.</summary>
    private void GolpeAlSuelo()
    {
        direccion = Vector2.down;
        EmpezarAnimacionGolpe();
        inicioAplastamiento = Time.time;
        CamaraPisoCenital.Temblar(0.07f, 0.12f);
        Vector2 pies = (Vector2)transform.position + Vector2.down * radioCuerpo;
        EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), pies, 399, 1f, 12f);
        GestorAudio.Reproducir(Snd.GolpeObjeto);
    }

    private void SoltarRoca(Vector2 destino)
    {
        Camera cam = Camera.main;
        float arriba = cam != null ? cam.transform.position.y + cam.orthographicSize + 1.5f : destino.y + 9f;
        RocaLluvia.Lanzar(destino, arriba - destino.y, def.avisoCaidaRoca, def, AlImpactarRoca);
    }

    private static bool MuyCercaDeOtra(Vector2 p, List<Vector2> otras, float minimo)
    {
        foreach (Vector2 o in otras) if (Vector2.Distance(o, p) < minimo) return true;
        foreach (RocaLluvia r in RocaLluvia.Activas) if (r != null && Vector2.Distance(r.Destino, p) < minimo) return true;
        return false;
    }

    /// <summary>
    /// Punto de caída de una roca. Cerca del jugador: a 1,5 a 3 baldosas, en una dirección distinta de las demás (así siempre queda un
    /// hueco para esquivar) y nunca con más de 3 sombras a menos de 2 baldosas de él. Si no, sobre un reparable al azar.
    /// </summary>
    private Vector2 ObjetivoRoca(bool cercaDelJugador, List<Vector2> tanda)
    {
        MapaCenital mapa = piso.Mapa;
        if (cercaDelJugador && jugador != null && RocaLluvia.SombrasCerca(jugador.Centro, def.radioCercaJugador) < def.maxSombrasCercaJugador)
        {
            List<float> usados = new List<float>();
            foreach (RocaLluvia r in RocaLluvia.Activas)
                if (r != null && Vector2.Distance(r.Destino, jugador.Centro) < def.radioCercaJugador * 1.6f)
                    usados.Add(Mathf.Atan2(r.Destino.y - jugador.Centro.y, r.Destino.x - jugador.Centro.x) * Mathf.Rad2Deg);
            foreach (Vector2 t in tanda)
                if (Vector2.Distance(t, jugador.Centro) < def.radioCercaJugador * 1.6f)
                    usados.Add(Mathf.Atan2(t.y - jugador.Centro.y, t.x - jugador.Centro.x) * Mathf.Rad2Deg);

            for (int intento = 0; intento < 20; intento++)
            {
                float ang = Random.value * 360f;
                bool libre = true;
                foreach (float u in usados) if (Mathf.Abs(Mathf.DeltaAngle(ang, u)) < 50f) { libre = false; break; }
                if (!libre) continue;

                float dist = Random.Range(def.distanciaRocaJugador.x, Mathf.Max(def.distanciaRocaJugador.x, def.distanciaRocaJugador.y));
                Vector2 p = jugador.Centro + new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)) * dist;
                if (!mapa.EsTransitable(mapa.CeldaDe(p))) continue;
                if (MuyCercaDeOtra(p, tanda, 1.5f)) continue;
                return p;
            }
        }

        // Sobre un reparable al azar.
        List<ObjetoReparable> lista = new List<ObjetoReparable>();
        foreach (ObjetoReparable o in mapa.objetos) if (o != null && o.Estado != EstadoObjeto.Roto) lista.Add(o);
        for (int intento = 0; intento < 10 && lista.Count > 0; intento++)
        {
            Vector2 p = lista[Random.Range(0, lista.Count)].Centro;
            if (!MuyCercaDeOtra(p, tanda, 1.2f)) return p;
        }
        if (mapa.CentrosTransitables.Count > 0) return mapa.CentrosTransitables[Random.Range(0, mapa.CentrosTransitables.Count)];
        return mapa.Limites.center;
    }

    private void AlImpactarRoca(Vector2 punto)
    {
        if (piso == null || piso.Mapa == null || def == null) return;
        bool dano = false;
        foreach (ObjetoReparable o in piso.Mapa.objetos)
        {
            if (o == null) continue;
            if (Vector2.Distance(punto, o.PuntoMasCercano(punto)) <= def.radioImpactoRoca && o.Danar()) dano = true;
        }
        if (dano) piso.RegistrarGolpe();
        // El jugador pierde una vida si está en la sombra (respeta su invulnerabilidad breve y los poderes).
        if (jugador != null && Vector2.Distance(jugador.Centro, punto) <= def.radioImpactoRoca) jugador.RecibirGolpe();
    }

    private void IniciarAgotado()
    {
        estado = Estado.Agotado;
        velDeseada = Vector2.zero;
        finEstado = Time.time + def.duracionAgotado;
        AvisoUlti.Mostrar("¡ESTÁ AGOTADO!", def.duracionAgotado, false, new Color(0.6f, 1f, 0.6f));
        GestorAudio.Reproducir(Snd.Agotado);
        GestorAudio.Tension("ulti", false);   // terminó la lluvia: vuelve la música del piso si ya no está en furia
    }

    private void UpdateAgotado()
    {
        velDeseada = Vector2.zero;
        if (zz != null)
        {
            if (!zz.gameObject.activeSelf) zz.gameObject.SetActive(true);   // jadea: las "zZ" sobre la cabeza
            zz.transform.localPosition = new Vector3(0.35f, alturaDibujo - radioCuerpo + 0.2f + Mathf.Sin(Time.time * 4f) * 0.08f, 0f);
        }
        if (Time.time < finEstado) return;

        if (zz != null) zz.gameObject.SetActive(false);
        estado = Estado.Decidiendo;
        finEstado = 0f;
    }

    // ------------------------------------------------------------ furia
    private void ActualizarFuria()
    {
        if (estado == Estado.Esperando || estado == Estado.Entrando || estado == Estado.Detenido) return;

        float destrozo = piso.DestrozoSeccionActual;
        if (!furia && destrozo > UmbralFuria)
        {
            furia = true;
            GestorAudio.Reproducir(Snd.Furia);
            GestorAudio.Tension("furia", true);
            TextoFlotante.Mostrar(Centro + Vector2.up * 2.4f, "¡FURIA!", new Color(1f, 0.25f, 0.2f));
        }
        else if (furia && destrozo < UmbralFuria - 0.05f)
        {
            furia = false;
            GestorAudio.Tension("furia", false);
        }
    }

    // ------------------------------------------------------------ lanzamiento
    private void IniciarLanzamiento()
    {
        velDeseada = Vector2.zero;
        ultimoLanzamiento = Time.time;
        estado = Estado.PrepLanzamiento;

        // Con el arte del bloque: se detiene, se agacha y lo levanta sobre su cabeza (el bloque se ve en sus manos).
        GestorAudio.Reproducir(Snd.LevantarBloque);
        Sprite[] bloque = ArteNiveles.De(a => a.bloqueEscombroGrande);
        usandoBloque = bloque != null;
        float prep = usandoBloque && def != null ? def.preparacionBloque : PrepLanzamiento_;
        duracionPrep = prep / Mathf.Max(0.5f, Ritmo);
        inicioPrep = Time.time;
        finEstado = Time.time + duracionPrep;
        if (usandoBloque)
        {
            if (bloqueManos == null) bloqueManos = Provisional.Crear(transform, "BloqueManos", bloque[0], Color.white, 0);
            bloqueManos.sprite = bloque[0];
            bloqueManos.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            bloqueManos.enabled = true;
        }
    }

    private void UpdatePrepLanzamiento()
    {
        velDeseada = Vector2.zero;
        if (Time.time < finEstado) return;

        Vector2 destino = ElegirDestinoLanzamiento();
        if (bloqueManos != null) bloqueManos.enabled = false;
        GestorAudio.Reproducir(Snd.Lanzar);
        float altoManos = usandoBloque ? alturaBloqueManos : 1.2f;
        EscombroCenital.Lanzar(Centro + Vector2.up * altoManos, destino, Vuelo, RadioImpacto, def, AlImpactar);
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
        if (!usandoBloque) CamaraPisoCenital.Temblar(0.1f, 0.2f);   // con el bloque, el temblor suave lo da el propio impacto
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
        bool usaEspecial = estado == Estado.PrepLanzamiento && animaciones.especial != null && animaciones.especial.Length > 0;
        if (usaEspecial)
        {
            // Preparando el lanzamiento: el salto, sin reiniciarse cuadro a cuadro.
            animador.Reproducir(animaciones.especial, cuadrosPorSegundoGolpe, false, false);
        }
        else if (!golpeando)
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
        if (furia) baseColor = Provisional_ ? new Color(1f, 0.08f, 0.06f) : new Color(1f, 0.4f, 0.35f);
        if (estado == Estado.PrepPunetazo)
            dibujo.color = ((int)(Time.time * 16f) % 2 == 0) ? new Color(1f, 0.4f, 0.35f) : baseColor;
        else if (estado == Estado.PrepLanzamiento)
            dibujo.color = ((int)(Time.time * 10f) % 2 == 0) ? new Color(1f, 0.9f, 0.3f) : baseColor;
        else if (estado == Estado.UltiAviso || estado == Estado.UltiLluvia)
            dibujo.color = ((int)(Time.time * 12f) % 2 == 0) ? new Color(1f, 0.25f, 0.2f) : Color.white;
        else if (estado == Estado.Agotado) dibujo.color = new Color(0.78f, 0.84f, 1f);
        else dibujo.color = baseColor;
    }

    private float elevacion;
    private float alphaSalto = 1f;

    private void AjustarDibujo()
    {
        Sprite s = dibujo.sprite;
        if (s == null) return;
        Bounds b = s.bounds;
        if (b.size.y <= 0.0001f) return;

        Vector3 tam;
        if (Provisional_) tam = new Vector3(1.1f, alturaDibujo * 0.9f, 1f);
        else { float e = alturaDibujo / b.size.y; tam = new Vector3(e, e, 1f); }

        // En el lanzamiento se estira hacia arriba (brazos en alto). Con el bloque: primero se agacha y luego lo levanta.
        if (estado == Estado.PrepLanzamiento)
        {
            if (usandoBloque)
            {
                float p = Mathf.Clamp01((Time.time - inicioPrep) / Mathf.Max(0.05f, duracionPrep));
                float f = p < 0.4f ? Mathf.Lerp(1f, 0.85f, p / 0.4f) : Mathf.Lerp(0.85f, 1.12f, (p - 0.4f) / 0.6f);
                tam.y *= f;
                tam.x *= 1f + (1f - f) * 0.5f;
            }
            else tam.y *= 1.1f;
        }
        // Aplastamiento rápido en cada golpe al suelo de la ulti: baja de golpe y recupera su tamaño en 0,18 s.
        if ((estado == Estado.UltiAviso || estado == Estado.UltiLluvia) && def != null)
        {
            float k = 1f - Mathf.Clamp01((Time.time - inicioAplastamiento) / 0.18f);
            tam.y *= 1f - def.aplastamientoGolpeUlti * k;
            tam.x *= 1f + def.aplastamientoGolpeUlti * 0.6f * k;
        }
        dibujo.transform.localScale = tam;
        float signo = dibujo.flipX ? -1f : 1f;
        // El origen del objeto es el centro del cuerpo: los pies del sprite van en la base del collider.
        dibujo.transform.localPosition = new Vector3(-b.center.x * tam.x * signo, -b.min.y * tam.y - radioCuerpo + elevacion, 0f);
        dibujo.sortingOrder = Provisional.OrdenY(transform.position.y - radioCuerpo);

        if (bloqueManos != null)
        {
            bool ver = estado == Estado.PrepLanzamiento && usandoBloque;
            if (bloqueManos.enabled != ver) bloqueManos.enabled = ver;
            if (ver)
            {
                float p = Mathf.Clamp01((Time.time - inicioPrep) / Mathf.Max(0.05f, duracionPrep));
                float y = Mathf.Lerp(0.3f, alturaBloqueManos, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((p - 0.35f) / 0.65f)));
                bloqueManos.transform.localPosition = new Vector3(0f, y, 0f);
                bloqueManos.sortingOrder = dibujo.sortingOrder + 2;
            }
        }

        // Durante el salto se desvanece al caer.
        if (estado == Estado.Saltando)
        {
            Color c = dibujo.color;
            c.a = alphaSalto;
            dibujo.color = c;
        }
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
