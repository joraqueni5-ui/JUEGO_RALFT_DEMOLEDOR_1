using UnityEngine;

/// <summary>Una marca en el piso (grietas del pisotón, quemaduras...) que se desvanece sola.</summary>
public class MarcaTemporal : MonoBehaviour
{
    private SpriteRenderer sr;
    private float inicio, duracion;
    private Color color;

    /// <summary>Crea la marca centrada en 'centro', de 'radio' baldosas, que dura 'segundos'.</summary>
    public static MarcaTemporal Crear(Sprite sprite, Vector2 centro, float radio, float segundos)
    {
        if (sprite == null) return null;
        GameObject g = new GameObject("MarcaTemporal");
        g.transform.position = centro;
        float tam = Mathf.Max(0.01f, sprite.bounds.size.x);
        float e = radio * 2f / tam;
        g.transform.localScale = new Vector3(e, e * 0.85f, 1f);
        g.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        MarcaTemporal m = g.AddComponent<MarcaTemporal>();
        m.sr = g.AddComponent<SpriteRenderer>();
        m.sr.sprite = sprite;
        m.sr.sortingOrder = -150;
        m.color = Color.white;
        m.inicio = Time.time;
        m.duracion = Mathf.Max(0.5f, segundos);
        return m;
    }

    private void Update()
    {
        float k = (Time.time - inicio) / duracion;
        if (k >= 1f) { Destroy(gameObject); return; }
        Color c = color;
        c.a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
        sr.color = c;
    }
}
