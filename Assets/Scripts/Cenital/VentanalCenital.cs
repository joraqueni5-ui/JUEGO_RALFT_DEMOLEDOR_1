using UnityEngine;

/// <summary>
/// El ventanal de la pared de arriba por donde entra el demoledor. Su collider lo mantiene
/// cerrado para el jugador; el demoledor entra "rompiéndolo" (cambia de arte y suelta un temblor).
/// </summary>
public class VentanalCenital : MonoBehaviour
{
    public Sprite spriteEntero;
    public Sprite spriteRoto;
    [Tooltip("Se rompe cuando entra el demoledor (una puerta de vidrio sí; unas escaleras no).")]
    public bool rompible = true;

    private SpriteRenderer dibujo;
    private Collider2D colision;
    public bool Roto { get; private set; }

    private static readonly Color Entero = new Color(0.65f, 0.88f, 1f, 0.95f);
    private static readonly Color Quebrado = new Color(0.25f, 0.35f, 0.45f, 0.6f);

    public void Preparar(DefinicionPiso d)
    {
        if (d != null)
        {
            if (d.ventanalEntero != null) spriteEntero = d.ventanalEntero;
            if (d.ventanalRoto != null) spriteRoto = d.ventanalRoto;
        }
        colision = GetComponent<Collider2D>();
        if (dibujo == null)
        {
            Transform previo = transform.Find("Dibujo");
            if (previo != null) Provisional.Eliminar(previo.gameObject);
            dibujo = Provisional.Crear(transform, "Dibujo", Provisional.Cuadrado, Entero, -140);
            if (colision != null)
            {
                Physics2D.SyncTransforms();
                dibujo.transform.position = colision.bounds.center;
            }
        }
        Restaurar();
    }

    public void Romper()
    {
        if (!rompible) return;
        GestorAudio.Reproducir(Snd.VidrioRoto);
        Roto = true;
        Mostrar();
        CamaraPisoCenital.Temblar(0.25f, 0.4f);
    }

    public void Restaurar()
    {
        Roto = false;
        Mostrar();
    }

    private void Mostrar()
    {
        if (dibujo == null) return;
        Sprite s = Roto ? spriteRoto : spriteEntero;
        if (s != null)
        {
            dibujo.sprite = s;
            dibujo.color = Color.white;
            dibujo.transform.localScale = Vector3.one;
        }
        else
        {
            dibujo.sprite = Provisional.Cuadrado;
            dibujo.color = Roto ? Quebrado : Entero;
            if (colision != null) dibujo.transform.localScale = new Vector3(colision.bounds.size.x, colision.bounds.size.y, 1f);
        }
    }
}
