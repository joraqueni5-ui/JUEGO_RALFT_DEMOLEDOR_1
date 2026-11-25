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

    [Header("Empujón: si el jugador se le acerca, lo aleja de un manotazo y le quita una vida")]
    [Tooltip("Distancia (baldosas) entre los centros a la que el demoledor reacciona.")]
    public float radioEmpujon = 1.4f;
    [Tooltip("Baldosas que sale despedido el jugador (los muros lo frenan: nunca sale del mapa).")]
    public float distanciaEmpujon = 2.8f;
    [Tooltip("Segundos que el jugador queda aturdido tras el empujón.")]
    public float aturdimientoEmpujon = 0.6f;
    [Tooltip("Aviso (\"!\") antes del manotazo, para poder escapar.")]
    public float preparacionEmpujon = 0.32f;
    [Tooltip("Segundos mínimos entre dos empujones.")]
    public float esperaEmpujon = 2.2f;

    [Header("Presencia (pasos y gruñidos)")]
    [Tooltip("Hasta esta distancia (baldosas) se oyen sus pasos; más cerca suenan más fuerte.")]
    public float distanciaOido = 10f;

    private enum Estado { Esperando, Entrando, Decidiendo, Caminando, PrepPunetazo, PrepLanzamiento, Recuperando, Aturdido, Rindiendo, Saliendo, Saltando, Ido, Detenido, UltiAviso, UltiLluvia, Agotado, PrepEmpujon, PrepEmbestida, Embistiendo, PrepPisoton, PrepTorbellino, Torbellino, PrepLaser, Laser, Escapando, PrepBrinco, Brinco, Agarrando }

    private float proximoEmpujon, proximoPasoPesado, proximoGrunido, finCongelado;
    private bool alarmaDada;
    private SpriteRenderer alerta, hielo;
    private Sprite[] arteAlerta;

    /// <summary>True mientras el reloj de hielo lo tiene congelado.</summary>
    public bool Congelado { get { return Time.time < finCongelado; } }
    /// <summary>True cuando está dentro del piso y puede actuar (no entrando, ni rendido, ni fuera).</summary>
    public bool EnPiso
    {
        get
        {
            return estado == Estado.Decidiendo || estado == Estado.Caminando || estado == Estado.PrepPunetazo ||
                   estado == Estado.PrepLanzamiento || estado == Estado.Recuperando || estado == Estado.Aturdido ||
                   estado == Estado.PrepEmpujon || estado == Estado.UltiAviso || estado == Estado.UltiLluvia || estado == Estado.Agotado ||
                   estado == Estado.PrepEmbestida || estado == Estado.Embistiendo || estado == Estado.PrepPisoton ||
                   estado == Estado.PrepTorbellino || estado == Estado.Torbellino || estado == Estado.PrepLaser || estado == Estado.Laser ||
                   estado == Estado.PrepBrinco || estado == Estado.Brinco || estado == Estado.Agarrando;
        }
    }
    /// <summary>Rompiendo el ventanal y entrando al piso (la escena de llegada lo enfoca).</summary>
    public bool Entrando { get { return estado == Estado.Entrando; } }
    /// <summary>Todavía fuera, esperando para entrar (el piso está tranquilo).</summary>
    public bool Esperando { get { return estado == Estado.Esperando; } }
    [Header("Modo rabia (energía baja)")]
    [Tooltip("Con la energía en este valor o menos (después de la lluvia de rocas) entra en rabia hasta rendirse.")]
    [Range(0f, 1f)] public float umbralRabia = 0.2f;
    [Tooltip("En rabia todo lo hace más rápido: caminar, preparar golpes y pausas.")]
    public float multiplicadorRabia = 1.45f;

    private bool rabia;
    private SpriteRenderer auraRabia;
    private Sprite[] cuadrosAura;

    /// <summary>Modo rabia continuo (energía por debajo del 20 %): más rápido y con oleadas de mini villanos.</summary>
    public bool EnRabia { get { return rabia; } }

    /// <summary>En furia (destrozo alto): llama más mini villanos.</summary>
    public bool EnFuria { get { return furia; } }

    // ------------------------------------------------------------ varios demoledores (multijugador)
    private static readonly List<DemoledorCenital> todos = new List<DemoledorCenital>();
    /// <summary>Los demoledores del piso (1 en modo normal, 2 en multijugador).</summary>
    public static List<DemoledorCenital> Todos { get { return todos; } }
    /// <summary>0 = el de la escena, 1 = el segundo del multijugador.</summary>
    [System.NonSerialized] public int numero;
    /// <summary>Corrimiento de su punto de aparición y de entrada (para que dos no se pisen).</summary>
    [System.NonSerialized] public Vector2 desplazamientoEntrada;
    /// <summary>Segundos extra antes de entrar.</summary>
    [System.NonSerialized] public float retrasoEntrada;

    // ------------------------------------------------------------ tipos de villano
    /// <summary>
    /// Clásico (camisa azul), Ingeniero (de blanco: lanza escombros de a tres), Demoledora (rápida: TORBELLINO) y
    /// Robo-demoledor (lento pero con LÁSER). Mismos movimientos, otra vestimenta (Resources/Extras/Villanos).
    /// </summary>
    public enum TipoDemoledor { Clasico, Ingeniero, Demoledora, Robot }
    [System.NonSerialized] public TipoDemoledor tipo = TipoDemoledor.Clasico;
    private bool tipoFijado;
    private TipoDemoledor tipoAplicado = TipoDemoledor.Clasico;

    public static string NombreDe(TipoDemoledor t)
    {
        switch (t)
        {
            case TipoDemoledor.Ingeniero: return "EL INGENIERO";
            case TipoDemoledor.Demoledora: return "LA DEMOLEDORA";
            case TipoDemoledor.Robot: return "ROBO-DEMOLEDOR";
            default: return "EL DEMOLEDOR";
        }
    }

    public string Nombre { get { return NombreDe(tipo); } }

    private float FactorTipo
    {
        get { return tipo == TipoDemoledor.Demoledora ? 1.2f : tipo == TipoDemoledor.Robot ? 0.85f : tipo == TipoDemoledor.Ingeniero ? 1.05f : 1f; }
    }

    /// <summary>Cambia el tipo de villano (vestimenta y habilidades). Lo usa la supervivencia entre oleadas.</summary>
    public void AplicarTipo(TipoDemoledor t)
    {
        tipo = t;
        tipoFijado = true;
        CargarArteTipo();
    }

    private AnimacionesCenital animacionesBase;

    /// <summary>Las animaciones del demoledor clásico (para crear copias con otra vestimenta).</summary>
    public AnimacionesCenital AnimacionesOriginales { get { return animacionesBase ?? animaciones; } }

    private void CargarArteTipo()
    {
        if (animacionesBase == null) return;
        if (tipo == tipoAplicado && animaciones != null && tipo == TipoDemoledor.Clasico) return;
        tipoAplicado = tipo;
        if (tipo == TipoDemoledor.Clasico) { animaciones = animacionesBase; return; }
        string id = tipo == TipoDemoledor.Ingeniero ? "blanco" : tipo == TipoDemoledor.Demoledora ? "demoledora" : "robot";
        Sprite[] caminar = ArteExtra.Serie("Villanos/" + id + "/caminar_", 6);
        Sprite[] golpe = ArteExtra.Serie("Villanos/" + id + "/golpe_", 6);
        Sprite[] salto = ArteExtra.Serie("Villanos/" + id + "/salto_", 4);
        if (caminar == null || golpe == null) { animaciones = animacionesBase; return; }
        animaciones = new AnimacionesCenital
        {
            caminarAbajo = caminar, caminarArriba = caminar, caminarDerecha = caminar,
            golpeAbajo = golpe, golpeArriba = golpe, golpeDerecha = golpe,
            especial = salto ?? animacionesBase.especial
        };
    }

    /// <summary>El demoledor más cercano a 'punto' (o null).</summary>
    public static DemoledorCenital MasCercano(Vector2 punto)
    {
        DemoledorCenital mejor = null;
        float dm = float.MaxValue;
        for (int i = 0; i < todos.Count; i++)
        {
            DemoledorCenital d = todos[i];
            if (d == null || !d.isActiveAndEnabled) continue;
            float dd = ((Vector2)d.transform.position - punto).sqrMagnitude;
            if (dd < dm) { dm = dd; mejor = d; }
        }
        return mejor;
    }

    private void OnEnable() { if (!todos.Contains(this)) todos.Add(this); }
    private void OnDestroy()
    {
        if (marcaEntrada != null) Destroy(marcaEntrada.gameObject);
        if (lineaLaser != null) Destroy(lineaLaser.gameObject);
        todos.Remove(this);
        foreach (string clave in new[] { "furia", "ulti", "cerca", "rabia" }) GestorAudio.Tension(clave + numero, false);
    }

    /// <summary>Música de tensión propia de este demoledor (cada uno lleva sus motivos).</summary>
    private void TensionPropia(string motivo, bool activa) { GestorAudio.Tension(motivo + numero, activa); }

    /// <summary>Golpe en un punto: todos los jugadores dentro del radio pierden una vida.</summary>
    private void GolpearJugadoresEn(Vector2 punto, float radio)
    {
        foreach (JugadorCenital j in JugadorCenital.Todos)
            if (j != null && j.isActiveAndEnabled && Vector2.Distance(j.Centro, punto) <= radio) j.RecibirGolpe();
    }

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
    private float Ritmo { get { return (piso != null ? piso.RitmoDemoledor : 1f) * (rabia ? multiplicadorRabia : 1f) * FactorTipo; } }
    private float PrepPunetazo_ { get { return def != null ? def.preparacionPunetazo : 0.5f; } }
    private float RadioPunetazo { get { return def != null ? def.radioPunetazo : 1f; } }
    private float PrepLanzamiento_ { get { return def != null ? def.preparacionLanzamiento : 0.8f; } }
    private float Vuelo { get { return def != null ? def.vueloLanzamiento : 0.9f; } }
    private float RadioImpacto { get { return def != null ? def.radioImpacto : 1f; } }
    private float Intervalo
    {
        get
        {
            float f = tipo == TipoDemoledor.Ingeniero ? 0.6f : 1f;   // el ingeniero lanza más seguido
            if (def == null) return (furia ? 3.5f : 6f) * f;
            return (furia ? def.intervaloLanzamientoFuria : def.intervaloLanzamiento) * f;
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

        // "!" de aviso antes del empujón y cristales de hielo cuando está congelado (arte extra).
        arteAlerta = ArteExtra.Serie("Efectos/alerta_", 2);
        alerta = Provisional.Crear(transform, "AlertaEmpujon", arteAlerta != null ? arteAlerta[0] : Provisional.Estrella, new Color(1f, 0.85f, 0.2f), 600);
        alerta.enabled = false;
        hielo = Provisional.Crear(transform, "Hielo", ArteExtra.Uno("Efectos/hielo_0"), new Color(1f, 1f, 1f, 0.9f), 0);
        hielo.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
        hielo.enabled = false;
    }

    private void Start()
    {
        piso = PisoCenital.Actual;
        def = piso != null ? piso.Definicion : null;
        jugador = JugadorCenital.MasCercano(transform.position);
        if (def != null) animaciones = AnimacionesCenital.Combinar(animaciones, def.demoledor);
        animaciones.QuitarInvalidos();
        animacionesBase = animaciones;
        // En multijugador el segundo villano es LA DEMOLEDORA (salvo que la supervivencia diga otra cosa).
        if (!tipoFijado && numero == 1) tipo = TipoDemoledor.Demoledora;
        CargarArteTipo();
        Reiniciar();
    }

    // ------------------------------------------------------------ control desde GestorJuego
    /// <summary>Se queda quieto (fin de partida).</summary>
    public void Detener()
    {
        CancelarEspecial();
        MostrarMarcaEntrada(false);
        estado = Estado.Detenido;
        velDeseada = Vector2.zero;
        if (cuerpo != null) cuerpo.linearVelocity = Vector2.zero;
        if (telegrafo != null) telegrafo.enabled = false;
        LimpiarAmenaza();
        AvisoUlti.Ocultar();
        if (rabia) TensionPropia("rabia", false);
        rabia = false;
        if (auraRabia != null) auraRabia.enabled = false;
        if (bloqueManos != null) bloqueManos.enabled = false;
        if (zz != null) zz.gameObject.SetActive(false);
        if (alerta != null) alerta.enabled = false;
        if (hielo != null) hielo.enabled = false;
        finCongelado = 0f;
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
        CancelarEspecial();
        proximoEspecial = Time.time + esperaInicial + retrasoEntrada + 9f;
        proximoBrinco = Time.time + esperaInicial + retrasoEntrada + 10f;
        proximoBrincoAzar = proximoBrinco + Random.Range(6f, 14f);
        golpesRepetidos = 0; sinCamino = 0; objetoRepetido = null; salaSeguida = -2;

        MapaCenital mapa = piso.Mapa;
        if (mapa.ventanal != null && numero == 0) mapa.ventanal.Restaurar();
        // Entrada tipo torpedo: cada villano elige su punto del borde (dos nunca entran por el mismo lado) y espera fuera.
        ElegirEntrada();
        Vector2 inicio = puntoEntrada - dirEntrada * (distBordeEntrada + 9f);
        impactoEntrada = false;
        salaPreferida = -1;
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
        if (rabia) TensionPropia("rabia", false);
        rabia = false;
        if (auraRabia != null) auraRabia.enabled = false;
        if (zz != null) zz.gameObject.SetActive(false);
        if (dibujo != null) { dibujo.enabled = true; dibujo.color = Color.white; }
        elevacion = 0f;
        alphaSalto = 1f;
        ultiUsada = false;
        TensionPropia("furia", false);
        TensionPropia("ulti", false);
        TensionPropia("cerca", false);
        musicaCerca = false;
        AvisoUlti.Ocultar();
        if (bloqueManos != null) bloqueManos.enabled = false;
        direccion = Vector2.down;
        telegrafo.enabled = false;
        ultimoLanzamiento = Time.time;
        estado = Estado.Esperando;
        finEstado = Time.time + esperaInicial + retrasoEntrada;
        atascos = 0;
        alarmaDada = false;
        finCongelado = 0f;
        proximoEmpujon = Time.time + 1.5f;
        proximoGrunido = Time.time + 6f;
        if (alerta != null) alerta.enabled = false;
        if (hielo != null) hielo.enabled = false;
    }

    // ------------------------------------------------------------ ciclo
    private void FixedUpdate()
    {
        // Seguro: mientras patrulla dentro del piso no puede salir del rectángulo del mapa (al entrar y al irse sí cruza el borde).
        if (piso != null && piso.Mapa != null &&
            (estado == Estado.Decidiendo || estado == Estado.Caminando || estado == Estado.PrepPunetazo ||
             estado == Estado.PrepLanzamiento || estado == Estado.Recuperando || estado == Estado.Aturdido || estado == Estado.PrepEmpujon ||
             estado == Estado.PrepEmbestida || estado == Estado.Embistiendo || estado == Estado.PrepPisoton ||
             estado == Estado.PrepTorbellino || estado == Estado.Torbellino || estado == Estado.PrepLaser || estado == Estado.Laser))
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
        if (JugadorCenital.Todos.Count > 1) jugador = JugadorCenital.MasCercano(Centro);   // persigue al más cercano

        if (piso.Terminado && estado != Estado.Detenido) Detener();
        if (piso.Rendido && PuedeRendirse) IniciarRendicion();
        if (marcaBrinco != null && marcaBrinco.enabled && estado != Estado.PrepBrinco && estado != Estado.Brinco) marcaBrinco.enabled = false;
        SeguirSala();
        if (!piso.Rendido) ActualizarFuria();
        if (!piso.Rendido) ComprobarRabia();
        ComprobarUlti();
        ComprobarEmpujon();
        SonidosDePresencia();

        switch (estado)
        {
            case Estado.PrepEmpujon: UpdatePrepEmpujon(); break;
            case Estado.PrepEmbestida: UpdatePrepEmbestida(); break;
            case Estado.Embistiendo: UpdateEmbistiendo(); break;
            case Estado.PrepPisoton: UpdatePrepPisoton(); break;
            case Estado.PrepTorbellino: UpdatePrepTorbellino(); break;
            case Estado.Torbellino: UpdateTorbellino(); break;
            case Estado.PrepLaser: UpdatePrepLaser(); break;
            case Estado.Laser: UpdateLaser(); break;
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
            case Estado.Escapando: UpdateEscapando(); break;
            case Estado.PrepBrinco: UpdatePrepBrinco(); break;
            case Estado.Brinco: UpdateBrinco(); break;
            case Estado.Agarrando: UpdateAgarre(); break;
            case Estado.Saltando: UpdateSalto(); break;
        }

        Animar();
        AjustarDibujo();
    }

    // ------------------------------------------------------------ entrada
    private void UpdateEsperando()
    {
        velDeseada = Vector2.zero;
        // Un segundo antes de entrar suena la alarma de obra: el jugador sabe que viene.
        if (!alarmaDada && Time.time >= finEstado - 1.4f)
        {
            alarmaDada = true;
            GestorAudio.Reproducir(SndExtra.AlarmaAparicion);
            TextoFlotante.Mostrar(puntoEntrada + Vector2.up * 1.5f, "¡CUIDADO! ¡AHÍ VIENE " + Nombre + "!", new Color(1f, 0.45f, 0.25f), 2.2f);
        }
        MostrarMarcaEntrada(Time.time >= finEstado - 1.4f);
        if (Time.time < finEstado) return;
        estado = Estado.Entrando;
        impactoEntrada = false;
        GestorAudio.Reproducir(SndExtra.EmbestidaCorrer);
        GestorAudio.Reproducir(SndExtra.RugidoDemoledor);
        ComentariosJugador.Evento(ComentariosJugador.Tipo.LlegaDemoledor);
    }

    // ------------------------------------------------------------ entrada tipo torpedo
    private Vector2 puntoEntrada, dirEntrada = Vector2.down;
    private float distBordeEntrada = 1.5f, inicioDerrape, proximaEstelaEntrada;
    private bool impactoEntrada, entradaElegida;
    private SpriteRenderer marcaEntrada;

    /// <summary>
    /// Punto donde aterriza al entrar y dirección del vuelo. El primero entra por la entrada del mapa (el ventanal); los demás,
    /// por otro lugar del borde, lejos de los otros villanos y del jugador.
    /// </summary>
    private void ElegirEntrada()
    {
        MapaCenital mapa = piso.Mapa;
        Rect lim = mapa.Limites;
        Vector2 baseEntrada = mapa.entradaDemoledor != null ? (Vector2)mapa.entradaDemoledor.position : new Vector2(lim.center.x, lim.yMax - 1.5f);
        bool usarBase = numero == 0;
        if (usarBase)
        {
            foreach (DemoledorCenital d in todos)
                if (d != null && d != this && d.entradaElegida && Vector2.Distance(d.puntoEntrada, baseEntrada) < 4f) usarBase = false;
        }
        if (usarBase) puntoEntrada = baseEntrada;
        else
        {
            List<Vector2> candidatos = new List<Vector2>();
            foreach (Vector2 c in mapa.CentrosTransitables)
            {
                float dBorde = Mathf.Min(Mathf.Min(c.x - lim.xMin, lim.xMax - c.x), Mathf.Min(c.y - lim.yMin, lim.yMax - c.y));
                if (dBorde > 1.6f) continue;
                if (mapa.spawnJugador != null && Vector2.Distance(c, mapa.spawnJugador.position) < 4f) continue;
                if (Vector2.Distance(c, baseEntrada) < 5f) continue;
                bool lejos = true;
                foreach (DemoledorCenital d in todos)
                    if (d != null && d != this && d.entradaElegida && Vector2.Distance(c, d.puntoEntrada) < 6f) { lejos = false; break; }
                if (lejos) candidatos.Add(c);
            }
            puntoEntrada = candidatos.Count > 0 ? candidatos[Random.Range(0, candidatos.Count)] : baseEntrada + desplazamientoEntrada;
        }
        float dl = puntoEntrada.x - lim.xMin, dr = lim.xMax - puntoEntrada.x, db = puntoEntrada.y - lim.yMin, dt = lim.yMax - puntoEntrada.y;
        float m = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(db, dt));
        dirEntrada = m == dl ? Vector2.right : m == dr ? Vector2.left : m == db ? Vector2.up : Vector2.down;
        distBordeEntrada = Mathf.Max(0.3f, m);
        entradaElegida = true;
    }

    private void MostrarMarcaEntrada(bool ver)
    {
        if (ver && marcaEntrada == null)
            marcaEntrada = Provisional.Crear(null, "MarcaEntrada", Provisional.Aro, new Color(1f, 0.2f, 0.15f, 0.8f), -100);
        if (marcaEntrada == null) return;
        marcaEntrada.enabled = ver;
        if (!ver) return;
        marcaEntrada.transform.position = puntoEntrada;
        float e = 1.1f + 0.25f * Mathf.Sin(Time.time * 14f);
        marcaEntrada.transform.localScale = new Vector3(e, e * 0.8f, 1f);
    }

    /// <summary>Atraviesa la pared: vidrios, polvo, temblor y "¡CRASH!".</summary>
    private void ImpactoEntrada()
    {
        impactoEntrada = true;
        inicioDerrape = Time.time;
        MostrarMarcaEntrada(false);
        Vector2 cruce = puntoEntrada - dirEntrada * distBordeEntrada;
        MapaCenital mapa = piso.Mapa;
        if (mapa.ventanal != null && Vector2.Distance(mapa.ventanal.transform.position, cruce) < 3.5f) mapa.ventanal.Romper();
        else
        {
            // Otra pared: queda el agujero con grietas un rato.
            Sprite grieta = ArteExtra.Uno("Efectos/grieta_pisoton");
            if (grieta == null) grieta = Provisional.Grietas(3);
            MarcaTemporal.Crear(grieta, cruce, 1.1f, 14f);
        }
        GestorAudio.Reproducir(Snd.VidrioRoto);
        GestorAudio.Reproducir(SndExtra.ChoqueMuro);
        CamaraPisoCenital.Temblar(0.25f, 0.5f);
        ImpactoPausa.Breve(0.07f);
        TrozoVolador.Lanzar(cruce, 9, 460);
        EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), cruce, 455, 1.5f, 12f);
        TextoFlotante.Mostrar(cruce + Vector2.up * 1.6f, "¡CRASH!", new Color(1f, 0.85f, 0.3f), 1.2f);
        Burbuja.Decir(transform, Frases.Una(Frases.Demoledor), Burbuja.Voz.Villano, alturaDibujo - radioCuerpo + 0.3f);
        // Quien esté justo donde aterriza sale volando.
        foreach (JugadorCenital j in JugadorCenital.Todos)
            if (j != null && j.isActiveAndEnabled && Vector2.Distance(j.Centro, puntoEntrada) < 1.2f)
                j.RecibirEmpujon(cruce, 2.2f, 0.7f, false);
    }

    private void UpdateEntrando()
    {
        float avance = Vector2.Dot((Vector2)transform.position - puntoEntrada, dirEntrada);   // negativo mientras está afuera
        direccion = dirEntrada;
        if (!impactoEntrada)
        {
            // En vuelo: rápido y recto, dejando estela.
            velDeseada = dirEntrada * 15f;
            if (Time.time >= proximaEstelaEntrada && dibujo != null && dibujo.sprite != null)
            {
                proximaEstelaEntrada = Time.time + 0.035f;
                EstelaFantasma.Crear(dibujo, new Color(1f, 0.55f, 0.3f, 0.5f), 0.22f);
            }
            if (avance >= -distBordeEntrada) ImpactoEntrada();
            return;
        }
        // Derrape hasta su punto de aterrizaje.
        float k = Mathf.Clamp01((Time.time - inicioDerrape) / 0.35f);
        if (avance >= -0.05f || k >= 1f)
        {
            velDeseada = Vector2.zero;
            transform.position = puntoEntrada;
            cuerpo.position = puntoEntrada;
            colision.enabled = true;
            EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), puntoEntrada, Provisional.OrdenY(puntoEntrada.y) - 1, 1.2f, 12f);
            // Pose de llegada: ruge golpeándose el pecho y la onda empuja a quien esté cerca.
            finPoseEntrada = Time.time + 1.2f;
            SpriteRenderer onda = EfectoSprite.Reproducir(ArteExtra.Serie("Efectos/onda_empujon_", 4), puntoEntrada, 455, 2.6f, 12f);
            if (onda != null) onda.color = new Color(1f, 0.6f, 0.3f);
            GestorAudio.Reproducir(SndExtra.RugidoDemoledor);
            CamaraPisoCenital.Temblar(0.18f, 0.6f);
            TextoFlotante.Mostrar(puntoEntrada + Vector2.up * 2.6f, "¡" + Nombre + " LLEGÓ!", new Color(1f, 0.55f, 0.3f), 1.8f);
            foreach (JugadorCenital j in JugadorCenital.Todos)
                if (j != null && j.isActiveAndEnabled && Vector2.Distance(j.Centro, puntoEntrada) < 2.4f) j.RecibirEmpujon(puntoEntrada, 1.6f, 0.4f, false);
            estado = Estado.Recuperando;
            finEstado = Time.time + 1.2f;
            return;
        }
        velDeseada = dirEntrada * Mathf.Lerp(Mathf.Max(3f, distBordeEntrada / 0.25f), 1.5f, k);
        if (Random.value < 0.4f) EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), Centro - dirEntrada * 0.3f, Provisional.OrdenY(transform.position.y) - 1, 0.6f, 14f);
    }

    // ------------------------------------------------------------ decidir
    private void Decidir()
    {
        velDeseada = Vector2.zero;
        // ¿Lo están encerrando (repite el mismo objeto, no sale de la sala, no encuentra camino)? Un gran brinco a otra zona.
        if (DebeBrincar() && IniciarBrinco()) return;
        if (IntentarEspecial()) return;

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
        // Recorrido impredecible: a veces solo merodea hasta un punto al azar del piso antes de elegir qué romper.
        if (!furia && !rabia && Random.value < 0.15f && Merodear()) return;

        List<ObjetoReparable> candidatos = new List<ObjetoReparable>();
        foreach (ObjetoReparable o in piso.Mapa.objetos)
            if (o != null && o.Estado != EstadoObjeto.Roto && o != ultimoObjetivo) candidatos.Add(o);

        // Regla: dos villanos nunca van a la misma sala. Y cada uno recorre una sala a la vez (rutas variadas).
        HashSet<int> ocupadas = SalasDeOtros();
        if (ocupadas.Count > 0)
        {
            List<ObjetoReparable> libres = candidatos.FindAll(o => !ocupadas.Contains(SalasMapa.DeObjeto(piso.Mapa, o)));
            if (libres.Count > 0) candidatos = libres;
        }
        ElegirSalaPreferida(candidatos, ocupadas);

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
            foreach (ObjetoReparable o in candidatos) total += PesoObjetivo(o);
            float azar = Random.value * total;
            int elegido = candidatos.Count - 1;
            for (int i = 0; i < candidatos.Count; i++)
            {
                azar -= PesoObjetivo(candidatos[i]);
                if (azar <= 0f) { elegido = i; break; }
            }
            orden.Add(candidatos[elegido]);
            candidatos.RemoveAt(elegido);
        }

        // Para cada objeto prueba sus celdas libres de al lado (en orden al azar, así no llega siempre por el mismo lado).
        foreach (ObjetoReparable o in orden)
        {
            List<Vector2> lados = CeldasDeAproximacion(o, 4);
            for (int i = lados.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                Vector2 tmp = lados[i]; lados[i] = lados[j]; lados[j] = tmp;
            }
            foreach (Vector2 celda in lados)
            {
                List<Vector2> r = BuscadorRutas.Buscar(piso.Mapa, transform.position, celda);
                if (r == null) continue;

                objetivo = o;
                destinoAproximacion = celda;
                ruta = r;
                indiceRuta = 0;
                atascos = 0;
                recalculos = 0;
                sinCamino = 0;
                posChequeo = transform.position;
                proximoChequeo = Time.time + 0.5f;
                estado = Estado.Caminando;
                return;
            }
            if (registrarAtascos) Debug.Log("[Demoledor] '" + o.name + "' no tiene un lado accesible: elige otro objeto.");
        }
        finEstado = Time.time + 0.5f;   // ninguno es alcanzable por ahora
        sinCamino++;
    }

    // ------------------------------------------------------------ salas y rutas variadas
    private int salaPreferida = -1, salaAnterior = -1, golpesEnSala, limiteGolpesSala = 3;

    /// <summary>Las salas donde están (o hacia donde van) los otros villanos.</summary>
    private HashSet<int> SalasDeOtros()
    {
        HashSet<int> r = new HashSet<int>();
        foreach (DemoledorCenital d in todos)
        {
            if (d == null || d == this || !d.isActiveAndEnabled || !d.EnPiso) continue;
            int s = SalasMapa.De(piso.Mapa, d.Centro);
            if (s >= 0) r.Add(s);
            if (d.objetivo != null) { int so = SalasMapa.DeObjeto(piso.Mapa, d.objetivo); if (so >= 0) r.Add(so); }
            if (d.salaPreferida >= 0) r.Add(d.salaPreferida);
        }
        return r;
    }

    /// <summary>Elige la sala que va a recorrer (cambia cada 2 a 4 golpes o si ya no le queda nada sano allí).</summary>
    private void ElegirSalaPreferida(List<ObjetoReparable> candidatos, HashSet<int> ocupadas)
    {
        bool quedan = salaPreferida >= 0 && !ocupadas.Contains(salaPreferida) &&
                      candidatos.Exists(o => SalasMapa.DeObjeto(piso.Mapa, o) == salaPreferida);
        if (quedan && golpesEnSala < limiteGolpesSala) return;
        List<int> salas = new List<int>();
        foreach (ObjetoReparable o in candidatos)
        {
            int s = SalasMapa.DeObjeto(piso.Mapa, o);
            if (s >= 0 && !salas.Contains(s) && s != salaPreferida && !ocupadas.Contains(s)) salas.Add(s);
        }
        if (salas.Count == 0) { salaPreferida = -1; return; }
        salaAnterior = salaPreferida;
        salaPreferida = salas[Random.Range(0, salas.Count)];
        golpesEnSala = 0;
        limiteGolpesSala = Random.Range(2, 5);
    }

    private float PesoObjetivo(ObjetoReparable o)
    {
        float p = o.Estado == EstadoObjeto.Intacto ? 3f : 2f;
        if (salaPreferida >= 0 && SalasMapa.DeObjeto(piso.Mapa, o) == salaPreferida) p *= 6f;
        return p;
    }

    /// <summary>Camina hasta una celda al azar a 3 a 9 baldosas (sin objetivo). Devuelve false si no encontró camino.</summary>
    private bool Merodear()
    {
        List<Vector2> celdas = piso.Mapa.CentrosTransitables;
        if (celdas == null || celdas.Count == 0) return false;
        HashSet<int> ocupadas = SalasDeOtros();
        for (int intento = 0; intento < 8; intento++)
        {
            Vector2 c = celdas[Random.Range(0, celdas.Count)];
            float d = Vector2.Distance(c, transform.position);
            if (d < 3f || d > 9f) continue;
            if (ocupadas.Contains(SalasMapa.De(piso.Mapa, c))) continue;
            List<Vector2> r = BuscadorRutas.Buscar(piso.Mapa, transform.position, c);
            if (r == null) continue;
            LimpiarAmenaza();
            objetivo = null;
            destinoAproximacion = c;
            ruta = r;
            indiceRuta = 0;
            atascos = 0;
            recalculos = 0;
            posChequeo = transform.position;
            proximoChequeo = Time.time + 0.5f;
            estado = Estado.Caminando;
            return true;
        }
        return false;
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
            else if (atascos == 0 && !furia && !rabia && Time.time >= proximoCambioIdea && Random.value < 0.07f)
            {
                // Impredecible: a mitad de camino cambia de idea (y a veces directamente pega un brinco a otra zona).
                proximoCambioIdea = Time.time + Random.Range(4f, 8f);
                if (Random.value < 0.35f && Time.time >= proximoBrinco && IniciarBrinco()) return;
                if (Random.value < 0.5f) TextoFlotante.Mostrar(Centro + Vector2.up * 2.2f, Random.value < 0.5f ? "¡MEJOR ALLÁ!" : "¿?", new Color(1f, 0.9f, 0.5f), 0.9f);
                LimpiarAmenaza();
                ultimoObjetivo = objetivo;
                velDeseada = Vector2.zero;
                estado = Estado.Decidiendo;
                finEstado = Time.time + Random.Range(0.1f, 0.4f);
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
        sinCamino++;
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
        // Si un jugador estaba en el radio del puñetazo, pierde una vida y sale despedido (sin salir del piso).
        foreach (JugadorCenital j in JugadorCenital.Todos)
            if (j != null && j.isActiveAndEnabled && Vector2.Distance(j.Centro, punto) <= RadioPunetazo &&
                j.RecibirEmpujon(Centro, distanciaEmpujon * 0.75f, aturdimientoEmpujon * 0.5f, true))
                GestorAudio.Reproducir(SndExtra.GolpeFuerteJugador);

        EmpezarAnimacionGolpe();
        if (objetivo != null && objetivo == objetoRepetido) golpesRepetidos++;
        else { objetoRepetido = objetivo; golpesRepetidos = 1; }
        ultimoObjetivo = objetivo;
        golpesEnSala++;
        estado = Estado.Recuperando;
        finEstado = Time.time + pausaTrasGolpe * Random.Range(0.6f, 1.8f) / Mathf.Max(0.5f, Ritmo);
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
        if (Random.value < 0.6f) Burbuja.Decir(transform, Frases.Una(Frases.DemoledorBloqueado), Burbuja.Voz.Villano, alturaDibujo - radioCuerpo + 0.3f);
        return true;
    }

    private void UpdateAturdido()
    {
        velDeseada = Vector2.zero;
        if (hielo != null)
        {
            bool frio = Congelado;
            if (hielo.enabled != frio) hielo.enabled = frio;
            if (frio)
            {
                hielo.transform.localPosition = new Vector3(0f, -radioCuerpo + 0.45f, 0f);
                hielo.sortingOrder = Provisional.OrdenY(transform.position.y - radioCuerpo) + 3;
            }
        }
        if (zz != null && !Congelado)
        {
            if (!zz.gameObject.activeSelf) zz.gameObject.SetActive(true);
            zz.transform.localPosition = new Vector3(0.35f, alturaDibujo - radioCuerpo + 0.2f + Mathf.Sin(Time.time * 4f) * 0.08f, 0f);
        }
        if (Time.time < finEstado) return;

        if (zz != null) zz.gameObject.SetActive(false);
        if (hielo != null) hielo.enabled = false;
        finCongelado = 0f;
        estado = Estado.Decidiendo;
        finEstado = 0f;
    }

    // ------------------------------------------------------------ empujón al jugador
    /// <summary>Si el jugador se acerca demasiado, se prepara (aviso "!") y lo aleja de un manotazo.</summary>
    private void ComprobarEmpujon()
    {
        if (estado != Estado.Decidiendo && estado != Estado.Caminando && estado != Estado.Recuperando) return;
        if (golpeando || Congelado || Time.time < proximoEmpujon || piso.Rendido || piso.Terminado) return;
        if (jugador == null || !jugador.enabled || jugador.Invencible || jugador.EnElAire) return;
        if (Vector2.Distance(Centro, jugador.Centro) > radioEmpujon) return;
        IniciarEmpujon();
    }

    private void IniciarEmpujon()
    {
        velDeseada = Vector2.zero;
        Vector2 hacia = jugador.Centro - Centro;
        if (hacia.sqrMagnitude > 0.0001f) direccion = hacia.normalized;
        estado = Estado.PrepEmpujon;
        finEstado = Time.time + preparacionEmpujon / Mathf.Max(0.7f, Ritmo);
        GestorAudio.Reproducir(SndExtra.GrunidoCerca);
        if (alerta != null) alerta.enabled = true;
    }

    private void UpdatePrepEmpujon()
    {
        velDeseada = Vector2.zero;
        if (jugador != null)
        {
            Vector2 hacia = jugador.Centro - Centro;
            if (hacia.sqrMagnitude > 0.0001f) direccion = hacia.normalized;
        }
        if (alerta != null)
        {
            if (arteAlerta != null) alerta.sprite = arteAlerta[(int)(Time.time * 12f) % 2];
            alerta.transform.localPosition = new Vector3(0f, alturaDibujo - radioCuerpo + 0.45f + Mathf.Abs(Mathf.Sin(Time.time * 18f)) * 0.12f, 0f);
            alerta.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        }
        if (Time.time < finEstado) return;
        EjecutarEmpujon();
    }

    /// <summary>
    /// El choque directo con el jugador también dispara el empujón (sin aviso). Devuelve false si ahora no puede empujar
    /// (atacando otra cosa, congelado o en espera), y entonces el jugador solo se aparta un poco.
    /// </summary>
    public bool EmpujarAhora()
    {
        if (estado != Estado.Decidiendo && estado != Estado.Caminando && estado != Estado.Recuperando && estado != Estado.PrepEmpujon) return false;
        if (Congelado || Time.time < proximoEmpujon || piso == null || piso.Rendido || piso.Terminado) return false;
        EjecutarEmpujon();
        return true;
    }

    private void EjecutarEmpujon()
    {
        if (alerta != null) alerta.enabled = false;
        velDeseada = Vector2.zero;
        proximoEmpujon = Time.time + esperaEmpujon;
        GestorAudio.Reproducir(SndExtra.EmpujonDemoledor);
        EmpezarAnimacionGolpe();
        CamaraPisoCenital.Temblar(0.12f, 0.25f);

        // Si lo tiene a mano lo agarra y lo lanza lejos, a otra sala (así nadie queda encerrado recibiendo golpes).
        if (jugador != null && jugador.enabled && !jugador.EnElAire && !jugador.Invencible &&
            Vector2.Distance(Centro, jugador.Centro) <= radioEmpujon + 0.7f && IniciarAgarre(jugador)) return;

        if (jugador != null && jugador.enabled && !jugador.EnElAire && Vector2.Distance(Centro, jugador.Centro) <= radioEmpujon + 0.7f)
        {
            Vector2 punto = Vector2.Lerp(Centro, jugador.Centro, 0.6f);
            EfectoSprite.Reproducir(ArteExtra.Serie("Efectos/onda_empujon_", 4), punto, 450, 1f, 14f);
            EfectoSprite.Reproducir(ArteNiveles.De(a => a.golpeEstallido), punto, 451, 1f, 14f);
            if (jugador.RecibirEmpujon(Centro, distanciaEmpujon, aturdimientoEmpujon, true))
            {
                GestorAudio.Reproducir(SndExtra.GolpeFuerteJugador);
                string[] frases = { "¡FUERA DE AQUÍ!", "¡NO ESTORBES!", "¡LARGO!", "¡APÁRTATE!" };
                Burbuja.Decir(transform, frases[Random.Range(0, frases.Length)], Burbuja.Voz.Villano, alturaDibujo - radioCuerpo + 0.3f);
            }
        }
        estado = Estado.Recuperando;
        finEstado = Time.time + 0.55f / Mathf.Max(0.5f, Ritmo);
    }

    // ------------------------------------------------------------ congelar (reloj de hielo)
    /// <summary>Lo deja congelado (quieto, sin atacar) unos segundos. No afecta a la entrada, la rendición, la salida ni la ulti.</summary>
    public void Congelar(float segundos)
    {
        if (tipo == TipoDemoledor.Robot) segundos *= 0.5f;   // el robot se descongela rápido
        bool activo = estado == Estado.Decidiendo || estado == Estado.Caminando || estado == Estado.PrepPunetazo ||
                      estado == Estado.PrepLanzamiento || estado == Estado.Recuperando || estado == Estado.Aturdido || estado == Estado.PrepEmpujon ||
                      estado == Estado.PrepEmbestida || estado == Estado.Embistiendo || estado == Estado.PrepPisoton ||
                      estado == Estado.PrepTorbellino || estado == Estado.Torbellino || estado == Estado.PrepLaser || estado == Estado.Laser ||
                      estado == Estado.PrepBrinco || estado == Estado.Agarrando;
        if (!activo || segundos <= 0f) return;
        CancelarEspecial();
        LimpiarAmenaza();
        telegrafo.enabled = false;
        if (alerta != null) alerta.enabled = false;
        if (bloqueManos != null) bloqueManos.enabled = false;
        if (zz != null) zz.gameObject.SetActive(false);
        golpeando = false;
        velDeseada = Vector2.zero;
        estado = Estado.Aturdido;
        finCongelado = Time.time + segundos;
        finEstado = finCongelado;
        GestorAudio.Reproducir(SndExtra.Congelado);
    }

    /// <summary>Aturdido unos segundos (la onda de la furia del estudiante). Mismos estados que el congelado.</summary>
    public void Aturdir(float segundos)
    {
        bool activo = estado == Estado.Decidiendo || estado == Estado.Caminando || estado == Estado.PrepPunetazo ||
                      estado == Estado.PrepLanzamiento || estado == Estado.Recuperando || estado == Estado.PrepEmpujon ||
                      estado == Estado.PrepEmbestida || estado == Estado.Embistiendo || estado == Estado.PrepPisoton ||
                      estado == Estado.PrepTorbellino || estado == Estado.Torbellino || estado == Estado.PrepLaser || estado == Estado.Laser ||
                      estado == Estado.PrepBrinco || estado == Estado.Agarrando;
        if (!activo || segundos <= 0f) return;
        CancelarEspecial();
        LimpiarAmenaza();
        telegrafo.enabled = false;
        if (alerta != null) alerta.enabled = false;
        if (bloqueManos != null) bloqueManos.enabled = false;
        golpeando = false;
        velDeseada = Vector2.zero;
        estado = Estado.Aturdido;
        finEstado = Time.time + segundos;
        CamaraPisoCenital.Temblar(0.06f, 0.15f);
        if (Random.value < 0.5f) Burbuja.Decir(transform, Frases.Una(Frases.DemoledorBloqueado), Burbuja.Voz.Villano, alturaDibujo - radioCuerpo + 0.3f);
    }

    // ------------------------------------------------------------ pasos y gruñidos
    /// <summary>Pasos pesados (más fuertes cuanto más cerca del jugador) y algún gruñido cuando lo tiene cerca.</summary>
    private bool musicaCerca;

    private void SonidosDePresencia()
    {
        if (jugador == null) return;
        float d = Vector2.Distance(Centro, jugador.Centro);

        // Música adaptativa: la capa de latido sube cuanto más cerca está; muy cerca (o en furia) entra la pista de tensión.
        bool activo = EnPiso && !piso.Rendido && !piso.Terminado;
        float amenaza = activo ? Mathf.Clamp01(1f - (d - 1.5f) / 7f) : 0f;
        if (activo && furia) amenaza = Mathf.Max(amenaza, 0.55f);
        if (activo && Congelado) amenaza *= 0.3f;
        if (estado == Estado.Entrando) amenaza = 1f;
        GestorAudio.Amenaza(amenaza);
        bool tensionCerca = activo && !Congelado && (musicaCerca ? d < 5.5f : d < 3.8f);
        if (tensionCerca != musicaCerca)
        {
            musicaCerca = tensionCerca;
            TensionPropia("cerca", tensionCerca);
        }

        bool camina = (estado == Estado.Caminando || estado == Estado.Entrando || estado == Estado.Saliendo) && velDeseada.sqrMagnitude > 0.05f;
        if (camina && Time.time >= proximoPasoPesado)
        {
            float cerca = Mathf.Clamp01(1f - d / Mathf.Max(1f, distanciaOido));
            if (cerca > 0f) GestorAudio.Reproducir(SndExtra.PasoDemoledor, 0.25f + 0.75f * cerca);
            if (d < 3.5f) CamaraPisoCenital.Temblar(0.015f, 0.06f);
            proximoPasoPesado = Time.time + 0.42f / Mathf.Max(0.5f, Ritmo * VelocidadFuria);
        }
        if (EnPiso && !Congelado && d < 4f && Time.time >= proximoGrunido)
        {
            GestorAudio.Reproducir(SndExtra.GrunidoCerca, 0.7f);
            proximoGrunido = Time.time + Random.Range(4f, 7f);
        }
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
                   estado != Estado.Saliendo && estado != Estado.Saltando && estado != Estado.Ido && estado != Estado.Detenido &&
                   estado != Estado.Escapando && estado != Estado.Brinco;   // sin esto, al escapar se volvía a rendir una y otra vez (bucle)
        }
    }

    /// <summary>Energía en 0: deja de atacar, avisa que se rinde y se prepara para irse.</summary>
    private void IniciarRendicion()
    {
        CancelarEspecial();
        LimpiarAmenaza();
        telegrafo.enabled = false;
        if (zz != null) zz.gameObject.SetActive(false);
        EscombroCenital.EliminarTodos();
        AvisoUlti.Ocultar();
        if (bloqueManos != null) bloqueManos.enabled = false;
        golpeando = false;
        furia = false;
        if (rabia) TensionPropia("rabia", false);
        rabia = false;
        if (auraRabia != null) auraRabia.enabled = false;
        velDeseada = Vector2.zero;
        ruta = null;
        objetivo = null;
        colision.enabled = false;   // ya no empuja ni choca con el jugador
        if (alerta != null) alerta.enabled = false;
        if (hielo != null) hielo.enabled = false;
        finCongelado = 0f;
        TextoFlotante.Mostrar(Centro + Vector2.up * 2.4f, "¡ME RINDO!", new Color(1f, 0.95f, 0.5f), 2.2f);
        Burbuja.Decir(transform, Random.value < 0.5f ? "¡ESTO NO SE QUEDA ASÍ!" : "¡NOS VEMOS ARRIBA!", Burbuja.Voz.Villano, alturaDibujo - radioCuerpo + 0.3f);
        GestorAudio.Reproducir(Snd.MeRindo);
        TensionPropia("cerca", false);
        musicaCerca = false;
        TensionPropia("furia", false);
        TensionPropia("ulti", false);
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
        float rapidez = velocidad * Ritmo * (def != null ? def.velocidadSalida : 1.3f) * (saltar ? 1.5f : 1f) * 1.7f;
        if (Time.time >= proximoSudor)
        {
            proximoSudor = Time.time + 0.12f;
            Sprite g1 = ArteExtra.Uno("Peligros/gota");
            if (g1 != null)
                ParticulaSimple.Crear(new[] { g1 }, Centro + Vector2.up * 1.3f + Random.insideUnitCircle * 0.2f,
                    new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(1f, 2f)), 0.5f, Provisional.OrdenY(transform.position.y) + 5, new Color(0.7f, 0.9f, 1f), 1f, 0.6f, 6f);
            EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), (Vector2)transform.position - direccion * 0.3f, Provisional.OrdenY(transform.position.y) - 1, 0.5f, 16f);
        }
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
                IniciarEscape();
                return;
            }
        }

        Vector2 d = meta - (Vector2)transform.position;
        direccion = d.normalized;
        velDeseada = direccion * rapidez;
    }

    // ------------------------------------------------------------ escape: súper salto hacia el piso de arriba
    private float inicioEscape, proximoSudor, finPoseEntrada;
    private bool escapeLanzado;

    private void IniciarEscape()
    {
        estado = Estado.Escapando;
        inicioEscape = Time.time;
        escapeLanzado = false;
        origenSalto = transform.position;
        GestorAudio.Reproducir(SndExtra.EmbestidaCarga);
    }

    /// <summary>Se agacha, salta atravesando el techo girando (caen trozos del techo) y desaparece hacia arriba.</summary>
    private void UpdateEscapando()
    {
        velDeseada = Vector2.zero;
        float t = Time.time - inicioEscape;
        if (t < 0.35f) return;
        if (!escapeLanzado)
        {
            escapeLanzado = true;
            GestorAudio.Reproducir(Snd.SaltoTerraza);
            GestorAudio.Reproducir(SndExtra.ChoqueMuro);
            CamaraPisoCenital.Temblar(0.22f, 0.5f);
            SpriteRenderer onda = EfectoSprite.Reproducir(ArteExtra.Serie("Efectos/onda_empujon_", 4), origenSalto, 455, 2.2f, 12f);
            if (onda != null) onda.color = new Color(1f, 0.9f, 0.6f);
            EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), origenSalto, 456, 1.8f, 12f);
            TextoFlotante.Mostrar(origenSalto + Vector2.up * 2.4f, "¡NOS VEMOS ARRIBA!", new Color(1f, 0.85f, 0.4f), 1.8f);
            // Del agujero del techo caen trozos alrededor.
            for (int i = 0; i < 4; i++) TrozoVolador.Lanzar(origenSalto + Random.insideUnitCircle * 1.2f + Vector2.up * 0.5f, 3, 460);
            MarcaTemporal.Crear(Provisional.Grietas(3), origenSalto, 1.3f, 12f);
        }
        float k = Mathf.Clamp01((t - 0.35f) / 0.7f);
        elevacion = 7f * k * k;
        alphaSalto = 1f - Mathf.Clamp01((k - 0.6f) / 0.4f);
        if (Time.time >= proximaEstelaEmb && dibujo != null && dibujo.sprite != null)
        {
            proximaEstelaEmb = Time.time + 0.04f;
            EstelaFantasma.Crear(dibujo, new Color(1f, 0.85f, 0.5f, 0.45f), 0.25f);
        }
        if (k >= 1f)
        {
            if (dibujo != null) dibujo.enabled = false;
            alphaSalto = 1f;
            elevacion = 0f;
            estado = Estado.Ido;
            piso.DemoledorSeFue();
        }
    }



    // ------------------------------------------------------------ agarre: te levanta y te lanza a otra sala
    private const float DuracionAgarre = 0.5f;
    private JugadorCenital agarrado;
    private Vector2 destinoLanzamiento;
    private float inicioAgarre;

    /// <summary>Agarra al jugador si encuentra dónde lanzarlo (otra sala, lejos de los villanos y, en multijugador,
    /// lejos del compañero para no lanzarlos uno encima del otro). False si no hay dónde.</summary>
    private bool IniciarAgarre(JugadorCenital j)
    {
        Vector2 destino;
        if (!DestinoLanzamiento(j, out destino)) return false;
        agarrado = j;
        destinoLanzamiento = destino;
        inicioAgarre = Time.time;
        // El agarrón cuesta una vida (con la invulnerabilidad y el casco de siempre).
        bool perdio = j.RecibirGolpe();
        j.SerAgarrado(transform, alturaDibujo - radioCuerpo + 0.15f);
        Vector2 hacia = destino - Centro;
        if (hacia.sqrMagnitude > 0.001f) direccion = hacia.normalized;
        estado = Estado.Agarrando;
        finEstado = Time.time + DuracionAgarre;
        GestorAudio.Reproducir(perdio ? SndExtra.GolpeFuerteJugador : SndExtra.GrunidoCerca);
        string[] frases = { "¡TE TENGO!", "¡A VOLAR!", "¡FUERA DE MI PISO!", "¡LARGO DE AQUÍ!" };
        Burbuja.Decir(transform, frases[Random.Range(0, frases.Length)], Burbuja.Voz.Villano, alturaDibujo - radioCuerpo + 0.9f);
        return true;
    }

    private void UpdateAgarre()
    {
        velDeseada = Vector2.zero;
        if (agarrado == null || !agarrado.isActiveAndEnabled) { agarrado = null; estado = Estado.Recuperando; finEstado = Time.time + 0.3f; return; }
        if (Time.time < finEstado) return;
        // ¡Lo lanza!
        float d = Vector2.Distance(agarrado.transform.position, destinoLanzamiento);
        agarrado.SerLanzado(destinoLanzamiento, Mathf.Clamp(0.55f + d * 0.05f, 0.7f, 1.2f), Mathf.Clamp(1.8f + d * 0.15f, 2f, 3.6f));
        agarrado = null;
        GestorAudio.Reproducir(SndExtra.EmpujonDemoledor);
        CamaraPisoCenital.Temblar(0.1f, 0.2f);
        EmpezarAnimacionGolpe();
        proximoEmpujon = Time.time + esperaEmpujon + 1.5f;
        estado = Estado.Recuperando;
        finEstado = Time.time + 0.6f / Mathf.Max(0.5f, Ritmo);
    }

    /// <summary>Una celda libre en otra sala, a 5-13 baldosas, lejos de todos los villanos y de los otros jugadores.</summary>
    private bool DestinoLanzamiento(JugadorCenital j, out Vector2 destino)
    {
        destino = Vector2.zero;
        MapaCenital m = piso.Mapa;
        List<Vector2> celdas = m.CentrosTransitables;
        if (celdas == null || celdas.Count == 0) return false;
        int salaYo = SalasMapa.De(m, Centro);
        HashSet<int> salasVetadas = SalasDeOtros();
        if (salaYo >= 0) salasVetadas.Add(salaYo);
        List<Vector2> otros = new List<Vector2>();
        foreach (JugadorCenital o in JugadorCenital.Todos)
            if (o != null && o != j && o.isActiveAndEnabled)
            {
                otros.Add(o.transform.position);
                int so = SalasMapa.De(m, o.transform.position);
                if (so >= 0) salasVetadas.Add(so);
            }
        List<Vector2> villanos = new List<Vector2>();
        foreach (DemoledorCenital dem in Todos) if (dem != null && dem.isActiveAndEnabled && dem.EnPiso) villanos.Add(dem.Centro);
        foreach (MiniVillano mv in MiniVillano.Activos) if (mv != null) villanos.Add(mv.transform.position);

        float mejor = float.MinValue;
        bool hay = false;
        for (int pasada = 0; pasada < 2 && !hay; pasada++)
        {
            for (int intento = 0; intento < 50; intento++)
            {
                Vector2 c = celdas[Random.Range(0, celdas.Count)];
                float d = Vector2.Distance(c, Centro);
                if (d < (pasada == 0 ? 5f : 3.5f) || d > 13f) continue;
                int s = SalasMapa.De(m, c);
                if (pasada == 0 && (s < 0 || salasVetadas.Contains(s))) continue;
                if (ParedEn(c, 0.35f)) continue;
                float lejos = 99f;
                foreach (Vector2 v in villanos) lejos = Mathf.Min(lejos, Vector2.Distance(v, c));
                if (lejos < 3f) continue;
                bool cercaDeOtro = false;
                foreach (Vector2 o in otros) if (Vector2.Distance(o, c) < 3.5f) cercaDeOtro = true;
                if (cercaDeOtro) continue;
                float puntaje = Mathf.Min(lejos, 8f) + Random.value * 2f;
                if (puntaje > mejor) { mejor = puntaje; destino = c; hay = true; }
            }
        }
        return hay;
    }

    // ------------------------------------------------------------ gran brinco: salta a otra zona del piso
    // Si el jugador lo encierra (repara una y otra vez lo mismo, lo bloquea en un cubículo o no encuentra camino),
    // el demoledor pega un gran brinco por encima de los muebles y cae en otra sala, rompiendo lo que tenga al lado.
    // También lo hace de vez en cuando, para que no sea predecible.
    private const float PrepBrincoSeg = 0.5f;
    private float proximoBrinco, proximoBrincoAzar, proximoCambioIdea, inicioBrinco, durBrinco, inicioEnSala;
    private Vector2 brincoDesde, brincoHasta;
    private ObjetoReparable objetoRepetido;
    private int golpesRepetidos, sinCamino, salaSeguida = -2;
    private SpriteRenderer marcaBrinco;

    /// <summary>Lleva la cuenta de cuánto tiempo seguido está en la misma sala.</summary>
    private void SeguirSala()
    {
        if (!EnPiso || piso == null || piso.Mapa == null) return;
        int s = SalasMapa.De(piso.Mapa, Centro);
        if (s < 0) return;   // en una puerta o pasillo no cuenta
        if (s != salaSeguida) { salaSeguida = s; inicioEnSala = Time.time; }
    }

    private bool DebeBrincar()
    {
        if (Time.time < proximoBrinco || Congelado || piso.Rendido) return false;
        if (golpesRepetidos >= 3) return true;                                   // le reparan lo mismo una y otra vez
        if (sinCamino >= 2) return true;                                         // encerrado: no llega a nada
        if (salaSeguida >= 0 && Time.time - inicioEnSala > 20f) return true;     // demasiado tiempo en la misma sala
        if (Time.time >= proximoBrincoAzar && Random.value < 0.12f)              // de vez en cuando, por sorpresa
        {
            proximoBrincoAzar = Time.time + Random.Range(14f, 24f);
            return true;
        }
        return false;
    }

    /// <summary>Elige dónde caer: otra sala (sin otro villano), con cosas sanas para romper, a 4-15 baldosas.</summary>
    private bool ElegirDestinoBrinco(out Vector2 destino)
    {
        destino = Vector2.zero;
        MapaCenital m = piso.Mapa;
        List<Vector2> celdas = m.CentrosTransitables;
        if (celdas == null || celdas.Count == 0) return false;
        int salaYo = SalasMapa.De(m, Centro);
        HashSet<int> ocupadas = SalasDeOtros();
        Dictionary<int, int> sanos = new Dictionary<int, int>();
        foreach (ObjetoReparable o in m.objetos)
        {
            if (o == null || o.Estado == EstadoObjeto.Roto) continue;
            int so = SalasMapa.DeObjeto(m, o);
            int n;
            sanos.TryGetValue(so, out n);
            sanos[so] = n + 1;
        }
        float mejor = float.MinValue;
        bool hay = false;
        for (int pasada = 0; pasada < 2 && !hay; pasada++)
        {
            for (int intento = 0; intento < 40; intento++)
            {
                Vector2 c = celdas[Random.Range(0, celdas.Count)];
                float d = Vector2.Distance(c, Centro);
                if (d < (pasada == 0 ? 4f : 2.5f) || d > 15f) continue;
                int s = SalasMapa.De(m, c);
                if (pasada == 0 && (s < 0 || (s == salaYo && salaYo >= 0) || ocupadas.Contains(s))) continue;
                // No cae pegado a una pared ni encima de un estudiante.
                if (ParedEn(c, 0.4f)) continue;
                bool encima = false;
                foreach (JugadorCenital j in JugadorCenital.Todos)
                    if (j != null && j.isActiveAndEnabled && Vector2.Distance(j.transform.position, c) < 1f) encima = true;
                if (encima) continue;
                int n;
                sanos.TryGetValue(s, out n);
                float puntaje = n + Random.value * 3f;
                if (puntaje > mejor) { mejor = puntaje; destino = c; hay = true; }
            }
        }
        return hay;
    }

    /// <summary>Empieza el gran brinco (se agacha y marca en el piso dónde va a caer). False si no hay dónde.</summary>
    private bool IniciarBrinco()
    {
        Vector2 destino;
        if (!ElegirDestinoBrinco(out destino)) { proximoBrinco = Time.time + 3f; sinCamino = 0; return false; }
        CancelarEspecial();
        LimpiarAmenaza();
        if (telegrafo != null) telegrafo.enabled = false;
        objetivo = null;
        ruta = null;
        velDeseada = Vector2.zero;
        brincoHasta = destino;
        Vector2 hacia = destino - Centro;
        if (hacia.sqrMagnitude > 0.001f) direccion = hacia.normalized;
        estado = Estado.PrepBrinco;
        finEstado = Time.time + PrepBrincoSeg / Mathf.Max(0.8f, Ritmo);
        golpesRepetidos = 0;
        sinCamino = 0;
        objetoRepetido = null;
        GestorAudio.Reproducir(SndExtra.PisotonSalto, 0.9f, 0.85f);
        TextoFlotante.Mostrar(Centro + Vector2.up * 2.4f, Random.value < 0.5f ? "¡GRAN BRINCO!" : "¡ME VOY DE AQUÍ!", new Color(1f, 0.75f, 0.25f), 1.1f);
        if (marcaBrinco == null)
        {
            Sprite s = ArteExtra.Uno("Efectos/aro_pisoton");
            marcaBrinco = Provisional.Crear(null, "MarcaBrinco", s != null ? s : Provisional.Aro, Color.white, -115);
            marcaBrinco.transform.SetParent(transform.parent, true);
        }
        marcaBrinco.transform.position = destino;
        marcaBrinco.enabled = true;
        return true;
    }

    private void UpdatePrepBrinco()
    {
        velDeseada = Vector2.zero;
        PulsarMarcaBrinco();
        if (Time.time < finEstado) return;
        // ¡Despega!
        brincoDesde = cuerpo != null ? cuerpo.position : (Vector2)transform.position;
        float d = Vector2.Distance(brincoDesde, brincoHasta);
        durBrinco = Mathf.Clamp(0.6f + d * 0.045f, 0.7f, 1.25f);
        inicioBrinco = Time.time;
        colision.enabled = false;   // pasa por encima de muebles y personas
        estado = Estado.Brinco;
        GestorAudio.Reproducir(Snd.SaltoTerraza, 0.8f);
        EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), brincoDesde, Provisional.OrdenY(brincoDesde.y) - 1, 1.2f, 12f);
        CamaraPisoCenital.Temblar(0.06f, 0.15f);
    }

    private void PulsarMarcaBrinco()
    {
        if (marcaBrinco == null || !marcaBrinco.enabled) return;
        Sprite s = marcaBrinco.sprite;
        float tam = s != null ? Mathf.Max(0.01f, s.bounds.size.x) : 1f;
        float e = 2.4f / tam * (0.9f + 0.1f * Mathf.Sin(Time.time * 12f));
        marcaBrinco.transform.localScale = new Vector3(e, e * 0.8f, 1f);
        marcaBrinco.color = new Color(1f, 0.35f, 0.2f, 0.45f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 14f)));
    }

    private void UpdateBrinco()
    {
        velDeseada = Vector2.zero;
        PulsarMarcaBrinco();
        float k = Mathf.Clamp01((Time.time - inicioBrinco) / durBrinco);
        Vector2 pos = Vector2.Lerp(brincoDesde, brincoHasta, k);
        transform.position = pos;
        if (cuerpo != null) cuerpo.position = pos;
        float d = Vector2.Distance(brincoDesde, brincoHasta);
        elevacion = Mathf.Sin(k * Mathf.PI) * Mathf.Clamp(1.6f + d * 0.18f, 2f, 3.6f);
        if (Time.time >= proximaEstelaEmb && dibujo != null && dibujo.sprite != null)
        {
            proximaEstelaEmb = Time.time + 0.05f;
            EstelaFantasma.Crear(dibujo, new Color(1f, 0.8f, 0.45f, 0.35f), 0.22f);
        }
        if (k < 1f) return;
        Aterrizar();
    }

    /// <summary>Cae de golpe: onda, polvo, grieta, daña lo que tenga al lado y empuja a los que estén cerca.</summary>
    private void Aterrizar()
    {
        elevacion = 0f;
        colision.enabled = true;
        if (marcaBrinco != null) marcaBrinco.enabled = false;
        finAplastePisoton = Time.time + 0.3f;
        Vector2 c = Centro;
        GestorAudio.Reproducir(SndExtra.PisotonImpacto, 0.9f);
        CamaraPisoCenital.Temblar(0.2f, 0.4f);
        SpriteRenderer onda = EfectoSprite.Reproducir(ArteExtra.Serie("Efectos/onda_empujon_", 4), c, 455, 2f, 12f);
        if (onda != null) onda.color = new Color(1f, 0.7f, 0.35f);
        EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), c, 454, 1.6f, 10f);
        MarcaTemporal.Crear(Provisional.Grietas(2), (Vector2)transform.position, 1.1f, 8f);
        // El impacto daña lo que esté pegado (como mucho dos cosas).
        int danados = 0;
        foreach (ObjetoReparable o in piso.Mapa.objetos)
        {
            if (danados >= 2) break;
            if (o == null || o.Estado == EstadoObjeto.Roto) continue;
            if (Vector2.Distance(c, o.PuntoMasCercano(c)) > 1.3f) continue;
            if (o.Danar()) { piso.RegistrarGolpe(); danados++; }
        }
        foreach (JugadorCenital j in JugadorCenital.Todos)
            if (j != null && j.isActiveAndEnabled && Vector2.Distance(j.Centro, c) < 1.7f)
                j.RecibirEmpujon(c, distanciaEmpujon * 0.6f, aturdimientoEmpujon * 0.4f, false);
        if (Random.value < 0.4f) Burbuja.Decir(transform, Random.value < 0.5f ? "¡SORPRESA!" : "¡AQUÍ ESTOY!", Burbuja.Voz.Villano, alturaDibujo - radioCuerpo + 0.3f);
        proximoBrinco = Time.time + Random.Range(9f, 15f);
        salaSeguida = -2;
        ultimoObjetivo = null;
        estado = Estado.Recuperando;
        finEstado = Time.time + 0.55f;
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
        if (golpeando || Congelado) return;
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
        TensionPropia("ulti", true);
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
        GolpearJugadoresEn(punto, def.radioImpactoRoca);
    }

    private void IniciarAgotado()
    {
        estado = Estado.Agotado;
        velDeseada = Vector2.zero;
        float agotado = Mathf.Min(def.duracionAgotado, 2f);   // descanso corto: enseguida entra en rabia
        finEstado = Time.time + agotado;
        AvisoUlti.Mostrar("¡ESTÁ AGOTADO!", agotado, false, new Color(0.6f, 1f, 0.6f));
        GestorAudio.Reproducir(Snd.Agotado);
        TensionPropia("ulti", false);   // terminó la lluvia: vuelve la música del piso si ya no está en furia
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

    // ------------------------------------------------------------ aspecto: variante, poses y accesorios
    private SpriteRenderer casco;
    private float finAplastePisoton, proximoVapor;
    private Sprite[] cuadrosVapor;

    /// <summary>Tono del cuerpo: el segundo demoledor (multijugador) es azulado para distinguirlo.</summary>
    private Color Variante { get { return Color.white; } }

    /// <summary>
    /// Poses por código sobre el dibujo (sin cuadros nuevos): agachado y luego inclinado al embestir, estirado al saltar y
    /// aplastado al caer del pisotón, tambaleo cuando está mareado, temblor en rabia. Con dos demoledores, cada uno lleva su
    /// casco (amarillo el primero, rojo el segundo). En rabia le sale vapor de la cabeza.
    /// </summary>
    private void PosesYAccesorios(Bounds b, Vector3 tam)
    {
        Transform t = dibujo.transform;
        float giro = 0f;
        Vector3 escala = t.localScale;
        float lado = mirandoIzquierda ? 1f : -1f;
        switch (estado)
        {
            case Estado.PrepEmbestida:
                escala.y *= 0.84f; escala.x *= 1.12f;
                giro = lado * 6f + Mathf.Sin(Time.time * 40f) * 2f;
                break;
            case Estado.Embistiendo:
                escala.x *= 1.1f; escala.y *= 0.94f;
                giro = (Mathf.Abs(dirEmbestida.x) > 0.2f ? -Mathf.Sign(dirEmbestida.x) : 0f) * 14f;
                break;
            case Estado.PrepPisoton:
                escala.y *= 1f + 0.12f * Mathf.Clamp01(elevacion);
                escala.x *= 1f - 0.06f * Mathf.Clamp01(elevacion);
                break;
            case Estado.Aturdido:
                if (!Congelado) giro = Mathf.Sin(Time.time * 9f) * 8f;
                break;
            case Estado.Recuperando:
                if (Time.time < finPoseEntrada)
                {
                    // Se golpea el pecho: se infla y se encoge rápido.
                    float g = Mathf.Abs(Mathf.Sin(Time.time * 16f));
                    escala.x *= 1f + 0.12f * g; escala.y *= 1f + 0.08f * g;
                    giro = Mathf.Sin(Time.time * 8f) * 4f;
                }
                break;
            case Estado.Rindiendo:
                // Mareado y agotado: se tambalea.
                giro = Mathf.Sin(Time.time * 5f) * 10f;
                escala.y *= 0.94f + 0.04f * Mathf.Sin(Time.time * 3f);
                break;
            case Estado.Saliendo:
                // Huye en pánico: inclinado hacia adelante y con saltitos.
                giro = -Mathf.Sign(direccion.x == 0f ? 1f : direccion.x) * 10f + Mathf.Sin(Time.time * 26f) * 4f;
                break;
            case Estado.Escapando:
            {
                float te = Time.time - inicioEscape;
                if (te < 0.35f) { float k = te / 0.35f; escala.x *= 1f + 0.25f * k; escala.y *= 1f - 0.3f * k; }   // se agacha
                else { float k = Mathf.Clamp01((te - 0.35f) / 0.7f); escala *= 1f + 0.5f * k; giro = k * 720f; }   // sale disparado girando
                break;
            }
            case Estado.PrepTorbellino:
                giro = Mathf.Sin(Time.time * 30f) * 12f;
                break;
            case Estado.Agarrando:
                // Lo levanta sobre la cabeza, tomando impulso.
                escala.y *= 1.06f + 0.04f * Mathf.Sin(Time.time * 20f);
                giro = lado * 8f * Mathf.Clamp01((Time.time - inicioAgarre) / DuracionAgarre);
                break;
            case Estado.PrepBrinco:
            {
                // Se agacha cargando el salto (tiembla de fuerza).
                float kb = Mathf.Clamp01(1f - (finEstado - Time.time) / Mathf.Max(0.05f, PrepBrincoSeg));
                escala.y *= 1f - 0.25f * kb; escala.x *= 1f + 0.18f * kb;
                giro = Mathf.Sin(Time.time * 45f) * 3f;
                break;
            }
            case Estado.Brinco:
            {
                // Estirado al subir, hecho bolita y girando en lo más alto, estirado al caer.
                float kb = Mathf.Clamp01((Time.time - inicioBrinco) / Mathf.Max(0.05f, durBrinco));
                escala.y *= 1f + 0.18f * Mathf.Abs(Mathf.Cos(kb * Mathf.PI)); escala.x *= 1f - 0.08f * Mathf.Abs(Mathf.Cos(kb * Mathf.PI));
                giro = -Mathf.Sign(brincoHasta.x - brincoDesde.x + 0.001f) * 360f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.75f, kb));
                break;
            }
            case Estado.Torbellino:
                // Gira como trompo: se voltea de lado a lado muy rápido.
                escala.x *= Mathf.Cos(Time.time * 28f);
                break;
            case Estado.PrepLaser:
                escala.y *= 1f + 0.05f * Mathf.Sin(Time.time * 40f);
                break;
            case Estado.Entrando:
                if (!impactoEntrada)
                {
                    // Vuela como un torpedo: el cuerpo acostado con la cabeza hacia adelante.
                    giro = Mathf.Atan2(dirEntrada.y, dirEntrada.x) * Mathf.Rad2Deg - 90f + Mathf.Sin(Time.time * 35f) * 5f;
                    escala.x *= 0.92f; escala.y *= 1.08f;
                }
                else
                {
                    float k = Mathf.Clamp01((Time.time - inicioDerrape) / 0.35f);
                    giro = Mathf.LerpAngle(Mathf.Atan2(dirEntrada.y, dirEntrada.x) * Mathf.Rad2Deg - 90f, 0f, k);
                }
                break;
        }
        if (Time.time < finAplastePisoton)
        {
            float k = (finAplastePisoton - Time.time) / 0.3f;
            escala.y *= 1f - 0.28f * k;
            escala.x *= 1f + 0.2f * k;
        }
        if (rabia && EnPiso && estado != Estado.Aturdido) giro += Mathf.Sin(Time.time * 55f) * 1.2f;
        t.localScale = escala;
        t.localRotation = Quaternion.Euler(0f, 0f, giro);

        // Altura de la cabeza (aprox.) en coordenadas del demoledor.
        float cabeza = t.localPosition.y + b.max.y * escala.y;

        // Casco: solo cuando hay más de un demoledor, para saber cuál es cuál.
        bool conCasco = false;   // ya no hay cascos: cada villano se distingue por su vestimenta
        if (conCasco && casco == null)
        {
            Sprite s = ArteExtra.Uno(numero == 0 ? "Mejoras/casco_obra" : "Mejoras/casco_rojo");
            if (s != null) casco = Provisional.Crear(t, "Casco", s, Color.white, 0);
        }
        if (casco != null)
        {
            casco.enabled = conCasco && dibujo.enabled;
            if (casco.enabled)
            {
                // Hijo del dibujo: se inclina y se aplasta con él. Se compensa la escala para que mida ~0,9 baldosas.
                Vector3 l = t.localScale;
                float ancho = 0.95f;
                float sx = ancho / Mathf.Max(0.01f, casco.sprite.bounds.size.x) / Mathf.Max(0.01f, Mathf.Abs(l.x));
                float sy = ancho / Mathf.Max(0.01f, casco.sprite.bounds.size.x) / Mathf.Max(0.01f, Mathf.Abs(l.y));
                casco.transform.localScale = new Vector3(sx, sy, 1f);
                casco.transform.localPosition = new Vector3(dibujo.flipX ? -b.center.x : b.center.x, b.max.y - 0.16f / Mathf.Max(0.01f, Mathf.Abs(l.y)), 0f);
                casco.flipX = dibujo.flipX;
                casco.sortingOrder = dibujo.sortingOrder + 1;
                Color c = dibujo.color;
                casco.color = new Color(1f, 1f, 1f, c.a);
            }
        }

        // Vapor de rabia.
        if (rabia && EnPiso && Time.time >= proximoVapor)
        {
            proximoVapor = Time.time + Random.Range(0.12f, 0.22f);
            if (cuadrosVapor == null) cuadrosVapor = ArteExtra.Serie("Mejoras/vapor_", 4);
            Vector2 pos = (Vector2)transform.position + new Vector2(Random.Range(-0.35f, 0.35f), cabeza - 0.1f);
            ParticulaSimple.Crear(cuadrosVapor, pos, new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(1.2f, 2f)),
                Random.Range(0.5f, 0.8f), dibujo.sortingOrder + 2, new Color(1f, 0.85f, 0.85f, 0.85f), 0.6f, 1.3f);
        }
    }

    // ------------------------------------------------------------ movimientos especiales
    [Header("Movimientos especiales")]
    [Tooltip("Segundos (aprox.) entre movimientos especiales. En rabia, más seguidos.")]
    public float esperaEspeciales = 11f;
    [Tooltip("EMBESTIDA: se agacha, marca una franja con flechas y carga en línea recta rompiendo todo a su paso.")]
    public float distanciaEmbestida = 6.5f, velocidadEmbestida = 9.5f, preparacionEmbestida = 0.8f;
    [Tooltip("PISOTÓN SÍSMICO: salta y cae haciendo temblar el piso; daña lo que haya en el radio y aturde a los jugadores.")]
    public float radioPisoton = 2.7f, preparacionPisoton = 0.85f;

    private float proximoEspecial;
    private Vector2 dirEmbestida, origenEmbestida;
    private float finEmbestida, proximaEstelaEmb;
    private readonly HashSet<ObjetoReparable> golpeadosEmb = new HashSet<ObjetoReparable>();
    private readonly HashSet<JugadorCenital> jugadoresEmb = new HashSet<JugadorCenital>();
    private SpriteRenderer franjaEmbestida, aroPisoton;

    private bool EmbestidaDisponible { get { return rabia || (def != null && def.numero >= 1) || piso.Energia < 0.75f; } }
    private bool PisotonDisponible { get { return rabia || (def != null && def.numero >= 2) || piso.Energia < 0.55f; } }

    /// <summary>A veces, en vez de caminar o golpear, usa un movimiento especial.</summary>
    private bool IntentarEspecial()
    {
        if (Time.time < proximoEspecial || piso.Rendido || piso.Terminado || Congelado || golpeando) return false;
        if (tipo == TipoDemoledor.Demoledora && jugador != null && jugador.isActiveAndEnabled &&
            Vector2.Distance(Centro, jugador.Centro) <= 8f && Random.value < 0.7f)
        {
            IniciarTorbellino();
            return true;
        }
        if (tipo == TipoDemoledor.Robot && Random.value < 0.75f && IniciarLaser()) return true;
        bool jugadorCerca = jugador != null && jugador.isActiveAndEnabled && Vector2.Distance(Centro, jugador.Centro) <= radioPisoton;
        if (PisotonDisponible && Random.value < 0.6f)
        {
            int cerca = 0;
            foreach (ObjetoReparable o in piso.Mapa.objetos)
                if (o != null && o.Estado != EstadoObjeto.Roto && Vector2.Distance(Centro, o.PuntoMasCercano(Centro)) <= radioPisoton) cerca++;
            if (jugadorCerca || cerca >= 3) { IniciarPisoton(); return true; }
        }
        if (EmbestidaDisponible && jugador != null && jugador.isActiveAndEnabled)
        {
            Vector2 hacia = jugador.Centro - Centro;
            float d = hacia.magnitude;
            if (d >= 2.2f && d <= distanciaEmbestida + 1f && LineaLibre(Centro, hacia.normalized, Mathf.Min(d, distanciaEmbestida)) && Random.value < 0.7f)
            {
                IniciarEmbestida(hacia.normalized);
                return true;
            }
        }
        // Sin jugador a tiro: a veces embiste igual hacia lo que haya más sano delante.
        if (EmbestidaDisponible && Random.value < 0.25f)
        {
            ObjetoReparable mejor = null;
            float md = float.MaxValue;
            foreach (ObjetoReparable o in piso.Mapa.objetos)
            {
                if (o == null || o.Estado != EstadoObjeto.Intacto) continue;
                Vector2 h = o.Centro - Centro;
                float d = h.magnitude;
                if (d < 2.5f || d > distanciaEmbestida || !LineaLibre(Centro, h.normalized, d - 0.6f)) continue;
                if (d < md) { md = d; mejor = o; }
            }
            if (mejor != null) { IniciarEmbestida((mejor.Centro - Centro).normalized); return true; }
        }
        proximoEspecial = Time.time + 1.5f;   // vuelve a probar en un rato
        return false;
    }

    private void SiguienteEspecial()
    {
        proximoEspecial = Time.time + esperaEspeciales * Random.Range(0.75f, 1.3f) / (rabia ? 1.7f : furia ? 1.25f : 1f);
    }

    /// <summary>¿Hay pared en la línea? (los objetos no cuentan: los atraviesa rompiéndolos).</summary>
    private bool LineaLibre(Vector2 desde, Vector2 dir, float largo)
    {
        RaycastHit2D[] golpes = Physics2D.CircleCastAll(desde, radioCuerpo * 0.8f, dir, largo);
        foreach (RaycastHit2D g in golpes)
        {
            if (g.collider == null || g.collider.isTrigger || g.collider == colision) continue;
            if (g.distance <= 0.0001f && Vector2.Dot(g.normal, dir) > -0.2f) continue;   // ya pegado, pero no de frente
            if (g.collider.GetComponentInParent<ObjetoReparable>() != null || g.collider.GetComponentInParent<JugadorCenital>() != null ||
                g.collider.GetComponentInParent<DemoledorCenital>() != null || g.collider.GetComponentInParent<MiniVillano>() != null ||
                g.collider.GetComponentInParent<EstudianteNPC>() != null) continue;
            return false;
        }
        return true;
    }

    private void CancelarEspecial()
    {
        if (lineaLaser != null) lineaLaser.enabled = false;
        if (aroTorbellino != null) aroTorbellino.enabled = false;
        if (franjaEmbestida != null) franjaEmbestida.enabled = false;
        if (aroPisoton != null) aroPisoton.enabled = false;
        if (colision != null && colision.isTrigger) colision.isTrigger = false;
        if (estado == Estado.PrepPisoton) elevacion = 0f;
        if (marcaBrinco != null) marcaBrinco.enabled = false;
        if (estado == Estado.PrepBrinco || estado == Estado.Brinco) elevacion = 0f;
        if (agarrado != null) { agarrado.SoltarAgarre(); agarrado = null; }
    }

    /// <summary>¿Hay una pared (u otro sólido que no sea un objeto, un personaje o un enemigo) en ese círculo?</summary>
    private bool ParedEn(Vector2 punto, float radio)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(punto, radio))
        {
            if (c == null || c.isTrigger || c == colision) continue;
            if (c.GetComponentInParent<ObjetoReparable>() != null || c.GetComponentInParent<JugadorCenital>() != null ||
                c.GetComponentInParent<DemoledorCenital>() != null || c.GetComponentInParent<MiniVillano>() != null ||
                c.GetComponentInParent<EstudianteNPC>() != null) continue;
            return true;
        }
        return false;
    }

    // --- embestida
    private void IniciarEmbestida(Vector2 dir)
    {
        velDeseada = Vector2.zero;
        dirEmbestida = dir;
        direccion = dir;
        estado = Estado.PrepEmbestida;
        finEstado = Time.time + preparacionEmbestida / Mathf.Max(0.7f, Ritmo);
        SiguienteEspecial();
        GestorAudio.Reproducir(SndExtra.EmbestidaCarga);
        TextoFlotante.Mostrar(Centro + Vector2.up * 2.2f, "¡EMBESTIDA!", new Color(1f, 0.6f, 0.2f), 1.2f);
        if (Random.value < 0.4f) Burbuja.Decir(transform, Frases.Una(Frases.Demoledor), Burbuja.Voz.Villano, alturaDibujo - radioCuerpo + 0.3f);
        if (franjaEmbestida == null)
        {
            Sprite s = ArteExtra.Uno("Efectos/embestida_flecha");
            franjaEmbestida = Provisional.Crear(transform, "FranjaEmbestida", s != null ? s : Provisional.Cuadrado, Color.white, 0);
            if (s == null) franjaEmbestida.color = new Color(1f, 0.3f, 0.2f, 0.45f);
        }
        franjaEmbestida.enabled = true;
    }

    private void ColocarFranja(float alfa)
    {
        if (franjaEmbestida == null) return;
        float largo = distanciaEmbestida;
        Transform t = franjaEmbestida.transform;
        t.position = (Vector3)(Centro + dirEmbestida * (largo * 0.5f + 0.3f)) + new Vector3(0f, 0f, 0f);
        t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dirEmbestida.y, dirEmbestida.x) * Mathf.Rad2Deg);
        Sprite s = franjaEmbestida.sprite;
        Vector2 tam = s != null ? (Vector2)s.bounds.size : Vector2.one;
        t.localScale = new Vector3(largo / Mathf.Max(0.01f, tam.x), 1.1f / Mathf.Max(0.01f, tam.y), 1f);
        franjaEmbestida.sortingOrder = -110;
        Color c = franjaEmbestida.color;
        c.a = alfa;
        franjaEmbestida.color = c;
    }

    private void UpdatePrepEmbestida()
    {
        velDeseada = Vector2.zero;
        // Se agacha y tiembla, raspando el piso.
        float k = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 12f));
        ColocarFranja(k);
        if (Time.time < finEstado) return;

        estado = Estado.Embistiendo;
        origenEmbestida = Centro;
        finEmbestida = Time.time + distanciaEmbestida / Mathf.Max(1f, velocidadEmbestida) + 0.4f;
        golpeadosEmb.Clear();
        jugadoresEmb.Clear();
        colision.isTrigger = true;   // atraviesa objetos (los rompe) y jugadores (los aparta); las paredes lo frenan a mano
        GestorAudio.Reproducir(SndExtra.EmbestidaCorrer);
        CamaraPisoCenital.Temblar(0.08f, 0.3f);
    }

    private void UpdateEmbistiendo()
    {
        ColocarFranja(0.35f);
        float vel = velocidadEmbestida * Mathf.Clamp(Ritmo, 0.85f, 1.35f);
        // ¿Pared delante? (con el cuerpo atravesable, la pared se busca a mano)
        if (ParedEn(Centro + dirEmbestida * 0.42f, radioCuerpo * 0.8f))
        {
            TerminarEmbestida(true);
            return;
        }
        if (Vector2.Distance(Centro, origenEmbestida) >= distanciaEmbestida || Time.time >= finEmbestida)
        {
            TerminarEmbestida(false);
            return;
        }
        velDeseada = dirEmbestida * vel;

        bool dano = false;
        foreach (ObjetoReparable o in piso.Mapa.objetos)
        {
            if (o == null || golpeadosEmb.Contains(o) || o.Estado == EstadoObjeto.Roto) continue;
            if (Vector2.Distance(Centro, o.PuntoMasCercano(Centro)) > 0.8f) continue;
            golpeadosEmb.Add(o);
            if (o.Danar())
            {
                dano = true;
                GestorAudio.Reproducir(Snd.GolpeObjeto, 0.8f);
                EfectoSprite.Reproducir(ArteNiveles.De(a => a.golpeEstallido), o.Centro, 400, 0.9f, 14f);
            }
        }
        if (dano) piso.RegistrarGolpe();

        foreach (JugadorCenital j in JugadorCenital.Todos)
        {
            if (j == null || !j.isActiveAndEnabled || jugadoresEmb.Contains(j)) continue;
            if (Vector2.Distance(Centro, j.Centro) > 0.95f) continue;
            jugadoresEmb.Add(j);
            // Lo aparta hacia el costado de la carga.
            Vector2 lado = new Vector2(-dirEmbestida.y, dirEmbestida.x);
            if (Vector2.Dot(j.Centro - Centro, lado) < 0f) lado = -lado;
            if (j.RecibirEmpujon(j.Centro - lado - dirEmbestida * 0.5f, 2.4f, 0.8f, true)) GestorAudio.Reproducir(SndExtra.GolpeFuerteJugador);
        }

        if (Time.time >= proximaEstelaEmb && dibujo != null && dibujo.sprite != null)
        {
            proximaEstelaEmb = Time.time + 0.04f;
            EstelaFantasma.Crear(dibujo, new Color(1f, 0.45f, 0.25f, 0.5f), 0.25f);
        }
        if (Random.value < 0.3f) EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), Centro - dirEmbestida * 0.4f, Provisional.OrdenY(transform.position.y) - 1, 0.6f, 14f);
    }

    private void TerminarEmbestida(bool contraPared)
    {
        velDeseada = Vector2.zero;
        colision.isTrigger = false;
        if (franjaEmbestida != null) franjaEmbestida.enabled = false;
        if (contraPared)
        {
            // Se estrella: temblor fuerte, cascotes y queda mareado (momento para atacarlo / alejarse).
            GestorAudio.Reproducir(SndExtra.ChoqueMuro);
            CamaraPisoCenital.Temblar(0.2f, 0.45f);
            EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), Centro + dirEmbestida * 0.5f, 450, 1.4f, 12f);
            TextoFlotante.Mostrar(Centro + Vector2.up * 2f, "¡PAF!", new Color(1f, 0.9f, 0.4f), 1f);
            ImpactoPausa.Breve(0.09f);
            TrozoVolador.Lanzar(Centro + dirEmbestida * 0.5f, 7, 452);
            estado = Estado.Aturdido;
            finEstado = Time.time + 1.4f;
            return;
        }
        estado = Estado.Recuperando;
        finEstado = Time.time + 0.55f / Mathf.Max(0.6f, Ritmo);
    }

    // --- TORBELLINO (la demoledora): gira como trompo persiguiendo al jugador y rompe lo que toca
    private SpriteRenderer aroTorbellino, lineaLaser;
    private float finTorbellino;
    private Vector2 dirTorbellino;

    private void IniciarTorbellino()
    {
        velDeseada = Vector2.zero;
        estado = Estado.PrepTorbellino;
        finEstado = Time.time + 0.7f / Mathf.Max(0.7f, Ritmo);
        SiguienteEspecial();
        GestorAudio.Reproducir(SndExtra.Torbellino);
        TextoFlotante.Mostrar(Centro + Vector2.up * 2.3f, "¡TORBELLINO!", new Color(1f, 0.45f, 0.8f), 1.2f);
        if (aroTorbellino == null) aroTorbellino = Provisional.Crear(transform, "AroTorbellino", Provisional.Aro, new Color(1f, 0.4f, 0.8f, 0.6f), -115);
        aroTorbellino.enabled = true;
        aroTorbellino.transform.localPosition = Vector3.zero;
    }

    private void UpdatePrepTorbellino()
    {
        velDeseada = Vector2.zero;
        float e = 1.6f + 0.4f * Mathf.Sin(Time.time * 20f);
        if (aroTorbellino != null) aroTorbellino.transform.localScale = new Vector3(e, e * 0.8f, 1f);
        if (Time.time < finEstado) return;
        estado = Estado.Torbellino;
        finTorbellino = Time.time + 2.4f;
        golpeadosEmb.Clear();
        jugadoresEmb.Clear();
        colision.isTrigger = true;
        dirTorbellino = jugador != null ? (jugador.Centro - Centro).normalized : Random.insideUnitCircle.normalized;
    }

    private void UpdateTorbellino()
    {
        if (aroTorbellino != null)
        {
            aroTorbellino.transform.localScale = new Vector3(1.9f, 1.5f, 1f);
            aroTorbellino.transform.Rotate(0f, 0f, 900f * Time.deltaTime);
        }
        if (Time.time >= finTorbellino)
        {
            // Termina mareada: un momento para escapar o bloquearla.
            colision.isTrigger = false;
            if (aroTorbellino != null) aroTorbellino.enabled = false;
            velDeseada = Vector2.zero;
            TextoFlotante.Mostrar(Centro + Vector2.up * 2f, "¡QUÉ MAREO!", new Color(1f, 0.75f, 0.9f), 1f);
            estado = Estado.Aturdido;
            finEstado = Time.time + 1.1f;
            return;
        }
        // Persigue al jugador girando de a poco; rebota en las paredes.
        if (jugador != null && jugador.isActiveAndEnabled)
            dirTorbellino = Vector3.RotateTowards(dirTorbellino, (jugador.Centro - Centro).normalized, 2.2f * Time.deltaTime, 0f);
        if (ParedEn(Centro + dirTorbellino * 0.45f, radioCuerpo * 0.8f)) dirTorbellino = -dirTorbellino + Random.insideUnitCircle * 0.5f;
        dirTorbellino.Normalize();
        velDeseada = dirTorbellino * velocidad * 2.1f * Mathf.Clamp(Ritmo, 0.8f, 1.3f);

        bool dano = false;
        foreach (ObjetoReparable o in piso.Mapa.objetos)
        {
            if (o == null || golpeadosEmb.Contains(o) || o.Estado == EstadoObjeto.Roto) continue;
            if (Vector2.Distance(Centro, o.PuntoMasCercano(Centro)) > 0.9f) continue;
            golpeadosEmb.Add(o);
            if (o.Danar()) { dano = true; EfectoSprite.Reproducir(ArteNiveles.De(a => a.golpeEstallido), o.Centro, 400, 0.8f, 14f); }
        }
        if (dano) { piso.RegistrarGolpe(); GestorAudio.Reproducir(Snd.GolpeObjeto, 0.7f); }
        foreach (JugadorCenital j in JugadorCenital.Todos)
        {
            if (j == null || !j.isActiveAndEnabled || jugadoresEmb.Contains(j)) continue;
            if (Vector2.Distance(Centro, j.Centro) > 0.9f) continue;
            jugadoresEmb.Add(j);
            if (j.RecibirEmpujon(Centro, 2.2f, 0.7f, true)) GestorAudio.Reproducir(SndExtra.GolpeFuerteJugador);
        }
        if (Time.time >= proximaEstelaEmb && dibujo != null && dibujo.sprite != null)
        {
            proximaEstelaEmb = Time.time + 0.05f;
            EstelaFantasma.Crear(dibujo, new Color(1f, 0.45f, 0.8f, 0.45f), 0.25f);
        }
    }

    // --- LÁSER (el robot): marca una línea roja y dispara un rayo que daña todo lo que cruza
    private Vector2 dirLaser;
    private float largoLaser;

    private bool IniciarLaser()
    {
        // Apunta al jugador si está a tiro; si no, a la fila de objetos sanos más larga.
        Vector2 dir = Vector2.zero;
        if (jugador != null && jugador.isActiveAndEnabled && Vector2.Distance(Centro, jugador.Centro) < 9f) dir = (jugador.Centro - Centro).normalized;
        else
        {
            ObjetoReparable mejor = null;
            float md = float.MaxValue;
            foreach (ObjetoReparable o in piso.Mapa.objetos)
            {
                if (o == null || o.Estado != EstadoObjeto.Intacto) continue;
                float d = Vector2.Distance(Centro, o.Centro);
                if (d < 2f || d > 8f || d >= md) continue;
                md = d; mejor = o;
            }
            if (mejor != null) dir = (mejor.Centro - Centro).normalized;
        }
        if (dir == Vector2.zero) return false;
        // Largo: hasta la primera pared.
        float largo = 0.6f;
        while (largo < 9f && !ParedEn(Centro + dir * largo, 0.2f)) largo += 0.25f;
        if (largo < 2f) return false;
        dirLaser = dir;
        largoLaser = largo;
        direccion = dir;
        velDeseada = Vector2.zero;
        estado = Estado.PrepLaser;
        finEstado = Time.time + 1.1f / Mathf.Max(0.7f, Ritmo);
        SiguienteEspecial();
        GestorAudio.Reproducir(SndExtra.LaserRobot);
        TextoFlotante.Mostrar(Centro + Vector2.up * 2.3f, "¡LÁSER!", new Color(1f, 0.3f, 0.3f), 1.2f);
        if (lineaLaser == null) lineaLaser = Provisional.Crear(null, "LineaLaser", Provisional.Cuadrado, Color.red, 470);
        lineaLaser.enabled = true;
        ColocarLaser(0.08f, new Color(1f, 0.15f, 0.1f, 0.5f));
        return true;
    }

    private void ColocarLaser(float grosor, Color color)
    {
        if (lineaLaser == null) return;
        Transform t = lineaLaser.transform;
        Vector2 origen = Centro + Vector2.up * 0.9f;
        t.position = origen + dirLaser * (largoLaser * 0.5f);
        t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dirLaser.y, dirLaser.x) * Mathf.Rad2Deg);
        Vector2 tam = lineaLaser.sprite != null ? (Vector2)lineaLaser.sprite.bounds.size : Vector2.one;
        t.localScale = new Vector3(largoLaser / Mathf.Max(0.01f, tam.x), grosor / Mathf.Max(0.01f, tam.y), 1f);
        lineaLaser.color = color;
    }

    private void UpdatePrepLaser()
    {
        velDeseada = Vector2.zero;
        float k = 0.5f + 0.5f * Mathf.Sin(Time.time * 30f);
        ColocarLaser(0.06f + 0.04f * k, new Color(1f, 0.15f, 0.1f, 0.35f + 0.35f * k));
        if (Time.time < finEstado) return;
        estado = Estado.Laser;
        finEstado = Time.time + 0.45f;
        CamaraPisoCenital.Temblar(0.12f, 0.35f);
        ImpactoPausa.Breve(0.05f);

        // Todo lo que cruza el rayo se daña; los jugadores salen despedidos hacia el costado.
        Vector2 a = Centro, b = Centro + dirLaser * largoLaser;
        bool dano = false;
        foreach (ObjetoReparable o in piso.Mapa.objetos)
        {
            if (o == null || o.Estado == EstadoObjeto.Roto) continue;
            Vector2 cerca = PuntoEnSegmento(a, b, o.Centro);
            if (Vector2.Distance(o.PuntoMasCercano(cerca), cerca) > 0.45f) continue;
            if (o.Danar()) { dano = true; EfectoSprite.Reproducir(ArteNiveles.De(x => x.golpeEstallido), o.Centro, 471, 0.9f, 14f); }
        }
        if (dano) { piso.RegistrarGolpe(); GestorAudio.Reproducir(Snd.GolpeObjeto); }
        foreach (JugadorCenital j in JugadorCenital.Todos)
        {
            if (j == null || !j.isActiveAndEnabled) continue;
            Vector2 cerca = PuntoEnSegmento(a, b, j.Centro);
            if (Vector2.Distance(cerca, j.Centro) > 0.6f) continue;
            if (j.RecibirEmpujon(cerca, 1.8f, 0.8f, true)) GestorAudio.Reproducir(SndExtra.GolpeFuerteJugador);
        }
    }

    private void UpdateLaser()
    {
        velDeseada = Vector2.zero;
        float k = Mathf.Clamp01((finEstado - Time.time) / 0.45f);
        ColocarLaser(0.35f * k + 0.05f, Color.Lerp(new Color(1f, 0.3f, 0.25f, 0.2f), new Color(1f, 0.95f, 0.9f, 1f), k));
        if (Time.time < finEstado) return;
        if (lineaLaser != null) lineaLaser.enabled = false;
        estado = Estado.Recuperando;
        finEstado = Time.time + 0.7f / Mathf.Max(0.6f, Ritmo);
    }

    private static Vector2 PuntoEnSegmento(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
        return a + ab * t;
    }

    // ------------------------------------------------------------ supervivencia
    /// <summary>Oleada vencida: se desvanece en una nube y queda fuera del piso hasta la próxima oleada.</summary>
    public void Desaparecer()
    {
        Detener();
        CancelarEspecial();
        EfectoSprite.Reproducir(ArteExtra.Serie("Efectos/humo_poof_", 4), Centro, 470, 2.2f, 12f);
        TextoFlotante.Mostrar(Centro + Vector2.up * 2f, "¡VENCIDO!", new Color(0.5f, 1f, 0.55f), 1.6f);
        GestorAudio.Reproducir(SndExtra.MiniVencido);
        if (colision != null) colision.enabled = false;
        estado = Estado.Ido;
        velDeseada = Vector2.zero;
        Vector2 fuera = piso != null && piso.Mapa != null ? new Vector2(piso.Mapa.Limites.center.x, piso.Mapa.Limites.yMax + 30f) : Centro + Vector2.up * 30f;
        transform.position = fuera;
        if (cuerpo != null) cuerpo.position = fuera;
    }

    /// <summary>Vuelve a entrar (nueva oleada).</summary>
    public void Volver(float espera)
    {
        retrasoEntrada = espera;
        entradaElegida = false;
        Reiniciar();
    }

    // --- pisotón sísmico
    private float inicioPisoton;

    private void IniciarPisoton()
    {
        velDeseada = Vector2.zero;
        estado = Estado.PrepPisoton;
        inicioPisoton = Time.time;
        finEstado = Time.time + preparacionPisoton / Mathf.Max(0.7f, Ritmo);
        SiguienteEspecial();
        GestorAudio.Reproducir(SndExtra.PisotonSalto);
        TextoFlotante.Mostrar(Centro + Vector2.up * 2.4f, "¡PISOTÓN!", new Color(1f, 0.75f, 0.2f), 1.2f);
        if (aroPisoton == null)
        {
            Sprite s = ArteExtra.Uno("Efectos/aro_pisoton");
            aroPisoton = Provisional.Crear(transform, "AroPisoton", s != null ? s : Provisional.Aro, Color.white, -115);
        }
        aroPisoton.enabled = true;
    }

    private void UpdatePrepPisoton()
    {
        velDeseada = Vector2.zero;
        float dur = Mathf.Max(0.05f, finEstado - inicioPisoton);
        float k = Mathf.Clamp01((Time.time - inicioPisoton) / dur);
        // Sube y, al final, cae de golpe.
        elevacion = k < 0.75f ? 1.1f * Mathf.Sin(k / 0.75f * Mathf.PI * 0.5f) : 1.1f * (1f - (k - 0.75f) / 0.25f);
        if (aroPisoton != null)
        {
            Sprite s = aroPisoton.sprite;
            float tam = s != null ? Mathf.Max(0.01f, s.bounds.size.x) : 1f;
            float e = radioPisoton * 2f / tam * (0.6f + 0.4f * k);
            aroPisoton.transform.position = (Vector3)Centro;
            aroPisoton.transform.localScale = new Vector3(e, e * 0.85f, 1f);
            aroPisoton.color = new Color(1f, 1f, 1f, 0.45f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 16f)));
        }
        if (Time.time < finEstado) return;
        Pisotear();
    }

    private void Pisotear()
    {
        elevacion = 0f;
        finAplastePisoton = Time.time + 0.3f;
        ImpactoPausa.Breve(0.08f);
        if (aroPisoton != null) aroPisoton.enabled = false;
        GestorAudio.Reproducir(SndExtra.PisotonImpacto);
        CamaraPisoCenital.Temblar(0.28f, 0.55f);
        SpriteRenderer onda = EfectoSprite.Reproducir(ArteExtra.Serie("Efectos/onda_empujon_", 4), Centro, 455, radioPisoton * 1.3f, 12f);
        if (onda != null) onda.color = new Color(1f, 0.75f, 0.35f);
        EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), Centro, 454, 2f, 10f);
        Sprite grieta = ArteExtra.Uno("Efectos/grieta_pisoton");
        if (grieta != null) MarcaTemporal.Crear(grieta, Centro, radioPisoton * 0.9f, 6f);

        bool dano = false;
        foreach (ObjetoReparable o in piso.Mapa.objetos)
        {
            if (o == null || o.Estado == EstadoObjeto.Roto) continue;
            if (Vector2.Distance(Centro, o.PuntoMasCercano(Centro)) > radioPisoton) continue;
            if (Random.value < 0.75f && o.Danar()) dano = true;
        }
        if (dano) { piso.RegistrarGolpe(); GestorAudio.Reproducir(Snd.GolpeObjeto); }

        foreach (JugadorCenital j in JugadorCenital.Todos)
        {
            if (j == null || !j.isActiveAndEnabled) continue;
            float d = Vector2.Distance(j.Centro, Centro);
            if (d > radioPisoton) continue;
            // Cerca del centro quita vida; en el borde solo empuja y marea.
            bool centro = d <= radioPisoton * 0.55f;
            if (j.RecibirEmpujon(Centro, centro ? 2.6f : 1.6f, centro ? 1f : 0.6f, centro)) GestorAudio.Reproducir(SndExtra.GolpeFuerteJugador);
        }
        estado = Estado.Recuperando;
        finEstado = Time.time + 0.9f / Mathf.Max(0.6f, Ritmo);
    }

    // ------------------------------------------------------------ rabia
    private void ComprobarRabia()
    {
        if (!rabia && EnPiso && piso.Energia <= umbralRabia && estado != Estado.UltiAviso && estado != Estado.UltiLluvia && estado != Estado.Agotado)
        {
            rabia = true;
            GestorAudio.Reproducir(SndExtra.RugidoDemoledor, 1f, 0.85f);
            TensionPropia("rabia", true);
            CamaraPisoCenital.Temblar(0.16f, 0.6f);
            AvisoUlti.Mostrar("¡MODO RABIA!", 2.4f, true, new Color(1f, 0.3f, 0.2f));
            Burbuja.Decir(transform, "¡AHORA SÍ ME ENOJÉ!", Burbuja.Voz.Villano, alturaDibujo - radioCuerpo + 0.3f);
        }
        if (auraRabia == null)
        {
            cuadrosAura = ArteExtra.Serie("Ira/aura_", 4);
            auraRabia = Provisional.Crear(transform, "AuraRabia", cuadrosAura != null ? cuadrosAura[0] : Provisional.Circulo, new Color(1f, 0.2f, 0.1f, 0f), 0);
        }
        bool ver = rabia && EnPiso && !Congelado;
        auraRabia.enabled = ver;
        if (!ver) return;
        if (cuadrosAura != null) auraRabia.sprite = cuadrosAura[(int)(Time.time * 12f) % cuadrosAura.Length];
        auraRabia.transform.localPosition = new Vector3(0f, -radioCuerpo - 0.1f, 0f);
        float e = alturaDibujo * 0.95f + 0.08f * Mathf.Sin(Time.time * 7f);
        auraRabia.transform.localScale = new Vector3(e * 0.95f, e, 1f);
        auraRabia.sortingOrder = dibujo.sortingOrder - 1;
        auraRabia.color = new Color(1f, 0.22f, 0.12f, 0.45f + 0.15f * Mathf.Sin(Time.time * 10f));
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
            TensionPropia("furia", true);
            TextoFlotante.Mostrar(Centro + Vector2.up * 2.4f, "¡FURIA!", new Color(1f, 0.25f, 0.2f));
            Burbuja.Decir(transform, Frases.Una(Frases.Demoledor), Burbuja.Voz.Villano, alturaDibujo - radioCuerpo + 0.3f);
        }
        else if (furia && destrozo < UmbralFuria - 0.05f)
        {
            furia = false;
            TensionPropia("furia", false);
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
        if (tipo == TipoDemoledor.Ingeniero)
        {
            // PLANOS DE DEMOLICIÓN: dos escombros más, a los costados del primero.
            Rect lim = piso.Mapa.Limites;
            Vector2 lado = Vector2.Perpendicular((destino - Centro).normalized) * 1.6f;
            foreach (Vector2 extra in new[] { destino + lado, destino - lado })
            {
                Vector2 d = new Vector2(Mathf.Clamp(extra.x, lim.xMin + 0.6f, lim.xMax - 0.6f), Mathf.Clamp(extra.y, lim.yMin + 0.6f, lim.yMax - 0.6f));
                EscombroCenital.Lanzar(Centro + Vector2.up * altoManos, d, Vuelo * 1.1f, RadioImpacto * 0.8f, def, AlImpactar);
            }
            TextoFlotante.Mostrar(Centro + Vector2.up * 2.3f, "¡PLANOS DE DEMOLICIÓN!", new Color(0.85f, 0.92f, 1f), 1.2f);
        }
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
        GolpearJugadoresEn(punto, RadioImpacto);
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
        bool usaEspecial = (estado == Estado.PrepLanzamiento || (estado == Estado.Entrando && !impactoEntrada)) &&
                           animaciones.especial != null && animaciones.especial.Length > 0;
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
                if (velDeseada.sqrMagnitude > 0.01f) animador.Reproducir(serie, cuadrosCaminar * (estado == Estado.Saliendo ? 1.9f : 1f), true, false);
                else animador.MostrarCuadroFijo(serie, 0);
            }
        }
        animador.Actualizar(Time.deltaTime);

        if (dibujo.sprite == null) dibujo.sprite = Provisional.Cuadrado;

        // Avisos visibles de preparación (también con arte real: parpadeo rojo / amarillo).
        Color baseColor = Provisional_ ? new Color(0.78f, 0.28f, 0.22f) : Color.white;
        if (furia) baseColor = Provisional_ ? new Color(1f, 0.08f, 0.06f) : new Color(1f, 0.4f, 0.35f);
        baseColor *= Variante;   // el segundo demoledor (multijugador) tiene otro tono
        if (estado == Estado.PrepPunetazo)
            dibujo.color = ((int)(Time.time * 16f) % 2 == 0) ? new Color(1f, 0.4f, 0.35f) : baseColor;
        else if (estado == Estado.PrepLanzamiento)
            dibujo.color = ((int)(Time.time * 10f) % 2 == 0) ? new Color(1f, 0.9f, 0.3f) : baseColor;
        else if (estado == Estado.UltiAviso || estado == Estado.UltiLluvia)
            dibujo.color = ((int)(Time.time * 12f) % 2 == 0) ? new Color(1f, 0.25f, 0.2f) : Color.white;
        else if (estado == Estado.Agotado) dibujo.color = new Color(0.78f, 0.84f, 1f);
        else if (estado == Estado.PrepEmpujon)
            dibujo.color = ((int)(Time.time * 18f) % 2 == 0) ? new Color(1f, 0.55f, 0.3f) : baseColor;
        else if (estado == Estado.PrepEmbestida || estado == Estado.PrepPisoton)
            dibujo.color = ((int)(Time.time * 14f) % 2 == 0) ? new Color(1f, 0.75f, 0.25f) : baseColor;
        else dibujo.color = baseColor;
        if (rabia && estado != Estado.PrepEmpujon && estado != Estado.Agotado && estado != Estado.Rindiendo)
            dibujo.color = Color.Lerp(baseColor, new Color(1f, 0.45f, 0.38f), 0.45f + 0.35f * Mathf.Sin(Time.time * 9f));
        if (Congelado) dibujo.color = new Color(0.55f, 0.85f, 1f);
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

        PosesYAccesorios(b, tam);

        // Durante el salto se desvanece al caer.
        if (estado == Estado.Saltando || estado == Estado.Escapando)
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
