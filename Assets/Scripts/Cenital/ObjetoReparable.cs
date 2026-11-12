using System;
using UnityEngine;

public enum EstadoObjeto { Intacto, Danado, Roto }

/// <summary>
/// Un objeto reparable del piso visto desde arriba (horno, camilla...). Es un obstáculo
/// (tiene collider) con 3 estados: intacto, dañado y roto. El arte sale de la DefinicionPiso
/// (ficha 'indice'); si no hay sprites se dibuja una figura del color del laboratorio.
/// El tamaño de la figura provisional es el del collider: se puede mover y estirar a mano en el mapa.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ObjetoReparable : MonoBehaviour
{
    [Tooltip("Posición de este objeto en la lista 'Objetos Cenital' de la DefinicionPiso.")]
    public int indice;
    [Tooltip("0 = laboratorio izquierdo, 1 = laboratorio derecho.")]
    [Range(0, 1)] public int laboratorio;

    public EstadoObjeto Estado { get; private set; }
    public event Action<ObjetoReparable> CambioEstado;

    private Collider2D colision;
    private SpriteRenderer dibujo;
    private SpriteRenderer marca;
    private DefinicionPiso definicion;

    public Bounds Area { get { return colision.bounds; } }
    public Vector2 Centro { get { return colision.bounds.center; } }
    public Vector2 PuntoMasCercano(Vector2 p) { return colision.ClosestPoint(p); }

    /// <summary>Se llama una vez al empezar el piso: crea el dibujo y lo deja intacto.</summary>
    public void Preparar(DefinicionPiso d)
    {
        definicion = d;
        colision = GetComponent<Collider2D>();
        Physics2D.SyncTransforms();

        if (dibujo == null)
        {
            Transform previo = transform.Find("Dibujo");
            if (previo != null) Destroy(previo.gameObject);
            Transform previoMarca = transform.Find("Marca");
            if (previoMarca != null) Destroy(previoMarca.gameObject);

            Bounds b = colision.bounds;
            int orden = Provisional.OrdenY(b.min.y);
            dibujo = Provisional.Crear(transform, "Dibujo", Provisional.Cuadrado, Color.white, orden);
            dibujo.transform.position = b.center;

            marca = Provisional.Crear(transform, "Marca", Provisional.Cuadrado, Color.green, orden + 1);
            marca.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
            marca.transform.position = new Vector3(b.max.x - 0.22f, b.max.y - 0.22f, 0f);
        }
        Reiniciar();
    }

    public bool Danar()
    {
        if (Estado == EstadoObjeto.Roto) return false;
        Estado = Estado == EstadoObjeto.Intacto ? EstadoObjeto.Danado : EstadoObjeto.Roto;
        Aplicar();
        if (CambioEstado != null) CambioEstado(this);
        return true;
    }

    public bool Reparar()
    {
        if (Estado == EstadoObjeto.Intacto) return false;
        Estado = Estado == EstadoObjeto.Roto ? EstadoObjeto.Danado : EstadoObjeto.Intacto;
        Aplicar();
        if (CambioEstado != null) CambioEstado(this);
        return true;
    }

    /// <summary>Intacto, sin avisar (el piso avisa una sola vez al reiniciar).</summary>
    public void Reiniciar()
    {
        Estado = EstadoObjeto.Intacto;
        Aplicar();
    }

    private void Aplicar()
    {
        if (dibujo == null) return;

        ObjetoCenital ficha = definicion != null ? definicion.ObjetoCenitalDe(indice) : null;
        Sprite[] sprites = ficha != null ? ficha.SpritesCompletos() : null;
        Bounds b = colision.bounds;

        if (sprites != null)
        {
            dibujo.sprite = sprites[(int)Estado];
            dibujo.color = Color.white;
            dibujo.transform.localScale = Vector3.one;
        }
        else
        {
            LaboratorioPiso lab = definicion != null ? definicion.LaboratorioDe(laboratorio) : null;
            Color baseColor = lab != null ? lab.color : new Color(0.7f, 0.7f, 0.7f);
            Color c = baseColor;
            if (Estado == EstadoObjeto.Danado) c = Color.Lerp(baseColor, new Color(0.95f, 0.8f, 0.2f), 0.45f) * 0.8f;
            else if (Estado == EstadoObjeto.Roto) c = Color.Lerp(baseColor, new Color(0.3f, 0.1f, 0.1f), 0.75f) * 0.7f;
            c.a = 1f;

            dibujo.sprite = Provisional.Cuadrado;
            dibujo.color = c;
            dibujo.transform.localScale = new Vector3(Mathf.Max(0.2f, b.size.x - 0.12f), Mathf.Max(0.2f, b.size.y - 0.12f), 1f);
        }

        if (marca != null)
        {
            marca.color = Estado == EstadoObjeto.Intacto ? new Color(0.25f, 0.85f, 0.35f)
                        : Estado == EstadoObjeto.Danado ? new Color(1f, 0.75f, 0.1f)
                        : new Color(0.9f, 0.15f, 0.15f);
        }
    }
}
