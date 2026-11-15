using UnityEngine;

/// <summary>
/// Ascensor del piso: cerrado (sólido) hasta que el demoledor se rinde y se va. Al abrirse parpadea en
/// verde y deja de bloquear; cuando el jugador entra en él se supera el piso.
/// </summary>
public class AscensorCenital : MonoBehaviour
{
    private SpriteRenderer dibujo;
    private SpriteRenderer destello;
    private Collider2D colision;
    private Sprite spriteCerrado;
    private Color colorAbierto = new Color(0.3f, 1f, 0.45f, 1f);
    private JugadorCenital jugador;

    public bool Abierto { get; private set; }

    public void Preparar(DefinicionPiso def)
    {
        colision = GetComponent<Collider2D>();
        dibujo = GetComponent<SpriteRenderer>();
        if (dibujo != null && spriteCerrado == null) spriteCerrado = dibujo.sprite;
        if (def != null) colorAbierto = def.colorAscensorAbierto;
        jugador = FindFirstObjectByType<JugadorCenital>();

        if (destello == null)
        {
            Transform previo = transform.Find("DestelloAbierto");
            if (previo != null) Provisional.Eliminar(previo.gameObject);
            destello = Provisional.Crear(transform, "DestelloAbierto", Provisional.Cuadrado, colorAbierto, -139);
            if (colision != null)
            {
                Physics2D.SyncTransforms();
                destello.transform.position = colision.bounds.center;
                destello.transform.localScale = new Vector3(colision.bounds.size.x, colision.bounds.size.y, 1f);
            }
        }
        Cerrar(def);
    }

    public void Cerrar(DefinicionPiso def)
    {
        Abierto = false;
        if (colision != null) colision.isTrigger = false;
        if (dibujo != null)
        {
            if (spriteCerrado != null) dibujo.sprite = spriteCerrado;
            dibujo.color = Color.white;
        }
        if (destello != null) destello.enabled = false;
    }

    public void Abrir(DefinicionPiso def)
    {
        if (Abierto) return;
        Abierto = true;
        GestorAudio.Reproducir(Snd.AscensorAbre);
        if (colision != null) colision.isTrigger = true;
        if (def != null && def.ascensorAbierto != null && dibujo != null) dibujo.sprite = def.ascensorAbierto;
        if (destello != null) destello.enabled = true;
        CamaraPisoCenital.Temblar(0.04f, 0.2f);
    }

    private void Update()
    {
        if (!Abierto) return;

        // Parpadeo verde (sobre el arte, que se queda visible por debajo).
        if (destello != null)
        {
            float k = 0.25f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.time * 7f));
            Color c = colorAbierto;
            c.a = k;
            destello.color = c;
        }

        if (jugador == null) jugador = FindFirstObjectByType<JugadorCenital>();
        PisoCenital piso = PisoCenital.Actual;
        if (jugador == null || piso == null || colision == null || piso.Terminado) return;
        if (colision.bounds.Contains(new Vector3(jugador.Centro.x, jugador.Centro.y, colision.bounds.center.z)))
            piso.JugadorLlegoAlAscensor();
    }
}
