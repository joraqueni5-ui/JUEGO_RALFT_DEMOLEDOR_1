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

    public event Action<RepisaEdificio> CambioEstado;

    public EstadoRepisa Estado { get; private set; }

    /// <summary>True si todavía se puede pisar (no está destruida).</summary>
    public bool SePuedePisar { get { return Estado != EstadoRepisa.Destruida; } }

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

    /// <summary>Baja un estado. Devuelve true si la repisa cambió.</summary>
    public bool Danar()
    {
        if (Estado == EstadoRepisa.Destruida) return false;

        Estado = Estado + 1;
        Aplicar();
        SoltarTrozos(Estado == EstadoRepisa.Destruida ? 5 : 2);
        if (CambioEstado != null) CambioEstado(this);
        return true;
    }

    /// <summary>Repara una repisa agrietada. Una destruida ya no se puede reparar.</summary>
    public bool Reparar()
    {
        if (Estado != EstadoRepisa.Agrietada) return false;

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
        int i = (int)Estado;
        if (dibujo != null && sprites != null && i < sprites.Length && sprites[i] != null) dibujo.sprite = sprites[i];
        if (colision != null) colision.enabled = SePuedePisar;
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
