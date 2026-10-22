using UnityEngine;

/// <summary>
/// Trocito de bloque que sale volando al destruirlo: cae, gira y se desvanece solo.
/// </summary>
public class Escombro : MonoBehaviour
{
    public float gravedad = 26f;
    public float vida = 0.9f;

    private SpriteRenderer dibujo;
    private Vector2 velocidad;
    private Color color;
    private float tiempo;

    public static void Crear(Sprite sprite, Vector3 posicion, int orden)
    {
        if (sprite == null) return;

        GameObject objeto = new GameObject("Escombro");
        objeto.transform.position = posicion;
        objeto.transform.localScale = Vector3.one * Random.Range(0.25f, 0.4f);
        objeto.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        SpriteRenderer sr = objeto.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = orden;

        Escombro escombro = objeto.AddComponent<Escombro>();
        escombro.dibujo = sr;
        escombro.color = sr.color;
        escombro.velocidad = new Vector2(Random.Range(-2.4f, 2.4f), Random.Range(1.5f, 4.5f));

        Destroy(objeto, 2.5f);
    }

    private void Update()
    {
        tiempo += Time.deltaTime;
        velocidad.y -= gravedad * Time.deltaTime;
        transform.position += (Vector3)(velocidad * Time.deltaTime);
        transform.Rotate(0f, 0f, 220f * Time.deltaTime);

        if (dibujo != null)
        {
            Color c = color;
            c.a = Mathf.Clamp01(1f - tiempo / Mathf.Max(0.01f, vida));
            dibujo.color = c;
        }
    }
}
