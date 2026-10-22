using UnityEngine;

/// <summary>
/// Base de los dos personajes: física 2D, salto, detección de suelo y animador.
/// El control (qué teclas usa cada uno) lo pone cada personaje en su propio Update.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public abstract class PersonajeBase : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Unidades por segundo al caminar.")]
    public float velocidad = 4f;
    [Tooltip("Impulso del salto. Con gravedad 3 llega a ~1.8 unidades de alto.")]
    public float fuerzaSalto = 10.3f;
    [Tooltip("Multiplica la gravedad mientras cae para que el salto no se sienta flotante.")]
    public float multiplicadorCaida = 1.5f;

    [Header("Suelo")]
    public LayerMask capaSuelo = 1;
    public float margenSuelo = 0.12f;

    protected Rigidbody2D cuerpo;
    protected Collider2D colisionador;
    protected SpriteRenderer dibujo;
    protected AnimadorSprites animador;

    protected float entradaHorizontal;
    protected bool enSuelo;
    protected bool mirandoDerecha = true;
    protected float gravedadBase = 3f;

    /// <summary>Se apaga mientras el personaje trepa la fachada (así no lo tira la gravedad).</summary>
    protected bool usaGravedad = true;

    public bool EnSuelo { get { return enSuelo; } }

    protected virtual void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        colisionador = GetComponent<Collider2D>();
        dibujo = GetComponentInChildren<SpriteRenderer>();
        animador = new AnimadorSprites(dibujo);

        cuerpo.freezeRotation = true;
        cuerpo.gravityScale = gravedadBase;
        cuerpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (colisionador != null) colisionador.isTrigger = false;
    }

    protected virtual void FixedUpdate()
    {
        if (!usaGravedad) return;

        RevisarSuelo();
        Mover(entradaHorizontal);
        AplicarGravedadExtra();
    }

    /// <summary>Lanza un rayo corto hacia abajo desde los pies para saber si puede saltar.</summary>
    protected void RevisarSuelo()
    {
        if (colisionador == null) return;

        Bounds caja = colisionador.bounds;
        Vector2 origen = new Vector2(caja.center.x, caja.min.y + 0.01f);
        RaycastHit2D[] golpes = Physics2D.RaycastAll(origen, Vector2.down, margenSuelo, capaSuelo);

        enSuelo = false;
        for (int i = 0; i < golpes.Length; i++)
        {
            if (golpes[i].collider == null) continue;
            if (golpes[i].collider == colisionador) continue;
            if (golpes[i].collider.isTrigger) continue;
            enSuelo = true;
            break;
        }
    }

    protected void Mover(float direccion)
    {
        Vector2 v = cuerpo.linearVelocity;
        v.x = direccion * velocidad;
        cuerpo.linearVelocity = v;
    }

    private void AplicarGravedadExtra()
    {
        cuerpo.gravityScale = cuerpo.linearVelocity.y < -0.01f ? gravedadBase * multiplicadorCaida : gravedadBase;
    }

    public bool Saltar()
    {
        if (!enSuelo) return false;

        cuerpo.linearVelocity = new Vector2(cuerpo.linearVelocity.x, fuerzaSalto);
        enSuelo = false;
        return true;
    }
}
