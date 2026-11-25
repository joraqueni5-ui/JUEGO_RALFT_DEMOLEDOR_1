using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using DemoledorUnifranz.Game;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// Gestor de audio único (sobrevive entre escenas y se crea solo, así funciona al abrir cualquier escena desde el editor).
/// - Efectos con un grupo de AudioSources reutilizables; el mismo sonido no suena más de 'maximoSimultaneos' veces a la vez ni dos
///   veces en menos de 'separacionMinima' s; variación leve de tono en los sonidos repetitivos.
/// - Música con fundido entre pistas; la pista depende de la escena y cambia a la de tensión en furia y en la ulti.
/// - En pausa la música baja y los efectos del juego se pausan (los de interfaz no).
/// - Volúmenes de Música y Efectos de ConfiguracionAudio (menú de Opciones).
/// - Sonido de ambiente en bucle según la escena (universidad, terraza, fachada, menú), con fundido.
/// Los sonidos y sus volúmenes están en la BibliotecaSonidos; los que no estén ahí se buscan en Resources/SonidosExtra (SndExtra).
/// </summary>
public class GestorAudio : MonoBehaviour
{
    private class Voz
    {
        public AudioSource fuente;
        public BusAudio bus;
        public string nombre;
        public float volumen;
        public bool pausada;
    }

    private class Pista
    {
        public AudioSource fuente;
        public string nombre;
        public float nivel;       // 0 a 1 del fundido
        public float objetivo;
        public float volumenPropio = 1f;
    }

    private static GestorAudio instancia;
    private static bool cerrando;

    public static bool Existe { get { return instancia != null; } }

    public static GestorAudio Instancia
    {
        get
        {
            if (instancia == null && !cerrando)
            {
                GameObject g = new GameObject("[GestorAudio]");
                DontDestroyOnLoad(g);
                instancia = g.AddComponent<GestorAudio>();
            }
            return instancia;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar() { instancia = null; cerrando = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CrearAlIniciar() { GestorAudio g = Instancia; }

    private void OnApplicationQuit() { cerrando = true; }

    private BibliotecaSonidos biblioteca;
    private AudioMixerGroup grupoMusica, grupoEfectos, grupoInterfaz;
    private readonly List<Voz> voces = new List<Voz>();
    private readonly Dictionary<string, AudioSource> bucles = new Dictionary<string, AudioSource>();
    private readonly Dictionary<string, float> ultimaVez = new Dictionary<string, float>();
    private readonly Pista[] pistas = new Pista[2];
    private int pistaActiva;
    private string pistaPedida = "";
    private bool jingleSonando;
    private bool pausado;
    private float factorPausa = 1f;
    private string musicaDeEscena = "";
    private bool escenaDeNivel, escenaNivelFinal;
    private readonly HashSet<string> tension = new HashSet<string>();
    private float ultimaNavegacion;
    // Música propia de cada piso (Resources/MusicaExtra), música forzada (jefe final, historia) y aceleración (poca vida).
    private readonly Dictionary<string, EntradaSonido> musicasExtra = new Dictionary<string, EntradaSonido>();
    private string musicaForzada = "";
    private bool acelerada;
    private float tonoMusica = 1f;

    // Sonidos extra (Resources/SonidosExtra) y ambiente.
    private readonly Dictionary<string, EntradaSonido> extras = new Dictionary<string, EntradaSonido>();
    private AudioSource fuenteAmbiente;
    private string ambienteActual = "", ambientePedido = "";
    private float nivelAmbiente, volumenAmbiente = 1f;
    private AudioSource fuenteAmenaza;
    private float amenazaPedida, nivelAmenaza, volumenAmenaza = 1f, ultimaAmenaza = -10f;

    // ------------------------------------------------------------ inicio
    private void Awake()
    {
        if (instancia != null && instancia != this) { Destroy(gameObject); return; }
        instancia = this;

        biblioteca = Resources.Load<BibliotecaSonidos>(BibliotecaSonidos.NombreRecurso);
        if (biblioteca == null)
            Debug.LogWarning("[Audio] No hay BibliotecaSonidos en Resources: ejecuta 'Demoledor Unifranz / Audio / Armar la biblioteca de sonidos'.");

        if (biblioteca != null && biblioteca.mezcla != null)
        {
            grupoMusica = Grupo("Musica");
            grupoEfectos = Grupo("Efectos");
            grupoInterfaz = Grupo("Interfaz");
        }

        int n = biblioteca != null ? biblioteca.fuentesDeEfectos : 20;
        for (int i = 0; i < n; i++) voces.Add(new Voz { fuente = NuevaFuente("Efecto" + i, false) });

        for (int i = 0; i < 2; i++)
        {
            AudioSource f = NuevaFuente("Musica" + i, true);
            f.outputAudioMixerGroup = grupoMusica;
            pistas[i] = new Pista { fuente = f };
        }

        fuenteAmbiente = NuevaFuente("Ambiente", true);
        fuenteAmbiente.outputAudioMixerGroup = grupoEfectos;
        fuenteAmbiente.volume = 0f;

        // Capa de amenaza: suena junto con la música y sube cuando el demoledor se acerca.
        fuenteAmenaza = NuevaFuente("CapaAmenaza", true);
        fuenteAmenaza.outputAudioMixerGroup = grupoMusica;
        fuenteAmenaza.volume = 0f;

        SceneManager.sceneLoaded += AlCargarEscena;
        AlCargarEscena(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        if (instancia == this) instancia = null;
    }

    private AudioMixerGroup Grupo(string nombre)
    {
        AudioMixerGroup[] g = biblioteca.mezcla.FindMatchingGroups(nombre);
        return g != null && g.Length > 0 ? g[0] : null;
    }

    private AudioSource NuevaFuente(string nombre, bool musica)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(transform, false);
        AudioSource f = g.AddComponent<AudioSource>();
        f.playOnAwake = false;
        f.spatialBlend = 0f;
        f.loop = musica;
        return f;
    }

    // ------------------------------------------------------------ volúmenes
    private static float VolMusica { get { return ConfiguracionAudio.Volumen; } }
    private static float VolEfectos { get { return ConfiguracionAudio.VolumenEfectos; } }

    private float VolumenDeBus(BusAudio bus) { return bus == BusAudio.Musica ? VolMusica : VolEfectos; }

    // ------------------------------------------------------------ efectos
    /// <summary>Reproduce un sonido de la biblioteca una vez. 'volumenExtra' y 'tono' lo ajustan en esa llamada.</summary>
    public static void Reproducir(string nombre, float volumenExtra = 1f, float tono = 1f)
    {
        if (SilencioLocal) return;
        if (AlReproducir != null) AlReproducir(nombre, volumenExtra, tono);
        GestorAudio g = Instancia;
        if (g != null) g.Sonar(nombre, volumenExtra, tono);
    }

    /// <summary>Reproduce un clip suelto (por ejemplo una voz grabada de Resources/Voces) por el grupo de efectos.</summary>
    public static void ReproducirClip(string clave, AudioClip clip, float volumen = 1f)
    {
        if (SilencioLocal) return;
        if (AlReproducirClip != null && clip != null) AlReproducirClip(clave, clip, volumen);
        GestorAudio g = Instancia;
        if (g == null || clip == null || string.IsNullOrEmpty(clave)) return;
        EntradaSonido e;
        if (!g.extras.TryGetValue(clave, out e) || e == null)
            g.extras[clave] = new EntradaSonido { nombre = clave, clip = clip, volumen = volumen, bus = BusAudio.Efectos, maximoSimultaneos = 1 };
        g.Sonar(clave, 1f, 1f);
    }

    /// <summary>El sonido de la biblioteca o, si no está, el de Resources/SonidosExtra (null si no existe).</summary>
    private EntradaSonido BuscarEntrada(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return null;
        EntradaSonido e = biblioteca != null ? biblioteca.Buscar(nombre) : null;
        if (e != null && e.clip != null) return e;

        EntradaSonido extra;
        if (extras.TryGetValue(nombre, out extra)) return extra;
        AudioClip clip = Resources.Load<AudioClip>(SndExtra.Carpeta + nombre);
        extra = clip == null ? null : new EntradaSonido
        {
            nombre = nombre,
            clip = clip,
            volumen = SndExtra.Volumen(nombre),
            variarTono = SndExtra.VariarTono(nombre),
            bus = BusAudio.Efectos,
            maximoSimultaneos = 2
        };
        extras[nombre] = extra;
        return extra;
    }

    private void Sonar(string nombre, float volumenExtra, float tono)
    {
        EntradaSonido e = BuscarEntrada(nombre);
        if (e == null || e.clip == null) return;

        // Límite de repeticiones: ni dos veces en menos de 0,05 s ni más de 'maximoSimultaneos' a la vez.
        float ahora = Time.unscaledTime;
        float ultimo;
        float separacion = biblioteca != null ? biblioteca.separacionMinima : 0.05f;
        if (ultimaVez.TryGetValue(nombre, out ultimo) && ahora - ultimo < separacion) return;
        int activas = 0;
        foreach (Voz v in voces) if (v.nombre == nombre && (v.fuente.isPlaying || v.pausada)) activas++;
        if (activas >= e.maximoSimultaneos) return;

        Voz libre = null;
        foreach (Voz v in voces) if (!v.fuente.isPlaying && !v.pausada) { libre = v; break; }
        if (libre == null) return;

        ultimaVez[nombre] = ahora;
        libre.nombre = nombre;
        libre.bus = e.bus;
        libre.volumen = e.volumen * volumenExtra;
        libre.fuente.outputAudioMixerGroup = e.bus == BusAudio.Interfaz ? grupoInterfaz : grupoEfectos;
        libre.fuente.clip = e.clip;
        libre.fuente.loop = false;
        libre.fuente.volume = libre.volumen * VolEfectos;
        float variacion = biblioteca != null ? biblioteca.variacionTono : 0.06f;
        libre.fuente.pitch = tono * (e.variarTono ? 1f + Random.Range(-variacion, variacion) : 1f);
        libre.pausada = false;
        libre.fuente.Play();
    }

    /// <summary>Enciende o apaga un sonido en bucle (aturdido, poder activo). Se puede llamar en cada cuadro: no reinicia el sonido.</summary>
    public static void Bucle(string clave, string nombre, bool activo, float volumenExtra = 1f)
    {
        if (AlBucle != null) AlBucle(clave, nombre, activo, volumenExtra);
        GestorAudio g = activo ? Instancia : instancia;
        if (g != null) g.PonerBucle(clave, nombre, activo, volumenExtra);
    }

    private void PonerBucle(string clave, string nombre, bool activo, float volumenExtra)
    {
        AudioSource f;
        bucles.TryGetValue(clave, out f);
        if (!activo)
        {
            if (f != null && f.isPlaying) f.Stop();
            return;
        }
        EntradaSonido e = BuscarEntrada(nombre);
        if (e == null || e.clip == null) return;

        if (f == null) { f = NuevaFuente("Bucle_" + clave, false); bucles[clave] = f; }
        f.outputAudioMixerGroup = grupoEfectos;
        f.loop = true;
        f.volume = e.volumen * volumenExtra * VolEfectos;
        if (f.clip != e.clip) { f.clip = e.clip; f.Stop(); }
        if (!f.isPlaying && !pausado) f.Play();
    }

    // ------------------------------------------------------------ música
    /// <summary>Cambia a esta pista con fundido (en bucle).</summary>
    public static void Musica(string nombre)
    {
        GestorAudio g = Instancia;
        if (g != null) g.CambiarPista(nombre, true, g.biblioteca != null ? g.biblioteca.fundidoMusica : 0.8f);
    }

    /// <summary>Detiene la música (fundido corto) y toca un jingle una sola vez.</summary>
    public static void Jingle(string nombre)
    {
        GestorAudio g = Instancia;
        if (g == null) return;
        g.jingleSonando = true;
        g.CambiarPista(nombre, false, 0.25f);
    }

    public static void DetenerMusica()
    {
        GestorAudio g = instancia;
        if (g != null) g.CambiarPista("", false, g.biblioteca != null ? g.biblioteca.fundidoMusica : 0.8f);
    }

    private float duracionFundido = 0.8f;

    private void CambiarPista(string nombre, bool bucle, float fundido)
    {
        if (biblioteca == null) return;
        if (nombre == pistaPedida && (bucle || nombre == "")) return;
        pistaPedida = nombre;
        duracionFundido = Mathf.Max(0.05f, fundido);

        // La pista que sonaba se apaga y entra la nueva en la otra fuente.
        Pista saliente = pistas[pistaActiva];
        saliente.objetivo = 0f;

        EntradaSonido e = BuscarMusica(nombre);
        if (e == null || e.clip == null) return;

        pistaActiva = 1 - pistaActiva;
        Pista nueva = pistas[pistaActiva];
        nueva.fuente.Stop();
        nueva.fuente.clip = e.clip;
        nueva.fuente.loop = bucle;
        nueva.nombre = nombre;
        nueva.volumenPropio = e.volumen;
        nueva.nivel = 0f;
        nueva.objetivo = 1f;
        nueva.fuente.volume = 0f;
        nueva.fuente.Play();
    }

    /// <summary>La pista de la biblioteca o, si no está, la de Resources/MusicaExtra (los temas de cada piso, el jefe y la historia).</summary>
    private EntradaSonido BuscarMusica(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return null;
        EntradaSonido e = biblioteca != null ? biblioteca.Buscar(nombre) : null;
        if (e != null && e.clip != null) return e;
        EntradaSonido extra;
        if (musicasExtra.TryGetValue(nombre, out extra)) return extra;
        AudioClip clip = Resources.Load<AudioClip>("MusicaExtra/" + nombre);
        extra = clip == null ? null : new EntradaSonido { nombre = nombre, clip = clip, volumen = 0.62f, bus = BusAudio.Musica, maximoSimultaneos = 1 };
        musicasExtra[nombre] = extra;
        return extra;
    }

    /// <summary>True si existe la pista (en la biblioteca o en Resources/MusicaExtra).</summary>
    public static bool ExisteMusica(string nombre)
    {
        GestorAudio g = Instancia;
        return g != null && g.BuscarMusica(nombre) != null;
    }

    /// <summary>
    /// Música que manda sobre la del piso y la de tensión (el jefe final, la historia). Con "" o null vuelve la normal.
    /// </summary>
    public static void ForzarMusica(string nombre)
    {
        GestorAudio g = Instancia;
        if (g == null) return;
        g.musicaForzada = nombre ?? "";
        g.jingleSonando = false;
        g.ActualizarMusicaDeEscena();
    }

    /// <summary>La música va un poco más rápida y aguda (te queda una sola vida).</summary>
    public static void Acelerar(bool activa)
    {
        GestorAudio g = Instancia;
        if (g != null) g.acelerada = activa;
    }

    // ------------------------------------------------------------ música por escena y tensión
    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        tension.Clear();
        jingleSonando = false;
        Pausar(false);
        foreach (KeyValuePair<string, AudioSource> par in bucles) if (par.Value != null) par.Value.Stop();

        string n = escena.name;
        escenaNivelFinal = n == Escenas.NivelFinal;
        escenaDeNivel = escenaNivelFinal || n.StartsWith("Piso");

        musicaForzada = "";
        acelerada = false;
        if (escenaNivelFinal) musicaDeEscena = Snd.MusicaNivelFinal;
        else if (escenaDeNivel)
        {
            // Cada piso tiene su tema (musica_piso_N); si falta, la música general de los pisos.
            musicaDeEscena = Snd.MusicaPisos;
            if (n.Length > 4 && char.IsDigit(n[4]) && BuscarMusica("musica_piso_" + n[4]) != null) musicaDeEscena = "musica_piso_" + n[4];
        }
        else if (n == Escenas.FinJuego && !GameSession.Victoria && !GameSession.FlujoFinal) musicaDeEscena = "";   // la derrota suena con su jingle
        else musicaDeEscena = Snd.MusicaMenu;

        // Ambiente: se siente el lugar (gente y aire acondicionado en los pisos, viento en la terraza y la fachada).
        if (escenaNivelFinal) PonerAmbiente(SndExtra.AmbienteFachada);
        else if (n.StartsWith("Piso6")) PonerAmbiente(SndExtra.AmbienteTerraza);
        else if (escenaDeNivel) PonerAmbiente(SndExtra.AmbienteUniversidad);
        else if (n == Escenas.MenuPrincipal) PonerAmbiente(SndExtra.AmbienteMenu);
        else PonerAmbiente("");

        if (n == Escenas.FinJuego && !GameSession.Victoria && !GameSession.FlujoFinal)
        {
            Jingle(Snd.JingleDerrota);
            return;
        }
        ActualizarMusicaDeEscena();
    }

    /// <summary>
    /// Música adaptativa: 0 = calma, 1 = el demoledor encima. Se llama en cada cuadro desde el demoledor; la capa de latido y
    /// bombo sube y baja con fundido, y si nadie la pide durante medio segundo se apaga sola.
    /// </summary>
    /// <summary>Nivel de amenaza pedido hace menos de medio segundo (para el multijugador).</summary>
    public static float AmenazaActual
    {
        get { return instancia != null && Time.unscaledTime - instancia.ultimaAmenaza < 0.5f ? instancia.amenazaPedida : 0f; }
    }

    /// <summary>Hay algún motivo de música de tensión (para el multijugador).</summary>
    public static bool HayTension { get { return instancia != null && instancia.tension.Count > 0; } }

    // ------------------------------------------------------------ multijugador
    /// <summary>El anfitrión escucha cada sonido para mandárselo al invitado.</summary>
    public static System.Action<string, float, float> AlReproducir;
    public static System.Action<string, AudioClip, float> AlReproducirClip;
    public static System.Action<string, string, bool, float> AlBucle;
    /// <summary>Mientras es true no suena nada (el invitado aplica cambios que ya sonaron en el anfitrión).</summary>
    public static bool SilencioLocal;

    public static void Amenaza(float nivel)
    {
        GestorAudio g = nivel > 0f ? Instancia : instancia;
        if (g == null) return;
        g.amenazaPedida = Mathf.Clamp01(nivel);
        g.ultimaAmenaza = Time.unscaledTime;
    }

    private void ActualizarAmenaza(float dt)
    {
        if (fuenteAmenaza == null) return;
        float objetivo = Time.unscaledTime - ultimaAmenaza < 0.5f && !jingleSonando ? amenazaPedida : 0f;
        nivelAmenaza = Mathf.MoveTowards(nivelAmenaza, objetivo, dt * (objetivo > nivelAmenaza ? 1.2f : 0.5f));
        if (nivelAmenaza > 0.001f && !fuenteAmenaza.isPlaying)
        {
            EntradaSonido e = BuscarEntrada(SndExtra.CapaAmenaza);
            if (e == null || e.clip == null) return;
            fuenteAmenaza.clip = e.clip;
            volumenAmenaza = e.volumen;
            fuenteAmenaza.loop = true;
            fuenteAmenaza.Play();
        }
        else if (nivelAmenaza <= 0.001f && fuenteAmenaza.isPlaying) fuenteAmenaza.Stop();
        fuenteAmenaza.volume = nivelAmenaza * volumenAmenaza * VolMusica * factorPausa;
    }

    /// <summary>Cambia el sonido de ambiente en bucle (con fundido). "" lo apaga.</summary>
    public static void Ambiente(string nombre)
    {
        GestorAudio g = Instancia;
        if (g != null) g.PonerAmbiente(nombre);
    }

    private void PonerAmbiente(string nombre)
    {
        ambientePedido = nombre ?? "";
    }

    /// <summary>Fundido del ambiente: baja el que suena y, cuando se apagó, entra el nuevo.</summary>
    private void ActualizarAmbiente(float dt)
    {
        if (fuenteAmbiente == null) return;
        if (ambientePedido != ambienteActual)
        {
            nivelAmbiente = Mathf.MoveTowards(nivelAmbiente, 0f, dt / 0.6f);
            if (nivelAmbiente <= 0f)
            {
                fuenteAmbiente.Stop();
                ambienteActual = ambientePedido;
                EntradaSonido e = BuscarEntrada(ambienteActual);
                if (e != null && e.clip != null)
                {
                    fuenteAmbiente.clip = e.clip;
                    volumenAmbiente = e.volumen;
                    fuenteAmbiente.loop = true;
                    // Empieza en un punto al azar para que no suene igual cada vez.
                    fuenteAmbiente.time = Random.Range(0f, Mathf.Max(0f, e.clip.length - 1f));
                    fuenteAmbiente.Play();
                }
            }
        }
        else if (fuenteAmbiente.isPlaying) nivelAmbiente = Mathf.MoveTowards(nivelAmbiente, 1f, dt / 1.5f);

        float pausa = pausado ? 0.35f : 1f;
        fuenteAmbiente.volume = nivelAmbiente * volumenAmbiente * VolEfectos * pausa;
    }

    private void ActualizarMusicaDeEscena()
    {
        if (jingleSonando) return;
        string pista = musicaDeEscena;
        if (escenaDeNivel && !escenaNivelFinal && tension.Count > 0) pista = Snd.MusicaTension;
        if (!string.IsNullOrEmpty(musicaForzada)) pista = musicaForzada;
        if (string.IsNullOrEmpty(pista)) return;
        CambiarPista(pista, true, biblioteca != null ? biblioteca.fundidoMusica : 0.8f);
    }

    /// <summary>La música de tensión suena mientras haya algún motivo activo (furia, ulti); al quitar todos vuelve la del piso.</summary>
    public static void Tension(string motivo, bool activa)
    {
        GestorAudio g = Instancia;
        if (g == null) return;
        bool cambio = activa ? g.tension.Add(motivo) : g.tension.Remove(motivo);
        if (cambio) g.ActualizarMusicaDeEscena();
    }

    // ------------------------------------------------------------ pausa
    /// <summary>Pausa: la música baja y los efectos del juego se pausan; al reanudar vuelven. Los sonidos de interfaz no se pausan.</summary>
    public static void Pausar(bool pausa)
    {
        GestorAudio g = pausa ? Instancia : instancia;
        if (g == null || g.pausado == pausa) return;
        g.pausado = pausa;
        foreach (Voz v in g.voces)
        {
            if (v.bus != BusAudio.Efectos) continue;
            if (pausa && v.fuente.isPlaying) { v.fuente.Pause(); v.pausada = true; }
            else if (!pausa && v.pausada) { v.fuente.UnPause(); v.pausada = false; }
        }
        foreach (KeyValuePair<string, AudioSource> par in g.bucles)
        {
            if (par.Value == null) continue;
            if (pausa && par.Value.isPlaying) par.Value.Pause();
            else if (!pausa) par.Value.UnPause();
        }
    }

    // ------------------------------------------------------------ ciclo
    private void Update()
    {
        // Si Unity recompiló los scripts con el juego en marcha, los campos internos vuelven vacíos y Awake no se repite:
        // este gestor se elimina y el próximo sonido crea uno nuevo y completo (evita miles de errores por cuadro).
        if (pistas[0] == null || pistas[1] == null)
        {
            if (instancia == this) instancia = null;
            Destroy(gameObject);
            return;
        }

        float dt = Time.unscaledDeltaTime;

        // Música: fundidos y volumen.
        float objetivoPausa = pausado && biblioteca != null ? biblioteca.musicaEnPausa : 1f;
        factorPausa = Mathf.MoveTowards(factorPausa, objetivoPausa, dt * 4f);
        foreach (Pista p in pistas)
        {
            if (p.fuente == null) continue;
            p.nivel = Mathf.MoveTowards(p.nivel, p.objetivo, dt / duracionFundido);
            p.fuente.volume = p.nivel * p.volumenPropio * VolMusica * factorPausa;
            if (p.nivel <= 0f && p.objetivo <= 0f && p.fuente.isPlaying) p.fuente.Stop();
        }
        // Música acelerada con una sola vida (no durante los jingles ni en pausa).
        tonoMusica = Mathf.MoveTowards(tonoMusica, acelerada && !pausado && !jingleSonando ? 1.1f : 1f, dt * 0.25f);
        foreach (Pista p in pistas) if (p.fuente != null) p.fuente.pitch = tonoMusica;

        ActualizarAmbiente(dt);
        ActualizarAmenaza(dt);

        // Efectos: el volumen general se aplica en vivo; las voces terminadas se liberan.
        foreach (Voz v in voces)
        {
            if (v.fuente.isPlaying) v.fuente.volume = v.volumen * VolEfectos;
            else if (!v.pausada) v.nombre = null;
        }

        // Navegación de menús (para que el sonido de "mover" solo suene al cambiar de opción con teclado o mando).
        UnityEngine.InputSystem.Keyboard k = UnityEngine.InputSystem.Keyboard.current;
        if (k != null && (k.upArrowKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame ||
                          k.rightArrowKey.wasPressedThisFrame || k.tabKey.wasPressedThisFrame))
            ultimaNavegacion = Time.unscaledTime;
    }

    /// <summary>True si hace un instante se pulsó una tecla de navegación.</summary>
    public static bool NavegandoConTeclas { get { return instancia != null && Time.unscaledTime - instancia.ultimaNavegacion < 0.25f; } }
}
