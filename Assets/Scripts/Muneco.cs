using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// El muñeco (el que hace de Félix). Es el ÚNICO personaje que controla el jugador:
/// camina, se trepa a la fachada del edificio y, con el martillo, repara de a un bloque.
/// </summary>
public class Muneco : PersonajeBase
{
    [Header("Animaciones")]
    public Sprite[] caminar;
    public Sprite[] caminarEspaldas;
    public Sprite[] martillo;
    public float cuadrosCaminar = 8f;
    public float cuadrosPorSegundoMartillo = 12f;

    [Header("Reparación")]
    [Tooltip("Distancia máxima a la que alcanza el martillo (un bloque por golpe).")]
    public float radioReparacion = 0.55f;
    [Tooltip("Altura del pecho: desde ahí se busca el bloque a reparar.")]
    public float alturaPecho = 0.45f;
    public float momentoDeReparacion = 0.15f;
    public float enfriamiento = 0.28f;

    [Header("Trepar")]
    public float velocidadTrepado = 2.1f;
    [Tooltip("Separación con los bordes del edificio al trepar.")]
    public float margenFachada = 0.25f;

    [Header("Controles")]
    public Key teclaIzquierda = Key.A;
    public Key teclaDerecha = Key.D;
    public Key teclaSubir = Key.UpArrow;
    public Key teclaBajar = Key.DownArrow;
    public Key teclaSaltar = Key.Space;
    public Key teclaReparar = Key.Enter;

    private bool reparando;
    private bool trepando;
    private float finDeReparacion;
    private float siguienteReparacion;
    private float duracionReparacion = 0.33f;
    private Edificio edificio;

    public bool Trepando { get { return trepando; } }

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
        edificio = Edificio.Instancia;
    }

    private void Update()
    {
        animador.Actualizar(Time.deltaTime);

        if (reparando && Time.time >= finDeReparacion) reparando = false;
        if (Keyboard.current == null) { entradaHorizontal = 0f; ActualizarAnimacion(); return; }

        if (trepando) ActualizarTrepado();
        else ActualizarEnElSuelo();

        if (Keyboard.current[teclaReparar].wasPressedThisFrame && !reparando && Time.time >= siguienteReparacion)
        {
            StartCoroutine(RutinaMartillar());
        }

        ActualizarAnimacion();
    }

    // ------------------------------------------------------------ en el suelo
    private void ActualizarEnElSuelo()
    {
        float direccion = 0f;
        if (Keyboard.current[teclaIzquierda].isPressed) direccion -= 1f;
        if (Keyboard.current[teclaDerecha].isPressed) direccion += 1f;
        if (!reparando && direccion != 0f) mirandoDerecha = direccion > 0f;
        entradaHorizontal = reparando ? 0f : direccion;

        if (reparando) return;

        // Arriba = agarrarse a la fachada si está debajo del edificio.
        if (Keyboard.current[teclaSubir].wasPressedThisFrame && Agarrarse()) return;

        if (Keyboard.current[teclaSaltar].wasPressedThisFrame) Saltar();
    }

    private bool PuedeAgarrarse()
    {
        if (edificio == null) return false;

        Rect limites = edificio.Limites;
        float x = transform.position.x;
        return x >= limites.xMin - 0.15f && x <= limites.xMax + 0.15f;
    }

    // ------------------------------------------------------------ trepando
    /// <summary>Se agarra a la fachada si está debajo del edificio. Devuelve true si lo logró.</summary>
    public bool Agarrarse()
    {
        if (trepando || edificio == null || !PuedeAgarrarse()) return false;

        EmpezarATrepar();
        return true;
    }

    private void EmpezarATrepar()
    {
        trepando = true;
        usaGravedad = false;
        entradaHorizontal = 0f;
        cuerpo.gravityScale = 0f;
        cuerpo.linearVelocity = Vector2.zero;
    }

    /// <summary>Se suelta de la fachada (también lo llama el gestor al terminar la partida).</summary>
    public void Soltar()
    {
        if (!trepando) return;

        trepando = false;
        usaGravedad = true;
        cuerpo.gravityScale = gravedadBase;
        cuerpo.linearVelocity = Vector2.zero;
    }

    private void ActualizarTrepado()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current[teclaIzquierda].isPressed) horizontal -= 1f;
        if (Keyboard.current[teclaDerecha].isPressed) horizontal += 1f;
        if (Keyboard.current[teclaSubir].isPressed) vertical += 1f;
        if (Keyboard.current[teclaBajar].isPressed) vertical -= 1f;

        if (Keyboard.current[teclaSaltar].wasPressedThisFrame)
        {
            Soltar();
            return;
        }

        if (Keyboard.current[teclaBajar].isPressed && transform.position.y <= 0.05f)
        {
            Soltar();
            return;
        }

        Vector2 movimiento = new Vector2(horizontal, vertical);
        if (movimiento.sqrMagnitude > 1f) movimiento = movimiento.normalized;

        if (reparando) movimiento = Vector2.zero;

        entradaHorizontal = horizontal;

        Vector2 nueva = (Vector2)transform.position + movimiento * (velocidadTrepado * Time.deltaTime);
        Rect limites = edificio != null ? edificio.Limites : new Rect(-100f, 0f, 200f, 4f);

        nueva.x = Mathf.Clamp(nueva.x, limites.xMin + margenFachada, limites.xMax - margenFachada);
        nueva.y = Mathf.Clamp(nueva.y, 0f, limites.yMax - 0.25f);
        transform.position = new Vector3(nueva.x, nueva.y, transform.position.z);
    }

    // ------------------------------------------------------------ martillo
    private IEnumerator RutinaMartillar()
    {
        reparando = true;
        siguienteReparacion = Time.time + enfriamiento;
        finDeReparacion = Time.time + duracionReparacion;

        yield return new WaitForSeconds(momentoDeReparacion);

        if (edificio == null) edificio = Edificio.Instancia;
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
            // De espaldas contra la fachada; la serie se anima solo si se mueve.
            bool moviendose = Mathf.Abs(entradaHorizontal) > 0.01f || TeclasDeTreparEnUso();
            if (moviendose) animador.Reproducir(caminarEspaldas, cuadrosCaminar, true);
            else animador.MostrarCuadroFijo(caminarEspaldas, 0);
            return;
        }

        dibujo.flipX = !mirandoDerecha;

        if (!enSuelo)
        {
            animador.MostrarCuadroFijo(caminar, 2);
            return;
        }

        if (Mathf.Abs(entradaHorizontal) > 0.01f) animador.Reproducir(caminar, cuadrosCaminar, true);
        else animador.MostrarCuadroFijo(caminar, 0);
    }

    private bool TeclasDeTreparEnUso()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current[teclaSubir].isPressed || Keyboard.current[teclaBajar].isPressed;
    }
}
