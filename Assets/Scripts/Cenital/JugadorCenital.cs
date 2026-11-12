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

    [Header("Dibujo")]
    [Tooltip("Alto en baldosas al que se escala el sprite.")]
    public float alturaDibujo = 1.25f;
    public AnimacionesCenital animaciones = new AnimacionesCenital();

    public event Action Golpeado;

    private Rigidbody2D cuerpo;
    private SpriteRenderer dibujo;
    private AnimadorSprites animador;
    private Vector2 entrada;
    private Vector2 direccion = Vector2.down;
    private bool martillando;
    private bool reparoHecho;
    private float inicioMartillo, finMartillo, proximoMartillo, finInvulnerable;
    private bool fueraDeCuadros;

    /// <summary>Centro del cuerpo (el collider está sobre los pies).</summary>
    public Vector2 Centro { get { return (Vector2)transform.position + new Vector2(0f, 0.3f); } }
    public bool PuedeRecibirGolpe { get { return enabled && Time.time >= finInvulnerable; } }

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
    }

    private void Start()
    {
        // El arte de la definición (si lo hay) reemplaza al de la escena.
        PisoCenital piso = PisoCenital.Actual;
        if (piso != null && piso.Definicion != null) animaciones = Combinar(animaciones, piso.Definicion.jugador);
        MostrarQuieto();
    }

    private static Sprite[] Si(Sprite[] nuevo, Sprite[] actual)
    {
        return nuevo != null && nuevo.Length > 0 ? nuevo : actual;
    }

    private static AnimacionesCenital Combinar(AnimacionesCenital baseA, AnimacionesCenital d)
    {
        if (d == null) return baseA;
        return new AnimacionesCenital
        {
            caminarAbajo = Si(d.caminarAbajo, baseA.caminarAbajo),
            caminarArriba = Si(d.caminarArriba, baseA.caminarArriba),
            caminarDerecha = Si(d.caminarDerecha, baseA.caminarDerecha),
            golpeAbajo = Si(d.golpeAbajo, baseA.golpeAbajo),
            golpeArriba = Si(d.golpeArriba, baseA.golpeArriba),
            golpeDerecha = Si(d.golpeDerecha, baseA.golpeDerecha),
            especial = Si(d.especial, baseA.especial),
        };
    }

    private void OnDisable()
    {
        if (cuerpo != null) cuerpo.linearVelocity = Vector2.zero;
        if (dibujo != null) dibujo.enabled = true;
    }

    private void Update()
    {
        LeerEntrada();

        if (martillando)
        {
            if (!reparoHecho && Time.time >= inicioMartillo + momentoDeReparacion) Reparar();
            if (Time.time >= finMartillo) martillando = false;
        }
        else
        {
            Keyboard k = Keyboard.current;
            bool enter = k != null && (k[Key.Enter].wasPressedThisFrame || k[Key.NumpadEnter].wasPressedThisFrame);
            if ((enter || ControlesTactiles.RepararPulsado()) && Time.time >= proximoMartillo) Martillar();
        }

        Animar();
        Parpadear();
        AjustarDibujo();
    }

    private void FixedUpdate()
    {
        cuerpo.linearVelocity = martillando ? Vector2.zero : entrada * velocidad;
    }

    private void LeerEntrada()
    {
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
        float duracion = serie != null && serie.Length > 0 ? serie.Length / Mathf.Max(1f, cuadrosPorSegundoMartillo) : 0.35f;
        finMartillo = inicioMartillo + Mathf.Max(0.3f, duracion);
        proximoMartillo = finMartillo + enfriamiento;
        if (serie != null) animador.Reproducir(serie, cuadrosPorSegundoMartillo, false, true);
    }

    private void Reparar()
    {
        reparoHecho = true;
        PisoCenital piso = PisoCenital.Actual;
        if (piso == null || piso.Mapa == null) return;

        ObjetoReparable mejor = null;
        float mejorDistancia = radioReparacion;
        foreach (ObjetoReparable o in piso.Mapa.objetos)
        {
            if (o == null || o.Estado == EstadoObjeto.Intacto) continue;
            float d = Vector2.Distance(Centro, o.PuntoMasCercano(Centro));
            if (d > mejorDistancia) continue;
            mejorDistancia = d;
            mejor = o;
        }
        if (mejor != null && mejor.Reparar()) piso.RegistrarReparacion();
    }

    // ------------------------------------------------------------ daño
    /// <summary>Quita una vida (la cuenta GestorJuego). Devuelve false si está invulnerable.</summary>
    public bool RecibirGolpe()
    {
        if (!PuedeRecibirGolpe) return false;
        finInvulnerable = Time.time + tiempoInvulnerable;
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
        finInvulnerable = 0f;
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
                if (entrada.sqrMagnitude > 0.01f) animador.Reproducir(serie, cuadrosCaminar, true, false);
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
        dibujo.transform.localScale = tam;
        float signo = dibujo.flipX ? -1f : 1f;
        dibujo.transform.localPosition = new Vector3(-b.center.x * tam.x * signo, -b.min.y * tam.y, 0f);
        dibujo.sortingOrder = Provisional.OrdenY(transform.position.y);
    }
}
