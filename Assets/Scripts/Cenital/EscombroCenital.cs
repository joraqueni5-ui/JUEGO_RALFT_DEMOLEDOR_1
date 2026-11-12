using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Escombros lanzados por el demoledor: vuelan en arco hasta un punto del suelo. Una sombra
/// marca dónde van a caer (crece mientras el escombro baja). Al caer avisa para que el
/// demoledor aplique el daño en el radio de impacto.
/// </summary>
public class EscombroCenital : MonoBehaviour
{
    private static readonly List<EscombroCenital> activos = new List<EscombroCenital>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        activos.Clear();
    }

    public static void EliminarTodos()
    {
        for (int i = activos.Count - 1; i >= 0; i--)
        {
            if (activos[i] != null) Destroy(activos[i].gameObject);
        }
        activos.Clear();
    }

    private Vector2 origen, destino;
    private float duracion, radio, t, alturaArco = 2.4f;
    private Action<Vector2> alImpactar;
    private Transform sombra;
    private SpriteRenderer sombraDibujo;
    private SpriteRenderer dibujo;

    /// <summary>Lanza un escombro de 'origen' a 'destino' en 'duracion' segundos.</summary>
    public static EscombroCenital Lanzar(Vector2 origen, Vector2 destino, float duracion, float radio,
        DefinicionPiso def, Action<Vector2> alImpactar)
    {
        GameObject o = new GameObject("EscombroLanzado");
        EscombroCenital e = o.AddComponent<EscombroCenital>();
        e.origen = origen;
        e.destino = destino;
        e.duracion = Mathf.Max(0.2f, duracion);
        e.radio = radio;
        e.alImpactar = alImpactar;

        Sprite sprite = def != null && def.escombro != null ? def.escombro : null;
        e.dibujo = o.AddComponent<SpriteRenderer>();
        e.dibujo.sprite = sprite != null ? sprite : Provisional.Cuadrado;
        e.dibujo.color = sprite != null ? Color.white : new Color(0.55f, 0.5f, 0.45f);
        e.dibujo.sortingOrder = 300;
        if (sprite == null) o.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
        o.transform.position = origen;

        GameObject s = new GameObject("SombraImpacto");
        e.sombra = s.transform;
        e.sombraDibujo = s.AddComponent<SpriteRenderer>();
        Sprite spriteSombra = def != null && def.sombraImpacto != null ? def.sombraImpacto : null;
        e.sombraDibujo.sprite = spriteSombra != null ? spriteSombra : Provisional.Circulo;
        e.sombraDibujo.color = new Color(0f, 0f, 0f, 0.15f);
        e.sombraDibujo.sortingOrder = -119;
        s.transform.position = destino;
        s.transform.localScale = new Vector3(0.3f, 0.15f, 1f);

        activos.Add(e);
        return e;
    }

    private void Update()
    {
        t += Time.deltaTime / duracion;
        float k = Mathf.Clamp01(t);

        Vector2 plano = Vector2.Lerp(origen, destino, k);
        float arco = alturaArco * 4f * k * (1f - k);
        transform.position = new Vector3(plano.x, plano.y + arco, 0f);
        transform.Rotate(0f, 0f, 540f * Time.deltaTime);

        // La sombra crece hasta el tamaño del impacto y se oscurece al acercarse.
        float ancho = Mathf.Lerp(0.3f, radio * 1.8f, k);
        sombra.localScale = new Vector3(ancho, ancho * 0.5f, 1f);
        sombraDibujo.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.15f, 0.55f, k));

        if (t >= 1f) Caer();
    }

    private void Caer()
    {
        GameObject aro = new GameObject("ImpactoEscombro");
        aro.transform.position = destino;
        aro.AddComponent<DestelloCenital>().Iniciar(radio);

        Action<Vector2> cb = alImpactar;
        alImpactar = null;
        if (cb != null) cb(destino);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        activos.Remove(this);
        if (sombra != null) Destroy(sombra.gameObject);
    }
}

/// <summary>Aro rojo que se desvanece al caer un escombro (muestra el radio de impacto).</summary>
public class DestelloCenital : MonoBehaviour
{
    private SpriteRenderer dibujo;
    private float t;
    private float radio;

    public void Iniciar(float radio)
    {
        this.radio = radio;
        dibujo = gameObject.AddComponent<SpriteRenderer>();
        dibujo.sprite = Provisional.Circulo;
        dibujo.sortingOrder = -118;
        dibujo.color = new Color(1f, 0.3f, 0.2f, 0.5f);
        transform.localScale = new Vector3(radio * 2f, radio * 2f, 1f);
    }

    private void Update()
    {
        t += Time.deltaTime / 0.35f;
        float k = Mathf.Clamp01(t);
        transform.localScale = Vector3.one * radio * 2f * Mathf.Lerp(0.6f, 1f, k);
        dibujo.color = new Color(1f, 0.3f, 0.2f, 0.5f * (1f - k));
        if (t >= 1f) Destroy(gameObject);
    }
}
