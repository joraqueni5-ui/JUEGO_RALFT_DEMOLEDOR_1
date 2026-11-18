using System;
using UnityEngine;

public enum EstadoRepisa
{
    Intacta = 0,
    Agrietada = 1,
    Destruida = 2
}

/// <summary>
/// Repisa bajo una ventana: es donde pisa el muñeco. Se atraviesa saltando desde
/// abajo (plataforma de un solo sentido). Los escombros la dañan:
/// intacta -> agrietada -> destruida. La agrietada se repara con el martillo;
/// la destruida ya no tiene arreglo y deja un hueco por donde se cae.
/// Las repisas de secciones a las que el demoledor todavía no llegó están
/// bloqueadas: se ven, pero no se pueden pisar.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class RepisaEdificio : MonoBehaviour
{
    [Header("Posición en la fachada")]
    public int piso;
    public int columna;
    public int seccion;

    [Header("Aspecto")]
    public SpriteRenderer dibujo;
    [Tooltip("Sprites en orden: intacta, agrietada, destruida.")]
    public Sprite[] sprites = new Sprite[3];
    [Tooltip("Sprite de los trocitos que saltan al dañarse.")]
    public Sprite spriteTrozos;

    [Header("Tamaño (unidades)")]
    [Tooltip("Ancho y grosor de la parte pisable. El objeto está en su esquina superior izquierda.")]
    public Vector2 tamano = new Vector2(1.24f, 0.12f);

    public BoxCollider2D colision;

    [Tooltip("La repisa nunca se daña (ni visualmente): Danar() no hace nada, se dibuja siempre intacta y siempre se puede pisar. " +
             "Lo marca EdificioAlto.repisasIndestructibles (nivel final).")]
    public bool indestructible;

    public event Action<RepisaEdificio> CambioEstado;

    public EstadoRepisa Estado { get; private set; }

    /// <summary>True si no está destruida (una indestructible siempre se puede pisar).</summary>
    public bool SePuedePisar { get { return indestructible || Estado != EstadoRepisa.Destruida; } }

    /// <summary>Se puede reparar con el martillo: solo la agrietada (una indestructible nunca se daña).</summary>
    public bool Reparable { get { return !indestructible && Estado == EstadoRepisa.Agrietada; } }

    /// <summary>False mientras su sección todavía no se habilitó (el demoledor no llegó).</summary>
    public bool Habilitada
    {
        get { return habilitada; }
        set
        {
            habilitada = value;
            Aplicar();
        }
    }

    /// <summary>Altura (mundo) donde quedan los pies al pararse encima.</summary>
    public float Superficie { get { return transform.position.y; } }

    public float CentroX { get { return transform.position.x + tamano.x * 0.5f; } }

    /// <summary>Recuadro de la repisa en coordenadas de mundo.</summary>
    public Rect Area
    {
        get
        {
            Vector3 p = transform.position;
            return new Rect(p.x, p.y - tamano.y, tamano.x, tamano.y);
        }
    }

    private bool lista;
    private bool habilitada = true;

    private void Awake()
    {
        Preparar();
    }

    /// <summary>Deja lista la repisa (la llama también el edificio, sin depender del orden de los Awake).</summary>
    public void Preparar()
    {
        if (lista) return;
        if (dibujo == null) dibujo = GetComponent<SpriteRenderer>();
        if (colision == null) colision = GetComponent<BoxCollider2D>();
        lista = true;
        Aplicar();
    }

    private Sprite[] spritesOriginales;
    private bool originalesGuardados;

    /// <summary>
    /// Cambia el aspecto (modo de pisos). Con 'nuevos' en null o incompleto vuelve a los
    /// sprites con que se construyó. El tinte se multiplica con el color del sprite.
    /// </summary>
    public void AplicarSprites(Sprite[] nuevos, Color tinte)
    {
        Preparar();
        if (!originalesGuardados)
        {
            spritesOriginales = sprites != null ? (Sprite[])sprites.Clone() : null;
            originalesGuardados = true;
        }
        bool completos = nuevos != null && nuevos.Length >= 3;
        sprites = completos ? nuevos : spritesOriginales;
        if (dibujo != null) dibujo.color = tinte;
        Aplicar();
    }

    /// <summary>Baja un estado. Devuelve true si la repisa cambió.</summary>
    public bool Danar()
    {
        if (indestructible || Estado == EstadoRepisa.Destruida) return false;

        Estado = Estado + 1;
        Aplicar();
        SoltarTrozos(Estado == EstadoRepisa.Destruida ? 5 : 2);
        if (CambioEstado != null) CambioEstado(this);
        return true;
    }

    /// <summary>Repara una repisa agrietada. Una destruida ya no se puede reparar.</summary>
    public bool Reparar()
    {
        if (!Reparable) return false;

        Estado = EstadoRepisa.Intacta;
        Aplicar();
        if (CambioEstado != null) CambioEstado(this);
        return true;
    }

    /// <summary>Vuelve a dejarla intacta sin avisar (lo usa el edificio al reiniciar).</summary>
    public void Reiniciar()
    {
        Preparar();
        Estado = EstadoRepisa.Intacta;
        Aplicar();
    }

    private void Aplicar()
    {
        int i = indestructible ? 0 : (int)Estado;   // una indestructible se dibuja siempre intacta
        if (dibujo != null && sprites != null && i < sprites.Length && sprites[i] != null) dibujo.sprite = sprites[i];
        if (colision != null) colision.enabled = SePuedePisar && habilitada;
    }

    private void SoltarTrozos(int cantidad)
    {
        if (spriteTrozos == null) return;
        int orden = dibujo != null ? dibujo.sortingOrder + 2 : 6;
        for (int i = 0; i < cantidad; i++)
        {
            Vector2 punto = new Vector2(CentroX + UnityEngine.Random.Range(-0.4f, 0.4f), Superficie - 0.06f);
            Escombro.Crear(spriteTrozos, punto, orden);
        }
    }
}
