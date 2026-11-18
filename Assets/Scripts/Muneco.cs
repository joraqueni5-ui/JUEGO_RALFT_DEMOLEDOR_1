using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// El muñeco (el que hace de Félix). Es el ÚNICO personaje que controla el jugador.
/// Camina sobre las repisas del edificio, salta a la repisa de arriba (ESPACIO o ↑),
/// se deja caer a la de abajo (S) y repara con el martillo, también en el aire.
///
/// Si cae por un hueco por debajo de su sección, rebota hacia arriba con un rato de
/// inmunidad para alcanzar una repisa; si sigue cayendo cuando se acaba, pierde una
/// vida y reaparece en una repisa segura. Si le cae un escombro o choca con el
/// demoledor también pierde una vida.
/// Solo se sostiene si el centro de sus pies está sobre una repisa: si queda
/// colgando del borde, resbala y cae al hueco.
/// </summary>
public class Muneco : PersonajeBase
{
    [Header("Animaciones")]
    public Sprite[] caminar;
    [Tooltip("Se usa mientras sube de un salto, de espaldas contra la fachada.")]
    public Sprite[] caminarEspaldas;
    public Sprite[] martillo;
    public float cuadrosCaminar = 8f;
    public float cuadrosPorSegundoMartillo = 12f;

    [Header("Reparación")]
    [Tooltip("Distancia máxima a la que alcanza el martillo (una cosa por golpe).")]
    public float radioReparacion = 0.55f;
    [Tooltip("Altura del pecho: desde ahí se busca lo que hay que reparar.")]
    public float alturaPecho = 0.45f;
    public float momentoDeReparacion = 0.15f;
    public float enfriamiento = 0.28f;

    [Header("Repisas")]
    [Tooltip("Segundos que atraviesa la repisa de abajo al pulsar la tecla para bajar.")]
    public float tiempoAtravesar = 0.3f;

    [Header("Caídas")]
    [Tooltip("Cuánto por debajo del piso de su sección cuenta como caída al hueco.")]
    public float margenCaida = 0.7f;
    [Tooltip("Segundos de inmunidad del rebote al caer por un hueco.")]
    public float tiempoInmunidadCaida = 1.4f;

    [Header("Bordes")]
    [Tooltip("Segundos colgando del borde (sin pisar con el centro) antes de resbalar.")]
    public float tiempoParaResbalar = 0.08f;

    [Header("Choque con el demoledor")]
    [Tooltip("Medio ancho y alto de la zona del demoledor que cuenta como choque.")]
    public Vector2 tamanoDemoledor = new Vector2(0.35f, 1.0f);
    [Tooltip("Empujón que recibe al chocar (hacia el costado y hacia abajo).")]
    public Vector2 empujeDemoledor = new Vector2(3f, -4f);

    [Header("Golpes de escombros")]
    [Tooltip("Segundos sin recibir otro golpe después de uno (parpadea).")]
    public float tiempoInvulnerable = 1.5f;
    [Tooltip("Segundos que queda aturdido sin moverse al recibir un golpe.")]
    public float tiempoAturdido = 0.35f;

    /// <summary>Perdió una vida (escombro o caída). El gestor descuenta la vida.</summary>
    public event System.Action Golpeado;

    [Header("Trepar (zonas trepables de la fachada)")]
    [Tooltip("Unidades por segundo al subir o bajar trepando (más lento que caminar).")]
    public float velocidadTrepar = 1.6f;
    [Tooltip("Impulso (lado, altura) al soltarse trepando con el salto.")]
    public Vector2 impulsoSoltarse = new Vector2(3.5f, 5f);
    [Tooltip("Cuadros por segundo de la animación de trepar.")]
    public float cuadrosTrepar = 6f;
    [Tooltip("Animación de trepar (2 a 4 cuadros). Vacía: usa el sprite de espaldas alternando el volteo horizontal.")]
    public Sprite[] animacionTrepar;
    [Tooltip("Segundos entre volteos del sprite de espaldas mientras se mueve trepando.")]
    public float segundosPorVolteo = 0.18f;
    [Tooltip("Distancia horizontal máxima a la repisa donde queda al soltarse arriba o abajo.")]
    public float alcanceRepisaAlSoltar = 1.6f;
    [Tooltip("Segundos sin poder agarrarse de nuevo después de soltarse.")]
    public float esperaParaAgarrar = 0.3f;

    [Header("Controles")]
    public Key teclaIzquierda = Key.A;
    public Key teclaDerecha = Key.D;
    public Key teclaSubir = Key.UpArrow;
    [Tooltip("Bajar a la repisa de abajo.")]
    public Key teclaBajarRepisa = Key.S;
    public Key teclaSaltar = Key.Space;
    public Key teclaReparar = Key.Enter;

    private bool reparando;
    private float finDeReparacion;
    private float siguienteReparacion;
    private float duracionReparacion = 0.33f;
    private EdificioAlto edificio;
    private float finInvulnerable;
    private float finAturdido;
    private bool rebotando;
    private bool abajoTactilAntes;
    private readonly System.Collections.Generic.List<Collider2D> repisasIgnoradas = new System.Collections.Generic.List<Collider2D>();
    private float colgandoDesde = -1f;
    private DemoledorAutomatico demoledor;
    private float empujeHorizontal;
    private bool trepando;
    private ZonaTrepable zonaTrepada;
    private float noAgarrarHasta;
    private float proximoVolteo;
    private bool volteoTrepar;

    /// <summary>Está agarrado a una zona trepable de la fachada.</summary>
    public bool Trepando { get { return trepando; } }

    /// <summary>La sección más alta a la que ya llegó: de ahí para abajo no puede volver.</summary>
    public int SeccionAlcanzada { get; private set; }

    /// <summary>True si un escombro le puede quitar una vida en este momento.</summary>
    public bool PuedeRecibirGolpe { get { return enabled && Time.time >= finInvulnerable; } }

    protected override void Awake()
    {
        base.Awake();

        if (martillo != null && martillo.Length > 0)
        {
            duracionReparacion = martillo.Length / Mathf.Max(1f, cuadrosPorSegundoMartillo);
        }
    }

    private void Start()
    {
        edificio = EdificioAlto.Instancia;
        demoledor = Object.FindFirstObjectByType<DemoledorAutomatico>();
    }

    private void OnDisable()
    {
        // Que no quede invisible si se pausa justo en un parpadeo.
        if (dibujo != null) dibujo.enabled = true;
    }

    private void Update()
    {
        animador.Actualizar(Time.deltaTime);
        if (edificio == null) edificio = EdificioAlto.Instancia;

        if (reparando && Time.time >= finDeReparacion) reparando = false;

        // Se leen siempre, para que un toque no quede guardado para después.
        bool repararAhora = Pulsada(teclaReparar) | ControlesTactiles.RepararPulsado();
        // Dentro de una zona trepable, arriba (o abajo) agarra en vez de saltar.
        ZonaTrepable zonaCerca = colisionador != null ? ZonaTrepable.En(colisionador.bounds.center) : null;
        bool soltarAhora = Pulsada(teclaSaltar) | ControlesTactiles.SaltarPulsado();
        bool saltarAhora = soltarAhora | (zonaCerca == null && (Pulsada(teclaSubir) | ControlesTactiles.ArribaPulsado()));
        bool arribaMantenida = Mantenida(teclaSubir) || ControlesTactiles.Arriba;
        bool abajoMantenida = Mantenida(teclaBajarRepisa) || ControlesTactiles.Abajo;
        bool abajoTactil = ControlesTactiles.Abajo;
        bool bajarAhora = Pulsada(teclaBajarRepisa) || (abajoTactil && !abajoTactilAntes);
        abajoTactilAntes = abajoTactil;

        if (Time.time < finAturdido)
        {
            // Aturdido: no se mueve ni martilla un instante (solo lo lleva el empujón).
            entradaHorizontal = empujeHorizontal;
        }
        else if (trepando)
        {
            ActualizarTrepar(arribaMantenida, abajoMantenida, soltarAhora);

            if (repararAhora && !reparando && Time.time >= siguienteReparacion)
            {
                StartCoroutine(RutinaMartillar());
            }
        }
        else
        {
            if (zonaCerca != null && !reparando && Time.time >= noAgarrarHasta && (arribaMantenida || abajoMantenida))
                Agarrarse(zonaCerca, arribaMantenida, abajoMantenida);

            if (!trepando)
            {
                Moverse(saltarAhora, bajarAhora);

                if (repararAhora && !reparando && Time.time >= siguienteReparacion)
                {
                    StartCoroutine(RutinaMartillar());
                }
            }
        }

        GestorAudio.Bucle("poder_muneco", Snd.PoderActivoBucle, Time.time < finPoderSonido);
        GestorAudio.Bucle("aturdido_muneco", Snd.AturdidoBucle, Time.time < finAturdido);
        RevisarRepisaPisada();
        RevisarBorde();
        RevisarChoqueConDemoledor();
        RevisarCaida();
        ActualizarAnimacion();
        ActualizarParpadeo();
    }

    // ------------------------------------------------------------ trepar
    /// <summary>Hasta dónde puede subir una zona: no más arriba de la cornisa del demoledor.</summary>
    private float TopeTrepar(Bounds zona)
    {
        float tope = zona.max.y;
        if (edificio != null) tope = Mathf.Min(tope, edificio.AlturaCornisa(edificio.SeccionActual) - 0.1f);
        return tope;
    }

    private void Agarrarse(ZonaTrepable zona, bool arriba, bool abajo)
    {
        Bounds zb = zona.Area.bounds;
        float pies = colisionador.bounds.min.y;
        bool puedeSubir = pies < TopeTrepar(zb) - 0.15f;
        bool puedeBajar = pies > zb.min.y + 0.15f;
        if (!((arriba && puedeSubir) || (abajo && puedeBajar))) return;

        trepando = true;
        zonaTrepada = zona;
        usaGravedad = false;
        enSuelo = false;
        entradaHorizontal = 0f;
        cuerpo.gravityScale = 0f;
        cuerpo.linearVelocity = Vector2.zero;
        DejarDeAtravesar();
        colisionador.excludeLayers = capaSuelo;   // trepando no choca con repisas ni suelo

        Vector3 p = new Vector3(zona.PuntoAgarreX, transform.position.y, transform.position.z);
        transform.position = p;
        cuerpo.position = p;
        volteoTrepar = false;
        proximoVolteo = Time.time + segundosPorVolteo;
    }

    private void ActualizarTrepar(bool arriba, bool abajo, bool soltar)
    {
        if (zonaTrepada == null || zonaTrepada.Area == null || !zonaTrepada.isActiveAndEnabled) { TerminarTrepar(); return; }

        // Salto: se suelta hacia el costado.
        if (soltar && !reparando)
        {
            float lado = 0f;
            if (Mantenida(teclaIzquierda) || ControlesTactiles.Izquierda) lado -= 1f;
            if (Mantenida(teclaDerecha) || ControlesTactiles.Derecha) lado += 1f;
            if (lado == 0f) lado = mirandoDerecha ? 1f : -1f;
            mirandoDerecha = lado > 0f;
            TerminarTrepar();
            cuerpo.linearVelocity = new Vector2(lado * impulsoSoltarse.x, impulsoSoltarse.y);
            return;
        }

        Bounds zb = zonaTrepada.Area.bounds;
        float v = 0f;
        if (arriba) v += 1f;
        if (abajo) v -= 1f;
        if (reparando) v = 0f;

        float pies = colisionador.bounds.min.y;
        if (v > 0f && pies >= TopeTrepar(zb) - 0.02f) { SoltarEnRepisa(true, zb); return; }
        if (v < 0f && pies <= zb.min.y + 0.02f) { SoltarEnRepisa(false, zb); return; }

        cuerpo.linearVelocity = new Vector2(0f, v * velocidadTrepar);
        if (v != 0f && Time.time >= proximoPasoSonido)
        {
            GestorAudio.Reproducir(Snd.Trepar);
            proximoPasoSonido = Time.time + 0.3f;
        }
    }

    private float proximoPasoSonido, finPoderSonido;
    private bool pasoAlternoSonido;

    /// <summary>Al llegar arriba o abajo de la zona queda sobre la repisa cercana (o sobre la vereda abajo).</summary>
    private void SoltarEnRepisa(bool arriba, Bounds zona)
    {
        Vector2 pos = transform.position;
        RepisaEdificio r = edificio != null ? edificio.RepisaMasCercana(pos, alcanceRepisaAlSoltar, arriba ? 0.4f : 0.1f) : null;
        Vector3 destino = transform.position;
        if (r != null) destino = new Vector3(r.CentroX, r.Superficie + 0.03f, transform.position.z);
        else if (!arriba) destino = new Vector3(transform.position.x, zona.min.y + 0.03f, transform.position.z);

        TerminarTrepar();
        Colocar(destino);
    }

    private void TerminarTrepar()
    {
        if (!trepando) return;
        trepando = false;
        zonaTrepada = null;
        usaGravedad = true;
        if (colisionador != null) colisionador.excludeLayers = 0;
        if (cuerpo != null)
        {
            cuerpo.gravityScale = gravedadBase;
            cuerpo.linearVelocity = Vector2.zero;
        }
        noAgarrarHasta = Time.time + esperaParaAgarrar;
    }

    /// <summary>Se acabó el suelo bajo sus pies (la vereda se derrumbó): cae al vacío y pierde una vida.</summary>
    public void CaerAlVacioForzado()
    {
        GestorAudio.Reproducir(Snd.CaidaVacio);
        TerminarTrepar();
        rebotando = true;
        finInvulnerable = 0f;
    }

    // ------------------------------------------------------------ movimiento
    private void Moverse(bool saltar, bool bajar)
    {
        float direccion = 0f;
        if (Mantenida(teclaIzquierda) || ControlesTactiles.Izquierda) direccion -= 1f;
        if (Mantenida(teclaDerecha) || ControlesTactiles.Derecha) direccion += 1f;
        if (!reparando && direccion != 0f) mirandoDerecha = direccion > 0f;

        // Martillando en una repisa se queda quieto; en el aire sigue su salto.
        entradaHorizontal = reparando && enSuelo ? 0f : direccion;

        // Pasos mientras camina por una repisa.
        if (enSuelo && !reparando && direccion != 0f && Time.time >= proximoPasoSonido)
        {
            GestorAudio.Reproducir(pasoAlternoSonido ? Snd.Paso2 : Snd.Paso1);
            pasoAlternoSonido = !pasoAlternoSonido;
            proximoPasoSonido = Time.time + 0.28f;
        }

        if (reparando) return;

        // Solo salta si está parado (no mientras atraviesa una repisa subiendo).
        if (saltar && cuerpo.linearVelocity.y <= 0.5f) { if (Saltar()) GestorAudio.Reproducir(Snd.Salto); }
        else if (bajar && enSuelo) AtravesarRepisa();
    }

    /// <summary>Se deja caer a través de la repisa que pisa (no la primera fila de su sección).</summary>
    private void AtravesarRepisa()
    {
        RepisaEdificio repisa = RepisaBajoLosPies();
        if (repisa == null || repisa.colision == null || colisionador == null) return;
        if (edificio != null && repisa.piso == edificio.PrimerPisoDeSeccion(SeccionAlcanzada)) return;

        StartCoroutine(RutinaAtravesar(repisa.colision));
    }

    private IEnumerator RutinaAtravesar(Collider2D repisa)
    {
        Ignorar(repisa);
        cuerpo.linearVelocity = new Vector2(cuerpo.linearVelocity.x, -1.5f);
        yield return new WaitForSeconds(tiempoAtravesar);
        DejarDeAtravesar();
    }

    /// <summary>Deja de chocar un rato con una repisa (para atravesarla o resbalar de ella).</summary>
    private void Ignorar(Collider2D repisa)
    {
        if (repisa == null || colisionador == null || repisasIgnoradas.Contains(repisa)) return;
        Physics2D.IgnoreCollision(colisionador, repisa, true);
        repisasIgnoradas.Add(repisa);
    }

    /// <summary>Vuelve a chocar con las repisas que estaba atravesando.</summary>
    private void DejarDeAtravesar()
    {
        for (int i = 0; i < repisasIgnoradas.Count; i++)
        {
            if (repisasIgnoradas[i] != null && colisionador != null)
                Physics2D.IgnoreCollision(colisionador, repisasIgnoradas[i], false);
        }
        repisasIgnoradas.Clear();
    }

    /// <summary>
    /// Si está apoyado en una repisa pero el centro de sus pies no la pisa (colgando
    /// del borde del hueco), resbala: deja de chocar con esa repisa y cae.
    /// </summary>
    private void RevisarBorde()
    {
        if (trepando) { colgandoDesde = -1f; return; }
        bool apoyado = !enSuelo && Mathf.Abs(cuerpo.linearVelocity.y) < 0.05f;
        if (!apoyado)
        {
            colgandoDesde = -1f;
            return;
        }

        if (colgandoDesde < 0f) colgandoDesde = Time.time;
        if (Time.time - colgandoDesde < tiempoParaResbalar) return;

        Bounds caja = colisionador.bounds;
        Collider2D[] tocadas = Physics2D.OverlapBoxAll(new Vector2(caja.center.x, caja.min.y - 0.03f),
            new Vector2(caja.size.x, 0.06f), 0f, capaSuelo);
        bool resbalo = false;
        for (int i = 0; i < tocadas.Length; i++)
        {
            if (tocadas[i] == null || tocadas[i].GetComponent<RepisaEdificio>() == null) continue;
            Ignorar(tocadas[i]);
            resbalo = true;
        }
        if (resbalo)
        {
            colgandoDesde = -1f;
            StartCoroutine(VolverAChocar());
        }
    }

    private IEnumerator VolverAChocar()
    {
        yield return new WaitForSeconds(0.3f);
        DejarDeAtravesar();
    }

    /// <summary>Si toca al demoledor pierde una vida y sale empujado.</summary>
    private void RevisarChoqueConDemoledor()
    {
        if (demoledor == null || !demoledor.isActiveAndEnabled || !PuedeRecibirGolpe) return;

        Vector2 d = demoledor.transform.position;
        Bounds caja = colisionador.bounds;
        bool tocaX = caja.max.x > d.x - tamanoDemoledor.x && caja.min.x < d.x + tamanoDemoledor.x;
        bool tocaY = caja.max.y > d.y && caja.min.y < d.y + tamanoDemoledor.y;
        if (!tocaX || !tocaY) return;

        RecibirGolpe();
        float lado = transform.position.x < d.x ? -1f : 1f;
        empujeHorizontal = lado * empujeDemoledor.x / Mathf.Max(0.01f, velocidad);
        cuerpo.linearVelocity = new Vector2(lado * empujeDemoledor.x, empujeDemoledor.y);
    }

    /// <summary>La repisa sobre la que está parado (o null).</summary>
    private RepisaEdificio RepisaBajoLosPies()
    {
        if (colisionador == null) return null;
        Bounds caja = colisionador.bounds;
        Vector2 origen = new Vector2(caja.center.x, caja.min.y + 0.02f);
        RaycastHit2D[] golpes = Physics2D.RaycastAll(origen, Vector2.down, margenSuelo + 0.05f, capaSuelo);
        for (int i = 0; i < golpes.Length; i++)
        {
            if (golpes[i].collider == null || golpes[i].collider == colisionador) continue;
            RepisaEdificio r = golpes[i].collider.GetComponent<RepisaEdificio>();
            if (r != null) return r;
        }
        return null;
    }

    /// <summary>Al pisar una repisa de una sección nueva (ya habilitada), esa pasa a ser su piso.</summary>
    private void RevisarRepisaPisada()
    {
        if (!enSuelo || edificio == null) return;

        RepisaEdificio r = RepisaBajoLosPies();
        if (r == null) return;

        rebotando = false;
        if (r.seccion > SeccionAlcanzada && r.seccion <= edificio.SeccionActual) SeccionAlcanzada = r.seccion;
    }

    // ------------------------------------------------------------ caídas
    private void RevisarCaida()
    {
        if (edificio == null || cuerpo == null) return;

        // Con la vereda firme, caer no cuesta una vida: aterriza en la vereda. Trepando tampoco se cae.
        if (trepando || edificio.VeredaFirme) { rebotando = false; return; }

        float piso = edificio.PisoSeccion(SeccionAlcanzada);
        if (transform.position.y >= piso - margenCaida || cuerpo.linearVelocity.y > 0f) return;

        if (!rebotando)
        {
            // Primera vez: rebote con inmunidad para que alcance una repisa.
            rebotando = true;
            finInvulnerable = Time.time + tiempoInmunidadCaida;
            Rebotar(piso);
        }
        else if (Time.time < finInvulnerable)
        {
            Rebotar(piso);
        }
        else
        {
            PerderVidaPorCaida();
        }
    }

    /// <summary>Lo lanza hacia arriba lo justo para volver a la altura de las repisas.</summary>
    private void Rebotar(float piso)
    {
        float gravedad = Mathf.Abs(Physics2D.gravity.y) * gravedadBase;
        float subir = Mathf.Max(0.5f, piso + 0.6f - transform.position.y);
        float velocidadY = Mathf.Min(17f, Mathf.Sqrt(2f * gravedad * subir));
        cuerpo.linearVelocity = new Vector2(cuerpo.linearVelocity.x, velocidadY);
    }

    private void PerderVidaPorCaida()
    {
        GestorAudio.Reproducir(Snd.CaidaVacio);
        rebotando = false;
        finInvulnerable = Time.time + tiempoInvulnerable;
        if (Golpeado != null) Golpeado();
        if (!enabled) return;   // se acabó la partida

        RepisaEdificio segura = edificio.RepisaSegura(SeccionAlcanzada, transform.position.x);
        Vector3 destino = segura != null
            ? new Vector3(segura.CentroX, segura.Superficie + 0.02f, transform.position.z)
            : new Vector3(transform.position.x, edificio.PisoSeccion(SeccionAlcanzada) + 0.02f, transform.position.z);
        Colocar(destino);
    }

    // ------------------------------------------------------------ golpes
    /// <summary>Lo vuelve invulnerable 'segundos' (poder): parpadea y no recibe golpes.</summary>
    public void DarInvulnerabilidad(float segundos)
    {
        finInvulnerable = Mathf.Max(finInvulnerable, Time.time + Mathf.Max(0f, segundos));
        finPoderSonido = Mathf.Max(finPoderSonido, Time.time + Mathf.Max(0f, segundos));
    }

    /// <summary>Centro del cuerpo, para detectar contactos (palomas, poderes).</summary>
    public Vector2 CentroCuerpo { get { return (Vector2)transform.position + Vector2.up * 0.5f; } }

    /// <summary>Le cae un escombro: avisa al gestor, queda aturdido y parpadea un rato.</summary>
    public void RecibirGolpe()
    {
        if (!PuedeRecibirGolpe) return;
        if (trepando) TerminarTrepar();   // un golpe lo suelta

        finInvulnerable = Time.time + tiempoInvulnerable;
        finAturdido = Time.time + tiempoAturdido;
        empujeHorizontal = 0f;
        GestorAudio.Reproducir(Snd.RecibirDano);
        if (Golpeado != null) Golpeado();
    }

    /// <summary>Lo deja en 'posicion', quieto y sin parpadear (al reiniciar la partida).</summary>
    public void Reaparecer(Vector3 posicion)
    {
        StopAllCoroutines();
        TerminarTrepar();
        DejarDeAtravesar();
        reparando = false;
        rebotando = false;
        finInvulnerable = 0f;
        finAturdido = 0f;
        SeccionAlcanzada = 0;
        Colocar(posicion);
        if (dibujo != null) dibujo.enabled = true;
    }

    private void Colocar(Vector3 posicion)
    {
        entradaHorizontal = 0f;
        transform.position = posicion;
        if (cuerpo != null)
        {
            cuerpo.position = posicion;
            cuerpo.linearVelocity = Vector2.zero;
        }
    }

    private void ActualizarParpadeo()
    {
        if (dibujo == null) return;
        bool parpadeando = Time.time < finInvulnerable;
        dibujo.enabled = !parpadeando || Mathf.FloorToInt(Time.time * 12f) % 2 == 0;
    }

    // ------------------------------------------------------------ martillo
    private IEnumerator RutinaMartillar()
    {
        reparando = true;
        siguienteReparacion = Time.time + enfriamiento;
        finDeReparacion = Time.time + duracionReparacion;

        yield return new WaitForSeconds(momentoDeReparacion);
        GestorAudio.Reproducir(Snd.Martillazo);

        if (edificio == null) edificio = EdificioAlto.Instancia;
        if (edificio != null)
        {
            Vector2 pecho = (Vector2)transform.position + new Vector2(0f, alturaPecho);
            edificio.RepararMasCercano(pecho, radioReparacion);
        }
    }

    // ------------------------------------------------------------ dibujo
    private void ActualizarAnimacion()
    {
        if (dibujo == null) return;

        if (reparando)
        {
            dibujo.flipX = false;
            animador.Reproducir(martillo, cuadrosPorSegundoMartillo, false, false);
            return;
        }

        if (trepando)
        {
            bool mueve = Mathf.Abs(cuerpo.linearVelocity.y) > 0.05f;
            if (animacionTrepar != null && animacionTrepar.Length > 1)
            {
                dibujo.flipX = false;
                if (mueve) animador.Reproducir(animacionTrepar, cuadrosTrepar, true, false);
                else animador.MostrarCuadroFijo(animacionTrepar, 0);
            }
            else if (caminarEspaldas != null && caminarEspaldas.Length > 0)
            {
                // De espaldas contra la fachada: alterna el volteo mientras se mueve y queda quieto si no.
                animador.MostrarCuadroFijo(caminarEspaldas, 0);
                if (mueve && Time.time >= proximoVolteo)
                {
                    volteoTrepar = !volteoTrepar;
                    proximoVolteo = Time.time + segundosPorVolteo;
                }
                dibujo.flipX = volteoTrepar;
            }
            return;
        }

        if (!enSuelo)
        {
            // Subiendo: de espaldas contra la fachada. Bajando: el cuadro de caminar con el pie arriba.
            if (cuerpo.linearVelocity.y > 0.1f && caminarEspaldas != null && caminarEspaldas.Length > 1)
            {
                dibujo.flipX = false;
                animador.MostrarCuadroFijo(caminarEspaldas, 1);
            }
            else
            {
                dibujo.flipX = !mirandoDerecha;
                animador.MostrarCuadroFijo(caminar, 2);
            }
            return;
        }

        dibujo.flipX = !mirandoDerecha;
        if (Mathf.Abs(entradaHorizontal) > 0.01f) animador.Reproducir(caminar, cuadrosCaminar, true, false);
        else animador.MostrarCuadroFijo(caminar, 0);
    }

    // ------------------------------------------------------------ entrada
    /// <summary>Tecla del teclado pulsada en este cuadro (sin fallar si no hay teclado, como en Android).</summary>
    private static bool Pulsada(Key tecla)
    {
        return Keyboard.current != null && Keyboard.current[tecla].wasPressedThisFrame;
    }

    /// <summary>Tecla del teclado mantenida (sin fallar si no hay teclado, como en Android).</summary>
    private static bool Mantenida(Key tecla)
    {
        return Keyboard.current != null && Keyboard.current[tecla].isPressed;
    }
}
