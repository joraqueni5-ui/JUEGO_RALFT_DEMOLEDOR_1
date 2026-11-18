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
/// Los sonidos y sus volúmenes están en la BibliotecaSonidos.
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
        GestorAudio g = Instancia;
        if (g != null) g.Sonar(nombre, volumenExtra, tono);
    }

    private void Sonar(string nombre, float volumenExtra, float tono)
    {
        if (biblioteca == null) return;
        EntradaSonido e = biblioteca.Buscar(nombre);
        if (e == null || e.clip == null) return;

        // Límite de repeticiones: ni dos veces en menos de 0,05 s ni más de 'maximoSimultaneos' a la vez.
        float ahora = Time.unscaledTime;
        float ultimo;
        if (ultimaVez.TryGetValue(nombre, out ultimo) && ahora - ultimo < biblioteca.separacionMinima) return;
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
        libre.fuente.pitch = tono * (e.variarTono ? 1f + Random.Range(-biblioteca.variacionTono, biblioteca.variacionTono) : 1f);
        libre.pausada = false;
        libre.fuente.Play();
    }

    /// <summary>Enciende o apaga un sonido en bucle (aturdido, poder activo). Se puede llamar en cada cuadro: no reinicia el sonido.</summary>
    public static void Bucle(string clave, string nombre, bool activo, float volumenExtra = 1f)
    {
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
        if (biblioteca == null) return;
        EntradaSonido e = biblioteca.Buscar(nombre);
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

        EntradaSonido e = string.IsNullOrEmpty(nombre) ? null : biblioteca.Buscar(nombre);
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

        if (escenaNivelFinal) musicaDeEscena = Snd.MusicaNivelFinal;
        else if (escenaDeNivel) musicaDeEscena = Snd.MusicaPisos;
        else if (n == Escenas.FinJuego && !GameSession.Victoria && !GameSession.FlujoFinal) musicaDeEscena = "";   // la derrota suena con su jingle
        else musicaDeEscena = Snd.MusicaMenu;

        if (n == Escenas.FinJuego && !GameSession.Victoria && !GameSession.FlujoFinal)
        {
            Jingle(Snd.JingleDerrota);
            return;
        }
        ActualizarMusicaDeEscena();
    }

    private void ActualizarMusicaDeEscena()
    {
        if (jingleSonando) return;
        string pista = musicaDeEscena;
        if (escenaDeNivel && !escenaNivelFinal && tension.Count > 0) pista = Snd.MusicaTension;
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
