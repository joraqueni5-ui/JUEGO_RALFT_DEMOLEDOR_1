using System.Collections;
using UnityEngine;

/// <summary>
/// Cuando el demoledor se va y se abre la salida (el ascensor, o el punto de salto en el último piso), la cámara viaja
/// hasta ella para mostrarla, vuelve a tu personaje y queda una flecha amarilla girando a su alrededor que apunta a la
/// salida hasta que llegues. Cada equipo la ve por su cuenta (en multijugador apunta desde su propio personaje).
/// </summary>
public class GuiaSalida : MonoBehaviour
{
    private static GuiaSalida actual;
    private Vector2 destino;
    private SpriteRenderer flecha;

    /// <summary>Muestra el camino a 'salida'. Se llama una vez cuando se abre la salida.</summary>
    public static void Mostrar(Vector2 salida)
    {
        PisoCenital piso = PisoCenital.Actual;
        if (piso == null || piso.Mapa == null) return;
        if (actual != null) Destroy(actual.gameObject);
        GameObject g = new GameObject("GuiaSalida");
        g.transform.SetParent(piso.Mapa.transform, true);   // dentro del mapa: cada equipo dibuja la suya
        actual = g.AddComponent<GuiaSalida>();
        actual.destino = salida;
        actual.StartCoroutine(actual.Recorrido());
    }

    private IEnumerator Recorrido()
    {
        Sprite s = ArteExtra.Uno("Mejoras/flecha_guia");
        flecha = Provisional.Crear(transform, "Flecha", s != null ? s : Provisional.Cuadrado, new Color(1f, 1f, 1f, 0f), 600);

        // La cámara va a la salida, se queda un momento y vuelve.
        yield return new WaitForSecondsRealtime(0.6f);
        float inicio = Time.unscaledTime;
        while (Time.unscaledTime - inicio < 2f)
        {
            CamaraPisoCenital.Enfocar(destino, 0.85f);
            yield return null;
        }
        CamaraPisoCenital.SoltarEnfoque();
    }

    private void Update()
    {
        PisoCenital piso = PisoCenital.Actual;
        Vector2 propia;
        if (flecha == null || piso == null) return;
        if (piso.Terminado || !CamaraPisoCenital.PosicionPropia(out propia)) { flecha.enabled = false; return; }
        Vector2 hacia = destino - propia;
        float d = hacia.magnitude;
        bool ver = d > 1.6f;
        flecha.enabled = ver;
        if (!ver) return;
        Vector2 dir = hacia / d;
        float pulso = 1f + 0.12f * Mathf.Sin(Time.time * 8f);
        flecha.transform.position = propia + dir * (1.15f + 0.1f * Mathf.Sin(Time.time * 6f)) + Vector2.up * 0.3f;
        flecha.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        flecha.transform.localScale = new Vector3(pulso, pulso, 1f);
        flecha.color = new Color(1f, 1f, 1f, Mathf.MoveTowards(flecha.color.a, 0.95f, Time.deltaTime * 3f));
    }

    private void OnDestroy() { if (actual == this) actual = null; }
}
