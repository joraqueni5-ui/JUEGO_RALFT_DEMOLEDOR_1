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
/// vida y reaparece en una repisa segura. Si le cae un escombro también pierde una vida.
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

    [Header("Golpes de escombros")]
    [Tooltip("Segundos sin recibir otro golpe después de uno (parpadea).")]
    public float tiempoInvulnerable = 1.5f;
    [Tooltip("Segundos que queda aturdido sin moverse al recibir un golpe.")]
    public float tiempoAturdido = 0.35f;

    /// <summary>Perdió una vida (escombro o caída). El gestor descuenta la vida.</summary>
    public event System.Action Golpeado;

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
    private Collider2D repisaIgnorada;

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
        bool saltarAhora = Pulsada(teclaSaltar) | Pulsada(teclaSubir) |
                           ControlesTactiles.SaltarPulsado() | ControlesTactiles.ArribaPulsado();
        bool abajoTactil = ControlesTactiles.Abajo;
        bool bajarAhora = Pulsada(teclaBajarRepisa) || (abajoTactil && !abajoTactilAntes);
        abajoTactilAntes = abajoTactil;

        if (Time.time < finAturdido)
        {
            // Aturdido por el escombro: no se mueve ni martilla un instante.
            entradaHorizontal = 0f;
        }
        else
        {
            Moverse(saltarAhora, bajarAhora);

            if (repararAhora && !reparando && Time.time >= siguienteReparacion)
            {
                StartCoroutine(RutinaMartillar());
            }
        }

        RevisarRepisaPisada();
        RevisarCaida();
        ActualizarAnimacion();
        ActualizarParpadeo();
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

        if (reparando) return;

        // Solo salta si está parado (no mientras atraviesa una repisa subiendo).
        if (saltar && cuerpo.linearVelocity.y <= 0.5f) Saltar();
        else if (bajar && enSuelo) AtravesarRepisa();
    }

    /// <summary>Se deja caer a través de la repisa que pisa (no la primera fila de su sección).</summary>
    private void AtravesarRepisa()
    {
        RepisaEdificio repisa = RepisaBajoLosPies();
        if (repisa == null || repisa.colision == null || colisionador == null) return;
        if (edificio != null && repisa.piso == SeccionAlcanzada * edificio.pisosPorSeccion) return;

        StartCoroutine(RutinaAtravesar(repisa.colision));
    }

    private IEnumerator RutinaAtravesar(Collider2D repisa)
    {
        DejarDeAtravesar();
        repisaIgnorada = repisa;
        Physics2D.IgnoreCollision(colisionador, repisa, true);
        cuerpo.linearVelocity = new Vector2(cuerpo.linearVelocity.x, -1.5f);
        yield return new WaitForSeconds(tiempoAtravesar);
        DejarDeAtravesar();
    }

    /// <summary>Vuelve a chocar con la repisa que estaba atravesando.</summary>
    private void DejarDeAtravesar()
    {
        if (repisaIgnorada != null && colisionador != null) Physics2D.IgnoreCollision(colisionador, repisaIgnorada, false);
        repisaIgnorada = null;
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
    /// <summary>Le cae un escombro: avisa al gestor, queda aturdido y parpadea un rato.</summary>
    public void RecibirGolpe()
    {
        if (!PuedeRecibirGolpe) return;

        finInvulnerable = Time.time + tiempoInvulnerable;
        finAturdido = Time.time + tiempoAturdido;
        if (Golpeado != null) Golpeado();
    }

    /// <summary>Lo deja en 'posicion', quieto y sin parpadear (al reiniciar la partida).</summary>
    public void Reaparecer(Vector3 posicion)
    {
        StopAllCoroutines();
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
