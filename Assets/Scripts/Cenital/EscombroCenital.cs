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
        RocaLluvia.EliminarTodas();
    }

    private Vector2 origen, destino;
    private float duracion, radio, t, alturaArco = 2.4f;
    private Action<Vector2> alImpactar;
    private Transform sombra;
    private SpriteRenderer sombraDibujo;
    private SpriteRenderer dibujo;
    private Sprite[] cuadrosSombra;
    private float escalaSombra = 1f;
    private bool girar = true;
    private bool esBloque;
    private float escalaMax = 1f, segundosBorde = 0.3f, fuerzaTemblor = 0.06f, duracionTemblor = 0.15f;
    private SpriteRenderer bordeRojo;

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

        // Con arte: el ladrillo gira por sus cuadros (4) en vez de girar el sprite.
        Sprite[] bloque = ArteNiveles.De(a => a.bloqueEscombroGrande);
        Sprite[] ladrillo = bloque != null ? bloque : ArteNiveles.De(a => a.escombroLadrillo);
        e.esBloque = bloque != null;
        e.segundosBorde = def != null ? def.segundosBordeRojo : 0.3f;
        e.fuerzaTemblor = def != null ? def.fuerzaTemblorImpacto : 0.06f;
        e.duracionTemblor = def != null ? def.duracionTemblorImpacto : 0.15f;
        if (bloque != null)
        {
            // Bloque grande: arco más alto y escala que sube en lo alto del arco (1 -> 1,3 -> 1).
            e.alturaArco = def != null ? def.alturaArcoBloque : 2.5f;
            e.escalaMax = def != null ? def.escalaMaximaBloque : 1.3f;
        }
        if (ladrillo != null)
        {
            e.dibujo.sprite = ladrillo[0];
            e.dibujo.color = Color.white;
            o.transform.localScale = Vector3.one;
            AnimadorSprite an = o.AddComponent<AnimadorSprite>();
            an.cuadros = ladrillo;
            an.cuadrosPorSegundo = bloque != null ? 14f : 12f;
            an.bucle = true;
            an.destruirAlTerminar = false;
            e.girar = false;
        }

        GameObject s = new GameObject("SombraImpacto");
        e.sombra = s.transform;
        e.sombraDibujo = s.AddComponent<SpriteRenderer>();
        Sprite spriteSombra = def != null && def.sombraImpacto != null ? def.sombraImpacto : null;
        e.sombraDibujo.sprite = spriteSombra != null ? spriteSombra : Provisional.Circulo;
        e.sombraDibujo.color = new Color(0f, 0f, 0f, 0.15f);
        e.sombraDibujo.sortingOrder = -119;
        s.transform.position = destino;
        s.transform.localScale = new Vector3(0.3f, 0.15f, 1f);

        // Con arte: la sombra son 4 cuadros que crecen hasta el tamaño del impacto.
        e.cuadrosSombra = bloque != null ? ArteNiveles.De(a => a.sombraImpactoGrande) : null;
        if (e.cuadrosSombra == null) e.cuadrosSombra = ArteNiveles.De(a => a.sombraImpacto);
        if (e.cuadrosSombra != null)
        {
            GameObject borde = new GameObject("BordeRojo");
            borde.transform.SetParent(s.transform, false);
            e.bordeRojo = borde.AddComponent<SpriteRenderer>();
            e.bordeRojo.sortingOrder = -120;
            e.bordeRojo.color = new Color(1f, 0.1f, 0.08f, 0.95f);
            e.bordeRojo.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            e.bordeRojo.enabled = false;
            e.sombraDibujo.sprite = e.cuadrosSombra[0];
            e.sombraDibujo.color = Color.white;
            float ultimo = Mathf.Max(0.25f, e.cuadrosSombra[e.cuadrosSombra.Length - 1].bounds.size.x);
            e.escalaSombra = Mathf.Max(0.5f, radio * 2f) / ultimo;
            s.transform.localScale = new Vector3(e.escalaSombra, e.escalaSombra, 1f);
        }

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
        if (girar) transform.Rotate(0f, 0f, 540f * Time.deltaTime);
        if (escalaMax > 1f) { float s = 1f + (escalaMax - 1f) * Mathf.Sin(k * Mathf.PI); transform.localScale = new Vector3(s, s, 1f); }

        if (cuadrosSombra != null)
        {
            // El cuadro de la sombra avanza con el vuelo: el último es el del impacto.
            sombraDibujo.sprite = cuadrosSombra[Mathf.Min(cuadrosSombra.Length - 1, (int)(k * cuadrosSombra.Length))];
            sombraDibujo.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.55f, 1f, k));
            if (bordeRojo != null)
            {
                // Los últimos instantes la sombra se marca con borde rojo.
                bordeRojo.sprite = sombraDibujo.sprite;
                bordeRojo.enabled = duracion * (1f - k) <= segundosBorde;
            }
        }
        else
        {
            // La sombra crece hasta el tamaño del impacto y se oscurece al acercarse.
            float ancho = Mathf.Lerp(0.3f, radio * 1.8f, k);
            sombra.localScale = new Vector3(ancho, ancho * 0.5f, 1f);
            sombraDibujo.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.15f, 0.55f, k));
        }

        if (t >= 1f) Caer();
    }

    private void Caer()
    {
        GameObject aro = new GameObject("ImpactoEscombro");
        aro.transform.position = destino;
        aro.AddComponent<DestelloCenital>().Iniciar(radio);
        GestorAudio.Reproducir(Snd.ImpactoEscombro);
        Sprite[] polvo = esBloque ? ArteNiveles.De(a => a.polvoImpactoGrande) : null;
        if (polvo == null) polvo = ArteNiveles.De(a => a.polvoImpacto);
        EfectoSprite.Reproducir(polvo, destino, 305, esBloque ? 1f : Mathf.Max(1f, radio * 1.4f));
        TrozoVolador.Lanzar(destino, esBloque ? UnityEngine.Random.Range(3, 5) : 4);
        if (esBloque) CamaraPisoCenital.Temblar(fuerzaTemblor, duracionTemblor);

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
