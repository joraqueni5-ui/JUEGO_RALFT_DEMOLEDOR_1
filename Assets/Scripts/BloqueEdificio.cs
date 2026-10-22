using System;
using System.Collections;
using UnityEngine;

public enum EstadoBloque
{
    Intacto,
    Agrietado,
    Destruido
}

/// <summary>
/// Un bloque (ladrillo) del edificio. Guarda tres estados:
/// intacto -> agrietado -> destruido, y el muñeco lo puede reparar.
/// </summary>
public class BloqueEdificio : MonoBehaviour
{
    [Header("Rejilla")]
    [Tooltip("Fila dentro del edificio (0 = planta baja).")]
    public int fila;
    [Tooltip("Columna dentro del edificio (0 = izquierda).")]
    public int columna;

    [Header("Aspecto")]
    public SpriteRenderer dibujo;
    [Tooltip("Sprite que se muestra cuando el bloque está agrietado (si está vacío se usa el normal).")]
    public Sprite spriteAgrietado;
    [Range(1, 6)]
    [Tooltip("Golpes del demoledor necesarios para destruirlo.")]
    public int golpesParaRomper = 2;
    [Tooltip("Escala extra para que no se vean líneas entre bloques vecinos.")]
    public float solape = 1.02f;
    [Tooltip("Cuántos trocitos salen volando al romperse.")]
    public int trozosDeEscombro = 3;

    public event Action<BloqueEdificio> CambioEstado;

    public EstadoBloque Estado { get; private set; }

    /// <summary>Recuadro del bloque en coordenadas de mundo (se calcula una sola vez).</summary>
    public Rect Area
    {
        get
        {
            if (!areaLista) Preparar();
            return area;
        }
    }

    private Sprite spriteOriginal;
    private Vector3 escalaBase;
    private int golpesRecibidos;
    private Coroutine animacion;
    private bool areaLista;
    private Rect area;

    private void Awake()
    {
        Preparar();
    }

    /// <summary>
    /// Deja listo el bloque (sprite original, escala y recuadro). Es público porque el
    /// edificio lo llama al construir la rejilla, sin depender del orden de los Awake.
    /// </summary>
    public void Preparar()
    {
        if (areaLista) return;

        if (dibujo == null) dibujo = GetComponent<SpriteRenderer>();
        if (dibujo != null) spriteOriginal = dibujo.sprite;

        escalaBase = transform.localScale * solape;
        transform.localScale = escalaBase;
        CachearArea();
        areaLista = true;
    }

    /// <summary>Recuadro del bloque en coordenadas de mundo (no cambia nunca).</summary>
    private void CachearArea()
    {
        if (dibujo == null || dibujo.sprite == null)
        {
            area = new Rect(transform.position, Vector2.one);
            return;
        }

        Bounds caja = dibujo.bounds;
        area = new Rect(caja.min, caja.size);
    }

    /// <summary>Recibe un golpe del demoledor. Devuelve true si el bloque cambió de estado.</summary>
    public bool RecibirGolpe(int cantidad = 1)
    {
        if (Estado == EstadoBloque.Destruido) return false;

        golpesRecibidos += Mathf.Max(1, cantidad);
        if (golpesRecibidos >= golpesParaRomper)
        {
            CambiarEstado(EstadoBloque.Destruido);
        }
        else
        {
            CambiarEstado(EstadoBloque.Agrietado);
        }
        return true;
    }

    /// <summary>El muñeco repara el bloque. Devuelve true si había algo que reparar.</summary>
    public bool Reparar()
    {
        if (Estado == EstadoBloque.Intacto) return false;

        golpesRecibidos = 0;
        CambiarEstado(EstadoBloque.Intacto);

        if (animacion != null) StopCoroutine(animacion);
        animacion = StartCoroutine(Aparecer());
        return true;
    }

    public void Reiniciar()
    {
        golpesRecibidos = 0;
        CambiarEstado(EstadoBloque.Intacto);
    }

    private void CambiarEstado(EstadoBloque nuevo)
    {
        Estado = nuevo;

        if (dibujo != null)
        {
            switch (nuevo)
            {
                case EstadoBloque.Intacto:
                    dibujo.sprite = spriteOriginal;
                    dibujo.enabled = true;
                    break;

                case EstadoBloque.Agrietado:
                    if (spriteAgrietado != null) dibujo.sprite = spriteAgrietado;
                    dibujo.enabled = true;
                    break;

                case EstadoBloque.Destruido:
                    dibujo.enabled = false;
                    LanzarEscombros();
                    break;
            }
        }

        if (CambioEstado != null) CambioEstado(this);
    }

    private void LanzarEscombros()
    {
        if (spriteOriginal == null || trozosDeEscombro <= 0) return;

        int orden = dibujo != null ? dibujo.sortingOrder + 1 : 5;
        for (int i = 0; i < trozosDeEscombro; i++)
        {
            Vector3 posicion = transform.position + new Vector3(
                UnityEngine.Random.Range(-0.4f, 0.4f),
                UnityEngine.Random.Range(0.1f, 0.9f),
                0f);
            Escombro.Crear(spriteOriginal, posicion, orden);
        }
    }

    /// <summary>Pequeño rebote al reparar para que se note que volvió.</summary>
    private IEnumerator Aparecer()
    {
        float duracion = 0.18f;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracion);
            float escala = Mathf.LerpUnclamped(0.6f, 1f, k) + Mathf.Sin(k * Mathf.PI) * 0.15f;
            transform.localScale = escalaBase * escala;
            yield return null;
        }
        transform.localScale = escalaBase;
        animacion = null;
    }
}
