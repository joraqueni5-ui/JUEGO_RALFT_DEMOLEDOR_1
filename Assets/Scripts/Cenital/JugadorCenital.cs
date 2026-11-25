using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// El jugador visto desde arriba: movimiento libre en 8 direcciones (WASD y flechas, o los
/// botones táctiles), sin gravedad, chocando con paredes y objetos. ENTER (o el botón MARTILLO)
/// repara un estado el objeto dañado o roto más cercano. Al recibir daño queda invulnerable 1 s
/// parpadeando. Las 4 direcciones usan los sprites de arriba, abajo y derecha (izquierda = derecha volteada).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class JugadorCenital : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 3.6f;

    [Header("Reparación")]
    [Tooltip("Alcance del martillo (baldosas), medido desde el cuerpo hasta el borde del objeto.")]
    public float radioReparacion = 0.9f;
    public float momentoDeReparacion = 0.15f;
    public float enfriamiento = 0.25f;
    public float cuadrosCaminar = 8f;
    public float cuadrosPorSegundoMartillo = 12f;

    [Header("Daño")]
    public float tiempoInvulnerable = 1f;

    [Header("Choque con el demoledor (distancia, aturdimiento y espera salen de la DefinicionPiso)")]
    [Tooltip("Segundos que dura el empujón del choque.")]
    public float duracionEmpuje = 0.15f;
    [Tooltip("Estrellas que giran sobre la cabeza mientras está aturdido.")]
    public int numeroEstrellas = 3;
    public float radioEstrellas = 0.4f;
    [Tooltip("Grados por segundo.")]
    public float velocidadEstrellas = 420f;
    public float tamanoEstrella = 0.3f;
    [Tooltip("Altura (baldosas) sobre los pies a la que giran las estrellas.")]
    public float alturaEstrellas = 1.45f;

    [Header("Poderes (duración, mensajes y colores salen de DefinicionPiso.poderes)")]
    [Tooltip("Tamaño (baldosas) del aro que rodea al jugador mientras un poder está activo.")]
    public Vector2 tamanoAro = new Vector2(1.1f, 1.6f);
    [Tooltip("Altura (baldosas) del centro del aro sobre los pies.")]
    public float alturaAro = 0.65f;
    [Tooltip("Velocidad de la pulsación del aro.")]
    public float pulsoAro = 6f;

    [Header("Potenciadores extra (duración y colores salen del poder)")]
    [Tooltip("Zapatillas turbo: multiplica la velocidad al caminar.")]
    public float multiplicadorTurbo = 1.6f;
    [Tooltip("Martillo dorado: multiplica la rapidez del martillazo y el alcance.")]
    public float multiplicadorMartilloRapido = 2f;
    public float alcanceExtraMartilloRapido = 0.45f;
    [Tooltip("Duración (s) del salto hacia atrás cuando el demoledor te empuja.")]
    public float duracionEmpujeFuerte = 0.3f;

    [Header("Dibujo")]
    [Tooltip("Alto en baldosas al que se escala el sprite.")]
    public float alturaDibujo = 1.25f;
    public AnimacionesCenital animaciones = new AnimacionesCenital();

    public event Action Golpeado;

    // ------------------------------------------------------------ varios jugadores (multijugador)
    private static readonly System.Collections.Generic.List<JugadorCenital> todos = new System.Collections.Generic.List<JugadorCenital>();
    /// <summary>Todos los jugadores del piso (1 en modo normal, 2 en multijugador).</summary>
    public static System.Collections.Generic.List<JugadorCenital> Todos { get { return todos; } }

    [NonSerialized] public int indiceRed;            // 0 = anfitrión, 1 = invitado
    [NonSerialized] public bool controlRemoto;       // lo maneja el otro equipo por la red
    [NonSerialized] public Personajes.Ficha personaje;
    [NonSerialized] public IraEstudiante ira;

    // Entrada que llega por la red para el jugador remoto.
    private Vector2 entradaRemota;
    private int martillazosRemotos;

    /// <summary>El jugador que se controla en ESTE equipo (el que sigue la cámara).</summary>
    public static JugadorCenital Local
    {
        get
        {
            for (int i = 0; i < todos.Count; i++)
                if (todos[i] != null && !todos[i].controlRemoto && todos[i].gameObject.activeInHierarchy) return todos[i];
            return null;
        }
    }

    /// <summary>El jugador activo más cercano a 'punto' (o null).</summary>
    public static JugadorCenital MasCercano(Vector2 punto)
    {
        JugadorCenital mejor = null;
        float dm = float.MaxValue;
        for (int i = 0; i < todos.Count; i++)
        {
            JugadorCenital j = todos[i];
            if (j == null || !j.isActiveAndEnabled) continue;
            float d = (j.Centro - punto).sqrMagnitude;
            if (d < dm) { dm = d; mejor = j; }
        }
        if (mejor == null)
            for (int i = 0; i < todos.Count; i++) if (todos[i] != null && todos[i].gameObject.activeInHierarchy) return todos[i];
        return mejor;
    }

    /// <summary>Jugador por su número de red (0 anfitrión, 1 invitado).</summary>
    public static JugadorCenital PorIndice(int indice)
    {
        for (int i = 0; i < todos.Count; i++) if (todos[i] != null && todos[i].indiceRed == indice) return todos[i];
        return null;
    }

    /// <summary>Entrada del jugador remoto (llega por la red).</summary>
    public void FijarEntradaRemota(Vector2 mover, int martillazos)
    {
        entradaRemota = Vector2.ClampMagnitude(mover, 1f);
        martillazosRemotos += Mathf.Max(0, martillazos);
    }

    /// <summary>Personaje de este jugador (el elegido en el selector o, en multijugador, el que tocó a cada uno).</summary>
    public Personajes.Ficha Ficha { get { return personaje != null ? personaje : Personajes.Actual; } }

    // ------------------------------------------------------------ peligros del piso (PeligrosPiso)
    /// <summary>Empuje del ambiente (el viento de la terraza), en baldosas por segundo.</summary>
    [NonSerialized] public Vector2 empujeAmbiente;
    private float finResbalon;
    private Vector2 velocidadActual;
    /// <summary>Patinando en un charco: el control responde con inercia.</summary>
    public bool Resbalando { get { return Time.time < finResbalon; } }
    /// <summary>Velocidad con la que se mueve ahora.</summary>
    public Vector2 VelocidadActual { get { return cuerpo != null ? cuerpo.linearVelocity : Vector2.zero; } }

    /// <summary>Pisa un charco: sale patinando con lo que traía (y un poco más) y tarda en frenar o girar.</summary>
    public void Resbalar(float segundos)
    {
        if (!enabled || Invencible) return;
        if (!Resbalando && cuerpo != null) velocidadActual = cuerpo.linearVelocity * 1.35f;
        finResbalon = Mathf.Max(finResbalon, Time.time + segundos);
    }

    /// <summary>Chispazo de un cable: lo sacude y lo aturde un momento (no quita vida).</summary>
    public void Electrocutar(Vector2 desde)
    {
        if (!enabled || Invencible) return;
        RecibirEmpujon(desde, 1.1f, 0.9f, false);
        finSacudida = Time.time + 0.5f;
        if (!controlRemoto) DemoledorUnifranz.UI.Menus.PreferenciasMenus.Vibrar(120);
    }

    private bool IraActiva { get { return ira != null && ira.EstaActiva; } }

    private void OnEnable() { if (!todos.Contains(this)) todos.Add(this); }
    private void OnDestroy() { todos.Remove(this); }

    private Rigidbody2D cuerpo;
    private SpriteRenderer dibujo;
    private AnimadorSprites animador;
    private Vector2 entrada;
    private Vector2 direccion = Vector2.down;
    private bool martillando;
    private bool reparoHecho;
    private float inicioMartillo, finMartillo, proximoMartillo, finInvulnerable;
    private bool fueraDeCuadros;

    private float finAturdido, finEmpuje, proximoChoque;
    private Vector2 velocidadEmpuje;
    private Transform estrellas;
    private SpriteRenderer[] spritesEstrellas;
    private bool estrellasConArte;

    private DemoledorCenital demoledorRef;
    private float duracionEmpujeActual = 0.15f;
    private float ritmoBase = 1f, duracionPoderes = 1f, factorAturdimiento = 1f;
    private float finTurbo, finMartilloRapido, proximaEstela;
    private bool cascoPuesto;
    private PoderPiso poderTurbo, poderMartillo;
    private SpriteRenderer aroTurbo, aroMartillo, dibujoCasco;
    private float finInvencible, finReparacionTotal;
    private PoderPiso poderInvencible, poderReparacion;
    private SpriteRenderer aroInvencible, aroReparacion;

    /// <summary>Centro del cuerpo (el collider está sobre los pies).</summary>
    public Vector2 Centro { get { return (Vector2)transform.position + new Vector2(0f, 0.3f); } }
    /// <summary>Aturdido por un choque: no se mueve ni repara y no puede recibir daño.</summary>
    public bool Aturdido { get { return Time.time < finAturdido; } }
    /// <summary>Con el poder de invulnerabilidad: no recibe daño ni choques.</summary>
    public bool Invencible { get { return Time.time < finInvencible || IraActiva; } }
    /// <summary>Con el poder de reparación total: cada martillazo deja el objeto intacto.</summary>
    public bool ReparacionTotal { get { return Time.time < finReparacionTotal || IraActiva; } }
    public bool PuedeRecibirGolpe { get { return enabled && Time.time >= finInvulnerable && !Aturdido && !Invencible; } }
    /// <summary>Zapatillas turbo activas: camina más rápido.</summary>
    public bool Turbo { get { return Time.time < finTurbo; } }
    /// <summary>Martillo dorado activo: martilla el doble de rápido y llega más lejos.</summary>
    public bool MartilloRapido { get { return Time.time < finMartilloRapido; } }
    /// <summary>Lleva el casco de obra: el próximo golpe no le quita vida.</summary>
    public bool TieneCasco { get { return cascoPuesto; } }
    private float RadioActual { get { return radioReparacion + (MartilloRapido ? alcanceExtraMartilloRapido : 0f) + (IraActiva ? 0.6f : 0f); } }
    private float RitmoMartillo { get { return ritmoBase * (MartilloRapido ? multiplicadorMartilloRapido : 1f) * (IraActiva ? 1.7f : 1f); } }

    /// <summary>Personaje elegido en el selector: su ropa y peinado (mismos movimientos) y su ventaja.</summary>
    private void AplicarPersonaje()
    {
        Personajes.Ficha f = Ficha;
        if (f == null) return;
        if (f.id != "sistemas" || Personajes.Serie8(f, "abajo") != null)   // Diego: el original, ahora con 8 cuadros
        {
            AnimacionesCenital propias = Personajes.Animaciones(f, animaciones != null ? animaciones.especial : null);
            if (propias != null)
            {
                animaciones = propias;
                alturaDibujo *= f.escalaAltura;
            }
        }
        switch (f.id)
        {
            case "sistemas": ritmoBase = 1.15f; break;
            case "medicina": cascoPuesto = true; break;
            case "administracion": duracionPoderes = 1.3f; break;
            case "derecho": factorAturdimiento = 0.6f; break;
            case "arquitectura": radioReparacion += 0.25f; break;
            case "odontologia": velocidad *= 1.12f; break;
        }
    }

    private DefinicionPiso Definicion
    {
        get { PisoCenital p = PisoCenital.Actual; return p != null ? p.Definicion : null; }
    }

    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        cuerpo.gravityScale = 0f;
        cuerpo.freezeRotation = true;
        cuerpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;

        dibujo = GetComponentInChildren<SpriteRenderer>();
        if (dibujo == null) dibujo = Provisional.Crear(transform, "Dibujo", null, Color.white, 0);
        animador = new AnimadorSprites(dibujo);
        // Tras perder una vida hay un margen real para escapar (antes podían quitarte dos vidas en 2 segundos).
        tiempoInvulnerable = Mathf.Max(tiempoInvulnerable, 1.6f);
        CrearEstrellas();
        fase = UnityEngine.Random.Range(0f, 10f);
    }

    private void Start()
    {
        // El arte de la definición (si lo hay) reemplaza al de la escena.
        PisoCenital piso = PisoCenital.Actual;
        if (piso != null && piso.Definicion != null) animaciones = AnimacionesCenital.Combinar(animaciones, piso.Definicion.jugador);
        AplicarPersonaje();
        animaciones.QuitarInvalidos();
        if (animaciones.caminarArriba == null) animaciones.caminarArriba = animaciones.caminarAbajo;
        MostrarQuieto();
    }

    private void OnDisable()
    {
        if (lanzado || agarrado)
        {
            // Lo desactivaron en pleno vuelo (fin del piso): cae directo donde iba.
            if (lanzado) transform.position = vueloHasta;
            agarrado = false; lanzado = false; elevacionVuelo = giroVuelo = 0f;
            if (cuerpo != null) { cuerpo.simulated = true; cuerpo.position = transform.position; }
        }
        if (cuerpo != null) cuerpo.linearVelocity = Vector2.zero;
        if (dibujo != null) dibujo.enabled = true;
    }

    private void Update()
    {
        if (EnElAire)
        {
            ActualizarVuelo();
            entrada = Vector2.zero;
            martillando = false;
            ControlesTactiles.RepararPulsado();
            Animar();
            AjustarDibujo();
            ActualizarEstrellas(false);
            return;
        }
        LeerEntrada();

        bool aturdido = Aturdido;
        if (aturdido)
        {
            entrada = Vector2.zero;
            martillando = false;
            ControlesTactiles.RepararPulsado();   // se descarta el toque: no se acumula para después
        }

        if (martillando)
        {
            if (!reparoHecho && Time.time >= inicioMartillo + momentoDeReparacion / RitmoMartillo) Reparar();
            if (Time.time >= finMartillo) martillando = false;
        }
        else if (!aturdido)
        {
            bool pide;
            if (controlRemoto)
            {
                pide = martillazosRemotos > 0;
                if (pide) martillazosRemotos--;
            }
            else
            {
                Keyboard k = Keyboard.current;
                bool enter = k != null && (k[Key.Enter].wasPressedThisFrame || k[Key.NumpadEnter].wasPressedThisFrame);
                bool tactil = ControlesTactiles.RepararPulsado();   // se lee siempre para consumir el toque
                pide = enter || tactil;
            }
            if (pide && Time.time >= proximoMartillo && !EntradaBloqueada) Martillar();
        }
        if (martillazosRemotos > 2) martillazosRemotos = 2;   // no se acumulan martillazos viejos

        Animar();
        Parpadear();
        AjustarDibujo();
        ActualizarEstrellas(aturdido);
        ActualizarAros();
        ActualizarExtras();
        SonidosDeMovimiento(aturdido);
    }

    private float proximoPaso;
    private bool pasoAlterno;
    private float finSacudida;
    private float fase;

    /// <summary>Pasos alternados cada 0,28 s al caminar, sonido de aturdido y sonido suave mientras dura un poder.</summary>
    private void SonidosDeMovimiento(bool aturdido)
    {
        GestorAudio.Bucle("aturdido_jugador", Snd.AturdidoBucle, aturdido);
        GestorAudio.Bucle("poder_jugador", Snd.PoderActivoBucle, Invencible || ReparacionTotal || Turbo || MartilloRapido);

        bool camina = !aturdido && !martillando && cuerpo != null && cuerpo.linearVelocity.sqrMagnitude > 0.2f;
        if (camina && Time.time >= proximoPaso)
        {
            GestorAudio.Reproducir(pasoAlterno ? Snd.Paso2 : Snd.Paso1);
            pasoAlterno = !pasoAlterno;
            proximoPaso = Time.time + (Turbo ? 0.18f : 0.28f);
        }
    }

    /// <summary>Seguro: si falla un collider, el jugador nunca sale del rectángulo del mapa.</summary>
    private void AcotarAlMapa()
    {
        PisoCenital piso = PisoCenital.Actual;
        if (piso == null || piso.Mapa == null || cuerpo == null) return;
        Rect lim = piso.Mapa.Limites;
        if (lim.width < 1f || lim.height < 1f) return;

        const float margen = 0.4f;
        Vector2 p = cuerpo.position;
        Vector2 c = new Vector2(Mathf.Clamp(p.x, lim.xMin + margen, lim.xMax - margen), Mathf.Clamp(p.y, lim.yMin + margen, lim.yMax - margen));
        if (c != p) cuerpo.position = c;
    }

    private void FixedUpdate()
    {
        if (EnElAire) return;
        AcotarAlMapa();
        if (Time.time < finEmpuje)
        {
            // El empujón frena solo (más rápido al principio); los muros lo detienen, así nunca sale del piso.
            float resto = Mathf.Clamp01((finEmpuje - Time.time) / Mathf.Max(0.02f, duracionEmpujeActual));
            cuerpo.linearVelocity = velocidadEmpuje * (2f * resto);
        }
        else
        {
            Vector2 objetivo = (martillando || Aturdido) ? Vector2.zero : entrada * velocidad * (Turbo ? multiplicadorTurbo : 1f) * (IraActiva ? 1.4f : 1f);
            // En un charco el personaje patina: la velocidad cambia de a poco.
            velocidadActual = Resbalando ? Vector2.MoveTowards(velocidadActual, objetivo, 4.5f * Time.fixedDeltaTime) : objetivo;
            cuerpo.linearVelocity = velocidadActual + empujeAmbiente;
            return;
        }
        velocidadActual = cuerpo.linearVelocity;
    }

    // ------------------------------------------------------------ choque con el demoledor
    private void OnCollisionEnter2D(Collision2D c) { Chocar(c); }
    private void OnCollisionStay2D(Collision2D c) { Chocar(c); }

    /// <summary>
    /// Tocar al demoledor: si puede, te aleja de un manotazo y te quita una vida (DemoledorCenital.EmpujarAhora).
    /// Si está ocupado (atacando otra cosa o congelado) solo te aparta un poco y te aturde.
    /// Después de un choque hay una espera antes de que otro vuelva a afectarte.
    /// </summary>
    private void Chocar(Collision2D c)
    {
        if (!enabled || Aturdido || Invencible || Time.time < proximoChoque) return;
        DemoledorCenital demoledor = c.collider.GetComponentInParent<DemoledorCenital>();
        if (demoledor == null) return;
        if (demoledor.EmpujarAhora()) return;

        DefinicionPiso def = Definicion;
        float empuje = def != null ? def.empujeChoque : 1f;
        float aturdimiento = def != null ? def.aturdimientoChoque : 1.5f;
        float espera = def != null ? def.esperaChoque : 3f;

        Vector2 dir = (Vector2)transform.position - (Vector2)demoledor.transform.position;
        if (dir.sqrMagnitude < 0.0001f) dir = -direccion;
        dir.Normalize();

        finAturdido = Time.time + aturdimiento * factorAturdimiento;
        proximoChoque = Time.time + Mathf.Max(espera, aturdimiento);
        duracionEmpujeActual = Mathf.Max(0.02f, duracionEmpuje);
        finEmpuje = Time.time + duracionEmpujeActual;
        velocidadEmpuje = dir * (empuje / duracionEmpujeActual);
        martillando = false;
        entrada = Vector2.zero;
    }

    /// <summary>
    /// Empujón fuerte (del demoledor o de un mini villano): sale despedido 'distancia' baldosas lejos de 'desde', queda aturdido
    /// 'aturdimiento' s y, si 'danar', pierde una vida (respeta la invulnerabilidad breve y el casco). Los muros lo frenan y
    /// AcotarAlMapa lo mantiene dentro del piso. Devuelve true si perdió una vida.
    /// </summary>
    public bool RecibirEmpujon(Vector2 desde, float distancia, float aturdimiento, bool danar)
    {
        if (!enabled || Invencible || EnElAire) return false;
        Vector2 dir = Centro - desde;
        if (dir.sqrMagnitude < 0.0001f) dir = -direccion;
        dir.Normalize();

        bool perdioVida = danar && RecibirGolpe();

        duracionEmpujeActual = Mathf.Max(0.05f, duracionEmpujeFuerte);
        finEmpuje = Time.time + duracionEmpujeActual;
        velocidadEmpuje = dir * (Mathf.Max(0f, distancia) / duracionEmpujeActual);
        if (aturdimiento > 0f) finAturdido = Mathf.Max(finAturdido, Time.time + aturdimiento * factorAturdimiento);
        proximoChoque = Time.time + Mathf.Max(1.2f, aturdimiento);
        martillando = false;
        entrada = Vector2.zero;
        EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), (Vector2)transform.position, Provisional.OrdenY(transform.position.y) - 1, 0.8f, 12f);
        return perdioVida;
    }

    // ------------------------------------------------------------ poderes
    /// <summary>
    /// Aplica el efecto de un poder. Devuelve un mensaje que reemplaza al 'mensajeRecogido' del poder
    /// (por ejemplo cuando el botiquín no sirve porque las vidas están al máximo), o null.
    /// </summary>
    public string AplicarPoder(PoderPiso poder)
    {
        if (poder == null) return null;
        ComentariosJugador.Evento(ComentariosJugador.Tipo.Poder, this);
        switch (poder.tipo)
        {
            case TipoPoder.Invulnerabilidad:
                poderInvencible = poder;
                finInvencible = Time.time + poder.duracion * duracionPoderes;
                if (aroInvencible == null) aroInvencible = CrearAro("AroInvencible");
                break;

            case TipoPoder.ReparacionTotal:
                poderReparacion = poder;
                finReparacionTotal = Time.time + poder.duracion * duracionPoderes;
                if (aroReparacion == null) aroReparacion = CrearAro("AroReparacion");
                break;

            case TipoPoder.VidaExtra:
                GestorJuego gestor = FindAnyObjectByType<GestorJuego>();
                PisoCenital piso = PisoCenital.Actual;
                int maximo = piso != null && piso.Definicion != null ? piso.Definicion.vidasMaximas : 3;
                if (gestor == null || !gestor.GanarVida(poder.valor, maximo)) return "VIDAS AL MÁXIMO";
                break;

            case TipoPoder.Velocidad:
                poderTurbo = poder;
                finTurbo = Time.time + poder.duracion * duracionPoderes;
                if (aroTurbo == null) aroTurbo = CrearAro("AroTurbo");
                GestorAudio.Reproducir(SndExtra.PoderTurbo);
                break;

            case TipoPoder.ReparacionRapida:
                poderMartillo = poder;
                finMartilloRapido = Time.time + poder.duracion * duracionPoderes;
                if (aroMartillo == null) aroMartillo = CrearAro("AroMartillo");
                GestorAudio.Reproducir(SndExtra.PoderMartillo);
                break;

            case TipoPoder.CongelarEnemigos:
                GestorAudio.Reproducir(SndExtra.PoderCongelar);
                int congelados = GestorMiniVillanos.CongelarTodos(poder.duracion);
                bool demoledorCongelado = false;
                foreach (DemoledorCenital dc in DemoledorCenital.Todos)
                {
                    if (dc == null || !dc.EnPiso) continue;
                    dc.Congelar(poder.duracion * 0.6f);
                    demoledorCongelado |= dc.Congelado;
                }
                if (congelados == 0 && !demoledorCongelado) return "¡NADA QUE CONGELAR!";
                break;

            case TipoPoder.Casco:
                if (cascoPuesto) return "YA TIENES CASCO";
                cascoPuesto = true;
                GestorAudio.Reproducir(SndExtra.PoderCasco);
                break;
        }
        return null;
    }

    /// <summary>Estela del turbo y casco sobre la cabeza.</summary>
    private void ActualizarExtras()
    {
        if (Turbo && dibujo != null && dibujo.sprite != null && cuerpo != null && cuerpo.linearVelocity.sqrMagnitude > 1f && Time.time >= proximaEstela)
        {
            proximaEstela = Time.time + 0.05f;
            EstelaFantasma.Crear(dibujo, new Color(0.45f, 0.85f, 1f, 0.55f), 0.22f);
        }

        if (cascoPuesto && dibujoCasco == null)
        {
            dibujoCasco = Provisional.Crear(transform, "Casco", ArteExtra.Uno("Poderes/poder_casco"), Color.white, 0);
            if (dibujoCasco.sprite == null) { dibujoCasco.sprite = Provisional.Circulo; dibujoCasco.color = new Color(1f, 0.8f, 0.1f); }
        }
        if (dibujoCasco != null)
        {
            if (dibujoCasco.gameObject.activeSelf != cascoPuesto) dibujoCasco.gameObject.SetActive(cascoPuesto);
            if (cascoPuesto)
            {
                dibujoCasco.transform.localPosition = new Vector3(0f, alturaDibujo + 0.05f + Mathf.Sin(Time.time * 3f) * 0.03f, 0f);
                dibujoCasco.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
                dibujoCasco.sortingOrder = Provisional.OrdenY(transform.position.y) + 6;
            }
        }
    }

    /// <summary>Poderes activos con su tiempo restante, para el HUD ("INVENCIBLE 3,2 s").</summary>
    public string TextoPoderes()
    {
        bool inv = Time.time < finInvencible, rep = Time.time < finReparacionTotal;
        string t = "";
        if (IraActiva) t = Sumar(t, "¡FURIA!");
        if (inv) t = Sumar(t, (poderInvencible != null ? poderInvencible.etiquetaHud : "INVENCIBLE") + "  " + (finInvencible - Time.time).ToString("0.0") + " s");
        if (rep) t = Sumar(t, (poderReparacion != null ? poderReparacion.etiquetaHud : "REPARACIÓN TOTAL") + "  " + (finReparacionTotal - Time.time).ToString("0.0") + " s");
        if (Turbo) t = Sumar(t, (poderTurbo != null ? poderTurbo.etiquetaHud : "TURBO") + "  " + (finTurbo - Time.time).ToString("0.0") + " s");
        if (MartilloRapido) t = Sumar(t, (poderMartillo != null ? poderMartillo.etiquetaHud : "MARTILLO DORADO") + "  " + (finMartilloRapido - Time.time).ToString("0.0") + " s");
        if (cascoPuesto) t = Sumar(t, "CASCO");
        return t;
    }

    private static string Sumar(string a, string b) { return string.IsNullOrEmpty(a) ? b : a + "     " + b; }

    private SpriteRenderer CrearAro(string nombre)
    {
        SpriteRenderer sr = Provisional.Crear(transform, nombre, Provisional.Aro, Color.white, 0);
        sr.name = nombre;
        sr.gameObject.SetActive(false);
        return sr;
    }

    private void ActualizarAros()
    {
        ActualizarAro(aroInvencible, Invencible, poderInvencible, 0f, ArteNiveles.De(a => a.aroPoderDorado));
        ActualizarAro(aroReparacion, ReparacionTotal, poderReparacion, 0.12f, ArteNiveles.De(a => a.aroPoderVerde));
        ActualizarAro(aroTurbo, Turbo, poderTurbo, 0.24f, null);
        ActualizarAro(aroMartillo, MartilloRapido, poderMartillo, 0.36f, null);
    }

    private void ActualizarAro(SpriteRenderer aro, bool activo, PoderPiso poder, float extra, Sprite[] arte)
    {
        if (aro == null) return;
        if (aro.gameObject.activeSelf != activo) aro.gameObject.SetActive(activo);
        if (!activo) return;

        // Aro del arte (2 cuadros en bucle): dorado = invulnerable, verde = reparación total.
        if (arte != null && (poder == null || poder.spriteAro == null))
        {
            if (aro.GetComponent<AnimadorSprite>() == null) EfectoSprite.EnBucle(aro, arte, 6f);
            aro.color = Color.white;
            float pulsoArte = 1f + 0.03f * Mathf.Sin(Time.time * pulsoAro);
            aro.transform.localPosition = new Vector3(0f, alturaAro, 0f);
            aro.transform.localScale = new Vector3(pulsoArte, pulsoArte, 1f);
            aro.sortingOrder = Provisional.OrdenY(transform.position.y) + 4;
            return;
        }

        if (poder != null)
        {
            if (poder.spriteAro != null && aro.sprite != poder.spriteAro) aro.sprite = poder.spriteAro;
            Color c = poder.colorAro;
            float pulso = 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(Time.time * pulsoAro));
            aro.color = new Color(c.r, c.g, c.b, pulso);
        }
        float k = 1f + 0.04f * Mathf.Sin(Time.time * pulsoAro);
        aro.transform.localPosition = new Vector3(0f, alturaAro, 0f);
        aro.transform.localScale = new Vector3((tamanoAro.x + extra) * k, (tamanoAro.y + extra) * k, 1f);
        aro.sortingOrder = Provisional.OrdenY(transform.position.y) + 4;
    }

    private void CrearEstrellas()
    {
        estrellas = new GameObject("Estrellas").transform;
        estrellas.SetParent(transform, false);

        // Con arte: una sola animación de 4 cuadros que gira sobre la cabeza.
        Sprite[] arteAturdido = ArteNiveles.De(a => a.estrellasAturdido);
        if (arteAturdido != null)
        {
            SpriteRenderer unica = Provisional.Crear(estrellas, "EstrellasAturdido", arteAturdido[0], Color.white, 0);
            EfectoSprite.EnBucle(unica, arteAturdido, 8f);
            spritesEstrellas = new[] { unica };
            estrellasConArte = true;
            estrellas.gameObject.SetActive(false);
            return;
        }
        spritesEstrellas = new SpriteRenderer[Mathf.Max(1, numeroEstrellas)];
        for (int i = 0; i < spritesEstrellas.Length; i++)
        {
            SpriteRenderer sr = Provisional.Crear(estrellas, "Estrella" + (i + 1), Provisional.Estrella, new Color(1f, 0.9f, 0.2f, 1f), 0);
            sr.transform.localScale = new Vector3(tamanoEstrella, tamanoEstrella, 1f);
            spritesEstrellas[i] = sr;
        }
        estrellas.gameObject.SetActive(false);
    }

    private void ActualizarEstrellas(bool visibles)
    {
        if (estrellas == null) return;
        if (estrellas.gameObject.activeSelf != visibles) estrellas.gameObject.SetActive(visibles);
        if (!visibles) return;

        int n = spritesEstrellas.Length;
        int orden = Provisional.OrdenY(transform.position.y) + 5;
        if (estrellasConArte)
        {
            spritesEstrellas[0].transform.localPosition = new Vector3(0f, alturaEstrellas, 0f);
            spritesEstrellas[0].sortingOrder = orden;
            return;
        }
        for (int i = 0; i < n; i++)
        {
            float ang = (Time.time * velocidadEstrellas + i * 360f / n) * Mathf.Deg2Rad;
            spritesEstrellas[i].transform.localPosition = new Vector3(Mathf.Cos(ang) * radioEstrellas,
                alturaEstrellas + Mathf.Sin(ang) * radioEstrellas * 0.35f, 0f);
            spritesEstrellas[i].transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 200f);
            spritesEstrellas[i].sortingOrder = orden;
        }
    }

    /// <summary>Durante una escena (la llegada del demoledor) el jugador no se mueve ni martilla.</summary>
    public static bool EntradaBloqueada;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstatico() { EntradaBloqueada = false; }

    private void LeerEntrada()
    {
        if (EntradaBloqueada) { entrada = Vector2.zero; return; }
        if (controlRemoto)
        {
            entrada = entradaRemota;
            if (entrada.sqrMagnitude > 0.01f && !martillando) direccion = entrada;
            return;
        }
        float x = 0f, y = 0f;
        Keyboard k = Keyboard.current;
        if (k != null)
        {
            if (k[Key.A].isPressed || k[Key.LeftArrow].isPressed) x -= 1f;
            if (k[Key.D].isPressed || k[Key.RightArrow].isPressed) x += 1f;
            if (k[Key.W].isPressed || k[Key.UpArrow].isPressed) y += 1f;
            if (k[Key.S].isPressed || k[Key.DownArrow].isPressed) y -= 1f;
        }
        if (ControlesTactiles.Izquierda) x -= 1f;
        if (ControlesTactiles.Derecha) x += 1f;
        if (ControlesTactiles.Arriba) y += 1f;
        if (ControlesTactiles.Abajo) y -= 1f;

        entrada = new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
        if (entrada.sqrMagnitude > 1f) entrada.Normalize();
        if (entrada.sqrMagnitude > 0.01f && !martillando) direccion = entrada;
    }

    // ------------------------------------------------------------ martillo
    private void Martillar()
    {
        martillando = true;
        reparoHecho = false;
        inicioMartillo = Time.time;
        Sprite[] serie = SerieGolpe();
        float cps = cuadrosPorSegundoMartillo * RitmoMartillo * Personajes.FactorCuadros(serie);
        float duracion = serie != null && serie.Length > 0 ? serie.Length / Mathf.Max(1f, cps) : 0.35f / RitmoMartillo;
        finMartillo = inicioMartillo + Mathf.Max(0.3f / RitmoMartillo, duracion);
        proximoMartillo = finMartillo + enfriamiento / RitmoMartillo;
        if (serie != null) animador.Reproducir(serie, cps, false, true);
    }

    private void Reparar()
    {
        reparoHecho = true;
        GestorAudio.Reproducir(Snd.Martillazo);
        PisoCenital piso = PisoCenital.Actual;
        if (piso == null || piso.Mapa == null) return;

        // Si un demoledor va a golpear un objeto que está al alcance, ese tiene prioridad: así se puede bloquear.
        demoledorRef = null;
        ObjetoReparable amenazado = null;
        foreach (DemoledorCenital dc in DemoledorCenital.Todos)
        {
            if (dc == null || dc.ObjetivoAmenazado == null) continue;
            if (Vector2.Distance(Centro, dc.ObjetivoAmenazado.PuntoMasCercano(Centro)) > RadioActual) continue;
            demoledorRef = dc;
            amenazado = dc.ObjetivoAmenazado;
            break;
        }
        if (demoledorRef == null) demoledorRef = DemoledorCenital.MasCercano(Centro);

        // El martillo también ahuyenta a los mini villanos que estén al alcance.
        GestorMiniVillanos.GolpearEnRadio(Centro, RadioActual + 0.35f);
        if (ira != null) ira.MartillazoEn(Centro);
        if (!controlRemoto) EfectoMartillo.Golpe(Centro + direccion.normalized * 0.55f, Provisional.OrdenY(transform.position.y) + 12);

        ObjetoReparable mejor = null;
        float mejorDistancia = RadioActual;
        if (amenazado != null && Vector2.Distance(Centro, amenazado.PuntoMasCercano(Centro)) <= RadioActual)
        {
            // Un objeto intacto también se defiende: martillarlo mientras lo amenazan basta para bloquear.
            if (amenazado.Estado == EstadoObjeto.Intacto)
            {
                if (demoledorRef.IntentarBloqueo(amenazado)) ConfirmarBloqueo(piso);
                return;
            }
            mejor = amenazado;
            mejorDistancia = 0f;
        }
        foreach (ObjetoReparable o in piso.Mapa.objetos)
        {
            if (o == null || o.Estado == EstadoObjeto.Intacto) continue;
            float d = Vector2.Distance(Centro, o.PuntoMasCercano(Centro));
            if (d > mejorDistancia) continue;
            mejorDistancia = d;
            mejor = o;
        }
        if (mejor == null) return;
        bool reparo = false;
        EfectoSprite.Reproducir(ArteNiveles.De(a => a.chispasReparar), mejor.Centro, Provisional.OrdenY(mejor.Area.min.y) - 5, 1f, 12f);
        if (ReparacionTotal)
        {
            int pasos = mejor.RepararTodo();
            for (int i = 0; i < pasos; i++) piso.RegistrarReparacion();
            reparo = pasos > 0;
        }
        else if (mejor.Reparar())
        {
            piso.RegistrarReparacion();
            reparo = true;
        }

        if (reparo && ira != null) ira.SumarReparo(1);
        if (reparo && mejor.Estado == EstadoObjeto.Intacto)
        {
            GestorAudio.Reproducir(Snd.ObjetoReparado);
            ComentariosJugador.Evento(ComentariosJugador.Tipo.Reparar, this);
        }

        // Bloqueo: reparó el objeto justo cuando un demoledor iba a golpearlo.
        if (reparo)
            foreach (DemoledorCenital dc in DemoledorCenital.Todos)
                if (dc != null && dc.IntentarBloqueo(mejor)) { ConfirmarBloqueo(piso); break; }
    }

    private void ConfirmarBloqueo(PisoCenital piso)
    {
        piso.RegistrarBloqueo();
        GestorAudio.Reproducir(Snd.BloqueoSalvado);
        int puntos = piso.Definicion != null ? piso.Definicion.puntosBloqueo : 50;
        GestorJuego gestor = FindAnyObjectByType<GestorJuego>();
        if (gestor != null) gestor.RegistrarBloqueo(puntos);
        TextoFlotante.Mostrar(Centro + Vector2.up * 1.3f, "¡SALVADO!", new Color(0.4f, 1f, 0.5f));
        ComentariosJugador.Evento(ComentariosJugador.Tipo.Bloqueo, this);
        if (ira != null) ira.SumarBloqueo();
    }

    // ------------------------------------------------------------ daño
    /// <summary>Quita una vida (la cuenta GestorJuego). Devuelve false si está invulnerable.</summary>
    public bool RecibirGolpe()
    {
        if (!PuedeRecibirGolpe || EnElAire) return false;
        if (cascoPuesto)
        {
            // El casco aguanta este golpe y se rompe.
            cascoPuesto = false;
            finInvulnerable = Time.time + tiempoInvulnerable;
            GestorAudio.Reproducir(SndExtra.CascoRoto);
            TextoFlotante.Mostrar(Centro + Vector2.up * 1.4f, "¡EL CASCO TE SALVÓ!", new Color(1f, 0.85f, 0.25f));
            EfectoSprite.Reproducir(ArteNiveles.De(a => a.golpeEstallido), Centro + Vector2.up * 0.9f, Provisional.OrdenY(transform.position.y) + 7, 1f, 14f);
            return false;
        }
        finInvulnerable = Time.time + tiempoInvulnerable;
        finSacudida = Time.time + 0.28f;
        ImpactoPausa.Breve(0.07f);
        if (!controlRemoto) DemoledorUnifranz.UI.Menus.PreferenciasMenus.Vibrar(90);
        GestorAudio.Reproducir(Snd.RecibirDano);
        ComentariosJugador.Evento(ComentariosJugador.Tipo.Golpeado, this);
        if (Golpeado != null) Golpeado();
        return true;
    }

    public void Reaparecer(Vector3 posicion)
    {
        transform.position = posicion;
        if (cuerpo != null)
        {
            cuerpo.position = posicion;
            cuerpo.linearVelocity = Vector2.zero;
        }
        agarrado = lanzado = false;
        agarrador = null;
        elevacionVuelo = giroVuelo = 0f;
        if (cuerpo != null) cuerpo.simulated = true;
        finInvulnerable = 0f;
        finAturdido = 0f;
        finEmpuje = 0f;
        proximoChoque = 0f;
        finInvencible = 0f;
        finReparacionTotal = 0f;
        finTurbo = 0f;
        finMartilloRapido = 0f;
        cascoPuesto = Ficha != null && Ficha.id == "medicina";
        martillando = false;
        direccion = Vector2.down;
        if (dibujo != null) dibujo.enabled = true;
        MostrarQuieto();
    }

    private void Parpadear()
    {
        if (dibujo == null) return;
        bool invulnerable = Time.time < finInvulnerable;
        dibujo.enabled = !invulnerable || ((int)(Time.time * 12f) % 2 == 0);
    }

    // ------------------------------------------------------------ dibujo
    private Sprite[] SerieCaminar()
    {
        if (Mathf.Abs(direccion.x) > Mathf.Abs(direccion.y)) return animaciones.caminarDerecha;
        return direccion.y > 0f ? animaciones.caminarArriba : animaciones.caminarAbajo;
    }

    private Sprite[] SerieGolpe()
    {
        Sprite[] s;
        if (Mathf.Abs(direccion.x) > Mathf.Abs(direccion.y)) s = animaciones.golpeDerecha;
        else s = direccion.y > 0f ? animaciones.golpeArriba : animaciones.golpeAbajo;
        if (s == null || s.Length == 0) s = animaciones.golpeDerecha;
        return s;
    }

    private void MostrarQuieto()
    {
        Sprite[] s = SerieCaminar();
        if (s != null && s.Length > 0 && animador != null) animador.MostrarCuadroFijo(s, 0);
    }

    private void Animar()
    {
        if (dibujo == null) return;
        if (Mathf.Abs(direccion.x) > Mathf.Abs(direccion.y)) dibujo.flipX = direccion.x < 0f;
        else dibujo.flipX = false;

        if (!martillando)
        {
            Sprite[] serie = SerieCaminar();
            if (serie != null && serie.Length > 0)
            {
                if (entrada.sqrMagnitude > 0.01f) animador.Reproducir(serie, cuadrosCaminar * Personajes.FactorCuadros(serie), true, false);
                else animador.MostrarCuadroFijo(serie, 0);
            }
        }
        animador.Actualizar(Time.deltaTime);

        // Sin arte: cuadro azul provisional (se marca el martillazo con otro tono).
        if (dibujo.sprite == null)
        {
            dibujo.sprite = Provisional.Cuadrado;
            fueraDeCuadros = true;
        }
        if (fueraDeCuadros)
        {
            dibujo.color = martillando ? new Color(1f, 0.85f, 0.3f) : new Color(0.3f, 0.55f, 1f);
        }
    }

    /// <summary>Escala el sprite a 'alturaDibujo' baldosas y apoya sus pies en el punto del jugador.</summary>
    private void AjustarDibujo()
    {
        Sprite s = dibujo.sprite;
        if (s == null) return;
        Bounds b = s.bounds;
        if (b.size.y <= 0.0001f) return;

        float escala;
        if (fueraDeCuadros && s == Provisional.Cuadrado) escala = 1f;
        else escala = alturaDibujo / b.size.y;

        Vector3 tam = fueraDeCuadros && s == Provisional.Cuadrado
            ? new Vector3(0.6f, alturaDibujo * 0.8f, 1f) : new Vector3(escala, escala, 1f);
        // Animación por código encima de los cuadros: respira quieto, rebota al caminar, se aplasta al recibir un golpe,
        // crece con la furia y salta de alegría al superar el piso.
        float t = Time.time + fase;
        float sube = 0f, giro = 0f;
        bool camina = entrada.sqrMagnitude > 0.01f && !martillando;
        if (!camina && !martillando) { tam.y *= 1f + 0.03f * Mathf.Sin(t * 3f); tam.x *= 1f - 0.012f * Mathf.Sin(t * 3f); }
        if (camina) sube = Mathf.Abs(Mathf.Sin(t * 11f)) * 0.06f;
        if (Time.time < finSacudida)
        {
            float k = (finSacudida - Time.time) / 0.28f;
            tam.x *= 1f + 0.22f * k; tam.y *= 1f - 0.2f * k;
            giro = Mathf.Sin(Time.time * 50f) * 8f * k;
        }
        if (IraActiva) { float e = 1.12f + 0.03f * Mathf.Sin(t * 8f); tam.x *= e; tam.y *= e; }
        if (Resbalando) giro += Mathf.Sin(Time.time * 18f) * 12f;
        if (ira != null && ira.Transformando) { float e = 1f + 0.15f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 18f)); tam.x *= e; tam.y *= e; giro = Mathf.Sin(Time.unscaledTime * 40f) * 4f; }
        PisoCenital piso = PisoCenital.Actual;
        if (piso != null && piso.Terminado) { sube = Mathf.Abs(Mathf.Sin(t * 7f)) * 0.3f; }
        if (EnElAire) { sube += elevacionVuelo; giro += giroVuelo; }

        dibujo.transform.localScale = tam;
        float signo = dibujo.flipX ? -1f : 1f;
        dibujo.transform.localPosition = new Vector3(-b.center.x * tam.x * signo, -b.min.y * tam.y + sube, 0f);
        dibujo.transform.localRotation = Quaternion.Euler(0f, 0f, giro);
        dibujo.sortingOrder = Provisional.OrdenY(transform.position.y) + (EnElAire ? 60 : 0);
    }

    // ------------------------------------------------------------ agarrado y lanzado por el demoledor
    // Si el demoledor te tiene cerca te agarra, te levanta y te lanza lejos, a otra sala: así nunca quedas encerrado
    // recibiendo golpe tras golpe. Al caer tienes unos segundos de invulnerabilidad para reponerte.
    private bool agarrado, lanzado;
    private Transform agarrador;
    private float alturaAgarre, inicioVuelo, duracionVuelo, alturaVuelo, elevacionVuelo, giroVuelo;
    private Vector2 vueloDesde, vueloHasta;

    /// <summary>Está en las manos del demoledor o volando.</summary>
    public bool EnElAire { get { return agarrado || lanzado; } }

    /// <summary>El demoledor lo agarra: queda en el aire sobre su cabeza hasta que lo lance o lo suelte.</summary>
    public void SerAgarrado(Transform quien, float altura)
    {
        agarrado = true;
        lanzado = false;
        agarrador = quien;
        alturaAgarre = altura;
        martillando = false;
        entrada = Vector2.zero;
        finEmpuje = 0f;
        if (cuerpo != null) { cuerpo.linearVelocity = Vector2.zero; cuerpo.simulated = false; }
        finSacudida = Time.time + 0.28f;
    }

    /// <summary>Sale volando en arco hasta 'destino' (pasa por encima de paredes y muebles).</summary>
    public void SerLanzado(Vector2 destino, float duracion, float altura)
    {
        agarrado = false;
        lanzado = true;
        agarrador = null;
        vueloDesde = transform.position;
        vueloHasta = destino;
        inicioVuelo = Time.time;
        duracionVuelo = Mathf.Max(0.2f, duracion);
        alturaVuelo = altura;
        if (cuerpo != null) { cuerpo.linearVelocity = Vector2.zero; cuerpo.simulated = false; }
    }

    /// <summary>El demoledor lo suelta sin lanzarlo (lo congelaron o lo aturdieron mientras lo tenía): cae ahí mismo.</summary>
    public void SoltarAgarre()
    {
        if (!agarrado) return;
        SerLanzado((Vector2)transform.position + Vector2.down * 0.6f, 0.3f, 0.6f);
    }

    private void ActualizarVuelo()
    {
        if (agarrado)
        {
            if (agarrador == null) { SoltarAgarre(); return; }
            Vector2 p = agarrador.position;
            transform.position = p;
            if (cuerpo != null) cuerpo.position = p;
            elevacionVuelo = alturaAgarre + Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.08f;
            giroVuelo = Mathf.Sin(Time.time * 22f) * 18f;   // patalea
            return;
        }
        float k = Mathf.Clamp01((Time.time - inicioVuelo) / duracionVuelo);
        Vector2 pos = Vector2.Lerp(vueloDesde, vueloHasta, k);
        transform.position = pos;
        if (cuerpo != null) cuerpo.position = pos;
        elevacionVuelo = Mathf.Sin(k * Mathf.PI) * alturaVuelo + (1f - k) * 0.4f * (alturaVuelo > 1f ? 1f : 0f);
        giroVuelo = -k * 720f * Mathf.Sign(vueloHasta.x - vueloDesde.x + 0.001f);
        if (k < 1f) return;
        Aterrizar();
    }

    private void Aterrizar()
    {
        lanzado = false;
        elevacionVuelo = 0f;
        giroVuelo = 0f;
        if (cuerpo != null) { cuerpo.simulated = true; cuerpo.position = vueloHasta; cuerpo.linearVelocity = Vector2.zero; }
        velocidadActual = Vector2.zero;
        // Margen para reponerse: un instante mareado y varios segundos sin que nada le quite vidas.
        finAturdido = Mathf.Max(finAturdido, Time.time + 0.35f);
        finInvulnerable = Mathf.Max(finInvulnerable, Time.time + 2.5f);
        proximoChoque = Time.time + 2.5f;
        finSacudida = Time.time + 0.28f;
        EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), (Vector2)transform.position, Provisional.OrdenY(transform.position.y) - 1, 1.1f, 12f);
        GestorAudio.Reproducir(Snd.RecibirDano, 0.6f, 0.8f);
        if (!controlRemoto) CamaraPisoCenital.Temblar(0.08f, 0.2f);
    }

}
