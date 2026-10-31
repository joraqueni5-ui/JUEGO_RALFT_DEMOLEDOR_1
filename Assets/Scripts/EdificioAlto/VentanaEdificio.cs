using System;
using System.Collections;
using UnityEngine;

public enum EstadoVentana
{
    Intacta = 0,
    Rota = 1,
    Destruida = 2
}

/// <summary>
/// Una ventana de la fachada del edificio alto. Tiene tres estados:
/// intacta -> rota -> destruida. Cada escombro que la golpea baja un estado
/// y cada martillazo del muñeco sube uno (una destruida necesita 2 martillazos).
/// </summary>
public class VentanaEdificio : MonoBehaviour
{
    [Header("Posición en la fachada")]
    [Tooltip("Piso del edificio (0 = el primero sobre la planta baja).")]
    public int piso;
    [Tooltip("Columna de la fachada (0 = izquierda).")]
    public int columna;
    [Tooltip("Sección a la que pertenece (0, 1 o 2).")]
    public int seccion;

    [Header("Aspecto")]
    public SpriteRenderer dibujo;
    [Tooltip("Sprites en orden: intacta, rota, destruida.")]
    public Sprite[] sprites = new Sprite[3];
    [Tooltip("Sprite de los trocitos que saltan al romperse.")]
    public Sprite spriteTrozos;

    [Header("Tamaño del marco (unidades)")]
    [Tooltip("El objeto está en la esquina superior izquierda del marco.")]
    public Vector2 tamanoMarco = new Vector2(0.9f, 0.74f);

    public event Action<VentanaEdificio> CambioEstado;

    public EstadoVentana Estado { get; private set; }

    /// <summary>Recuadro del marco en coordenadas de mundo.</summary>
    public Rect Area
    {
        get
        {
            Vector3 p = transform.position;
            return new Rect(p.x, p.y - tamanoMarco.y, tamanoMarco.x, tamanoMarco.y);
        }
    }

    public Vector2 Centro { get { return Area.center; } }

    private Coroutine animacion;
    private Vector3 posicionBase;
    private bool lista;

    private void Awake()
    {
        Preparar();
    }

    /// <summary>Deja lista la ventana (la llama también el edificio, sin depender del orden de los Awake).</summary>
    public void Preparar()
    {
        if (lista) return;
        if (dibujo == null) dibujo = GetComponent<SpriteRenderer>();
        posicionBase = transform.localPosition;
        lista = true;
        Aplicar();
    }

    /// <summary>Baja un estado. Devuelve true si la ventana cambió.</summary>
    public bool Danar()
    {
        if (Estado == EstadoVentana.Destruida) return false;

        Estado = Estado + 1;
        Aplicar();
        SoltarTrozos(Estado == EstadoVentana.Destruida ? 4 : 2);
        Animar(Sacudir());
        if (CambioEstado != null) CambioEstado(this);
        return true;
    }

    /// <summary>Sube un estado (un martillazo). Devuelve true si la ventana cambió.</summary>
    public bool Reparar()
    {
        if (Estado == EstadoVentana.Intacta) return false;

        Estado = Estado - 1;
        Aplicar();
        Animar(Rebotar());
        if (CambioEstado != null) CambioEstado(this);
        return true;
    }

    /// <summary>Vuelve a dejarla intacta sin avisar (lo usa el edificio al reiniciar).</summary>
    public void Reiniciar()
    {
        Preparar();
        if (animacion != null) StopCoroutine(animacion);
        animacion = null;
        transform.localPosition = posicionBase;
        transform.localScale = Vector3.one;
        Estado = EstadoVentana.Intacta;
        Aplicar();
    }

    private void Aplicar()
    {
        if (dibujo == null || sprites == null) return;
        int i = (int)Estado;
        if (i < sprites.Length && sprites[i] != null) dibujo.sprite = sprites[i];
    }

    private void SoltarTrozos(int cantidad)
    {
        if (spriteTrozos == null || dibujo == null) return;
        for (int i = 0; i < cantidad; i++)
        {
            Vector2 c = Centro + new Vector2(UnityEngine.Random.Range(-0.3f, 0.3f), UnityEngine.Random.Range(-0.2f, 0.2f));
            Escombro.Crear(spriteTrozos, c, dibujo.sortingOrder + 2);
        }
    }

    private void Animar(IEnumerator rutina)
    {
        if (!isActiveAndEnabled) return;
        if (animacion != null) StopCoroutine(animacion);
        transform.localPosition = posicionBase;
        transform.localScale = Vector3.one;
        animacion = StartCoroutine(rutina);
    }

    private IEnumerator Sacudir()
    {
        float t = 0f;
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            transform.localPosition = posicionBase + new Vector3(UnityEngine.Random.Range(-0.03f, 0.03f), UnityEngine.Random.Range(-0.02f, 0.02f), 0f);
            yield return null;
        }
        transform.localPosition = posicionBase;
        animacion = null;
    }

    private IEnumerator Rebotar()
    {
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float k = 1f + 0.08f * Mathf.Sin(Mathf.Clamp01(t / 0.15f) * Mathf.PI);
            transform.localScale = new Vector3(k, k, 1f);
            yield return null;
        }
        transform.localScale = Vector3.one;
        animacion = null;
    }
}
