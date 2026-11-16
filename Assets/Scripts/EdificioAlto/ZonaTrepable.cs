using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zona de la fachada por la que el muñeco puede trepar (una canaleta o columna vertical, o un borde del
/// edificio). Es un collider de tipo trigger: se puede agregar, mover o estirar a mano en la escena. El muñeco
/// se agarra con arriba o abajo cuando está dentro de ella.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ZonaTrepable : MonoBehaviour
{
    [Tooltip("Dibuja una canaleta provisional sobre la zona (para cuando el arte no la muestra).")]
    public bool marcaVisual = true;
    public Color colorCanaleta = new Color(0.9f, 0.5f, 0.12f, 1f);
    [Tooltip("Tamaño del sprite de la canaleta (1 = 32 px de arte por unidad; la fachada usa 50 px por unidad, así que 0,64 iguala los píxeles).")]
    public float escalaCanaletaArte = 0.64f;
    [Tooltip("Ancho (unidades) de la canaleta provisional.")]
    public float anchoCanaleta = 0.14f;
    [Tooltip("Corrimiento horizontal (unidades) de la canaleta y del punto donde se agarra el muñeco, respecto al centro de la zona.")]
    public float desplazamientoX = 0f;

    /// <summary>X (mundo) por donde sube el muñeco: la canaleta.</summary>
    public float PuntoAgarreX { get { return Area != null ? Area.bounds.center.x + desplazamientoX : transform.position.x; } }

    private static readonly List<ZonaTrepable> todas = new List<ZonaTrepable>();

    /// <summary>Todas las zonas trepables activas de la escena.</summary>
    public static IList<ZonaTrepable> Todas { get { return todas; } }

    public Collider2D Area { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstatico()
    {
        todas.Clear();
    }

    private void Awake()
    {
        Area = GetComponent<Collider2D>();
        Area.isTrigger = true;
    }

    private void OnEnable()
    {
        if (!todas.Contains(this)) todas.Add(this);
    }

    private void OnDisable()
    {
        todas.Remove(this);
    }

    private void Start()
    {
        if (!marcaVisual || Area == null) return;
        Transform previa = transform.Find("Canaleta");
        if (previa != null) Destroy(previa.gameObject);

        Bounds b = Area.bounds;

        // Con arte: la canaleta es el sprite repetido a lo alto de la zona (un píxel del arte = un píxel de la fachada).
        ArteNiveles arte = ArteNiveles.Actual;
        if (arte != null && arte.canaletaTrepable != null)
        {
            float e = escalaCanaletaArte;
            SpriteRenderer tubo = Provisional.Crear(transform, "Canaleta", arte.canaletaTrepable, Color.white, 4);
            tubo.drawMode = SpriteDrawMode.Tiled;
            tubo.tileMode = SpriteTileMode.Continuous;
            tubo.size = new Vector2(1f, b.size.y / e);
            tubo.transform.position = new Vector3(PuntoAgarreX, b.center.y, 0f);
            tubo.transform.localScale = new Vector3(e, e, 1f);
            return;
        }

        // Tubería provisional bien visible: cuerpo naranja con un borde oscuro y un brillo.
        SpriteRenderer borde = Provisional.Crear(transform, "Canaleta", Provisional.Cuadrado, new Color(0.25f, 0.12f, 0.04f, 1f), 3);
        borde.transform.position = new Vector3(PuntoAgarreX, b.center.y, 0f);
        borde.transform.localScale = new Vector3(anchoCanaleta + 0.05f, b.size.y, 1f);
        SpriteRenderer cuerpo = Provisional.Crear(borde.transform, "Cuerpo", Provisional.Cuadrado, colorCanaleta, 3);
        cuerpo.sortingOrder = 4;
        cuerpo.transform.localScale = new Vector3(anchoCanaleta / (anchoCanaleta + 0.05f), 1f, 1f);
        SpriteRenderer brillo = Provisional.Crear(borde.transform, "Brillo", Provisional.Cuadrado, new Color(1f, 0.85f, 0.5f, 1f), 3);
        brillo.sortingOrder = 5;
        brillo.transform.localPosition = new Vector3(-0.18f, 0f, 0f);
        brillo.transform.localScale = new Vector3(0.18f, 1f, 1f);
    }

    /// <summary>La zona que contiene el punto (por ejemplo el centro del cuerpo), o null.</summary>
    public static ZonaTrepable En(Vector2 punto)
    {
        for (int i = 0; i < todas.Count; i++)
        {
            ZonaTrepable z = todas[i];
            if (z != null && z.Area != null && z.Area.bounds.Contains(new Vector3(punto.x, punto.y, z.Area.bounds.center.z))) return z;
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Bounds b = c.bounds;
        Gizmos.DrawCube(b.center, b.size);
    }
}
