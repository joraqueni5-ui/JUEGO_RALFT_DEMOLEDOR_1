using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Más vida en cada piso de la U (Unifranz La Paz):
///   - Ventanas con vista a La Paz (el Illimani, las laderas con casas y el teleférico pasando), según la hora del piso:
///     mañana abajo, tarde en el medio y atardecer en la terraza.
///   - Afiches con humor de la U, distintos por piso, el letrero naranja de UNIFRANZ en la planta baja y la placa del piso
///     junto al ascensor.
///   - Objetos con vida propia según la carrera: hornos con fuego, monitor con latido junto a la camilla, computadoras con
///     código en pantalla, pizarras con diapositivas, proyector con su haz de luz, parlante con notas, cafetera con vapor,
///     refrigerador con frío, lavamanos que gotean, impresora que saca hojas y libros que se caen del estante al temblar.
///   - Reparar con gracia: las chispas vuelven al objeto, destello y el objeto "salta". Y cada cosa se rompe a su manera
///     (chispas, agua, fuego).
///   - El edificio se va rompiendo a la vista: grietas que aparecen con el % de destrozo, polvo que cae, lámparas que
///     tiemblan y, desde el 75 %, escombros en el piso y la alarma roja girando.
/// Todo cuelga del mapa: en multijugador cada equipo lo arma igual. InstaladorExtras lo agrega a cada piso cenital.
/// </summary>
public class VidaEscenario : MonoBehaviour
{
    private PisoCenital piso;
    private MapaCenital mapa;
    private Transform raiz;
    private int numero;
    private bool listo;

    // Vistas y teleférico.
    private readonly List<Transform> cabinas = new List<Transform>();
    private readonly List<float> fasesCabina = new List<float>();

    // Objetos con vida.
    private enum Clase { Ninguna, Fuego, Monitor, Computadora, Pantalla, Proyector, Parlante, Cafetera, Frio, Agua, Impresora, Libros, Microscopio }
    private class Vivo
    {
        public ObjetoReparable o;
        public Clase clase;
        public readonly List<SpriteRenderer> pantallas = new List<SpriteRenderer>();
        public Light2D luz;
        public float proximo, fase;
        public SpriteRenderer haz;
    }
    private readonly List<Vivo> vivos = new List<Vivo>();
    private readonly Dictionary<ObjetoReparable, EstadoObjeto> estadoPrevio = new Dictionary<ObjetoReparable, EstadoObjeto>();

    // Daño visible.
    private class Marca { public SpriteRenderer sr; public float umbral; public float alfaMax; }
    private readonly List<Marca> grietas = new List<Marca>();
    private readonly List<Marca> escombros = new List<Marca>();
    private float proximoPolvo, proximoCrujido;

    private Sprite[] llamas, vapor, polvo, chispas, ecg, codigo, diapos, notas, libros, salpica;
    private Sprite ecgPlano, papel, gota;

    private IEnumerator Start()
    {
        for (int i = 0; i < 5; i++) yield return null;
        piso = PisoCenital.Actual;
        if (piso == null || piso.Mapa == null) { enabled = false; yield break; }
        mapa = piso.Mapa;
        numero = piso.Definicion != null ? piso.Definicion.numero : 0;
        raiz = new GameObject("VidaEscenario").transform;
        raiz.SetParent(mapa.transform, false);

        llamas = ArteExtra.Serie("Martillos/llama_", 3);
        vapor = ArteExtra.Serie("Mejoras/vapor_", 4);
        polvo = ArteExtra.Serie("Mejoras/polvo_", 2);
        chispas = ArteExtra.Serie("Mejoras/chispa_", 3);
        ecg = ArteExtra.Serie("Vida/ecg_", 8);
        codigo = ArteExtra.Serie("Vida/codigo_", 4);
        diapos = ArteExtra.Serie("Vida/diapo_", 3);
        notas = ArteExtra.Serie("Vida/nota_", 2);
        libros = ArteExtra.Serie("Vida/libro_", 4);
        salpica = ArteExtra.Serie("Peligros/salpica_", 3);
        ecgPlano = ArteExtra.Uno("Vida/ecg_plano");
        papel = ArteExtra.Uno("Vida/papel");
        gota = ArteExtra.Uno("Peligros/gota");

        Random.State antes = Random.state;
        Random.InitState(5150 + numero * 23);
        List<Vector2> ocupadas = PosicionesDecoracion();
        Ventanas(ocupadas);
        Carteles(ocupadas);
        Letreros();
        ObjetosVivos();
        DanoDelEdificio();
        Random.state = antes;

        CamaraPisoCenital.AlTemblar += AlTemblar;
        listo = true;
    }

    private void OnDestroy()
    {
        CamaraPisoCenital.AlTemblar -= AlTemblar;
        foreach (Vivo v in vivos) if (v.o != null) v.o.CambioEstado -= AlCambiarObjeto;
        IluminacionNivel.Inestabilidad = 0f;
        IluminacionNivel.AlarmaDerrumbe = false;
    }

    // ================================================================== paredes: ventanas, afiches, letreros
    /// <summary>Lo que ya colgó DecoracionParedes (para no tapar nada).</summary>
    private List<Vector2> PosicionesDecoracion()
    {
        List<Vector2> r = new List<Vector2>();
        Transform deco = mapa.transform.Find("DecoracionParedes");
        if (deco != null) foreach (Transform t in deco) r.Add(t.position);
        return r;
    }

    private static bool Libre(List<Vector2> ocupadas, Vector2 p, float distancia)
    {
        foreach (Vector2 o in ocupadas) if (Mathf.Abs(o.x - p.x) < distancia && Mathf.Abs(o.y - p.y) < 1.2f) return false;
        return true;
    }

    /// <summary>Celdas libres con pared justo arriba (la cara de la pared que da a la sala).</summary>
    private List<Vector2> CarasDePared(bool soloBordeDeArriba)
    {
        List<Vector2> r = new List<Vector2>();
        Rect lim = mapa.Limites;
        foreach (Vector2 c in mapa.CentrosTransitables)
        {
            if (soloBordeDeArriba && c.y < lim.yMax - 2.2f) continue;
            if (EsPared(c + Vector2.up)) r.Add(c);
        }
        for (int i = r.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); Vector2 t = r[i]; r[i] = r[j]; r[j] = t; }
        return r;
    }

    private void Ventanas(List<Vector2> ocupadas)
    {
        Sprite vista = ArteExtra.Uno("Vista/ventana_" + (numero == 9 ? 9 : Mathf.Clamp(numero, 0, 6)));
        Sprite cabina = ArteExtra.Uno("Vista/cabina");
        if (vista == null) return;
        int puestas = 0;
        foreach (Vector2 c in CarasDePared(true))
        {
            if (puestas >= 8) break;
            Vector2 p = new Vector2(c.x, c.y + 0.3f);
            if (!Libre(ocupadas, p, 2.2f)) continue;
            if (mapa.ventanal != null && Vector2.Distance(mapa.ventanal.transform.position, p) < 2.5f) continue;
            ocupadas.Add(p);
            puestas++;
            SpriteRenderer sr = Provisional.Crear(raiz, "VentanaVista", vista, Color.white, -141);
            sr.transform.position = p;
            if (cabina != null && puestas % 2 == 1)
            {
                SpriteRenderer cb = Provisional.Crear(sr.transform, "Teleferico", cabina, Color.white, -140);
                cabinas.Add(cb.transform);
                fasesCabina.Add(Random.value);
            }
        }
    }

    private static readonly string[][] AfichesGenerales =
    {
        new[] { "SE BUSCA", "EL DEMOLEDOR\nRecompensa:\n1 punto extra" },
        new[] { "¡OJO!", "Examen final\nmañana.\n¡No entres\nen pánico!" },
        new[] { "AVISO", "Prohibido\nromper\nla U" },
        new[] { "UNIFRANZ", "La Paz\nHéroes del\nAcre 1855" },
    };

    private static string[][] AfichesDelPiso(int n)
    {
        switch (n)
        {
            case 0: return new[] { new[] { "MATRÍCULA", "¡Abierta!\nPasa por\nrecepción" }, new[] { "CAFETERÍA", "Hoy:\nsalteñas\ny api" } };
            case 1: return new[] { new[] { "COCINA", "No dejes\nel horno\nprendido" }, new[] { "SALUD", "Lávate las\nmanos. El\ndemoledor\nno lo hace" } };
            case 2: return new[] { new[] { "WIFI", "Red: UNIFRANZ\nClave:\n¿cuál clave?" }, new[] { "TIP", "Ctrl+Z no\nfunciona en\nla vida real" } };
            case 3: return new[] { new[] { "SILENCIO", "Se está\nestudiando" }, new[] { "CLUB", "Debate hoy\n18:00\naula 305" } };
            case 4: return new[] { new[] { "RECUERDA", "Entrega del\nproyecto:\nel viernes" }, new[] { "PERDIDO", "Martillo\namarillo.\nRecompensa" } };
            case 5: return new[] { new[] { "CUIDADO", "Los pupitres\nno son\ntrampolines" }, new[] { "FERIA", "Feria de\nproyectos\naula 512" } };
            case 6: return new[] { new[] { "SHHH", "Biblioteca:\nel demoledor\nno lee" }, new[] { "TERRAZA", "Prohibido\nsaltar.\nSí, va por ti" } };
            default: return new[] { new[] { "SOBREVIVE", "Aguanta\nhasta el\namanecer" }, new[] { "¡ALERTA!", "Villanos en\nel Gran Hall" } };
        }
    }

    private void Carteles(List<Vector2> ocupadas)
    {
        List<string[]> lista = new List<string[]>(AfichesDelPiso(numero));
        string[][] generales = AfichesGenerales;
        lista.Add(generales[(numero + 0) % generales.Length]);
        lista.Add(generales[(numero + 1) % generales.Length]);
        int k = 0;
        foreach (Vector2 c in CarasDePared(false))
        {
            if (k >= lista.Count) break;
            Vector2 p = new Vector2(c.x, c.y + 0.28f);
            if (!Libre(ocupadas, p, 1.6f)) continue;
            if (mapa.spawnJugador != null && Vector2.Distance(c, mapa.spawnJugador.position) < 1.5f) continue;
            ocupadas.Add(p);
            string[] a = lista[k];
            int color = a[0] == "UNIFRANZ" || a[0] == "MATRÍCULA" ? 0 : (k % 3) + 1;
            Sprite s = ArteExtra.Uno("Carteles/cartel_" + color);
            if (s == null) break;
            SpriteRenderer sr = Provisional.Crear(raiz, "Afiche", s, Color.white, -140);
            sr.transform.position = p;
            float alto = s.bounds.size.y;
            TextMesh t = Provisional.CrearTexto(sr.transform, "Titulo", a[0], 0.13f, Color.white, -139);
            t.transform.localPosition = new Vector3(0f, alto - 0.17f, 0f);
            TextMesh cuerpo = Provisional.CrearTexto(sr.transform, "Texto", a[1], 0.1f, new Color(0.18f, 0.16f, 0.2f), -139);
            cuerpo.transform.localPosition = new Vector3(0f, alto * 0.42f, 0f);
            k++;
        }
    }

    private void Letreros()
    {
        // Placa del piso junto al ascensor ("PISO 3 · AULAS 300").
        Sprite placa = ArteExtra.Uno("Carteles/placa");
        if (placa != null && mapa.ascensor != null && piso.Definicion != null)
        {
            SpriteRenderer sr = Provisional.Crear(raiz, "PlacaPiso", placa, Color.white, -139);
            sr.transform.position = (Vector2)mapa.ascensor.transform.position + new Vector2(0f, 1.05f);
            string area = piso.Definicion.NombreArea.ToUpperInvariant();
            if (area.Length > 22) area = area.Substring(0, 22);
            TextMesh t = Provisional.CrearTexto(sr.transform, "Texto", (numero == 0 ? "PLANTA BAJA" : numero == 9 ? "EL GRAN HALL" : "PISO " + numero) + "\n" + area,
                0.085f, new Color(1f, 0.75f, 0.45f), -138);
            t.transform.localPosition = new Vector3(0f, placa.bounds.size.y * 0.5f, 0f);
        }

        // Letrero naranja de la universidad en la planta baja (y en el Gran Hall).
        Sprite letrero = ArteExtra.Uno("Carteles/letrero");
        if (letrero != null && (numero == 0 || numero == 9))
        {
            Vector2 cerca = mapa.spawnJugador != null ? (Vector2)mapa.spawnJugador.position : mapa.Limites.center;
            Vector2 mejor = Vector2.zero;
            float md = float.MaxValue;
            foreach (Vector2 c in CarasDePared(false))
            {
                float d = Vector2.Distance(c, cerca);
                if (d < md) { md = d; mejor = c; }
            }
            if (md < float.MaxValue)
            {
                SpriteRenderer sr = Provisional.Crear(raiz, "LetreroUnifranz", letrero, Color.white, -139);
                sr.transform.position = new Vector2(mejor.x, mejor.y + 0.25f);
                TextMesh t = Provisional.CrearTexto(sr.transform, "Texto", "UNIFRANZ  ·  LA PAZ", 0.2f, Color.white, -138);
                t.transform.localPosition = new Vector3(0f, letrero.bounds.size.y * 0.5f, 0f);
            }
            // Cartel de la calle junto a la entrada.
            if (placa != null && mapa.ventanal != null)
            {
                SpriteRenderer calle = Provisional.Crear(raiz, "CartelCalle", placa, new Color(0.7f, 1f, 0.75f), -139);
                calle.transform.position = (Vector2)mapa.ventanal.transform.position + new Vector2(0f, -1.6f);
                TextMesh t = Provisional.CrearTexto(calle.transform, "Texto", "AV. HÉROES DEL ACRE\nESQ. LANDAETA", 0.08f, Color.white, -138);
                t.transform.localPosition = new Vector3(0f, placa.bounds.size.y * 0.5f, 0f);
            }
        }
    }

    // ================================================================== objetos con vida
    private static string Normalizar(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.ToLowerInvariant();
        return s.Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u');
    }

    private static Clase ClaseDe(string nombre)
    {
        string n = Normalizar(nombre);
        if (n.Contains("horno") || n.Contains("estacion de trabajo")) return Clase.Fuego;
        if (n.Contains("camilla")) return Clase.Monitor;
        if (n.Contains("computadora") || n.Contains("imac")) return Clase.Computadora;
        if (n.Contains("pizarra interactiva") || n.Contains("pantalla")) return Clase.Pantalla;
        if (n.Contains("proyector")) return Clase.Proyector;
        if (n.Contains("parlante")) return Clase.Parlante;
        if (n.Contains("cafe")) return Clase.Cafetera;
        if (n.Contains("refrigerador")) return Clase.Frio;
        if (n.Contains("lavaplatos") || n.Contains("lavamanos")) return Clase.Agua;
        if (n.Contains("impresora")) return Clase.Impresora;
        if (n.Contains("estante") || n.Contains("libros")) return Clase.Libros;
        if (n.Contains("microscopio")) return Clase.Microscopio;
        return Clase.Ninguna;
    }

    private void ObjetosVivos()
    {
        foreach (ObjetoReparable o in mapa.objetos)
        {
            if (o == null) continue;
            estadoPrevio[o] = o.Estado;
            Vivo v = new Vivo { o = o, clase = ClaseDe(o.Nombre), fase = Random.value * 3f };
            o.CambioEstado += AlCambiarObjeto;
            vivos.Add(v);
            Bounds b = o.Area;
            int orden = o.Dibujo != null ? o.Dibujo.sortingOrder + 1 : Provisional.OrdenY(b.min.y) + 2;
            switch (v.clase)
            {
                case Clase.Fuego:
                    v.luz = Luz(b.center, new Color(1f, 0.55f, 0.2f), 0.6f, 1.8f);
                    break;
                case Clase.Monitor:
                    if (ecg != null)
                    {
                        SpriteRenderer m = Provisional.Crear(raiz, "MonitorSignos", ecg[0], Color.white, orden);
                        m.transform.position = new Vector2(b.max.x + 0.1f, b.max.y - 0.1f);
                        v.pantallas.Add(m);
                        v.luz = Luz(m.transform.position, new Color(0.4f, 1f, 0.5f), 0.35f, 1.2f);
                    }
                    break;
                case Clase.Computadora:
                    if (codigo != null)
                    {
                        int n = Mathf.Clamp(Mathf.RoundToInt(b.size.x / 0.9f), 1, 4);
                        for (int i = 0; i < n; i++)
                        {
                            SpriteRenderer s = Provisional.Crear(raiz, "Pantalla", codigo[i % codigo.Length], Color.white, orden);
                            s.transform.position = new Vector2(b.min.x + (i + 0.5f) * b.size.x / n, b.center.y + b.extents.y * 0.35f);
                            v.pantallas.Add(s);
                        }
                        v.luz = Luz(b.center, new Color(0.45f, 0.75f, 1f), 0.35f, 1.6f);
                    }
                    break;
                case Clase.Pantalla:
                    if (diapos != null)
                    {
                        SpriteRenderer s = Provisional.Crear(raiz, "Diapositiva", diapos[0], Color.white, orden);
                        s.transform.position = b.center;
                        float e = Mathf.Clamp(b.size.x * 0.7f / Mathf.Max(0.01f, diapos[0].bounds.size.x), 0.5f, 2.2f);
                        s.transform.localScale = new Vector3(e, e, 1f);
                        v.pantallas.Add(s);
                        v.luz = Luz(b.center, new Color(1f, 0.95f, 0.85f), 0.3f, 2f);
                    }
                    break;
                case Clase.Proyector:
                {
                    Sprite haz = ArteExtra.Uno("Vida/haz");
                    if (haz != null)
                    {
                        v.haz = Provisional.Crear(raiz, "HazProyector", haz, new Color(1f, 1f, 1f, 0.7f), 300);
                        v.haz.transform.position = b.center;
                        Vector2 dir = Vector2.up;
                        foreach (ObjetoReparable otro in mapa.objetos)
                            if (otro != null && ClaseDe(otro.Nombre) == Clase.Pantalla) { dir = ((Vector2)otro.Area.center - (Vector2)b.center).normalized; break; }
                        v.haz.transform.rotation = Quaternion.FromToRotation(Vector3.right, dir);
                        v.haz.transform.position = (Vector2)b.center + dir * 0.7f;
                    }
                    break;
                }
            }
        }
    }

    private Light2D Luz(Vector2 pos, Color color, float intensidad, float radio)
    {
        GameObject g = new GameObject("LuzObjeto");
        g.transform.SetParent(raiz, false);
        g.transform.position = pos;
        Light2D l = g.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = color;
        l.intensity = intensidad;
        l.pointLightOuterRadius = radio;
        l.pointLightInnerRadius = radio * 0.15f;
        return l;
    }

    private void AnimarObjetos()
    {
        float t = Time.time;
        foreach (Vivo v in vivos)
        {
            if (v.o == null) continue;
            EstadoObjeto e = v.o.Estado;
            bool roto = e == EstadoObjeto.Roto, danado = e == EstadoObjeto.Danado;
            Bounds b = v.o.Area;
            int orden = v.o.Dibujo != null ? v.o.Dibujo.sortingOrder + 1 : 0;
            switch (v.clase)
            {
                case Clase.Fuego:
                    if (v.luz != null) v.luz.intensity = roto ? 0f : 0.45f + 0.35f * Mathf.PerlinNoise(t * 8f, v.fase);
                    if (!roto && llamas != null && t >= v.proximo)
                    {
                        v.proximo = t + (danado ? 0.12f : 0.22f);
                        ParticulaSimple.Crear(llamas, new Vector2(Random.Range(b.min.x + 0.1f, b.max.x - 0.1f), b.center.y + b.extents.y * 0.3f),
                            new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(0.5f, 0.9f)), Random.Range(0.35f, 0.55f), orden + 2,
                            Color.white, danado ? 1.1f : 0.8f, 0.3f, -0.5f, raiz);
                    }
                    break;
                case Clase.Monitor:
                    if (v.pantallas.Count > 0 && v.pantallas[0] != null)
                    {
                        v.pantallas[0].sprite = roto && ecgPlano != null ? ecgPlano : ecg[(int)((t + v.fase) * (danado ? 16f : 9f)) % ecg.Length];
                        if (v.luz != null) v.luz.color = roto ? new Color(1f, 0.3f, 0.3f) : new Color(0.4f, 1f, 0.5f);
                        if (!roto && t >= v.proximo)
                        {
                            v.proximo = t + (danado ? 0.45f : 0.9f);
                            if (CercaDelJugador(b.center, 3.5f)) GestorAudio.Reproducir(SndExtra.Latido, 0.35f, danado ? 1.15f : 1f);
                        }
                    }
                    break;
                case Clase.Computadora:
                    for (int i = 0; i < v.pantallas.Count; i++)
                    {
                        SpriteRenderer s = v.pantallas[i];
                        if (s == null) continue;
                        s.enabled = !roto && !(danado && Mathf.PerlinNoise(t * 12f, i + v.fase) < 0.35f);
                        s.sprite = codigo[(int)((t + v.fase + i * 0.7f) * 2.5f) % codigo.Length];
                    }
                    if (v.luz != null) v.luz.intensity = roto ? 0f : 0.25f + 0.2f * Mathf.PerlinNoise(t * 3f, v.fase);
                    break;
                case Clase.Pantalla:
                    if (v.pantallas.Count > 0 && v.pantallas[0] != null)
                    {
                        v.pantallas[0].enabled = !roto && !(danado && Mathf.PerlinNoise(t * 10f, v.fase) < 0.3f);
                        v.pantallas[0].sprite = diapos[(int)((t + v.fase) / 3f) % diapos.Length];
                    }
                    if (v.luz != null) v.luz.intensity = roto ? 0f : 0.3f;
                    break;
                case Clase.Proyector:
                    if (v.haz != null)
                    {
                        v.haz.enabled = !roto;
                        v.haz.color = new Color(1f, 1f, 1f, (danado ? 0.35f + 0.35f * Mathf.PerlinNoise(t * 14f, 1f) : 0.6f + 0.1f * Mathf.Sin(t * 2f)));
                    }
                    break;
                case Clase.Parlante:
                    if (!roto && notas != null && t >= v.proximo)
                    {
                        v.proximo = t + Random.Range(0.6f, 1.1f);
                        ParticulaSimple.Crear(new[] { notas[Random.Range(0, notas.Length)] }, (Vector2)b.center + Vector2.up * 0.4f,
                            new Vector2(Random.Range(-0.3f, 0.3f), 0.7f), 1.4f, orden + 2, Color.white, 1f, 1f, 0f, raiz);
                    }
                    break;
                case Clase.Cafetera:
                    if (!roto && vapor != null && t >= v.proximo)
                    {
                        v.proximo = t + 0.4f;
                        ParticulaSimple.Crear(vapor, (Vector2)b.center + new Vector2(Random.Range(-0.15f, 0.15f), b.extents.y * 0.5f),
                            new Vector2(0f, 0.5f), 1.2f, orden + 2, new Color(1f, 1f, 1f, 0.5f), 0.35f, 0.9f, 0f, raiz);
                    }
                    break;
                case Clase.Frio:
                    if (!roto && vapor != null && t >= v.proximo)
                    {
                        v.proximo = t + 0.6f;
                        ParticulaSimple.Crear(vapor, new Vector2(Random.Range(b.min.x, b.max.x), b.min.y + 0.1f),
                            new Vector2(Random.Range(-0.2f, 0.2f), -0.2f), 1.4f, orden + 2, new Color(0.75f, 0.9f, 1f, 0.45f), 0.4f, 0.9f, 0f, raiz);
                    }
                    break;
                case Clase.Agua:
                    if (gota != null && t >= v.proximo)
                    {
                        v.proximo = t + (roto ? 0.15f : Random.Range(1.5f, 3f));
                        ParticulaSimple.Crear(new[] { gota }, (Vector2)b.center + new Vector2(Random.Range(-0.2f, 0.2f), 0.1f),
                            new Vector2(Random.Range(-0.3f, 0.3f), roto ? 1.5f : 0.3f), 0.6f, orden + 2, Color.white, 1f, 0.8f, 5f, raiz);
                    }
                    break;
                case Clase.Impresora:
                    if (!roto && papel != null && t >= v.proximo)
                    {
                        v.proximo = t + Random.Range(3f, 6f);
                        ParticulaSimple.Crear(new[] { papel }, (Vector2)b.center + Vector2.down * 0.2f, new Vector2(Random.Range(-0.4f, 0.4f), -0.8f),
                            1.2f, orden + 2, Color.white, 1f, 1f, 0f, raiz);
                    }
                    break;
                case Clase.Microscopio:
                    if (v.luz == null) v.luz = Luz(b.center, new Color(0.6f, 0.9f, 1f), 0.2f, 0.9f);
                    v.luz.intensity = roto ? 0f : (Mathf.Repeat(t + v.fase, 2f) < 0.2f ? 0.6f : 0.15f);
                    break;
            }
        }
    }

    private static bool CercaDelJugador(Vector2 p, float distancia)
    {
        Vector2 j;
        return CamaraPisoCenital.PosicionPropia(out j) && Vector2.Distance(j, p) < distancia;
    }

    /// <summary>Temblor fuerte: los estantes sueltan libros.</summary>
    private void AlTemblar(float fuerza, float duracion)
    {
        if (!listo || libros == null || fuerza < 0.1f) return;
        bool alguno = false;
        foreach (Vivo v in vivos)
        {
            if (v.o == null || v.clase != Clase.Libros || !CercaDeLaCamara(v.o.Area.center)) continue;
            alguno = true;
            Bounds b = v.o.Area;
            int n = Random.Range(2, 4);
            for (int i = 0; i < n; i++) StartCoroutine(CaeLibro((Vector2)b.center + new Vector2(Random.Range(-0.3f, 0.3f), 0.2f)));
        }
        if (alguno) GestorAudio.Reproducir(SndExtra.LibrosCaen, 0.7f);
    }

    private IEnumerator CaeLibro(Vector2 desde)
    {
        SpriteRenderer sr = Provisional.Crear(raiz, "LibroCaido", libros[Random.Range(0, libros.Length)], Color.white, Provisional.OrdenY(desde.y - 1f) + 3);
        Vector2 v = new Vector2(Random.Range(-1.6f, 1.6f), Random.Range(1.5f, 2.6f));
        float suelo = desde.y - Random.Range(0.6f, 1.2f);
        Vector2 p = desde;
        float giro = Random.Range(-500f, 500f);
        while (sr != null && (p.y > suelo || v.y > 0f))
        {
            v.y -= 9f * Time.deltaTime;
            p += v * Time.deltaTime;
            sr.transform.position = p;
            sr.transform.Rotate(0f, 0f, giro * Time.deltaTime);
            yield return null;
        }
        if (sr == null) yield break;
        sr.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-30f, 30f));
        yield return new WaitForSeconds(6f);
        float t = 0f;
        while (sr != null && t < 1f) { t += Time.deltaTime; sr.color = new Color(1f, 1f, 1f, 1f - t); yield return null; }
        if (sr != null) Destroy(sr.gameObject);
    }

    // ================================================================== reparar y romper
    private void AlCambiarObjeto(ObjetoReparable o)
    {
        if (o == null || !listo) return;
        EstadoObjeto antes;
        if (!estadoPrevio.TryGetValue(o, out antes)) antes = EstadoObjeto.Intacto;
        estadoPrevio[o] = o.Estado;
        if (o.Estado < antes) StartCoroutine(AnimarReparacion(o));
        else if (o.Estado > antes) EfectoRotura(o);
    }

    /// <summary>Las chispas vuelven al objeto, destello y el objeto da un saltito.</summary>
    private IEnumerator AnimarReparacion(ObjetoReparable o)
    {
        Bounds b = o.Area;
        Vector2 c = b.center;
        int orden = (o.Dibujo != null ? o.Dibujo.sortingOrder : 0) + 3;
        if (chispas != null)
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                Vector2 desde = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 1.1f;
                ParticulaSimple.Crear(chispas, desde, (c - desde) / 0.28f, 0.28f, orden, new Color(1f, 0.95f, 0.6f), 0.9f, 0.4f, 0f, raiz);
            }
        GestorAudio.Reproducir(SndExtra.Reparado, 0.45f, Random.Range(0.95f, 1.1f));
        yield return new WaitForSeconds(0.26f);
        if (o == null) yield break;
        SpriteRenderer destello = Provisional.Crear(raiz, "Destello", Provisional.Circulo, new Color(1f, 1f, 0.85f, 0.8f), orden);
        destello.transform.position = c;
        Transform d = o.Dibujo != null ? o.Dibujo.transform : null;
        Vector3 escala = d != null ? d.localScale : Vector3.one;
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float k = t / 0.3f;
            if (destello != null)
            {
                float e = Mathf.Max(b.size.x, b.size.y) * (0.6f + 1.2f * k);
                destello.transform.localScale = new Vector3(e, e, 1f);
                destello.color = new Color(1f, 1f, 0.85f, 0.8f * (1f - k));
            }
            if (d != null) d.localScale = escala * (1f + 0.14f * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }
        if (d != null) d.localScale = escala;
        if (destello != null) Destroy(destello.gameObject);
    }

    /// <summary>Cada cosa se rompe a su manera.</summary>
    private void EfectoRotura(ObjetoReparable o)
    {
        Bounds b = o.Area;
        Vector2 c = b.center;
        int orden = (o.Dibujo != null ? o.Dibujo.sortingOrder : 0) + 3;
        Clase clase = ClaseDe(o.Nombre);
        switch (clase)
        {
            case Clase.Computadora: case Clase.Pantalla: case Clase.Proyector: case Clase.Parlante: case Clase.Monitor: case Clase.Microscopio: case Clase.Impresora:
                if (chispas != null)
                    for (int i = 0; i < 9; i++)
                        ParticulaSimple.Crear(chispas, c, new Vector2(Random.Range(-2f, 2f), Random.Range(0.5f, 2.5f)), 0.45f, orden,
                            new Color(0.7f, 0.9f, 1f), 1f, 0.5f, 5f, raiz);
                break;
            case Clase.Agua: case Clase.Frio: case Clase.Cafetera:
                if (salpica != null) EfectoSprite.Reproducir(salpica, c, orden, 1.6f, 12f);
                if (gota != null)
                    for (int i = 0; i < 8; i++)
                        ParticulaSimple.Crear(new[] { gota }, c, new Vector2(Random.Range(-1.8f, 1.8f), Random.Range(1f, 2.5f)), 0.6f, orden,
                            Color.white, 1f, 0.7f, 6f, raiz);
                break;
            case Clase.Fuego:
                if (llamas != null)
                    for (int i = 0; i < 8; i++)
                        ParticulaSimple.Crear(llamas, c + Random.insideUnitCircle * 0.3f, new Vector2(Random.Range(-1f, 1f), Random.Range(1f, 2f)), 0.5f,
                            orden, Color.white, 1.3f, 0.3f, -1f, raiz);
                break;
            case Clase.Libros:
                for (int i = 0; i < 3 && libros != null; i++) StartCoroutine(CaeLibro(c + new Vector2(Random.Range(-0.3f, 0.3f), 0.2f)));
                break;
        }
    }

    // ================================================================== daño visible del edificio
    private void DanoDelEdificio()
    {
        Sprite[] g = ArteExtra.Serie("Dano/grieta_", 4);
        Sprite[] esc = ArteExtra.Serie("Dano/escombros_", 3);
        List<Vector2> celdas = new List<Vector2>(mapa.CentrosTransitables);
        for (int i = celdas.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); Vector2 t = celdas[i]; celdas[i] = celdas[j]; celdas[j] = t; }
        List<Vector2> usadas = new List<Vector2>();
        int nSuelo = 16;
        foreach (Vector2 c in celdas)
        {
            if (g == null || grietas.Count >= nSuelo) break;
            bool lejos = true;
            foreach (Vector2 u in usadas) if (Vector2.Distance(u, c) < 2.6f) { lejos = false; break; }
            if (!lejos) continue;
            usadas.Add(c);
            SpriteRenderer sr = Provisional.Crear(raiz, "Grieta", g[Random.Range(0, g.Length)], new Color(1f, 1f, 1f, 0f), -175);
            sr.transform.position = c + Random.insideUnitCircle * 0.25f;
            sr.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            float e = Random.Range(0.9f, 1.5f);
            sr.transform.localScale = new Vector3(e, e, 1f);
            grietas.Add(new Marca { sr = sr, umbral = 0.1f + 0.85f * grietas.Count / nSuelo, alfaMax = 0.85f });
        }
        // Grietas en las paredes.
        int nPared = 0;
        foreach (Vector2 c in CarasDePared(false))
        {
            if (g == null || nPared >= 8) break;
            SpriteRenderer sr = Provisional.Crear(raiz, "GrietaPared", g[Random.Range(0, g.Length)], new Color(1f, 1f, 1f, 0f), -138);
            sr.transform.position = new Vector2(c.x + Random.Range(-0.3f, 0.3f), c.y + 0.75f);
            sr.transform.localScale = new Vector3(0.8f, 1.1f, 1f);
            grietas.Add(new Marca { sr = sr, umbral = 0.2f + 0.7f * nPared / 8f, alfaMax = 0.9f });
            nPared++;
        }
        // Montones de escombros (desde el 75 %).
        for (int i = 0; i < 7 && esc != null && i < celdas.Count; i++)
        {
            Vector2 c = celdas[(i * 7 + 3) % celdas.Count];
            SpriteRenderer sr = Provisional.Crear(raiz, "Escombros", esc[i % esc.Length], new Color(1f, 1f, 1f, 0f), Provisional.OrdenY(c.y) - 2);
            sr.transform.position = c;
            escombros.Add(new Marca { sr = sr, umbral = 0.75f + 0.03f * i, alfaMax = 1f });
        }
    }

    private void ActualizarDano()
    {
        float destrozo = piso.Terminado ? 0f : piso.DestrozoSeccionActual;
        float k = 1f - Mathf.Exp(-3f * Time.deltaTime);
        foreach (Marca m in grietas)
        {
            if (m.sr == null) continue;
            float a = Mathf.Clamp01((destrozo - m.umbral) / 0.08f) * m.alfaMax;
            Color c = m.sr.color; c.a = Mathf.Lerp(c.a, a, k); m.sr.color = c;
        }
        foreach (Marca m in escombros)
        {
            if (m.sr == null) continue;
            float a = destrozo >= m.umbral ? m.alfaMax : 0f;
            Color c = m.sr.color; c.a = Mathf.Lerp(c.a, a, k); m.sr.color = c;
        }
        IluminacionNivel.Inestabilidad = destrozo;
        IluminacionNivel.AlarmaDerrumbe = destrozo >= 0.75f && !piso.Terminado;

        // Polvo que cae del techo, más seguido cuanto más dañado.
        if (destrozo > 0.3f && polvo != null && Time.time >= proximoPolvo)
        {
            proximoPolvo = Time.time + Mathf.Lerp(1.4f, 0.15f, (destrozo - 0.3f) / 0.7f);
            Camera cam = Camera.main;
            if (cam != null)
            {
                float alto = cam.orthographicSize, ancho = alto * cam.aspect;
                Vector3 c = cam.transform.position;
                ParticulaSimple.Crear(polvo, new Vector2(c.x + Random.Range(-ancho, ancho), c.y + Random.Range(-alto * 0.3f, alto)),
                    new Vector2(Random.Range(-0.1f, 0.1f), -0.3f), Random.Range(1f, 1.6f), 480, new Color(1f, 1f, 1f, 0.7f), 0.8f, 0.6f, 2f, raiz);
            }
        }
        // Desde el 90 % el edificio cruje y tiembla un poco cada tanto.
        if (destrozo >= 0.9f && Time.time >= proximoCrujido)
        {
            proximoCrujido = Time.time + Random.Range(2.5f, 4f);
            CamaraPisoCenital.Temblar(0.05f, 0.4f);
        }
    }

    // ================================================================== ciclo
    private void Update()
    {
        if (!listo) return;
        AnimarObjetos();
        ActualizarDano();
        // Cabinas del teleférico cruzando cada ventana (de izquierda a derecha, colgadas del cable).
        for (int i = 0; i < cabinas.Count; i++)
        {
            Transform t = cabinas[i];
            if (t == null) continue;
            float k = Mathf.Repeat(Time.time / 16f + fasesCabina[i], 1f);
            float x = Mathf.Lerp(-22f, 20f, k) / 32f;
            float y = (34f - Mathf.Lerp(11f, 17f, k)) / 32f - 0.12f;
            t.localPosition = new Vector3(x, y, 0f);
        }
    }

    private static bool CercaDeLaCamara(Vector2 p)
    {
        Camera cam = Camera.main;
        if (cam == null) return true;
        Vector2 c = cam.transform.position;
        return Mathf.Abs(p.x - c.x) < cam.orthographicSize * cam.aspect + 1f && Mathf.Abs(p.y - c.y) < cam.orthographicSize + 1f;
    }

    /// <summary>¿En ese punto hay pared (el collider del mapa) y no un objeto?</summary>
    private static bool EsPared(Vector2 p)
    {
        foreach (Collider2D c in Physics2D.OverlapPointAll(p))
        {
            if (c == null || c.isTrigger) continue;
            if (c.GetComponentInParent<ObjetoReparable>() != null || c.GetComponentInParent<JugadorCenital>() != null ||
                c.GetComponentInParent<DemoledorCenital>() != null || c.GetComponentInParent<EstudianteNPC>() != null ||
                c.GetComponentInParent<AscensorCenital>() != null || c.GetComponentInParent<VentanalCenital>() != null) continue;
            if (c is CompositeCollider2D || c.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null) return true;
        }
        return false;
    }
}
