using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Copia fija de un sprite que se desvanece (estela de las zapatillas turbo, golpes rápidos). Se crea con
/// EstelaFantasma.Crear y se destruye sola.
/// </summary>
public class EstelaFantasma : MonoBehaviour
{
    private SpriteRenderer dibujo;
    private Color color;
    private float inicio, duracion;
    private static readonly Stack<EstelaFantasma> libres = new Stack<EstelaFantasma>();

    public static EstelaFantasma Crear(SpriteRenderer origen, Color color, float duracion)
    {
        if (origen == null || origen.sprite == null) return null;
        EstelaFantasma e = null;
        while (e == null && libres.Count > 0) e = libres.Pop();
        GameObject g;
        SpriteRenderer sr;
        if (e != null) { g = e.gameObject; sr = e.dibujo; sr.enabled = true; EspejoAnfitrion.Redecidir(sr); g.SetActive(true); }
        else
        {
            g = new GameObject("Estela");
            sr = g.AddComponent<SpriteRenderer>();
            e = g.AddComponent<EstelaFantasma>();
        }
        g.transform.position = origen.transform.position;
        g.transform.rotation = origen.transform.rotation;
        g.transform.localScale = origen.transform.lossyScale;
        sr.sprite = origen.sprite;
        sr.flipX = origen.flipX;
        sr.flipY = origen.flipY;
        sr.sortingOrder = origen.sortingOrder - 1;
        sr.color = color;
        e.dibujo = sr;
        e.color = color;
        e.inicio = Time.time;
        e.duracion = Mathf.Max(0.05f, duracion);
        return e;
    }

    private void Update()
    {
        float k = (Time.time - inicio) / duracion;
        if (k >= 1f)
        {
            if (libres.Count < 80) { gameObject.SetActive(false); libres.Push(this); }
            else Destroy(gameObject);
            return;
        }
        Color c = color;
        c.a = color.a * (1f - k);
        dibujo.color = c;
    }
}
