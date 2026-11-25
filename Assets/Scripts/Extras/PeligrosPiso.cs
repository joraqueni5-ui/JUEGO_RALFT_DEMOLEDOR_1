using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Un peligro propio de cada piso, para que cada uno se juegue distinto:
///   Planta baja      charcos de limpieza (con cartel de "piso mojado"): resbalas y patinas
///   Piso 1           aceite de la cocina de Gastronomía: patinas todavía más
///   Piso 2           cables pelados de Informática: cada tanto dan un chispazo que te sacude
///   Piso 3           apagones: el piso queda a oscuras y solo alumbra tu personaje
///   Piso 4           goteras: caen gotas del techo y forman charcos
///   Piso 5           apagones y cables a la vez
///   Terraza          ráfagas de viento que te empujan
/// El dibujo se arma igual en los dos equipos (lugares fijos por piso); los efectos sobre los jugadores solo los aplica
/// quien simula (el anfitrión o el juego de un jugador). InstaladorExtras lo agrega a cada piso cenital.
/// </summary>
public class PeligrosPiso : MonoBehaviour
{
    private enum Tipo { Charcos, Aceite, Cables, Apagon, Goteras, ApagonYCables, Viento, Supervivencia }

    private class Charco { public SpriteRenderer sr; public Vector2 pos; public Vector2 radio; public Sprite[] cuadros; public float fase; public float escala = 1f; }
    private class Cable { public SpriteRenderer sr, chispa; public Light2D luz; public Vector2 pos; public float proximo; public int estado; public float finEstado; }
    private class Gotera { public SpriteRenderer sombra; public Vector2 pos; public float proxima; public Charco charco; }

    private PisoCenital piso;
    private MapaCenital mapa;
    private Tipo tipo;
    private Transform raiz;
    private readonly List<Charco> charcos = new List<Charco>();
    private readonly List<Cable> cables = new List<Cable>();
    private readonly List<Gotera> goteras = new List<Gotera>();
    private readonly Dictionary<JugadorCenital, float> esperaResbalon = new Dictionary<JugadorCenital, float>();
    private bool simula;
    private float inicio;
    private bool avisoInicial;

    // Apagón y viento.
    private float apagon;
    private bool enApagon;
    private Vector2 viento, vientoObjetivo;
    private float proximaHoja;

    private static bool Simula { get { return !RedJuego.EsInvitadoEnPartida; } }

    private IEnumerator Start()
    {
        yield return null;
        yield return null;
        yield return null;
        piso = PisoCenital.Actual;
        if (piso == null || piso.Mapa == null) { enabled = false; yield break; }
        mapa = piso.Mapa;
        int numero = piso.Definicion != null ? piso.Definicion.numero : 0;
        tipo = ModoSupervivencia.Activo || DemoledorUnifranz.Game.GameSession.Supervivencia ? Tipo.Supervivencia : TipoDe(numero);
        simula = Simula;
        inicio = Time.time;

        raiz = new GameObject("PeligrosPiso").transform;
        raiz.SetParent(mapa.transform, false);

        Random.State antes = Random.state;
        Random.InitState(9100 + numero * 17);
        List<Vector2> lugares = Lugares();
        switch (tipo)
        {
            case Tipo.Charcos: CrearCharcos(lugares, 5, "Peligros/charco_", true, 0.95f); break;
            case Tipo.Aceite: CrearCharcos(lugares, 5, "Peligros/aceite_", false, 1.4f); break;
            case Tipo.Cables: CrearCables(lugares, 5); break;
            case Tipo.Apagon: CrearCharcos(lugares, 2, "Peligros/charco_", true, 0.95f); break;
            case Tipo.Goteras: CrearGoteras(lugares, 5); break;
            case Tipo.ApagonYCables: CrearCables(lugares, 3); break;
            case Tipo.Supervivencia:
                // De todo un poco: charcos, cables y apagones.
                CrearCharcos(lugares, 3, "Peligros/charco_", true, 0.95f);
                CrearCables(lugares, 3);
                break;
        }
        Random.state = antes;
    }

    private static Tipo TipoDe(int numero)
    {
        switch (numero)
        {
            case 0: return Tipo.Charcos;
            case 1: return Tipo.Aceite;
            case 2: return Tipo.Cables;
            case 3: return Tipo.Apagon;
            case 4: return Tipo.Goteras;
            case 5: return Tipo.ApagonYCables;
            default: return Tipo.Viento;
        }
    }

    private string NombrePeligro()
    {
        switch (tipo)
        {
            case Tipo.Charcos: return "¡CUIDADO: PISO MOJADO! LOS CHARCOS RESBALAN";
            case Tipo.Aceite: return "¡ACEITE DE LA COCINA! PATINAS SI LO PISAS";
            case Tipo.Cables: return "¡CABLES PELADOS! ALÉJATE CUANDO CHISPEEN";
            case Tipo.Apagon: return "¡ESTE PISO TIENE APAGONES!";
            case Tipo.Goteras: return "¡GOTERAS! LAS GOTAS DEJAN CHARCOS";
            case Tipo.ApagonYCables: return "¡APAGONES Y CABLES PELADOS!";
            case Tipo.Supervivencia: return "¡CHARCOS, CABLES Y APAGONES! ¡AGUANTA!";
            default: return "¡VIENTO FUERTE EN LA TERRAZA!";
        }
    }

    /// <summary>Celdas libres lejos de la llegada del jugador, del ascensor, del ventanal y de los objetos.</summary>
    private List<Vector2> Lugares()
    {
        List<Vector2> r = new List<Vector2>();
        List<Vector2> evitar = new List<Vector2>();
        if (mapa.spawnJugador != null) evitar.Add(mapa.spawnJugador.position);
        if (mapa.ascensor != null) evitar.Add(mapa.ascensor.transform.position);
        if (mapa.ventanal != null) evitar.Add(mapa.ventanal.transform.position);
        if (mapa.entradaDemoledor != null) evitar.Add(mapa.entradaDemoledor.position);
        foreach (Vector2 c in mapa.CentrosTransitables)
        {
            bool ok = true;
            foreach (Vector2 e in evitar) if (Vector2.Distance(c, e) < 2.6f) { ok = false; break; }
            if (!ok) continue;
            foreach (ObjetoReparable o in mapa.objetos)
                if (o != null && Vector2.Distance(c, o.transform.position) < 1.3f) { ok = false; break; }
            // Con espacio alrededor (que no tape un pasillo de una baldosa).
            int libres = 0;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (mapa.EsTransitable(mapa.CeldaDe(c + new Vector2(dx, dy)))) libres++;
            if (ok && libres >= 8) r.Add(c);
        }
        for (int i = r.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); Vector2 t = r[i]; r[i] = r[j]; r[j] = t; }
        return r;
    }

    private static Vector2 Tomar(List<Vector2> lugares, List<Vector2> usados, float separacion)
    {
        foreach (Vector2 c in lugares)
        {
            bool lejos = true;
            foreach (Vector2 u in usados) if (Vector2.Distance(u, c) < separacion) { lejos = false; break; }
            if (!lejos) continue;
            usados.Add(c);
            return c;
        }
        return new Vector2(float.NaN, float.NaN);
    }

    // ------------------------------------------------------------ construcción
    private readonly List<Vector2> usados = new List<Vector2>();

    private void CrearCharcos(List<Vector2> lugares, int cantidad, string prefijo, bool conCartel, float escala)
    {
        Sprite[] cuadros = ArteExtra.Serie(prefijo, 3);
        if (cuadros == null) return;
        for (int i = 0; i < cantidad; i++)
        {
            Vector2 p = Tomar(lugares, usados, 4.5f);
            if (float.IsNaN(p.x)) break;
            Charco c = NuevoCharco(p, cuadros, escala);
            if (conCartel)
            {
                Sprite cartel = ArteExtra.Uno("Peligros/cartel_mojado");
                if (cartel != null)
                {
                    SpriteRenderer sr = Provisional.Crear(raiz, "CartelMojado", cartel, Color.white, Provisional.OrdenY(p.y + 0.45f));
                    sr.transform.position = p + new Vector2(-0.75f * escala, 0.45f);
                }
            }
            charcos.Add(c);
        }
    }

    private Charco NuevoCharco(Vector2 p, Sprite[] cuadros, float escala)
    {
        SpriteRenderer sr = Provisional.Crear(raiz, "Charco", cuadros[0], Color.white, -160);
        sr.transform.position = p;
        sr.transform.localScale = new Vector3(escala, escala, 1f);
        return new Charco { sr = sr, pos = p, cuadros = cuadros, radio = new Vector2(0.62f, 0.36f) * escala, fase = Random.value * 3f, escala = escala };
    }

    private void CrearCables(List<Vector2> lugares, int cantidad)
    {
        Sprite s0 = ArteExtra.Uno("Peligros/cable_0");
        if (s0 == null) return;
        for (int i = 0; i < cantidad; i++)
        {
            Vector2 p = Tomar(lugares, usados, 4f);
            if (float.IsNaN(p.x)) break;
            SpriteRenderer sr = Provisional.Crear(raiz, "Cable", s0, Color.white, -158);
            sr.transform.position = p;
            sr.flipX = Random.value < 0.5f;
            SpriteRenderer ch = Provisional.Crear(raiz, "Chispazo", ArteExtra.Uno("Peligros/chispazo_0"), new Color(1f, 1f, 1f, 0f), 600);
            Vector2 punta = p + new Vector2(sr.flipX ? -0.7f : 0.7f, 0f);
            ch.transform.position = punta;
            GameObject gl = new GameObject("LuzChispazo");
            gl.transform.SetParent(raiz, false);
            gl.transform.position = punta;
            Light2D luz = gl.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Point;
            luz.color = new Color(0.55f, 0.85f, 1f);
            luz.intensity = 0f;
            luz.pointLightOuterRadius = 2.6f;
            cables.Add(new Cable { sr = sr, chispa = ch, luz = luz, pos = punta, proximo = Time.time + 4f + i * 1.7f });
        }
    }

    private void CrearGoteras(List<Vector2> lugares, int cantidad)
    {
        Sprite[] agua = ArteExtra.Serie("Peligros/charco_", 3);
        for (int i = 0; i < cantidad; i++)
        {
            Vector2 p = Tomar(lugares, usados, 4f);
            if (float.IsNaN(p.x)) break;
            Charco c = agua != null ? NuevoCharco(p, agua, 0.35f) : null;
            if (c != null) charcos.Add(c);
            SpriteRenderer sombra = Provisional.Crear(raiz, "SombraGota", ArteExtra.Uno("Peligros/mancha_techo"), new Color(1f, 1f, 1f, 0f), -155);
            sombra.transform.position = p;
            goteras.Add(new Gotera { sombra = sombra, pos = p, proxima = Time.time + 3f + i * 1.3f, charco = c });
            // A veces alguien puso un balde... al lado.
            if (i % 2 == 0)
            {
                Sprite b = ArteExtra.Uno("Peligros/balde");
                if (b != null)
                {
                    SpriteRenderer sb = Provisional.Crear(raiz, "Balde", b, Color.white, Provisional.OrdenY(p.y - 0.2f));
                    sb.transform.position = p + new Vector2(0.9f, -0.2f);
                }
            }
        }
    }

    // ------------------------------------------------------------ ciclo
    private void Update()
    {
        if (piso == null) return;
        bool activo = !piso.Terminado && !piso.Rendido && Time.timeScale > 0f;

        if (!avisoInicial && Time.time - inicio > 7f)
        {
            avisoInicial = true;
            Vector2 donde;
            if (CamaraPisoCenital.PosicionPropia(out donde))
                TextoFlotante.Mostrar(donde + Vector2.up * 1.8f, NombrePeligro(), new Color(1f, 0.85f, 0.3f), 3.2f);
        }

        AnimarCharcos();
        if (cables.Count > 0) ActualizarCables(activo);
        if (goteras.Count > 0) ActualizarGoteras(activo);
        if (tipo == Tipo.Apagon || tipo == Tipo.ApagonYCables || tipo == Tipo.Supervivencia) ActualizarApagon(activo);
        if (tipo == Tipo.Viento) ActualizarViento(activo);
        if (simula && charcos.Count > 0) RevisarResbalones();
    }

    private void AnimarCharcos()
    {
        foreach (Charco c in charcos)
        {
            if (c == null || c.sr == null) continue;
            int k = (int)((Time.time + c.fase) * 2.5f) % c.cuadros.Length;
            c.sr.sprite = c.cuadros[k];
        }
    }

    private void RevisarResbalones()
    {
        foreach (JugadorCenital j in JugadorCenital.Todos)
        {
            if (j == null || !j.isActiveAndEnabled) continue;
            Vector2 pie = j.transform.position;
            foreach (Charco c in charcos)
            {
                if (c == null || c.radio.x <= 0.05f) continue;
                Vector2 d = pie - c.pos;
                float e = (d.x * d.x) / (c.radio.x * c.radio.x) + (d.y * d.y) / (c.radio.y * c.radio.y);
                if (e > 1f) continue;
                if (j.VelocidadActual.sqrMagnitude < 0.5f) break;   // parado en el charco no resbala
                float espera;
                if (esperaResbalon.TryGetValue(j, out espera) && Time.time < espera) break;
                esperaResbalon[j] = Time.time + 1.6f;
                j.Resbalar(tipo == Tipo.Aceite ? 1.3f : 0.9f);
                GestorAudio.Reproducir(SndExtra.Resbalon, 0.8f);
                TextoFlotante.Mostrar(j.Centro + Vector2.up * 1.2f, "¡RESBALÓN!", new Color(0.6f, 0.85f, 1f), 1f);
                if (!j.controlRemoto) Progreso.Sumar("resbalones");
                break;
            }
        }
    }

    private void ActualizarCables(bool activo)
    {
        Sprite s0 = ArteExtra.Uno("Peligros/cable_0"), s1 = ArteExtra.Uno("Peligros/cable_1");
        Sprite[] chispas = ArteExtra.Serie("Peligros/chispazo_", 3);
        foreach (Cable c in cables)
        {
            if (c.sr == null) continue;
            switch (c.estado)
            {
                case 0:   // tranquilo
                    c.sr.sprite = s0;
                    c.chispa.color = new Color(1f, 1f, 1f, 0f);
                    c.luz.intensity = Mathf.MoveTowards(c.luz.intensity, 0f, Time.deltaTime * 4f);
                    if (activo && Time.time >= c.proximo) { c.estado = 1; c.finEstado = Time.time + 1f; }
                    break;
                case 1:   // aviso: el cable brilla y salta alguna chispita
                    c.sr.sprite = Mathf.Repeat(Time.time * 10f, 1f) < 0.5f ? s1 : s0;
                    c.luz.intensity = 0.3f + 0.3f * Mathf.Abs(Mathf.Sin(Time.time * 30f));
                    if (Time.time >= c.finEstado)
                    {
                        c.estado = 2;
                        c.finEstado = Time.time + 0.7f;
                        if (simula && CercaDeLaCamara(c.pos)) GestorAudio.Reproducir(SndExtra.Chispazo, 0.9f, Random.Range(0.9f, 1.1f));
                    }
                    break;
                default:  // ¡chispazo!
                    c.sr.sprite = s1;
                    if (chispas != null) c.chispa.sprite = chispas[(int)(Time.time * 18f) % chispas.Length];
                    c.chispa.color = Color.white;
                    float esc = 1.2f + 0.3f * Mathf.Sin(Time.time * 40f);
                    c.chispa.transform.localScale = new Vector3(esc, esc, 1f);
                    c.luz.intensity = 1.4f + Random.value * 0.8f;
                    if (simula) Electrocutar(c.pos);
                    if (Time.time >= c.finEstado) { c.estado = 0; c.proximo = Time.time + Random.Range(4.5f, 7.5f); }
                    break;
            }
        }
    }

    private readonly Dictionary<JugadorCenital, float> esperaChispazo = new Dictionary<JugadorCenital, float>();

    private void Electrocutar(Vector2 punto)
    {
        foreach (JugadorCenital j in JugadorCenital.Todos)
        {
            if (j == null || !j.isActiveAndEnabled) continue;
            if (Vector2.Distance(j.Centro, punto) > 0.95f) continue;
            float espera;
            if (esperaChispazo.TryGetValue(j, out espera) && Time.time < espera) continue;
            esperaChispazo[j] = Time.time + 1.5f;
            j.Electrocutar(punto);
            TextoFlotante.Mostrar(j.Centro + Vector2.up * 1.2f, "¡BZZZT!", new Color(0.6f, 0.9f, 1f), 1f);
            if (!j.controlRemoto) Progreso.Sumar("chispazos");
        }
    }

    private void ActualizarGoteras(bool activo)
    {
        foreach (Gotera g in goteras)
        {
            if (g.sombra == null) continue;
            float falta = g.proxima - Time.time;
            // La sombra crece justo antes de que caiga la gota.
            float aviso = Mathf.Clamp01(1f - falta / 1.1f);
            g.sombra.color = new Color(1f, 1f, 1f, aviso * 0.9f);
            float e = 0.4f + 0.6f * aviso;
            g.sombra.transform.localScale = new Vector3(e, e, 1f);
            if (!activo || falta > 0f) continue;
            g.proxima = Time.time + Random.Range(3.2f, 5.5f);
            StartCoroutine(CaerGota(g));
        }
    }

    private IEnumerator CaerGota(Gotera g)
    {
        Sprite gota = ArteExtra.Uno("Peligros/gota");
        SpriteRenderer sr = Provisional.Crear(raiz, "Gota", gota, Color.white, 650);
        float t = 0f;
        const float dur = 0.35f;
        while (t < dur && sr != null)
        {
            t += Time.deltaTime;
            sr.transform.position = g.pos + new Vector2(0f, Mathf.Lerp(3.2f, 0.05f, t / dur));
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
        EfectoSprite.Reproducir(ArteExtra.Serie("Peligros/salpica_", 3), g.pos, -150, 1.2f, 14f);
        if (simula && CercaDeLaCamara(g.pos)) GestorAudio.Reproducir(SndExtra.GotaCae, 0.7f, Random.Range(0.85f, 1.2f));
        // El charco crece con cada gota (hasta un tope).
        if (g.charco != null && g.charco.escala < 1.15f)
        {
            g.charco.escala = Mathf.Min(1.15f, g.charco.escala + 0.12f);
            g.charco.sr.transform.localScale = new Vector3(g.charco.escala, g.charco.escala, 1f);
            g.charco.radio = new Vector2(0.62f, 0.36f) * g.charco.escala;
        }
        if (!simula) yield break;
        foreach (JugadorCenital j in JugadorCenital.Todos)
        {
            if (j == null || !j.isActiveAndEnabled || Vector2.Distance(j.transform.position, g.pos) > 0.5f) continue;
            TextoFlotante.Mostrar(j.Centro + Vector2.up * 1.2f, "¡PLIC!", new Color(0.6f, 0.85f, 1f), 0.9f);
            j.Resbalar(0.5f);
        }
    }

    // ------------------------------------------------------------ apagón
    private void ActualizarApagon(bool activo)
    {
        // Horario fijo desde que empezó el piso (así los dos equipos lo ven casi a la vez): cada 40 s, 8 s a oscuras.
        float t = Time.time - inicio - 28f;
        float ciclo = Mathf.Repeat(t, 40f);
        bool aviso = t > 0f && activo && ciclo < 1.2f;
        bool oscuro = t > 0f && activo && ciclo >= 1.2f && ciclo < 9.2f;
        float objetivo = oscuro ? 1f : 0f;
        if (aviso) objetivo = Mathf.PerlinNoise(Time.time * 18f, 3f) > 0.5f ? 0.85f : 0.1f;   // parpadeo antes de cortarse
        apagon = Mathf.MoveTowards(apagon, objetivo, Time.deltaTime * (aviso ? 12f : 2.5f));
        IluminacionNivel.Apagon = apagon;

        if (oscuro && !enApagon)
        {
            enApagon = true;
            // En el invitado el sonido y el aviso llegan del anfitrión.
            if (simula)
            {
                GestorAudio.Reproducir(SndExtra.Apagon);
                AvisoUlti.Mostrar("¡SE FUE LA LUZ!", 1.6f, false, new Color(0.7f, 0.8f, 1f));
            }
        }
        else if (!oscuro && enApagon)
        {
            enApagon = false;
            if (simula) GestorAudio.Reproducir(SndExtra.LuzVuelve);
        }
    }

    // ------------------------------------------------------------ viento
    private float proximaRafaga = -1f, finRafaga, inicioRafaga;
    private bool avisando;

    private void ActualizarViento(bool activo)
    {
        if (proximaRafaga < 0f) proximaRafaga = Time.time + 16f;
        if (activo && !avisando && Time.time >= proximaRafaga && Time.time > finRafaga)
        {
            avisando = true;
            inicioRafaga = Time.time + 1.3f;
            finRafaga = inicioRafaga + 3.2f;
            // Dirección fija según el número de ráfaga (igual en los dos equipos).
            int n = Mathf.FloorToInt((Time.time - inicio) / 15f);
            float a = (n * 137f) % 360f;
            vientoObjetivo = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad) * 0.6f).normalized * 1.9f;
            if (simula)
            {
                AvisoUlti.Mostrar(vientoObjetivo.x < 0 ? "¡RÁFAGA HACIA LA IZQUIERDA!" : "¡RÁFAGA HACIA LA DERECHA!", 1.3f, false, new Color(0.75f, 0.95f, 1f));
                GestorAudio.Reproducir(SndExtra.Rafaga);
            }
        }
        bool sopla = activo && Time.time >= inicioRafaga && Time.time < finRafaga;
        if (avisando && Time.time >= finRafaga) { avisando = false; proximaRafaga = Time.time + Random.Range(12f, 17f); }
        viento = Vector2.MoveTowards(viento, sopla ? vientoObjetivo : Vector2.zero, Time.deltaTime * 4f);

        if (simula)
            foreach (JugadorCenital j in JugadorCenital.Todos)
                if (j != null) j.empujeAmbiente = j.Aturdido ? Vector2.zero : viento;

        // Hojas y líneas de viento cruzando la pantalla.
        if ((sopla || (avisando && Time.time > inicioRafaga - 0.6f)) && Time.time >= proximaHoja)
        {
            proximaHoja = Time.time + 0.06f;
            Camera cam = Camera.main;
            if (cam != null)
            {
                float alto = cam.orthographicSize, ancho = alto * cam.aspect;
                Vector2 dir = vientoObjetivo.normalized;
                Vector2 c = cam.transform.position;
                Vector2 p = c - dir * (ancho + 0.5f) + new Vector2(-dir.y, dir.x) * Random.Range(-alto, alto);
                bool linea = Random.value < 0.4f;
                Sprite[] s = linea ? ArteExtra.Serie("Peligros/rafaga_", 2) : new[] { ArteExtra.Uno("Peligros/hoja_" + Random.Range(0, 3)) };
                if (s != null && s[0] != null)
                {
                    ParticulaSimple pt = ParticulaSimple.Crear(s, p, dir * Random.Range(9f, 13f) + Random.insideUnitCircle, 1.6f, 640,
                        new Color(1f, 1f, 1f, linea ? 0.55f : 1f), linea ? 1.3f : 1f, linea ? 1.3f : 1f, 0f, raiz);
                    if (pt != null && linea) pt.transform.rotation = Quaternion.FromToRotation(Vector3.right, dir);
                }
            }
        }
    }

    private static bool CercaDeLaCamara(Vector2 p)
    {
        Camera cam = Camera.main;
        if (cam == null) return true;
        Vector2 c = cam.transform.position;
        return Mathf.Abs(p.x - c.x) < cam.orthographicSize * cam.aspect + 1f && Mathf.Abs(p.y - c.y) < cam.orthographicSize + 1f;
    }

    private void OnDestroy()
    {
        IluminacionNivel.Apagon = 0f;
        foreach (JugadorCenital j in JugadorCenital.Todos) if (j != null) j.empujeAmbiente = Vector2.zero;
    }
}
