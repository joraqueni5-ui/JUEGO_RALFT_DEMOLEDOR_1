using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Vida en el piso: ventiladores de techo que giran (su sombra pasa sobre todo), vapor que sale de las ollas en
/// gastronomía, pantallas de laptops que brillan y parpadean en informática, hojas que vuela el viento en la terraza
/// y polvo que cae del techo con cada temblor fuerte (pisotón, choque, rugido).
/// Todo cuelga del mapa: en multijugador cada equipo lo dibuja por su cuenta. InstaladorExtras lo agrega a cada piso.
/// </summary>
public class AnimacionesAmbiente : MonoBehaviour
{
    private readonly List<Transform> ventiladores = new List<Transform>();
    private readonly List<float> velocidades = new List<float>();
    private readonly List<Transform> ollas = new List<Transform>();
    private readonly List<Light2D> pantallas = new List<Light2D>();
    private Sprite[] vapor, polvo, hojas;
    private Transform padre;
    private int numero;
    private float proximoVapor, proximaHoja;
    private bool listo;

    private IEnumerator Start()
    {
        // Espera a que el piso, los detalles del suelo y las luces estén armados.
        for (int i = 0; i < 4; i++) yield return null;
        PisoCenital piso = PisoCenital.Actual;
        if (piso == null || piso.Mapa == null) yield break;
        MapaCenital mapa = piso.Mapa;
        padre = mapa.transform;
        numero = piso.Definicion != null ? piso.Definicion.numero : 0;
        vapor = ArteExtra.Serie("Mejoras/vapor_", 4);
        polvo = ArteExtra.Serie("Mejoras/polvo_", 2);
        Sprite hoja = ArteExtra.Uno("Detalles/hoja_seca");
        if (hoja != null) hojas = new[] { hoja };

        Random.State antes = Random.state;
        Random.InitState(999 + numero * 7);

        // Ventiladores de techo (no en la terraza, que está al aire libre).
        Sprite spVent = ArteExtra.Uno("Mejoras/ventilador");
        List<Vector2> celdas = mapa.CentrosTransitables;
        if (spVent != null && numero != 6 && celdas.Count > 0)
        {
            List<Vector2> puestos = new List<Vector2>();
            for (int intento = 0; intento < 40 && puestos.Count < 3; intento++)
            {
                Vector2 c = celdas[Random.Range(0, celdas.Count)];
                bool lejos = true;
                foreach (Vector2 p in puestos) if (Vector2.Distance(p, c) < 6f) lejos = false;
                if (!lejos) continue;
                puestos.Add(c);
                SpriteRenderer sr = Provisional.Crear(padre, "Ventilador", spVent, new Color(0.15f, 0.12f, 0.12f, 0.28f), 470);
                sr.transform.position = c + new Vector2(0.3f, 0.4f);
                sr.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
                ventiladores.Add(sr.transform);
                velocidades.Add(Random.Range(260f, 380f) * (Random.value < 0.5f ? 1f : -1f));
            }
        }
        Random.state = antes;

        // Ollas (gastronomía) y laptops (informática) que dejó DetallesEscenario en el suelo.
        foreach (SpriteRenderer sr in mapa.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.name == "olla") ollas.Add(sr.transform);
            else if (sr.name == "laptop" && pantallas.Count < 6)
            {
                GameObject g = new GameObject("Pantalla");
                g.transform.SetParent(padre, true);
                g.transform.position = sr.transform.position;
                Light2D l = g.AddComponent<Light2D>();
                l.lightType = Light2D.LightType.Point;
                l.color = new Color(0.45f, 0.75f, 1f);
                l.intensity = 0.5f;
                l.pointLightOuterRadius = 1.6f;
                l.pointLightInnerRadius = 0.2f;
                pantallas.Add(l);
            }
        }

        CamaraPisoCenital.AlTemblar += AlTemblar;
        listo = true;
    }

    private void OnDestroy()
    {
        CamaraPisoCenital.AlTemblar -= AlTemblar;
    }

    /// <summary>Temblor fuerte: cae polvo del techo alrededor de la cámara.</summary>
    private void AlTemblar(float fuerza, float duracion)
    {
        if (!listo || polvo == null || fuerza < 0.1f) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        float alto = cam.orthographicSize, ancho = alto * cam.aspect;
        Vector3 c = cam.transform.position;
        int n = Mathf.Clamp(Mathf.RoundToInt(fuerza * 120f), 8, 30);
        for (int i = 0; i < n; i++)
        {
            Vector2 p = new Vector2(c.x + Random.Range(-ancho, ancho), c.y + Random.Range(-alto * 0.4f, alto));
            ParticulaSimple.Crear(polvo, p, new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(-0.4f, -0.1f)), Random.Range(0.8f, 1.5f),
                480, new Color(1f, 1f, 1f, 0.8f), 1f, 0.8f, 2.2f, padre);
        }
    }

    private void Update()
    {
        if (!listo) return;
        for (int i = 0; i < ventiladores.Count; i++)
            if (ventiladores[i] != null) ventiladores[i].Rotate(0f, 0f, velocidades[i] * Time.deltaTime);

        // Vapor de las ollas.
        if (vapor != null && ollas.Count > 0 && Time.time >= proximoVapor)
        {
            proximoVapor = Time.time + 0.35f;
            Transform o = ollas[Random.Range(0, ollas.Count)];
            if (o != null)
                ParticulaSimple.Crear(vapor, (Vector2)o.position + new Vector2(Random.Range(-0.15f, 0.15f), 0.2f),
                    new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(0.4f, 0.7f)), Random.Range(1f, 1.5f), 460,
                    new Color(1f, 1f, 1f, 0.55f), 0.4f, 1f, 0f, padre);
        }

        // Pantallas que parpadean.
        for (int i = 0; i < pantallas.Count; i++)
        {
            Light2D l = pantallas[i];
            if (l == null) continue;
            float n = Mathf.PerlinNoise(Time.time * 3f, i * 7.3f);
            l.intensity = 0.3f + 0.35f * n;
        }

        // Hojas al viento en la terraza.
        if (numero == 6 && hojas != null && Time.time >= proximaHoja)
        {
            proximaHoja = Time.time + Random.Range(0.4f, 0.9f);
            Camera cam = Camera.main;
            if (cam != null)
            {
                float alto = cam.orthographicSize, ancho = alto * cam.aspect;
                Vector3 c = cam.transform.position;
                Vector2 p = new Vector2(c.x - ancho - 0.5f, c.y + Random.Range(-alto, alto));
                ParticulaSimple.Crear(hojas, p, new Vector2(Random.Range(1.4f, 2.4f), Random.Range(-0.5f, 0.3f)), Random.Range(4f, 6f),
                    465, Color.white, 0.6f, 0.6f, 0f, padre);
            }
        }
    }
}
