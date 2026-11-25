using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DemoledorUnifranz.Game;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// Progreso a largo plazo: logros, premios que se desbloquean con las estrellas (accesorios para la cabeza y martillos con
/// efectos) y récords de cada piso (mejor puntaje y mejor tiempo). Todo se guarda con PlayerPrefs.
/// Los contadores los alimentan PisoCenital, IraEstudiante, MiniVillano, los peligros y GestorJuego.
/// </summary>
public static class Progreso
{
    // ------------------------------------------------------------ premios
    public class Premio
    {
        public string id, nombre, sprite;
        public int estrellas;
        public bool porJefe;
        public string Requisito { get { return porJefe ? "VENCE AL JEFE FINAL" : (estrellas <= 0 ? "" : "CONSIGUE " + estrellas + " ESTRELLAS"); } }
    }

    public static readonly Premio[] Accesorios =
    {
        new Premio { id = "ninguno", nombre = "SIN ACCESORIO" },
        new Premio { id = "gorra", nombre = "GORRA ROJA", sprite = "Accesorios/gorra", estrellas = 2 },
        new Premio { id = "auriculares", nombre = "AURICULARES", sprite = "Accesorios/auriculares", estrellas = 4 },
        new Premio { id = "fiesta", nombre = "GORRO DE FIESTA", sprite = "Accesorios/fiesta", estrellas = 7 },
        new Premio { id = "sombrero", nombre = "SOMBRERO DE PAJA", sprite = "Accesorios/sombrero", estrellas = 10 },
        new Premio { id = "birrete", nombre = "BIRRETE", sprite = "Accesorios/birrete", estrellas = 13 },
        new Premio { id = "vikingo", nombre = "CASCO VIKINGO", sprite = "Accesorios/vikingo", estrellas = 16 },
        new Premio { id = "corona", nombre = "CORONA DORADA", sprite = "Accesorios/corona", porJefe = true },
    };

    public static readonly Premio[] Martillos =
    {
        new Premio { id = "clasico", nombre = "MARTILLO CLÁSICO" },
        new Premio { id = "dorado", nombre = "CHISPAS DORADAS", sprite = "Mejoras/chispa_0", estrellas = 3 },
        new Premio { id = "corazones", nombre = "CORAZONES", sprite = "Martillos/corazon", estrellas = 6 },
        new Premio { id = "estrellas", nombre = "LLUVIA DE ESTRELLAS", sprite = "Martillos/estrellita", estrellas = 9 },
        new Premio { id = "hielo", nombre = "MARTILLO DE HIELO", sprite = "Martillos/copo", estrellas = 12 },
        new Premio { id = "fuego", nombre = "MARTILLO DE FUEGO", sprite = "Martillos/llama_0", estrellas = 15 },
        new Premio { id = "arcoiris", nombre = "ARCOÍRIS", sprite = "Mejoras/chispa_1", estrellas = 21 },
    };

    public static bool Desbloqueado(Premio p)
    {
        if (p == null) return false;
        if (p.porJefe) return Contador("jefe") > 0;
        return GameSession.EstrellasTotales() >= p.estrellas;
    }

    public static int AccesorioElegido
    {
        get
        {
            int i = Mathf.Clamp(PlayerPrefs.GetInt("prog.accesorio", 0), 0, Accesorios.Length - 1);
            return Desbloqueado(Accesorios[i]) ? i : 0;
        }
        set { PlayerPrefs.SetInt("prog.accesorio", Mathf.Clamp(value, 0, Accesorios.Length - 1)); PlayerPrefs.Save(); }
    }

    public static int MartilloElegido
    {
        get
        {
            int i = Mathf.Clamp(PlayerPrefs.GetInt("prog.martillo", 0), 0, Martillos.Length - 1);
            return Desbloqueado(Martillos[i]) ? i : 0;
        }
        set { PlayerPrefs.SetInt("prog.martillo", Mathf.Clamp(value, 0, Martillos.Length - 1)); PlayerPrefs.Save(); }
    }

    // ------------------------------------------------------------ logros
    public class Logro
    {
        public string id, titulo, descripcion;
        public string contador;
        public int meta = 1;
    }

    public static readonly Logro[] Logros =
    {
        new Logro { id = "primer_piso", titulo = "PRIMER DÍA", descripcion = "Supera la planta baja.", contador = "pisos" },
        new Logro { id = "reparador", titulo = "REPARADOR NOVATO", descripcion = "Repara 50 objetos.", contador = "reparaciones", meta = 50 },
        new Logro { id = "manos_oro", titulo = "MANOS DE ORO", descripcion = "Repara 500 objetos.", contador = "reparaciones", meta = 500 },
        new Logro { id = "muro", titulo = "MURO HUMANO", descripcion = "Bloquea 25 golpes del demoledor.", contador = "bloqueos", meta = 25 },
        new Logro { id = "sin_rasguno", titulo = "¡SIN UN RASGUÑO!", descripcion = "Supera un piso sin perder vidas.", contador = "pisos_sin_dano" },
        new Logro { id = "furioso", titulo = "FURIOSO", descripcion = "Usa la furia 10 veces.", contador = "furias", meta = 10 },
        new Logro { id = "bichos", titulo = "CAZA BICHOS", descripcion = "Vence a 50 mini villanos.", contador = "minis", meta = 50 },
        new Logro { id = "estrella", titulo = "ESTRELLA FUGAZ", descripcion = "Consigue 3 estrellas en un piso.", contador = "pisos_3" },
        new Logro { id = "perfecto", titulo = "PERFECCIONISTA", descripcion = "Consigue las 21 estrellas.", contador = "estrellas", meta = 21 },
        new Logro { id = "lector", titulo = "RATÓN DE BIBLIOTECA", descripcion = "Lee 20 puntos \"?\".", contador = "info", meta = 20 },
        new Logro { id = "veloz", titulo = "A TODA PRISA", descripcion = "Supera un piso en menos de 2 minutos.", contador = "pisos_rapidos" },
        new Logro { id = "resbalon", titulo = "PIES MOJADOS", descripcion = "Resbala 10 veces en los charcos.", contador = "resbalones", meta = 10 },
        new Logro { id = "electrico", titulo = "ELECTRIZANTE", descripcion = "Recibe 5 chispazos de los cables.", contador = "chispazos", meta = 5 },
        new Logro { id = "jefe", titulo = "HÉROE DE UNIFRANZ", descripcion = "Vence al Demoledor Supremo.", contador = "jefe" },
        new Logro { id = "despierta", titulo = "¿FUE UN SUEÑO?", descripcion = "Mira el final de la historia.", contador = "final_visto" },
        new Logro { id = "equipo", titulo = "TRABAJO EN EQUIPO", descripcion = "Supera un piso en multijugador.", contador = "equipo" },
        new Logro { id = "coleccion", titulo = "COLECCIONISTA", descripcion = "Desbloquea todos los accesorios.", contador = "accesorios", meta = 7 },
        new Logro { id = "sobreviviente", titulo = "SOBREVIVIENTE", descripcion = "Supera 5 oleadas en SUPERVIVENCIA.", contador = "oleada_max", meta = 5 },
        new Logro { id = "leyenda", titulo = "LEYENDA DEL GRAN HALL", descripcion = "Supera 10 oleadas en SUPERVIVENCIA.", contador = "oleada_max", meta = 10 },
    };

    public static int Contador(string clave)
    {
        if (clave == "estrellas") return GameSession.EstrellasTotales();
        if (clave == "accesorios")
        {
            int n = 0;
            for (int i = 1; i < Accesorios.Length; i++) if (Desbloqueado(Accesorios[i])) n++;
            return n;
        }
        return PlayerPrefs.GetInt("prog." + clave, 0);
    }

    public static bool Obtenido(Logro l) { return PlayerPrefs.GetInt("logro." + l.id, 0) == 1; }

    public static int LogrosObtenidos()
    {
        int n = 0;
        foreach (Logro l in Logros) if (Obtenido(l)) n++;
        return n;
    }

    /// <summary>Suma a un contador y revisa los logros (los avisos salen al momento).</summary>
    public static void Sumar(string clave, int cantidad = 1)
    {
        if (RedJuego.EsInvitadoEnPartida && clave != "equipo") return;   // en el invitado cuenta el anfitrión
        PlayerPrefs.SetInt("prog." + clave, PlayerPrefs.GetInt("prog." + clave, 0) + cantidad);
        RevisarLogros();
    }

    private static void Marcar(string clave)
    {
        if (PlayerPrefs.GetInt("prog." + clave, 0) > 0) return;
        PlayerPrefs.SetInt("prog." + clave, 1);
    }

    public static void RevisarLogros()
    {
        bool cambio = false;
        foreach (Logro l in Logros)
        {
            if (Obtenido(l) || Contador(l.contador) < l.meta) continue;
            PlayerPrefs.SetInt("logro." + l.id, 1);
            cambio = true;
            AvisoLogro.Mostrar("¡LOGRO: " + l.titulo + "!", l.descripcion, null, new Color(1f, 0.8f, 0.2f), true);
        }
        if (cambio) PlayerPrefs.Save();
    }

    /// <summary>Avisa de los premios recién desbloqueados (una sola vez cada uno).</summary>
    public static void RevisarDesbloqueos()
    {
        bool cambio = false;
        foreach (Premio p in Accesorios) cambio |= AvisarPremio(p, "¡NUEVO ACCESORIO!");
        foreach (Premio p in Martillos) cambio |= AvisarPremio(p, "¡NUEVO MARTILLO!");
        if (cambio) PlayerPrefs.Save();
        RevisarLogros();
    }

    private static bool AvisarPremio(Premio p, string titulo)
    {
        if (string.IsNullOrEmpty(p.sprite) || !Desbloqueado(p)) return false;
        string clave = "prog.visto." + p.id;
        if (PlayerPrefs.GetInt(clave, 0) == 1) return false;
        PlayerPrefs.SetInt(clave, 1);
        AvisoLogro.Mostrar(titulo, p.nombre + "  ·  Equípalo en PREMIOS", ArteExtra.Uno(p.sprite), new Color(0.5f, 0.9f, 1f), false);
        return true;
    }

    // ------------------------------------------------------------ nivel en curso
    private static int golpesNivel;
    private static float inicioNivel;

    /// <summary>Lo llama InstaladorExtras al cargar cada escena.</summary>
    public static void EmpezarNivel()
    {
        golpesNivel = 0;
        inicioNivel = Time.time;
        PlayerPrefs.Save();   // guarda los contadores del nivel anterior
    }

    public static void GolpeRecibido() { golpesNivel++; }

    /// <summary>Segundos desde que empezó el nivel.</summary>
    public static float TiempoNivel { get { return Time.time - inicioNivel; } }

    /// <summary>Al superar un piso: logros, récords del piso y premios desbloqueados. Devuelve true si batió algún récord.</summary>
    public static bool PisoCompletado(int piso, int estrellas, int puntosDelPiso)
    {
        if (RedJuego.EsInvitadoEnPartida) { Sumar("equipo"); return false; }
        float tiempo = TiempoNivel;
        PlayerPrefs.SetInt("prog.pisos", PlayerPrefs.GetInt("prog.pisos", 0) + 1);
        if (golpesNivel == 0) Marcar("pisos_sin_dano");
        if (estrellas >= 3) Marcar("pisos_3");
        if (tiempo < 120f) Marcar("pisos_rapidos");
        if (RedJuego.EnPartida) Marcar("equipo");

        bool record = false;
        if (puntosDelPiso > MejorPuntaje(piso)) { PlayerPrefs.SetInt("rec.puntos." + piso, puntosDelPiso); record = true; }
        float mejor = MejorTiempo(piso);
        if (mejor <= 0f || tiempo < mejor) { PlayerPrefs.SetFloat("rec.tiempo." + piso, tiempo); record = mejor > 0f || record; }
        PlayerPrefs.Save();
        RevisarDesbloqueos();
        return record;
    }

    public static void JefeVencido()
    {
        Marcar("jefe");
        PlayerPrefs.Save();
        RevisarDesbloqueos();
    }

    public static int MejorPuntaje(int piso) { return PlayerPrefs.GetInt("rec.puntos." + piso, 0); }
    public static float MejorTiempo(int piso) { return PlayerPrefs.GetFloat("rec.tiempo." + piso, 0f); }

    public static string FormatoTiempo(float s)
    {
        if (s <= 0f) return "--:--";
        int t = Mathf.RoundToInt(s);
        return (t / 60) + ":" + (t % 60).ToString("00");
    }

    /// <summary>Borra logros, contadores, récords de piso y premios vistos (BORRAR PROGRESO).</summary>
    public static void Borrar()
    {
        string[] contadores = { "pisos", "reparaciones", "bloqueos", "pisos_sin_dano", "furias", "minis", "pisos_3", "info", "pisos_rapidos",
            "resbalones", "chispazos", "jefe", "final_visto", "equipo", "oleada_max" };
        foreach (string c in contadores) PlayerPrefs.DeleteKey("prog." + c);
        foreach (Logro l in Logros) PlayerPrefs.DeleteKey("logro." + l.id);
        foreach (Premio p in Accesorios) PlayerPrefs.DeleteKey("prog.visto." + p.id);
        foreach (Premio p in Martillos) PlayerPrefs.DeleteKey("prog.visto." + p.id);
        for (int i = 0; i < GameSession.TotalPisosEstrellas; i++) { PlayerPrefs.DeleteKey("rec.puntos." + i); PlayerPrefs.DeleteKey("rec.tiempo." + i); }
        PlayerPrefs.DeleteKey("prog.accesorio");
        PlayerPrefs.DeleteKey("prog.martillo");
        PlayerPrefs.Save();
    }
}

/// <summary>
/// Aviso que baja desde arriba a la derecha: logro conseguido o premio desbloqueado. Se encolan y se ven uno tras otro,
/// también en pausa y al cambiar de escena.
/// </summary>
public class AvisoLogro : MonoBehaviour
{
    private struct Pedido { public string titulo, texto; public Sprite icono; public Color color; public bool trofeo; }

    private static AvisoLogro inst;
    private readonly Queue<Pedido> cola = new Queue<Pedido>();
    private bool mostrando;
    private Canvas lienzo;

    public static void Mostrar(string titulo, string texto, Sprite icono, Color color, bool trofeo)
    {
        if (inst == null)
        {
            GameObject g = new GameObject("[AvisoLogro]");
            DontDestroyOnLoad(g);
            inst = g.AddComponent<AvisoLogro>();
        }
        inst.cola.Enqueue(new Pedido { titulo = titulo, texto = texto, icono = icono, color = color, trofeo = trofeo });
        if (!inst.mostrando) inst.StartCoroutine(inst.Rutina());
    }

    private IEnumerator Rutina()
    {
        mostrando = true;
        while (cola.Count > 0)
        {
            Pedido p = cola.Dequeue();
            yield return Uno(p);
        }
        mostrando = false;
    }

    private IEnumerator Uno(Pedido p)
    {
        if (lienzo == null)
        {
            lienzo = FabricaUIArcade.CrearCanvas("LienzoLogros", 950);
            lienzo.transform.SetParent(transform, false);
        }
        KitSpritesArcade kit = KitSpritesArcade.Actual();
        Image caja = FabricaUIArcade.CrearImagen(lienzo.transform, "Aviso", kit != null ? kit.panel : null, new Color(1f, 1f, 1f, 0.97f));
        RectTransform rt = caja.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(640f, 150f);

        Image franja = FabricaUIArcade.CrearImagen(caja.transform, "Franja", null, p.color);
        FabricaUIArcade.Colocar(franja.rectTransform, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(14f, 120f));
        franja.rectTransform.pivot = new Vector2(0f, 0.5f);

        Sprite icono = p.icono;
        if (icono == null && kit != null) icono = p.trofeo ? kit.iconoTrofeo : kit.iconoEstrella;
        Image ico = FabricaUIArcade.CrearImagen(caja.transform, "Icono", icono, icono != null ? Color.white : new Color(0, 0, 0, 0));
        ico.preserveAspect = true;
        FabricaUIArcade.Colocar(ico.rectTransform, new Vector2(0f, 0.5f), new Vector2(90f, 0f), new Vector2(96f, 96f));

        Text titulo = FabricaUIArcade.CrearTexto(caja.transform, "Titulo", p.titulo, 32, p.color, TextAnchor.UpperLeft, 3f, 3f);
        FabricaUIArcade.Colocar(titulo.rectTransform, new Vector2(0f, 1f), new Vector2(150f, -22f), new Vector2(470f, 44f));
        titulo.rectTransform.pivot = new Vector2(0f, 1f);
        titulo.horizontalOverflow = HorizontalWrapMode.Wrap;
        titulo.resizeTextForBestFit = true; titulo.resizeTextMinSize = 18; titulo.resizeTextMaxSize = 32;
        Text texto = FabricaUIArcade.CrearTexto(caja.transform, "Texto", p.texto, 24, PaletaArcade.Crema, TextAnchor.UpperLeft, 2f, 0f, true);
        FabricaUIArcade.Colocar(texto.rectTransform, new Vector2(0f, 1f), new Vector2(150f, -70f), new Vector2(470f, 70f));
        texto.rectTransform.pivot = new Vector2(0f, 1f);
        texto.horizontalOverflow = HorizontalWrapMode.Wrap;

        GestorAudio.Reproducir(p.trofeo ? SndExtra.Logro : SndExtra.Desbloqueo);

        // Entra desde la derecha, se queda y sale.
        float t = 0f;
        while (t < 3.6f && rt != null)
        {
            t += Time.unscaledDeltaTime;
            float entra = Mathf.Clamp01(t / 0.3f), sale = Mathf.Clamp01((t - 3.2f) / 0.4f);
            float e = 1f - Mathf.Pow(1f - entra, 3f);
            rt.anchoredPosition = new Vector2(Mathf.Lerp(700f, -24f, e) + sale * 720f, -24f);
            if (ico != null) ico.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 6f) * 8f * (1f - Mathf.Clamp01(t)));
            yield return null;
        }
        if (caja != null) Destroy(caja.gameObject);
    }
}

/// <summary>Efecto del martillo elegido en PREMIOS: partículas en cada martillazo del jugador propio.</summary>
public static class EfectoMartillo
{
    private static readonly Color[] Arcoiris =
    {
        new Color(1f, 0.3f, 0.3f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.95f, 0.3f), new Color(0.4f, 1f, 0.4f),
        new Color(0.35f, 0.75f, 1f), new Color(0.75f, 0.45f, 1f),
    };

    public static void Golpe(Vector2 punto, int orden)
    {
        int m = Progreso.MartilloElegido;
        if (m <= 0) return;
        switch (Progreso.Martillos[m].id)
        {
            case "dorado":
                Estallido(ArteExtra.Serie("Mejoras/chispa_", 3), punto, 9, 2.6f, orden, new Color(1f, 0.85f, 0.25f), 4f);
                break;
            case "corazones":
                for (int i = 0; i < 4; i++)
                    ParticulaSimple.Crear(Uno("Martillos/corazon"), punto + Random.insideUnitCircle * 0.25f,
                        new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(1f, 1.8f)), Random.Range(0.7f, 1f), orden, Color.white, 0.9f, 1.3f, 0f);
                break;
            case "estrellas":
                Estallido(Uno("Martillos/estrellita"), punto, 7, 2.4f, orden, Color.white, 3f);
                break;
            case "hielo":
                Estallido(Uno("Martillos/copo"), punto, 8, 1.8f, orden, new Color(0.85f, 0.95f, 1f), 0.5f);
                break;
            case "fuego":
                for (int i = 0; i < 6; i++)
                    ParticulaSimple.Crear(ArteExtra.Serie("Martillos/llama_", 3), punto + new Vector2(Random.Range(-0.35f, 0.35f), 0f),
                        new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(1.2f, 2f)), Random.Range(0.4f, 0.6f), orden, Color.white, 1.1f, 0.4f, -1f);
                break;
            case "arcoiris":
                Sprite[] ch = ArteExtra.Serie("Mejoras/chispa_", 3);
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI * 2f / 12f;
                    ParticulaSimple.Crear(ch, punto, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 2.4f, 0.55f, orden, Arcoiris[i % Arcoiris.Length], 1.1f, 0.6f, 2f);
                }
                break;
        }
    }

    private static Sprite[] Uno(string ruta)
    {
        Sprite s = ArteExtra.Uno(ruta);
        return s != null ? new[] { s } : null;
    }

    private static void Estallido(Sprite[] cuadros, Vector2 punto, int n, float fuerza, int orden, Color color, float gravedad)
    {
        if (cuadros == null) return;
        for (int i = 0; i < n; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.7f + 0.6f) * Random.Range(0.6f, 1f) * fuerza;
            ParticulaSimple.Crear(cuadros, punto, v, Random.Range(0.45f, 0.7f), orden, color, 1f, 0.6f, gravedad);
        }
    }
}

/// <summary>
/// Dibuja el accesorio elegido en PREMIOS sobre la cabeza del personaje (sigue su animación: respira, rebota, se aplasta).
/// Solo se agrega al jugador propio.
/// </summary>
public class AccesorioJugador : MonoBehaviour
{
    private SpriteRenderer cuerpo, gorro;
    private float hundir = 0.16f;

    /// <summary>Altura de los cuadros de los personajes (78 px a 100 px por unidad).</summary>
    private const float AlturaPersonaje = 0.78f;

    public static void Poner(GameObject quien)
    {
        if (quien == null || Progreso.AccesorioElegido <= 0) return;
        if (quien.GetComponent<AccesorioJugador>() == null) quien.AddComponent<AccesorioJugador>();
    }

    private void Start()
    {
        Progreso.Premio p = Progreso.Accesorios[Progreso.AccesorioElegido];
        Sprite s = ArteExtra.Uno(p.sprite);
        cuerpo = GetComponentInChildren<SpriteRenderer>();
        if (s == null || cuerpo == null) { enabled = false; return; }
        switch (p.id)
        {
            case "auriculares": hundir = 0.3f; break;
            case "fiesta": case "corona": hundir = 0.1f; break;
            case "vikingo": hundir = 0.2f; break;
            case "birrete": hundir = 0.12f; break;
        }
        GameObject g = new GameObject("Accesorio");
        g.transform.SetParent(cuerpo.transform, false);
        gorro = g.AddComponent<SpriteRenderer>();
        gorro.sprite = s;
    }

    private void LateUpdate()
    {
        if (gorro == null || cuerpo == null) return;
        Sprite s = cuerpo.sprite;
        bool ver = cuerpo.enabled && s != null && cuerpo.gameObject.activeInHierarchy;
        gorro.enabled = ver;
        if (!ver) return;
        Bounds b = s.bounds;
        float e = b.size.y / AlturaPersonaje;
        gorro.transform.localScale = new Vector3(e, e, 1f);
        gorro.transform.localPosition = new Vector3(b.center.x, b.max.y - hundir * b.size.y, 0f);
        gorro.flipX = cuerpo.flipX;
        gorro.sortingLayerID = cuerpo.sortingLayerID;
        gorro.sortingOrder = cuerpo.sortingOrder + 1;
        gorro.color = cuerpo.color;
    }
}
