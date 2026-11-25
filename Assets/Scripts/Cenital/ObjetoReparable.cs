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
    private SpriteRenderer amenaza;
    private SpriteRenderer grietas;
    private bool amenazado;
    private DefinicionPiso definicion;

    public Bounds Area { get { return colision.bounds; } }
    public Vector2 Centro { get { return colision.bounds.center; } }
    public Vector2 PuntoMasCercano(Vector2 p) { return colision.ClosestPoint(p); }

    /// <summary>Nombre del objeto en la definición del piso ("Horno 1", "Fila de computadoras"...).</summary>
    public string Nombre
    {
        get
        {
            if (definicion != null && definicion.objetosCenital != null && indice >= 0 && indice < definicion.objetosCenital.Length &&
                definicion.objetosCenital[indice] != null) return definicion.objetosCenital[indice].nombre ?? name;
            return name;
        }
    }

    /// <summary>El dibujo del objeto (para animarlo al repararlo).</summary>
    public SpriteRenderer Dibujo { get { return dibujo; } }

    /// <summary>Se llama una vez al empezar el piso: crea el dibujo y lo deja intacto.</summary>
    public void Preparar(DefinicionPiso d)
    {
        definicion = d;
        colision = GetComponent<Collider2D>();
        Physics2D.SyncTransforms();

        if (dibujo == null)
        {
            Transform previo = transform.Find("Dibujo");
            if (previo != null) Provisional.Eliminar(previo.gameObject);
            Transform previoMarca = transform.Find("Marca");
            if (previoMarca != null) Provisional.Eliminar(previoMarca.gameObject);
            Transform previaSombra = transform.Find("Sombra");
            if (previaSombra != null) Provisional.Eliminar(previaSombra.gameObject);
            Transform previaGrieta = transform.Find("Grietas");
            if (previaGrieta != null) Provisional.Eliminar(previaGrieta.gameObject);
            grietas = null;

            Bounds b = colision.bounds;
            int orden = Provisional.OrdenY(b.min.y);

            SpriteRenderer sombra = Provisional.Crear(transform, "Sombra", Provisional.Circulo, new Color(0f, 0f, 0f, 0.28f), orden - 1);
            sombra.transform.position = new Vector3(b.center.x, b.min.y + 0.12f, 0f);
            sombra.transform.localScale = new Vector3(b.size.x * 0.95f, 0.45f, 1f);

            dibujo = Provisional.Crear(transform, "Dibujo", Provisional.Cuadrado, Color.white, orden);
            dibujo.transform.position = b.center;

            marca = Provisional.Crear(transform, "Marca", Provisional.Cuadrado, Color.green, orden + 1);
            marca.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
            marca.transform.position = new Vector3(b.max.x - 0.22f, b.max.y - 0.22f, 0f);
        }
        Reiniciar();

        // Al armar el mapa en el editor no se ven las marcas de depuración.
        if (!Application.isPlaying && marca != null) marca.enabled = false;
    }

    private void Update()
    {
        if (amenaza != null && amenazado) amenaza.enabled = ((int)(Time.time * 10f) % 2 == 0);

        if (marca == null) return;
        PisoCenital piso = PisoCenital.Actual;
        bool ver = piso != null && piso.mostrarMarcasEstado;
        if (marca.enabled != ver) marca.enabled = ver;
    }

    /// <summary>Marca en rojo parpadeante el objeto que el demoledor está a punto de golpear.</summary>
    public void MarcarAmenaza(bool activa)
    {
        amenazado = activa;
        if (activa && amenaza == null && colision != null)
        {
            Bounds b = colision.bounds;
            amenaza = Provisional.Crear(transform, "Amenaza", Provisional.Cuadrado, new Color(1f, 0.1f, 0.1f, 0.5f), Provisional.OrdenY(b.min.y) + 2);
            amenaza.transform.position = b.center;
            amenaza.transform.localScale = new Vector3(b.size.x, b.size.y, 1f);
        }
        if (amenaza != null) amenaza.enabled = activa;
    }

    public bool Danar()
    {
        if (Estado == EstadoObjeto.Roto) return false;
        Estado = Estado == EstadoObjeto.Intacto ? EstadoObjeto.Danado : EstadoObjeto.Roto;
        if (Estado == EstadoObjeto.Roto) GestorAudio.Reproducir(Snd.ObjetoRotoAlerta);
        Aplicar();
        // Trozos que saltan al romperse (más si queda roto del todo).
        if (colision != null) TrozoVolador.Lanzar(Centro, Estado == EstadoObjeto.Roto ? 7 : 4, Provisional.OrdenY(colision.bounds.min.y) + 6);
        if (CambioEstado != null) CambioEstado(this);
        return true;
    }

    /// <summary>Lo deja intacto de una vez (poder de reparación total). Devuelve cuántos estados reparó.</summary>
    public int RepararTodo()
    {
        int pasos = 0;
        while (Estado != EstadoObjeto.Intacto && Reparar()) pasos++;
        return pasos;
    }

    public bool Reparar()
    {
        if (Estado == EstadoObjeto.Intacto) return false;
        Estado = Estado == EstadoObjeto.Roto ? EstadoObjeto.Danado : EstadoObjeto.Intacto;
        Aplicar();
        if (CambioEstado != null) CambioEstado(this);
        return true;
    }

    /// <summary>Invitado en multijugador: copia el estado que manda el anfitrión (sin sonidos; avisa el cambio al HUD).</summary>
    public void FijarEstadoRed(EstadoObjeto e, bool marcado)
    {
        if (marcado != amenazaRed) { amenazaRed = marcado; MarcarAmenaza(marcado); }
        if (e == Estado) return;
        Estado = e;
        Aplicar();
        if (CambioEstado != null) CambioEstado(this);
    }
    private bool amenazaRed;

    /// <summary>Hay una marca roja de amenaza encima (algún enemigo va a golpearlo).</summary>
    public bool Amenazado { get { return amenazado; } }

    /// <summary>Intacto, sin avisar (el piso avisa una sola vez al reiniciar).</summary>
    public void Reiniciar()
    {
        Estado = EstadoObjeto.Intacto;
        Aplicar();
    }

    private void Aplicar()
    {
        if (dibujo == null) return;

        dibujo.transform.rotation = Quaternion.identity;
        ObjetoCenital ficha = definicion != null ? definicion.ObjetoCenitalDe(indice) : null;
        Sprite[] sprites = ficha != null ? ficha.SpritesCompletos() : null;
        Sprite soloIntacto = sprites == null && ficha != null && ficha.sprites != null && ficha.sprites.Length > 0 ? ficha.sprites[0] : null;
        Bounds b = colision.bounds;

        if (sprites != null)
        {
            dibujo.sprite = sprites[(int)Estado];
            dibujo.color = Color.white;

            // Tamaño natural del arte (reducido solo si no cabe en el collider). La medida sale del sprite
            // intacto, así el objeto no cambia de tamaño ni de lugar al romperse.
            Provisional.AjustarAlArea(dibujo, sprites[0], b);
            MostrarGrietas(b, false);
        }
        else if (soloIntacto != null)
        {
            // Solo hay arte intacto: el dañado y el roto se generan oscureciéndolo y superponiendo grietas.
            dibujo.sprite = soloIntacto;
            float k = Estado == EstadoObjeto.Intacto ? 1f : Estado == EstadoObjeto.Danado ? 0.85f : 0.65f;
            dibujo.color = new Color(k, k, k, 1f);
            Provisional.AjustarAlArea(dibujo, soloIntacto, b);
            if (Estado == EstadoObjeto.Roto)
            {
                // Roto: desplazado e inclinado un poco, para que no parezca el mismo objeto con grietas.
                dibujo.transform.position += new Vector3(0.05f, -0.04f, 0f);
                dibujo.transform.rotation = Quaternion.Euler(0f, 0f, 3f);
            }
            MostrarGrietas(b, Estado == EstadoObjeto.Roto);
        }
        else
        {
            LaboratorioPiso lab = definicion != null ? definicion.LaboratorioDe(laboratorio) : null;
            Color baseColor = ficha != null && ficha.colorProvisional.a > 0.01f ? ficha.colorProvisional
                            : lab != null ? lab.color : new Color(0.7f, 0.7f, 0.7f);
            Color c = baseColor;
            if (Estado == EstadoObjeto.Danado) c = Color.Lerp(baseColor, new Color(0.95f, 0.8f, 0.2f), 0.45f) * 0.8f;
            else if (Estado == EstadoObjeto.Roto) c = Color.Lerp(baseColor, new Color(0.3f, 0.1f, 0.1f), 0.75f) * 0.7f;
            c.a = 1f;

            dibujo.sprite = Provisional.Cuadrado;
            dibujo.color = c;
            dibujo.transform.localScale = new Vector3(Mathf.Max(0.2f, b.size.x - 0.12f), Mathf.Max(0.2f, b.size.y - 0.12f), 1f);
            MostrarGrietas(b, Estado == EstadoObjeto.Roto);
        }

        if (marca != null)
        {
            marca.color = Estado == EstadoObjeto.Intacto ? new Color(0.25f, 0.85f, 0.35f)
                        : Estado == EstadoObjeto.Danado ? new Color(1f, 0.75f, 0.1f)
                        : new Color(0.9f, 0.15f, 0.15f);
        }
    }

    /// <summary>
    /// Superpone las grietas del arte: chica (objetos de 1 x 1), mediana (2 x 1 y 1 x 2, girada si el objeto es vertical)
    /// o grande (3 x 1, 2 x 2 y mayores; se encoge al tamaño del objeto). Un objeto roto siempre lleva las grandes.
    /// </summary>
    private void MostrarGrietas(Bounds b, bool forzarGrandes)
    {
        if (Estado == EstadoObjeto.Intacto || !ConGrietas)
        {
            if (grietas != null) grietas.enabled = false;
            return;
        }

        if (grietas == null)
        {
            Transform previo = transform.Find("Grietas");
            if (previo != null) Provisional.Eliminar(previo.gameObject);
            grietas = Provisional.Crear(transform, "Grietas", null, Color.white, Provisional.OrdenY(b.min.y) + 1);
        }

        float ancho = b.size.x, alto = b.size.y;
        float largo = Mathf.Max(ancho, alto), corto = Mathf.Min(ancho, alto);
        int nivel = forzarGrandes || Estado == EstadoObjeto.Roto ? 2 : largo <= 1.01f ? 0 : (largo <= 2.01f && corto <= 1.01f) ? 1 : 2;
        Sprite arte = Provisional.Grietas(nivel);
        grietas.sprite = arte;

        Vector3 centro = new Vector3(b.center.x, b.center.y, 0f);
        bool esArte = Mathf.Approximately(arte.pixelsPerUnit, 32f);
        if (esArte)
        {
            Vector2 u = arte.bounds.size;
            bool girar = alto > ancho * 1.01f && u.x > u.y * 1.01f;
            float sw = girar ? u.y : u.x, sh = girar ? u.x : u.y;
            float k = Mathf.Min(1f, ancho / sw), j = Mathf.Min(1f, alto / sh);
            // El arte conserva su tamaño natural; solo se encoge si el objeto es más chico.
            Vector3 escala = girar ? new Vector3(j, k, 1f) : new Vector3(k, j, 1f);
            grietas.transform.rotation = Quaternion.Euler(0f, 0f, girar ? 90f : 0f);
            grietas.transform.localScale = escala;
        }
        else
        {
            grietas.transform.rotation = Quaternion.identity;
            grietas.transform.localScale = new Vector3(ancho * 0.9f, alto * 0.9f, 1f);
        }
        grietas.transform.position = centro;
        grietas.enabled = true;
    }

    // Con arte completo de tres estados no hace falta superponer grietas.
    private bool ConGrietas
    {
        get
        {
            ObjetoCenital ficha = definicion != null ? definicion.ObjetoCenitalDe(indice) : null;
            return ficha == null || ficha.SpritesCompletos() == null;
        }
    }
}
