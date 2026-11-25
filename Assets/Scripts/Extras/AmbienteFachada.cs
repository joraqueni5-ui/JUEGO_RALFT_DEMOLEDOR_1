using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nivel final (la fachada): cielo con vida. Nubes que pasan despacio detrás del edificio (a distintas alturas y
/// velocidades) y bandadas de pájaros que cruzan aleteando. Todo alrededor de la cámara.
/// Además, la ciudad de La Paz con vida: el Illimani y las laderas en capas que se mueven a distinta velocidad
/// (paralaje), el teleférico cruzando, autos y minibuses por la avenida y gente en la vereda que mira y señala el
/// edificio (y grita de vez en cuando).
/// InstaladorExtras lo agrega en la escena del nivel final.
/// </summary>
public class AmbienteFachada : MonoBehaviour
{
    private class Cosa { public Transform t; public SpriteRenderer sr; public float vel; public bool pajaro; public float fase; }

    private readonly List<Cosa> cosas = new List<Cosa>();
    private Sprite[] nubes, pajaros;
    private float proximaBandada;
    private Camera cam;

    // Ciudad (paralaje), calle y gente.
    private class Capa { public Transform t; public Vector2 base0; public float fx, fy; }
    private readonly List<Capa> capas = new List<Capa>();
    private Vector2 camInicial;
    private Transform cabinaTeleferico;
    private Vector2 cableA, cableB;
    private float faseCabina;
    private class Auto { public SpriteRenderer sr; public float vel; }
    private readonly List<Auto> autos = new List<Auto>();
    private Sprite[] spAutos;
    private float proximoAuto, ySuelo, yCalle;
    private class Persona { public SpriteRenderer sr; public Sprite[] serie; public float fase, y0; public Personajes.Ficha ficha; }
    private readonly List<Persona> gente = new List<Persona>();
    private float proximoGrito;
    private static readonly string[] Gritos =
    {
        "¡MIREN ARRIBA!", "¡ES EL DEMOLEDOR!", "¡VAMOS, TÚ PUEDES!", "¡CUIDADO CON LAS PALOMAS!", "¡SALVEN LA U!",
        "¡QUÉ VALIENTE!", "¡YO NO SUBO NI LOCO!", "¡ARREGLA ESA VENTANA!",
    };

    private void Start()
    {
        cam = Camera.main;
        nubes = new[] { ArteExtra.Uno("Mejoras/nube_0"), ArteExtra.Uno("Mejoras/nube_1"), ArteExtra.Uno("Mejoras/nube_2") };
        pajaros = ArteExtra.Serie("Mejoras/pajaro_", 2);
        if (cam == null) { enabled = false; return; }
        Ciudad();
        if (nubes[0] == null) { nubes = null; return; }
        float alto = cam.orthographicSize, ancho = alto * cam.aspect;
        for (int i = 0; i < 6; i++) CrearNube(cam.transform.position.x + Random.Range(-ancho, ancho));
        proximaBandada = Time.time + 3f;
    }

    private void CrearNube(float x)
    {
        float alto = cam.orthographicSize;
        Sprite s = nubes[Random.Range(0, nubes.Length)];
        if (s == null) return;
        SpriteRenderer sr = Provisional.Crear(transform, "Nube", s, new Color(1f, 1f, 1f, Random.Range(0.55f, 0.85f)), -8);
        float e = Random.Range(1.2f, 2.2f);
        sr.transform.localScale = new Vector3(e, e, 1f);
        sr.transform.position = new Vector3(x, cam.transform.position.y + Random.Range(-alto * 0.2f, alto * 0.9f), 0f);
        cosas.Add(new Cosa { t = sr.transform, sr = sr, vel = Random.Range(0.15f, 0.45f) * e });
    }

    private void Bandada()
    {
        if (pajaros == null) return;
        float alto = cam.orthographicSize, ancho = alto * cam.aspect;
        bool izq = Random.value < 0.5f;
        Vector3 c = cam.transform.position;
        float y = c.y + Random.Range(0.1f, 0.8f) * alto;
        int n = Random.Range(3, 7);
        for (int i = 0; i < n; i++)
        {
            SpriteRenderer sr = Provisional.Crear(transform, "Pajaro", pajaros[0], Color.white, -6);
            sr.transform.localScale = Vector3.one * 1.4f;
            sr.transform.position = new Vector3(c.x + (izq ? -ancho - 1f - i * 0.4f : ancho + 1f + i * 0.4f), y + (i % 2) * 0.3f + i * 0.08f, 0f);
            cosas.Add(new Cosa { t = sr.transform, sr = sr, vel = (izq ? 1f : -1f) * Random.Range(2f, 2.8f), pajaro = true, fase = Random.value * 3f });
        }
    }

    private void Update()
    {
        if (cam == null) return;
        float alto = cam.orthographicSize, ancho = alto * cam.aspect;
        Vector3 c = cam.transform.position;
        for (int i = cosas.Count - 1; i >= 0; i--)
        {
            Cosa k = cosas[i];
            if (k.t == null) { cosas.RemoveAt(i); continue; }
            k.t.position += new Vector3(k.vel * Time.deltaTime, k.pajaro ? Mathf.Sin(Time.time * 3f + k.fase) * 0.2f * Time.deltaTime : 0f, 0f);
            if (k.pajaro && pajaros != null) k.sr.sprite = pajaros[(int)((Time.time + k.fase) * 8f) % pajaros.Length];
            float dx = k.t.position.x - c.x;
            bool fuera = Mathf.Abs(dx) > ancho + 6f || Mathf.Abs(k.t.position.y - c.y) > alto * 3f;
            if (!fuera) continue;
            Destroy(k.t.gameObject);
            cosas.RemoveAt(i);
            if (!k.pajaro && nubes != null) CrearNube(c.x - ancho - 3f);   // las nubes se reponen por la izquierda
        }
        ActualizarCiudad();
        if (Time.time >= proximaBandada)
        {
            proximaBandada = Time.time + Random.Range(7f, 13f);
            Bandada();
        }
    }

    // ================================================================== la ciudad de La Paz
    private void Ciudad()
    {
        camInicial = cam.transform.position;
        Rect edificio = EdificioAlto.Instancia != null ? EdificioAlto.Instancia.Limites : new Rect(0f, 0f, 7.22f, 13.18f);
        ySuelo = edificio.yMin;
        yCalle = ySuelo - 1.05f;
        float cx = edificio.center.x;

        // Capas: el Illimani al fondo, las laderas lejos y los barrios más cerca (se mueven menos cuanto más lejos).
        CrearCapa("Fachada/montanas", cx, ySuelo + 0.7f, -48, 1.7f, 0.85f, 0.75f, new Color(0.92f, 0.94f, 1f));
        CrearCapa("Fachada/ciudad_lejos", cx, ySuelo + 0.15f, -47, 1.5f, 0.7f, 0.55f, new Color(0.9f, 0.92f, 1f));
        CrearCapa("Fachada/ciudad_cerca", cx, ySuelo - 0.35f, -46, 1.35f, 0.5f, 0.35f, Color.white);

        // Teleférico: cable cruzando el cielo y una cabina roja que va y viene.
        Sprite cab = ArteExtra.Uno("Fachada/cabina");
        if (cab != null)
        {
            cableA = new Vector2(cx - 14f, ySuelo + 4.6f);
            cableB = new Vector2(cx + 14f, ySuelo + 6.2f);
            Transform grupo = new GameObject("Teleferico").transform;
            grupo.SetParent(transform, false);
            SpriteRenderer cable = Provisional.Crear(grupo, "Cable", Provisional.Cuadrado, new Color(0.2f, 0.2f, 0.25f, 0.8f), -45);
            Vector2 d = cableB - cableA;
            cable.transform.localPosition = (cableA + cableB) * 0.5f;
            cable.transform.localScale = new Vector3(d.magnitude, 0.03f, 1f);
            cable.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            SpriteRenderer c = Provisional.Crear(grupo, "Cabina", cab, Color.white, -44);
            c.transform.localScale = Vector3.one * 1.3f;
            cabinaTeleferico = c.transform;
            capas.Add(new Capa { t = grupo, base0 = Vector2.zero, fx = 0.6f, fy = 0.45f });
        }

        // Autos y minibuses por la avenida.
        spAutos = ArteExtra.Serie("Fachada/auto_", 4);
        if (spAutos != null) for (int i = 0; i < 3; i++) CrearAuto(cx + Random.Range(-9f, 9f));
        proximoAuto = Time.time + 2f;

        // Gente en la vereda, a los costados del edificio, mirando hacia arriba.
        Personajes.Ficha[] todos = Personajes.Todos;
        int n = 0;
        for (int i = 0; i < 8; i++)
        {
            Personajes.Ficha f = todos[(i * 5 + 3) % todos.Length];
            Sprite[] serie = Personajes.Serie(f, "arriba");
            if (serie == null) continue;
            bool izquierda = n % 2 == 0;
            float x = izquierda ? edificio.xMin - 0.9f - (n / 2) * 0.75f : edificio.xMax + 0.9f + (n / 2) * 0.75f;
            x += Random.Range(-0.15f, 0.15f);
            SpriteRenderer sr = Provisional.Crear(transform, "Mirón", serie[0], Color.white, 4);
            float e = 0.95f * f.escalaAltura / Mathf.Max(0.01f, serie[0].bounds.size.y);
            sr.transform.localScale = new Vector3(e, e, 1f);
            float y0 = ySuelo - 0.12f - serie[0].bounds.min.y * e - (n % 3) * 0.06f;
            sr.transform.position = new Vector3(x, y0, 0f);
            sr.sortingOrder = 4 + (n % 3);
            gente.Add(new Persona { sr = sr, serie = serie, fase = Random.value * 6f, y0 = y0, ficha = f });
            n++;
        }
        proximoGrito = Time.time + 4f;
    }

    private void CrearCapa(string ruta, float cx, float yBase, int orden, float escala, float fx, float fy, Color color)
    {
        Sprite s = ArteExtra.Uno(ruta);
        if (s == null) return;
        Transform grupo = new GameObject("Capa_" + ruta).transform;
        grupo.SetParent(transform, false);
        float ancho = s.bounds.size.x * escala;
        for (int i = -1; i <= 1; i++)
        {
            SpriteRenderer sr = Provisional.Crear(grupo, "Tramo", s, color, orden);
            sr.transform.localScale = new Vector3(escala * (i == 0 ? 1f : -1f), escala, 1f);   // los costados en espejo: sin cortes
            sr.transform.localPosition = new Vector3(i * (ancho - 0.02f), 0f, 0f);
        }
        Vector2 b = new Vector2(cx, yBase);
        grupo.position = b;
        capas.Add(new Capa { t = grupo, base0 = b, fx = fx, fy = fy });
    }

    private void CrearAuto(float x)
    {
        if (spAutos == null) return;
        int k = Random.Range(0, spAutos.Length);
        bool derecha = Random.value < 0.5f;
        float carril = derecha ? 0f : -0.22f;
        SpriteRenderer sr = Provisional.Crear(transform, k == 3 ? "Minibus" : "Auto", spAutos[k], Color.white, derecha ? 3 : 2);
        sr.transform.localScale = Vector3.one * 1.35f;
        sr.flipX = !derecha;
        sr.transform.position = new Vector3(x, yCalle + carril, 0f);
        autos.Add(new Auto { sr = sr, vel = (derecha ? 1f : -1f) * (k == 3 ? Random.Range(1.6f, 2.2f) : Random.Range(2.4f, 3.6f)) });
    }

    private void ActualizarCiudad()
    {
        Vector2 dc = (Vector2)cam.transform.position - camInicial;
        foreach (Capa c in capas)
        {
            if (c.t == null) continue;
            c.t.position = c.base0 + new Vector2(dc.x * c.fx, dc.y * c.fy);
        }
        if (cabinaTeleferico != null)
        {
            float k = Mathf.PingPong(Time.time / 18f + faseCabina, 1f);
            cabinaTeleferico.localPosition = Vector2.Lerp(cableA, cableB, k) + new Vector2(0f, -0.28f);
            cabinaTeleferico.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.7f) * 4f);
        }

        // Autos: entran por un costado de la cámara y salen por el otro.
        float alto = cam.orthographicSize, ancho = alto * cam.aspect;
        float cx = cam.transform.position.x;
        for (int i = autos.Count - 1; i >= 0; i--)
        {
            Auto a = autos[i];
            if (a.sr == null) { autos.RemoveAt(i); continue; }
            Vector3 p = a.sr.transform.position;
            p.x += a.vel * Time.deltaTime;
            p.y = (a.vel > 0f ? yCalle : yCalle - 0.22f) + Mathf.Abs(Mathf.Sin(Time.time * 14f + i)) * 0.02f;
            a.sr.transform.position = p;
            if (Mathf.Abs(p.x - cx) > ancho + 4f) { Destroy(a.sr.gameObject); autos.RemoveAt(i); }
        }
        if (spAutos != null && Time.time >= proximoAuto && autos.Count < 6)
        {
            proximoAuto = Time.time + Random.Range(1.5f, 3.5f);
            bool porIzq = Random.value < 0.5f;
            CrearAuto(cx + (porIzq ? -ancho - 2f : ancho + 2f));
            Auto nuevo = autos[autos.Count - 1];
            if ((nuevo.vel > 0f) != porIzq) { nuevo.vel = -nuevo.vel; nuevo.sr.flipX = !nuevo.sr.flipX; nuevo.sr.sortingOrder = nuevo.vel > 0f ? 3 : 2; }
        }

        // Gente: se balancea, alguno salta y señala (se voltea hacia el edificio).
        Muneco m = muneco != null ? muneco : (muneco = Object.FindAnyObjectByType<Muneco>());
        foreach (Persona g in gente)
        {
            if (g.sr == null) continue;
            float t = Time.time + g.fase;
            bool salta = Mathf.Repeat(t, 6f) < 0.5f;
            float y = g.y0 + (salta ? Mathf.Sin(Mathf.Repeat(t, 6f) / 0.5f * Mathf.PI) * 0.18f : Mathf.Abs(Mathf.Sin(t * 1.6f)) * 0.03f);
            g.sr.transform.position = new Vector3(g.sr.transform.position.x, y, 0f);
            g.sr.sprite = g.serie[salta ? 1 % g.serie.Length : 0];
            if (m != null) g.sr.flipX = m.transform.position.x < g.sr.transform.position.x;
        }
        if (gente.Count > 0 && Time.time >= proximoGrito)
        {
            proximoGrito = Time.time + Random.Range(6f, 11f);
            Persona g = gente[Random.Range(0, gente.Count)];
            if (g.sr != null && Mathf.Abs(g.sr.transform.position.y - cam.transform.position.y) < alto)
                Burbuja.Decir(g.sr.transform, Gritos[Random.Range(0, Gritos.Length)], g.ficha.Voz, 1.2f);
        }
    }

    private Muneco muneco;
}
