using UnityEngine;

/// <summary>
/// El daño se ve: los objetos ROTOS echan humo y los DAÑADOS sueltan alguna chispa. Las partículas cuelgan del mapa, así
/// en multijugador cada equipo las dibuja por su cuenta según el estado de cada objeto.
/// InstaladorExtras lo agrega a cada piso cenital.
/// </summary>
public class DanoVisible : MonoBehaviour
{
    private Sprite[] humo, chispas;
    private float proximo;

    private void Start()
    {
        humo = ArteExtra.Serie("Mejoras/humo_", 4);
        chispas = ArteExtra.Serie("Mejoras/chispa_", 3);
    }

    private void Update()
    {
        if (Time.time < proximo) return;
        proximo = Time.time + 0.2f;
        PisoCenital piso = PisoCenital.Actual;
        if (piso == null || piso.Mapa == null || piso.Terminado) return;
        Transform padre = piso.Mapa.transform;
        foreach (ObjetoReparable o in piso.Mapa.objetos)
        {
            if (o == null || o.Estado == EstadoObjeto.Intacto) continue;
            Bounds b = o.Area;
            int orden = Provisional.OrdenY(b.min.y) + 4;
            if (o.Estado == EstadoObjeto.Roto && humo != null && Random.value < 0.22f)
            {
                Vector2 p = new Vector2(Random.Range(b.min.x, b.max.x), b.center.y + b.extents.y * 0.5f);
                ParticulaSimple.Crear(humo, p, new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(0.5f, 0.9f)), Random.Range(1.1f, 1.6f),
                    orden, new Color(1f, 1f, 1f, 0.7f), 0.5f, 1.2f, 0f, padre);
            }
            else if (o.Estado == EstadoObjeto.Danado && chispas != null && Random.value < 0.06f)
            {
                Vector2 p = new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y));
                ParticulaSimple.Crear(chispas, p, new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(0.6f, 1.4f)), 0.35f,
                    orden, Color.white, 0.8f, 0.5f, 4f, padre);
            }
        }
    }
}
