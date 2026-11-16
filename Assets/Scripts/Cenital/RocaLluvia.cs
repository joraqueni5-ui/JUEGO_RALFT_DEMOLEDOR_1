using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Una roca de la lluvia de escombros: cae en vertical desde arriba de la pantalla girando (roca_lluvia_1/2/3). Su sombra está en el
/// suelo, en el punto de impacto, y crece con sombra_impacto_grande; los últimos segundos se marca con borde rojo. Al caer suelta
/// polvo, trozos y un temblor suave de cámara, y avisa para que el demoledor aplique el daño.
/// </summary>
public class RocaLluvia : MonoBehaviour
{
    private static readonly List<RocaLluvia> activas = new List<RocaLluvia>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar() { activas.Clear(); }

    public static IReadOnlyList<RocaLluvia> Activas { get { return activas; } }
    public static int Cantidad { get { return activas.Count; } }

    public static void EliminarTodas()
    {
        for (int i = activas.Count - 1; i >= 0; i--) if (activas[i] != null) Destroy(activas[i].gameObject);
        activas.Clear();
    }

    /// <summary>Cuántas sombras activas hay a menos de 'radio' de 'punto'.</summary>
    public static int SombrasCerca(Vector2 punto, float radio)
    {
        int n = 0;
        foreach (RocaLluvia r in activas) if (r != null && Vector2.Distance(r.destino, punto) < radio) n++;
        return n;
    }

    public Vector2 Destino { get { return destino; } }

    private Vector2 destino;
    private float alturaInicio, duracion, t, segundosBorde, fuerzaTemblor, duracionTemblor;
    private Action<Vector2> alImpactar;
    private SpriteRenderer dibujo;
    private SpriteRenderer sombra, bordeRojo;
    private Sprite[] cuadrosSombra;
    private float escalaSombra = 1f;

    /// <summary>Suelta una roca que caerá en 'destino' dentro de 'caida' segundos, empezando 'alturaInicio' baldosas más arriba.</summary>
    public static RocaLluvia Lanzar(Vector2 destino, float alturaInicio, float caida, DefinicionPiso def, Action<Vector2> alImpactar)
    {
        GameObject o = new GameObject("RocaLluvia");
        RocaLluvia r = o.AddComponent<RocaLluvia>();
        r.destino = destino;
        r.alturaInicio = Mathf.Max(3f, alturaInicio);
        r.duracion = Mathf.Max(0.3f, caida);
        r.alImpactar = alImpactar;
        r.segundosBorde = def != null ? def.segundosBordeRojo : 0.3f;
        r.fuerzaTemblor = def != null ? def.fuerzaTemblorImpacto * 0.7f : 0.04f;
        r.duracionTemblor = def != null ? def.duracionTemblorImpacto : 0.15f;

        // La roca: una de las tres series al azar, girando.
        ArteNiveles arte = ArteNiveles.Actual;
        Sprite[] serie = null;
        if (arte != null)
        {
            int cual = UnityEngine.Random.Range(0, 3);
            serie = ArteNiveles.Serie(cual == 0 ? arte.rocaLluvia1 : cual == 1 ? arte.rocaLluvia2 : arte.rocaLluvia3);
        }
        r.dibujo = o.AddComponent<SpriteRenderer>();
        r.dibujo.sortingOrder = 320;
        if (serie != null)
        {
            r.dibujo.sprite = serie[0];
            AnimadorSprite a = o.AddComponent<AnimadorSprite>();
            a.cuadros = serie;
            a.cuadrosPorSegundo = 14f;
            a.bucle = true;
            a.destruirAlTerminar = false;
        }
        else
        {
            r.dibujo.sprite = Provisional.Cuadrado;
            r.dibujo.color = new Color(0.5f, 0.47f, 0.43f);
            o.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        }
        o.transform.position = new Vector3(destino.x, destino.y + r.alturaInicio, 0f);

        // La sombra queda en el suelo, en el punto de impacto.
        GameObject s = new GameObject("SombraRoca");
        s.transform.SetParent(o.transform, false);
        r.sombra = s.AddComponent<SpriteRenderer>();
        r.sombra.sortingOrder = -119;
        r.cuadrosSombra = arte != null ? ArteNiveles.Serie(arte.sombraImpactoGrande) : null;
        GameObject b = new GameObject("BordeRojo");
        b.transform.SetParent(s.transform, false);
        r.bordeRojo = b.AddComponent<SpriteRenderer>();
        r.bordeRojo.sortingOrder = -120;
        r.bordeRojo.color = new Color(1f, 0.1f, 0.08f, 0.95f);
        r.bordeRojo.enabled = false;
        if (r.cuadrosSombra != null)
        {
            r.sombra.sprite = r.cuadrosSombra[0];
            r.escalaSombra = 1f;
        }
        else
        {
            r.sombra.sprite = Provisional.Circulo;
            r.sombra.color = new Color(0f, 0f, 0f, 0.3f);
            r.bordeRojo.sprite = Provisional.Circulo;
        }

        activas.Add(r);
        return r;
    }

    private void LateUpdate()
    {
        t += Time.deltaTime / duracion;
        float k = Mathf.Clamp01(t);

        // Cae acelerando; la sombra no se mueve del punto de impacto.
        float y = destino.y + alturaInicio * (1f - k * k);
        transform.position = new Vector3(destino.x, y, 0f);
        sombra.transform.position = new Vector3(destino.x, destino.y, 0f);
        sombra.transform.rotation = Quaternion.identity;
        sombra.transform.localScale = Vector3.one / Mathf.Max(0.0001f, transform.localScale.x) * escalaSombra;

        if (cuadrosSombra != null)
        {
            Sprite f = cuadrosSombra[Mathf.Min(cuadrosSombra.Length - 1, (int)(k * cuadrosSombra.Length))];
            sombra.sprite = f;
            sombra.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.55f, 1f, k));
            bordeRojo.sprite = f;
        }
        else
        {
            float d = Mathf.Lerp(0.5f, 1.6f, k);
            sombra.transform.localScale = new Vector3(d, d * 0.5f, 1f) / Mathf.Max(0.0001f, transform.localScale.x);
        }

        bool marcar = duracion * (1f - k) <= segundosBorde;
        bordeRojo.enabled = marcar;
        if (marcar) bordeRojo.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

        if (t >= 1f) Caer();
    }

    private void Caer()
    {
        GestorAudio.Reproducir(Snd.RocaCayendo);
        ArteNiveles arte = ArteNiveles.Actual;
        Sprite[] polvo = arte != null ? ArteNiveles.Serie(arte.polvoImpactoGrande) : null;
        if (polvo == null && arte != null) polvo = ArteNiveles.Serie(arte.polvoImpacto);
        EfectoSprite.Reproducir(polvo, destino, 305, 1f, 12f);
        TrozoVolador.Lanzar(destino, UnityEngine.Random.Range(3, 5));
        CamaraPisoCenital.Temblar(fuerzaTemblor, duracionTemblor);

        Action<Vector2> cb = alImpactar;
        alImpactar = null;
        if (cb != null) cb(destino);
        Destroy(gameObject);
    }

    private void OnDestroy() { activas.Remove(this); }
}
