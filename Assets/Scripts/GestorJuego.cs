using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DemoledorUnifranz.Game;
using DemoledorUnifranz.UI.Menus;
using System.Collections;

/// <summary>
/// Marcador del juego con el edificio alto: puntos, vidas, sección y destrozo.
/// Se pierde si el muñeco se queda sin vidas o si la sección actual pasa el límite
/// de destrozo (el edificio se cae). Se gana al completar las 3 secciones.
/// Incluye sistema de pausa con Escape y cambio a FinJuego al terminar.
/// </summary>
public class GestorJuego : MonoBehaviour
{
    [Header("Reglas")]
    [Tooltip("Si la sección actual pasa de este % de destrozo, el edificio se cae y termina la partida.")]
    [Range(0.5f, 1f)]
    public float limiteDerrumbe = 0.7f;
    [Tooltip("Puntos por cada martillazo que repara una ventana o repisa.")]
    public int puntosPorReparacion = 10;
    [Tooltip("Puntos extra al completar cada sección.")]
    public int bonoPorSeccion = 100;
    [Tooltip("Vidas del muñeco al empezar.")]
    public int vidasIniciales = 3;
    [Tooltip("Segundos de espera antes de ir a la escena FinJuego")]
    public float tiempoEsperaFin = 1.5f;
    [Tooltip("Segundos para ver al demoledor en la terraza antes de ir a FinJuego al ganar.")]
    public float tiempoEsperaVictoria = 3f;

    private EdificioAlto edificio;
    private DemoledorAutomatico demoledor;
    private Muneco muneco;
    // Piso 1 visto desde arriba (escena PisoCenital): reemplaza al muñeco y al demoledor laterales.
    private JugadorCenital jugadorC;
    private DemoledorCenital demoledorC;
    private PisoCenital cenital;

    // Nivel final: la fachada del edificio alto (vista lateral) con palomas, poderes y el final del juego.
    [Header("Nivel final")]
    [Tooltip("Los pisos del juego (de la planta baja al 6), para mostrar sus estrellas en la pantalla final.")]
    public DefinicionPiso[] pisosDelJuego = new DefinicionPiso[0];
    private bool nivelFinal;
    private PeligrosFachada peligros;
    private EnergiaDemoledorFachada energiaFachada;

    private Text textoEstado;
    private Text textoDestrozo;
    private Text textoMensaje;
    private RectTransform rellenoBarra;
    private Image[] corazones;
    private static Sprite spriteCorazon;
    private bool partidaTerminada;
    private bool estaEnPausa;
    private int puntos;
    private int reparaciones;
    private int vidas;
    private Vector3 inicioMuneco;
    private string mensajeFinal = "";
    private string mensajeTemporal = "";
    private float finMensajeTemporal;

    // Interfaz de la pausa (Assets/Scripts/Menus/MenuPausaArcade.cs).
    private MenuPausaArcade menuPausa;
    private bool munecoActivoAntesDePausa;
    private bool reactivarMuneco;

    // Espera antes de cargar FinJuego (se cancela si se reinicia la partida).
    private Coroutine rutinaFin;

    // Modo de 6 pisos: solo se usa si SelectorModoJuego lo activó. En el modo original
    // 'modoPisos' es false y nada de esto se ejecuta.
    private EdificioPisos pisos;
    private bool modoPisos;
    private bool enTransicion;
    private TransicionPiso transicion;
    private int puntosInicioPiso;
    private int vidasInicioPiso;
    private int reparacionesInicioPiso;

    /// <summary>Destrozo a partir del cual el edificio se cae: el del piso en curso o el general.</summary>
    private float LimiteActual { get { return modoPisos ? pisos.LimiteDerrumbe : cenital != null ? cenital.LimiteDerrumbe : limiteDerrumbe; } }

    // Activar, pausar y reaparecer al jugador, sea el muñeco lateral o el cenital.
    private bool JugadorActivo { get { return muneco != null ? muneco.enabled : jugadorC != null && jugadorC.enabled; } }
    private void ActivarJugador(bool activo)
    {
        if (muneco != null) muneco.enabled = activo;
        if (jugadorC != null) jugadorC.enabled = activo;
    }
    private void ReaparecerJugador()
    {
        if (muneco != null) muneco.Reaparecer(inicioMuneco);
        if (jugadorC != null) jugadorC.Reaparecer(inicioMuneco);
    }

    private const float AnchoBarra = 300f;

    private void Awake()
    {
        Physics2D.queriesStartInColliders = false;

        demoledor = Object.FindFirstObjectByType<DemoledorAutomatico>();
        muneco = Object.FindFirstObjectByType<Muneco>();
        jugadorC = Object.FindFirstObjectByType<JugadorCenital>();
        demoledorC = Object.FindFirstObjectByType<DemoledorCenital>();
        if (muneco != null) inicioMuneco = muneco.transform.position;
        else if (jugadorC != null) inicioMuneco = jugadorC.transform.position;
        vidas = vidasIniciales;
    }

    private void Start()
    {
        // Aseguramos que el tiempo corra con normalidad al iniciar
        Time.timeScale = 1f;

        edificio = EdificioAlto.Instancia != null ? EdificioAlto.Instancia : Object.FindFirstObjectByType<EdificioAlto>();

        // Piso cenital: el jugador empieza (y reaparece) en el SpawnJugador del mapa, no donde esté en la escena.
        PisoCenital pisoActual = PisoCenital.Actual;
        if (jugadorC != null && pisoActual != null && pisoActual.Mapa != null && pisoActual.Mapa.spawnJugador != null)
        {
            inicioMuneco = pisoActual.Mapa.spawnJugador.position;
            jugadorC.transform.position = inicioMuneco;
            Physics2D.SyncTransforms();
        }

        if (edificio != null)
        {
            edificio.Cambio += Refrescar;
            edificio.Reparacion += AlReparar;
            edificio.SeccionCompletada += AlCompletarSeccion;
            edificio.Salvado += AlSalvarEdificio;
        }
        if (muneco != null) muneco.Golpeado += AlGolpearMuneco;
        if (jugadorC != null) jugadorC.Golpeado += AlGolpearMuneco;

        cenital = edificio as PisoCenital;
        pisos = edificio as EdificioPisos;

        // Nivel final: se juega con 3 vidas y el puntaje se suma al de la partida.
        nivelFinal = cenital == null && edificio != null && edificio.esNivelFinal;
        if (nivelFinal)
        {
            GameSession.EnNivelFinal = true;
            GameSession.NivelActual = SceneManager.GetActiveScene().name;
            puntos = GameSession.PuntosAcumulados;
            vidas = vidasIniciales;
            puntosInicioPiso = puntos;
            vidasInicioPiso = vidas;
            reparacionesInicioPiso = 0;
            peligros = Object.FindFirstObjectByType<PeligrosFachada>();
            energiaFachada = Object.FindFirstObjectByType<EnergiaDemoledorFachada>();
        }

        // Esta escena es el modo cenital: "volver a jugar" regresa al mismo piso, nunca al mapa lateral.
        bool partidaNueva = false;
        if (cenital != null)
        {
            DefinicionPiso defPiso = cenital.Definicion;
            GameSession.ModoSeleccionado = GameSession.ModoElegido.Pisos;
            GameSession.NivelActual = SceneManager.GetActiveScene().name;
            if (defPiso != null) GameSession.PisoActual = defPiso.numero;

            // Los puntos y las vidas con que empezó el piso (los de la partida en curso o, al reintentar, los del inicio).
            partidaNueva = GameSession.VidasAlEmpezarPiso <= 0;
            puntos = GameSession.PuntosAcumulados;
            vidas = GameSession.VidasAlEmpezarPiso > 0 ? Mathf.Min(GameSession.VidasAlEmpezarPiso, vidasIniciales) : vidasIniciales;
            puntosInicioPiso = puntos;
            vidasInicioPiso = vidas;
            reparacionesInicioPiso = 0;
            GameSession.GuardarInicioPiso(GameSession.PisoActual, puntos, vidas);
        }
        modoPisos = SelectorModoJuego.ModoPisosActivo && pisos != null;
        if (modoPisos) IniciarModoPisos();

        CrearHud();
        Refrescar();
        if (modoPisos) MostrarAviso(NombrePisoActual(), 2.5f);
        if (cenital != null && partidaNueva && cenital.Definicion != null && cenital.Definicion.mostrarFichaAlEmpezar)
            StartCoroutine(FichaInicial(cenital.Definicion));

        // ControlesTactiles se crea automáticamente solo en la primera escena que abre el juego.
        // Ahora el juego arranca en MenuPrincipal, así que aquí se asegura que existan.
        if (Object.FindAnyObjectByType<ControlesTactiles>() == null)
        {
            new GameObject("ControlesTactiles").AddComponent<ControlesTactiles>();
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f; // Restaurar el tiempo por si se destruye el objeto

        if (edificio != null)
        {
            edificio.Cambio -= Refrescar;
            edificio.Reparacion -= AlReparar;
            edificio.SeccionCompletada -= AlCompletarSeccion;
            edificio.Salvado -= AlSalvarEdificio;
        }
        if (muneco != null) muneco.Golpeado -= AlGolpearMuneco;
        if (jugadorC != null) jugadorC.Golpeado -= AlGolpearMuneco;
        if (pisos != null) pisos.PisoCompletado -= AlCompletarPiso;
    }

    private void Update()
    {
        // Poderes activos del jugador (tiempo restante).
        if (textoPoder != null && jugadorC != null)
        {
            string poderes = jugadorC.TextoPoderes();
            if (textoPoder.text != poderes) textoPoder.text = poderes;
        }

        // Mientras se muestra la ficha del piso siguiente no hay pausa ni reinicio.
        if (enTransicion) return;

        Keyboard teclado = Keyboard.current;
        bool reiniciarTactil = ControlesTactiles.ReiniciarPulsado();
        bool reiniciarTecla = teclado != null && teclado[Key.R].wasPressedThisFrame;

        // Pausa con Escape (solo si la partida no ha terminado).
        // En Android sin teclado Keyboard.current es null: se revisa antes de usarlo.
        bool pausaTactil = ControlesTactiles.PausaPulsada();
        if (((teclado != null && teclado[Key.Escape].wasPressedThisFrame) || pausaTactil) && !partidaTerminada)
        {
            AlternarPausa();
        }

        // Reinicio con R o con el botón táctil (no mientras el juego está en pausa).
        if ((reiniciarTecla || reiniciarTactil) && !estaEnPausa)
        {
            ReiniciarPartida();
        }

        // Borra el aviso temporal ("¡SECCIÓN COMPLETA!", "-1 VIDA") cuando se cumple su tiempo.
        if (mensajeTemporal.Length > 0 && Time.time >= finMensajeTemporal)
        {
            mensajeTemporal = "";
            Refrescar();
        }
    }

    private void LateUpdate()
    {
        // El muñeco se reactiva un cuadro después de cerrar la pausa: así el ENTER
        // que pulsó CONTINUAR no se toma también como un martillazo.
        if (!reactivarMuneco) return;
        reactivarMuneco = false;
        if (!partidaTerminada && !estaEnPausa) ActivarJugador(munecoActivoAntesDePausa);
    }

    private void AlternarPausa()
    {
        if (estaEnPausa) Reanudar();
        else Pausar();
    }

    /// <summary>Congela el juego y muestra el menú de pausa.</summary>
    public void Pausar()
    {
        if (estaEnPausa || partidaTerminada || enTransicion) return;
        estaEnPausa = true;
        GestorAudio.Pausar(true);
        GestorAudio.Reproducir(Snd.Pausa);

        // Congela la física y el movimiento del juego.
        Time.timeScale = 0f;

        // El muñeco lee el teclado en Update: se desactiva para que no se mueva ni repare.
        if (muneco != null || jugadorC != null)
        {
            munecoActivoAntesDePausa = JugadorActivo;
            ActivarJugador(false);
        }

        if (menuPausa != null && edificio != null) menuPausa.Mostrar(puntos, reparaciones, edificio.DestrozoSeccionActual);
        else if (menuPausa != null) menuPausa.Mostrar(puntos, reparaciones, 0f);
    }

    /// <summary>Cierra el menú de pausa y continúa la partida.</summary>
    public void Reanudar()
    {
        if (!estaEnPausa) return;
        estaEnPausa = false;
        GestorAudio.Pausar(false);
        GestorAudio.Reproducir(Snd.Pausa);
        Time.timeScale = 1f;
        reactivarMuneco = true;

        // Descarta los toques hechos sobre los controles táctiles mientras estaba en pausa.
        ControlesTactiles.ArribaPulsado();
        ControlesTactiles.SaltarPulsado();
        ControlesTactiles.RepararPulsado();
        ControlesTactiles.ReiniciarPulsado();

        if (menuPausa != null) menuPausa.Ocultar();
    }

    private void AlReparar()
    {
        if (partidaTerminada) return;

        reparaciones++;
        puntos += puntosPorReparacion;
        Refrescar();
    }

    /// <summary>Suma vidas sin pasar de 'maximo' ni de los corazones del HUD. Devuelve false si ya estaba al tope.</summary>
    public bool GanarVida(int cantidad, int maximo)
    {
        if (partidaTerminada) return false;
        int tope = Mathf.Max(1, maximo);
        if (corazones != null && corazones.Length > 0) tope = Mathf.Min(tope, corazones.Length);
        if (vidas >= tope) return false;
        vidas = Mathf.Min(tope, vidas + Mathf.Max(1, cantidad));
        Refrescar();
        return true;
    }

    /// <summary>Puntos por bloquear un golpe del demoledor (el aviso flotante lo muestra el jugador).</summary>
    public void RegistrarBloqueo(int puntosExtra)
    {
        if (partidaTerminada) return;
        puntos += Mathf.Max(0, puntosExtra);
        MostrarAviso("¡SALVADO!  +" + puntosExtra, 1.5f);
    }

    private void AlGolpearMuneco()
    {
        if (partidaTerminada) return;

        vidas = Mathf.Max(0, vidas - 1);
        if (vidas <= 0)
        {
            TerminarPartida(false);
            return;
        }

        MostrarAviso("¡AY!  -1 VIDA", 1.5f);
    }

    private void AlCompletarSeccion(int nuevaSeccion)
    {
        if (partidaTerminada || edificio == null) return;

        puntos += bonoPorSeccion;
        if (nuevaSeccion < edificio.secciones)
        {
            MostrarAviso(string.Format("¡SECCIÓN {0} REPARADA! +{1}   ¡EL DEMOLEDOR SUBE!", nuevaSeccion, bonoPorSeccion), 2.5f);
        }
        else
        {
            Refrescar();
        }
    }

    // ------------------------------------------------------------ modo de 6 pisos
    private void IniciarModoPisos()
    {
        // La ficha entre pisos se encarga de cargar el siguiente piso.
        pisos.cargarSiguienteSolo = false;
        pisos.PisoCompletado += AlCompletarPiso;

        // Continúa desde el piso donde se cayó (o desde el piso de pruebas del Inspector).
        int piso = Mathf.Max(GameSession.PisoActual, pisos.pisoInicial);
        if (piso != pisos.PisoActual) pisos.CargarPiso(piso);

        // Los puntos y las vidas con que empezó ese piso.
        puntos = GameSession.PuntosAcumulados;
        vidas = GameSession.VidasAlEmpezarPiso > 0
            ? Mathf.Min(GameSession.VidasAlEmpezarPiso, vidasIniciales)
            : vidasIniciales;
        GuardarInicioPiso();

        transicion = TransicionPiso.Crear(transform);
    }

    /// <summary>Guarda cómo empieza el piso en curso: a ese estado se vuelve al perder o con R.</summary>
    private void GuardarInicioPiso()
    {
        puntosInicioPiso = puntos;
        vidasInicioPiso = vidas;
        reparacionesInicioPiso = reparaciones;
        GameSession.GuardarInicioPiso(pisos.PisoActual, puntos, vidas);
    }

    private string NombrePisoActual()
    {
        DefinicionPiso d = pisos.DefinicionActual;
        string nombre = d != null ? d.TituloCompleto : "Piso " + (pisos.PisoActual + 1);
        return nombre.ToUpperInvariant();
    }

    private void AlCompletarPiso(int indice)
    {
        if (partidaTerminada || pisos == null) return;

        puntos += bonoPorSeccion;
        // Al completar el último piso, EdificioPisos avisa 'Salvado' y termina la partida.
        if (indice >= pisos.TotalPisos - 1)
        {
            Refrescar();
            return;
        }
        StartCoroutine(SubirDePiso(indice + 1));
    }

    private IEnumerator SubirDePiso(int siguiente)
    {
        enTransicion = true;
        Time.timeScale = 0f;
        if (muneco != null) muneco.enabled = false;
        Refrescar();

        bool cerrada = false;
        System.Action alCerrar = () => cerrada = true;
        transicion.Cerrada += alCerrar;
        transicion.Mostrar(pisos.DefinicionDe(siguiente));
        while (!cerrada) yield return null;
        transicion.Cerrada -= alCerrar;
        yield return new WaitForSecondsRealtime(0.25f);   // deja que la ficha termine de cerrarse

        EscombroCayendo.EliminarTodos();
        pisos.CargarPiso(siguiente);
        if (muneco != null) muneco.Reaparecer(inicioMuneco);
        if (demoledor != null) demoledor.Reiniciar();
        GuardarInicioPiso();

        Time.timeScale = 1f;
        enTransicion = false;
        // El muñeco se reactiva un cuadro después, como al cerrar la pausa: el ENTER que
        // cerró la ficha no cuenta como martillazo.
        munecoActivoAntesDePausa = true;
        reactivarMuneco = true;
        MostrarAviso(NombrePisoActual(), 2.5f);
    }

    private static string NombreCorto(DefinicionPiso d)
    {
        return d == null ? "" : d.NombreArea;
    }

    /// <summary>Ficha de bienvenida al empezar una partida nueva (por ejemplo la planta baja): el juego espera a CONTINUAR.</summary>
    private IEnumerator FichaInicial(DefinicionPiso def)
    {
        yield return null;
        enTransicion = true;
        ActivarJugador(false);
        Time.timeScale = 0f;
        if (transicion == null) transicion = TransicionPiso.Crear(transform);

        bool cerrada = false;
        System.Action alCerrar = () => cerrada = true;
        transicion.Cerrada += alCerrar;
        transicion.Mostrar(def, string.IsNullOrEmpty(def.tituloVentanaFicha) ? "¡BIENVENIDO!" : def.tituloVentanaFicha);
        while (!cerrada) yield return null;
        transicion.Cerrada -= alCerrar;
        yield return new WaitForSecondsRealtime(0.25f);

        Time.timeScale = 1f;
        enTransicion = false;
        // El jugador se reactiva un cuadro después: el ENTER que cerró la ficha no cuenta como martillazo.
        munecoActivoAntesDePausa = true;
        reactivarMuneco = true;
    }

    // ------------------------------------------------------------ piso cenital superado
    /// <summary>Mensaje temporal en el centro de la pantalla.</summary>
    /// <summary>Hay un mensaje grande temporal en pantalla (por ejemplo "¡SÍGUELO!").</summary>
    public bool AvisoGrandeActivo { get { return Time.time < finMensajeTemporal && !string.IsNullOrEmpty(mensajeTemporal); } }

    public void Aviso(string texto, float segundos)
    {
        MostrarAviso(texto, segundos);
    }

    /// <summary>Suma puntos sueltos (por ejemplo los de un punto de información).</summary>
    public void SumarPuntos(int cantidad, string aviso)
    {
        if (partidaTerminada || cantidad <= 0) return;
        puntos += cantidad;
        Refrescar();
    }

    /// <summary>
    /// El jugador llegó al ascensor: pantalla de estrellas, ficha del piso siguiente y carga de su escena.
    /// Sin piso siguiente en la definición, la partida termina con la pantalla de victoria.
    /// </summary>
    private IEnumerator FinPisoCenital()
    {
        enTransicion = true;
        partidaTerminada = true;
        ActivarJugador(false);
        if (demoledorC != null) demoledorC.Detener();
        Refrescar();

        DefinicionPiso def = cenital.Definicion;
        int indicePiso = def != null ? Mathf.Max(0, def.numero) : 0;
        bool[] cumplidas = cenital.CondicionesEstrellas();
        int estrellas = cenital.Estrellas;
        bool record = GameSession.GuardarEstrellas(indicePiso, estrellas);

        float maximo = def != null ? def.destrozoMaximoEstrella : 0.3f;
        string[] textos =
        {
            "Vencer al demoledor",
            "Destrozo menor a " + Mathf.RoundToInt(maximo * 100f) + " % al rendirse (fue " +
                Mathf.RoundToInt(cenital.DestrozoAlRendirse * 100f) + " %)",
            "Leer los " + cenital.InfoTotal + " puntos \"?\" (" + cenital.InfoLeidasCuenta + "/" + cenital.InfoTotal + ")",
        };

        Time.timeScale = 0f;
        PantallaEstrellas pantalla = PantallaEstrellas.Crear(transform);
        bool cerrada = false;
        System.Action alCerrar = () => cerrada = true;
        pantalla.Cerrada += alCerrar;
        pantalla.Mostrar(puntos, cumplidas, textos, record, def != null ? def.NombreArea : "");
        while (!cerrada) yield return null;
        pantalla.Cerrada -= alCerrar;
        yield return new WaitForSecondsRealtime(0.25f);

        DefinicionPiso siguiente = cenital.Siguiente;
        if (siguiente == null)
        {
            // Último piso: el demoledor escapó a la fachada. Se desbloquea el nivel final, se muestra su ficha y se carga.
            GameSession.NivelFinalDesbloqueado = true;

            DefinicionPiso ficha = ScriptableObject.CreateInstance<DefinicionPiso>();
            ficha.numero = 7;
            ficha.nombre = "Fachada de Unifranz";
            ficha.nombreArea = DefinicionPiso.NombreNivelFinal;
            ficha.fichaTitulo = "¡El demoledor escapó a la fachada! - NIVEL FINAL";
            ficha.fichaDescripcion = "Sube por la fachada reparando las ventanas antes de que se derrumbe, y cuidado con las palomas.";
            ficha.fichaDatoUtil = "Es el nivel más difícil: tienes 3 vidas.";
            if (transicion == null) transicion = TransicionPiso.Crear(transform);
            bool fichaFinal = false;
            System.Action alCerrarFinal = () => fichaFinal = true;
            transicion.Cerrada += alCerrarFinal;
            transicion.Mostrar(ficha, "¡NIVEL FINAL!", "NIVEL FINAL");
            while (!fichaFinal) yield return null;
            transicion.Cerrada -= alCerrarFinal;
            yield return new WaitForSecondsRealtime(0.25f);

            int acumulados = puntos;
            GameSession.NuevaPartida();
            GameSession.PuntosAcumulados = acumulados;
            GameSession.EnNivelFinal = true;
            Time.timeScale = 1f;
            string escenaFinal = def != null ? def.EscenaSiguiente : Escenas.NivelFinal;
            if (!NavegadorEscenas.Disponible(escenaFinal)) escenaFinal = Escenas.NivelFinal;
            if (!NavegadorEscenas.Cargar(escenaFinal)) SceneManager.LoadScene(escenaFinal);
            yield break;
        }

        // Ficha "Subiendo al piso N" y carga del piso siguiente.
        if (transicion == null) transicion = TransicionPiso.Crear(transform);
        bool fichaCerrada = false;
        System.Action alCerrarFicha = () => fichaCerrada = true;
        transicion.Cerrada += alCerrarFicha;
        transicion.Mostrar(siguiente);
        while (!fichaCerrada) yield return null;
        transicion.Cerrada -= alCerrarFicha;
        yield return new WaitForSecondsRealtime(0.25f);

        int indiceSiguiente = Mathf.Max(0, siguiente.numero);
        GameSession.SeleccionarNivel(true, indiceSiguiente);
        GameSession.GuardarInicioPiso(indiceSiguiente, puntos, vidas);
        Time.timeScale = 1f;
        // La definición del piso que se acaba de superar guarda el nombre de la escena que sigue.
        string escena = def != null ? def.EscenaSiguiente : Escenas.ParaPiso(indiceSiguiente);
        if (!NavegadorEscenas.Disponible(escena)) escena = Escenas.ParaPiso(indiceSiguiente);
        if (!NavegadorEscenas.Disponible(escena)) escena = Escenas.Nivel1;
        if (!NavegadorEscenas.Cargar(escena)) SceneManager.LoadScene(escena);
    }

    private string[] NombresDeLosPisos()
    {
        System.Collections.Generic.List<string> lista = new System.Collections.Generic.List<string>();
        DefinicionPiso[] origen = cenital != null && cenital.pisos != null ? cenital.pisos : pisosDelJuego;
        if (origen != null)
        {
            foreach (DefinicionPiso d in origen)
                if (d != null && d.disponible) lista.Add(d.NombreArea);
        }
        return lista.ToArray();
    }

    /// <summary>
    /// Final del nivel final: el demoledor, en la terraza, es levantado y lanzado por los estudiantes. Luego la
    /// pantalla "¡UNIVERSIDAD SALVADA!" con el sello, los créditos y la tabla de récords.
    /// </summary>
    private IEnumerator FinNivelFinal()
    {
        partidaTerminada = true;
        enTransicion = true;
        ActivarJugador(false);
        if (peligros != null) peligros.enabled = false;
        Refrescar();

        FinalFachada escena = new GameObject("FinalFachada").AddComponent<FinalFachada>();
        yield return escena.Reproducir(demoledor, edificio, Object.FindFirstObjectByType<CamaraSeguimiento>());

        Time.timeScale = 0f;
        PantallaFinal pantalla = PantallaFinal.Crear(transform, NombresDeLosPisos(), true);
        bool cerrada = false;
        System.Action alCerrar = () => cerrada = true;
        pantalla.Cerrada += alCerrar;
        pantalla.Mostrar(puntos);
        while (!cerrada) yield return null;
        pantalla.Cerrada -= alCerrar;
        yield return new WaitForSecondsRealtime(0.25f);

        mensajeFinal = "¡UNIVERSIDAD SALVADA!";
        Time.timeScale = 1f;
        GameSession.RegistrarVictoria(puntos, reparaciones, SceneManager.GetActiveScene().name);
        GameSession.NuevaPartida();         // la próxima partida empieza en la planta baja
        GameSession.FlujoFinal = true;      // créditos y luego récords
        Refrescar();
        enTransicion = false;
        if (!NavegadorEscenas.Cargar(Escenas.MenuPrincipal)) SceneManager.LoadScene(Escenas.FinJuego);
    }

    private void AlSalvarEdificio()
    {
        if (partidaTerminada) return;
        if (cenital != null)
        {
            StartCoroutine(FinPisoCenital());
            return;
        }
        if (nivelFinal)
        {
            StartCoroutine(FinNivelFinal());
            return;
        }
        partidaTerminada = true;
        mensajeFinal = "¡EDIFICIO SALVADO!";

        // El demoledor sigue en la terraza (no se detiene); el muñeco ya no se controla.
        ActivarJugador(false);
        if (demoledorC != null) demoledorC.Detener();

        Time.timeScale = 1f;
        GameSession.RegistrarVictoria(puntos, reparaciones, SceneManager.GetActiveScene().name);
        if (modoPisos) GameSession.NuevaPartida();   // ganó: la próxima partida empieza en el Piso 1
        Refrescar();
        rutinaFin = StartCoroutine(CargarFinJuego(tiempoEsperaVictoria));
    }

    private void MostrarAviso(string texto, float segundos)
    {
        mensajeTemporal = texto;
        finMensajeTemporal = Time.time + segundos;
        Refrescar();
    }

    private void Refrescar()
    {
        if (edificio == null) return;

        float destrozo = edificio.DestrozoSeccionActual;
        int seccionVisible = Mathf.Min(edificio.SeccionActual + 1, edificio.secciones);

        if (textoEstado != null && cenital != null)
        {
            textoEstado.text = string.Format(
                "PUNTOS  {0}      {1}      OBJETOS ROTOS  {2}/{3}",
                puntos, NombreCorto(cenital.Definicion), edificio.Terminado ? 0 : cenital.ObjetosRotos, edificio.VentanasPorSeccion);
            textoEstado.horizontalOverflow = HorizontalWrapMode.Overflow;
            textoEstado.verticalOverflow = VerticalWrapMode.Overflow;
        }
        else if (textoEstado != null && nivelFinal)
        {
            textoEstado.text = string.Format(
                "PUNTOS  {0}      NIVEL FINAL      SECCIÓN  {1}/{2}",
                puntos, seccionVisible, edificio.secciones);
            textoEstado.horizontalOverflow = HorizontalWrapMode.Overflow;
            textoEstado.verticalOverflow = VerticalWrapMode.Overflow;
        }
        else if (textoEstado != null && modoPisos)
        {
            textoEstado.text = string.Format(
                "PUNTOS  {0}\nPISO  {1}/{2}\nOBJETOS ROTOS  {3}/{4}",
                puntos, pisos.PisoActual + 1, pisos.TotalPisos,
                edificio.Terminado ? 0 : edificio.DanadasEnSeccion(edificio.SeccionActual), edificio.VentanasPorSeccion);
        }
        else if (textoEstado != null)
        {
            textoEstado.text = string.Format(
                "PUNTOS  {0}\nSECCIÓN  {1}/{2}\nVENTANAS ROTAS  {3}/{4}",
                puntos, seccionVisible, edificio.secciones,
                edificio.Terminado ? 0 : edificio.DanadasEnSeccion(edificio.SeccionActual), edificio.VentanasPorSeccion);
        }
        if (textoDestrozo != null) textoDestrozo.text = "DESTROZO  " + Mathf.RoundToInt(destrozo * 100f) + "%";

        float energiaDemoledor = cenital != null ? cenital.Energia : energiaFachada != null ? energiaFachada.Energia : -1f;
        if (energiaDemoledor >= 0f && textoEnergia != null)
        {
            textoEnergia.text = "DEMOLEDOR  " + Mathf.CeilToInt(energiaDemoledor * 100f) + "%";
            if (rellenoEnergia != null) rellenoEnergia.sizeDelta = new Vector2(energiaDemoledor * AnchoBarra, 16f);
        }

        if (corazones != null)
        {
            for (int i = 0; i < corazones.Length; i++)
            {
                if (corazones[i] == null) continue;
                ArteNiveles arte = ArteNiveles.Actual;
                if (arte != null && arte.corazonLleno != null && arte.corazonVacio != null)
                {
                    // Corazones del arte: lleno o vacío, sin teñir.
                    corazones[i].sprite = i < vidas ? arte.corazonLleno : arte.corazonVacio;
                    corazones[i].color = Color.white;
                }
                else
                {
                    corazones[i].color = i < vidas ? new Color(0.92f, 0.2f, 0.24f, 1f) : new Color(0.25f, 0.25f, 0.3f, 0.6f);
                }
            }
        }

        if (rellenoBarra != null)
        {
            // La barra se llena hasta el límite de derrumbe.
            float lleno = Mathf.Clamp01(destrozo / Mathf.Max(0.01f, LimiteActual));
            rellenoBarra.sizeDelta = new Vector2(lleno * AnchoBarra, 16f);
            Image imagen = rellenoBarra.GetComponent<Image>();
            if (imagen != null) imagen.color = Color.Lerp(new Color(0.95f, 0.75f, 0.2f), new Color(0.85f, 0.15f, 0.12f), lleno);
        }

        if (!partidaTerminada && destrozo >= LimiteActual)
        {
            TerminarPartida(true);
        }

        if (textoMensaje == null) return;

        if (partidaTerminada) textoMensaje.text = mensajeFinal;
        else textoMensaje.text = mensajeTemporal;
    }

    public bool PartidaTerminada { get { return partidaTerminada; } }
    public int Puntos { get { return puntos; } }
    public int Reparaciones { get { return reparaciones; } }
    public bool EstaEnPausa { get { return estaEnPausa; } }

    /// <summary>Pierde la partida: por derrumbe (true) o porque el muñeco se quedó sin vidas (false).</summary>
    private void TerminarPartida(bool porDerrumbe)
    {
        if (partidaTerminada) return;
        partidaTerminada = true;
        mensajeFinal = porDerrumbe ? "¡EL EDIFICIO SE CAYÓ!" : "¡TE QUEDASTE SIN VIDAS!";

        if (demoledor != null) demoledor.Detener();
        if (demoledorC != null) demoledorC.Detener();
        ActivarJugador(false);

        // Si estaba en pausa, restaurar escala de tiempo antes del cambio
        Time.timeScale = 1f;

        // Datos reales de la partida para la pantalla FinJuego.
        string escena = SceneManager.GetActiveScene().name;
        if (porDerrumbe)
            GameSession.RegistrarDerrumbe(puntos, reparaciones, edificio != null ? edificio.DestrozoSeccionActual : 1f, escena);
        else
            GameSession.RegistrarSinVidas(puntos, reparaciones, escena);

        Refrescar();
        rutinaFin = StartCoroutine(CargarFinJuego(tiempoEsperaFin));
    }

    private IEnumerator CargarFinJuego(float espera)
    {
        yield return new WaitForSeconds(espera);
        // Con fundido; si FinJuego no estuviera en Build Settings se carga como antes.
        if (!NavegadorEscenas.Cargar(Escenas.FinJuego)) SceneManager.LoadScene(Escenas.FinJuego);
    }

    /// <summary>
    /// Vuelve a empezar: ventanas intactas, sección 1, vidas llenas y puntos a cero.
    /// Mantiene compatibilidad con scripts de verificación y tests.
    /// </summary>
    public void ReiniciarPartida()
    {
        if (rutinaFin != null)
        {
            StopCoroutine(rutinaFin);
            rutinaFin = null;
        }

        Time.timeScale = 1f;
        partidaTerminada = false;
        bool estabaEnPausa = estaEnPausa;
        estaEnPausa = false;
        reactivarMuneco = false;
        if (menuPausa != null) menuPausa.OcultarInmediato();
        if (estabaEnPausa) ControlesTactiles.ReiniciarPulsado();

        // Modo pisos: R reinicia solo el piso en curso, con los puntos y las vidas con que empezó.
        bool porPiso = modoPisos || cenital != null || nivelFinal;
        puntos = porPiso ? puntosInicioPiso : 0;
        reparaciones = porPiso ? reparacionesInicioPiso : 0;
        vidas = porPiso ? vidasInicioPiso : vidasIniciales;
        mensajeFinal = "";
        mensajeTemporal = "";

        EscombroCayendo.EliminarTodos();
        if (edificio != null)
        {
            if (modoPisos) pisos.ReiniciarPiso();
            else edificio.Reiniciar();
        }

        ActivarJugador(true);
        ReaparecerJugador();
        if (peligros != null) peligros.Reiniciar();
        if (demoledor != null) demoledor.Reiniciar();
        if (demoledorC != null) demoledorC.Reiniciar();

        Refrescar();
    }

    // ------------------------------------------------------------ HUD
    private void CrearHud()
    {
        GameObject lienzo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        lienzo.transform.SetParent(transform, false);

        Canvas canvas = lienzo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler escalador = lienzo.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        Transform raiz = lienzo.transform;

        if (cenital != null || nivelFinal)
        {
            CrearFranjaCenital(canvas, raiz);
        }
        else
        {
        // Panel de estado a la izquierda, bajo el botón de pausa (que está en 24, -20 y mide 112 x 96).
        Image panel = CrearImagen(raiz, "PanelEstado", new Color(0.05f, 0.06f, 0.08f, 0.6f),
            new Vector2(0f, 1f), new Vector2(24f, -128f), new Vector2(380f, 196f));
        panel.rectTransform.pivot = new Vector2(0f, 1f);

        textoEstado = CrearTexto(raiz, "", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -140f),
            new Vector2(350f, 110f), TextAnchor.UpperLeft, 28, Color.white, FontStyle.Bold);

        textoDestrozo = CrearTexto(raiz, "", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -252f),
            new Vector2(350f, 34f), TextAnchor.UpperLeft, 24, new Color(1f, 0.85f, 0.3f, 1f), FontStyle.Bold);

        // Barra de destrozo (se llena hasta el límite de derrumbe).
        Image fondoBarra = CrearImagen(raiz, "BarraFondo", new Color(0.02f, 0.02f, 0.03f, 0.8f),
            new Vector2(0f, 1f), new Vector2(42f, -290f), new Vector2(AnchoBarra + 8f, 24f));
        fondoBarra.rectTransform.pivot = new Vector2(0f, 1f);

        Image relleno = CrearImagen(raiz, "BarraRelleno", new Color(0.95f, 0.75f, 0.2f, 1f),
            new Vector2(0f, 1f), new Vector2(46f, -302f), new Vector2(0f, 16f));
        rellenoBarra = relleno.rectTransform;
        rellenoBarra.pivot = new Vector2(0f, 0.5f);

        // Vidas: un corazón por vida, arriba a la derecha (se apagan al perderlas).
        // En el celular el botón REINICIAR ocupa esa esquina, así que bajan un poco.
        int totalCorazones = Mathf.Max(1, vidasIniciales);
        corazones = new Image[totalCorazones];
        const float AnchoCorazon = 60f;
        const float SeparacionCorazon = 72f;
        float alturaCorazones = ControlesTactiles.DebeMostrarse ? -140f : -56f;
        for (int i = 0; i < totalCorazones; i++)
        {
            float x = -(40f + AnchoCorazon * 0.5f + (totalCorazones - 1 - i) * SeparacionCorazon);
            Image corazon = CrearImagen(raiz, "Vida" + (i + 1), Color.white, new Vector2(1f, 1f),
                new Vector2(x, alturaCorazones), new Vector2(AnchoCorazon, AnchoCorazon * 8f / 9f));
            corazon.sprite = SpriteCorazon();
            corazon.preserveAspect = true;
            corazones[i] = corazon;
        }
        }

        textoMensaje = CrearTexto(raiz, "", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 260f),
            new Vector2(1500f, 100f), TextAnchor.MiddleCenter, 58, new Color(1f, 0.85f, 0.3f, 1f), FontStyle.Bold);

        // --- Menú de pausa (ventana con CONTINUAR / REINICIAR / MENÚ PRINCIPAL) ---
        menuPausa = MenuPausaArcade.Crear(this);
    }

    [Header("HUD del piso cenital (píxeles del lienzo de referencia 1920 x 1080)")]
    [Tooltip("Altura de la franja del HUD, sin contar el área no segura de la pantalla (muesca, esquinas redondeadas).")]
    public float alturaFranja = 130f;
    [Tooltip("Espacio entre el borde seguro de arriba y la primera línea de texto.")]
    public float margenSuperiorHud = 16f;
    [Tooltip("Tamaño de letra de la primera línea (puntos, piso, objetos rotos).")]
    public int tamanoLetraHud = 30;
    [Tooltip("Separación mínima fija (en píxeles de pantalla del lienzo) entre lo más alto del HUD y el borde de arriba. " +
             "Si algo sobresale, el HUD baja solo lo necesario.")]
    public float paddingFijoSuperior = 14f;

    private Text textoPoder, textoEnergia;
    private RectTransform rellenoEnergia;
    private static readonly Color ColorEnergia = new Color(0.35f, 0.8f, 1f, 1f);
    private Canvas canvasHud;
    private RectTransform franjaHud, contenidoHud;
    private float alturaFranjaBase;

    /// <summary>Escala que aplica el CanvasScaler (1920 x 1080, ajuste 0,5) al tamaño actual de la pantalla.</summary>
    private static float EscalaLienzo()
    {
        float w = Mathf.Max(0.1f, Screen.width / 1920f);
        float h = Mathf.Max(0.1f, Screen.height / 1080f);
        return Mathf.Exp(Mathf.Lerp(Mathf.Log(w), Mathf.Log(h), 0.5f));
    }

    /// <summary>
    /// HUD del piso visto desde arriba: una franja a todo el ancho, anclada al borde de arriba, que el
    /// mapa no cubre (la cámara del mapa empieza justo debajo). Los textos se anclan a la izquierda,
    /// las vidas a la derecha y todo escala con el lienzo, así que se ve completo en 16:9 y en 20:9.
    /// </summary>
    private void CrearFranjaCenital(Canvas canvas, Transform raiz)
    {
        // Áreas no seguras (muesca, barra de estado, esquinas): el texto se desplaza para no quedar bajo ellas
        // y la franja crece lo mismo, así el mapa sigue empezando justo debajo.
        float escala = EscalaLienzo();
        Rect segura = Screen.safeArea;
        float insetArriba = Mathf.Max(0f, (Screen.height - segura.yMax) / escala);
        float insetIzq = Mathf.Max(0f, segura.xMin / escala);
        float insetDer = Mathf.Max(0f, (Screen.width - segura.xMax) / escala);
        float altura = alturaFranja + insetArriba;

        GameObject fondo = new GameObject("FranjaHud", typeof(RectTransform), typeof(Image));
        fondo.transform.SetParent(raiz, false);
        RectTransform rtFondo = (RectTransform)fondo.transform;
        rtFondo.anchorMin = new Vector2(0f, 1f);
        rtFondo.anchorMax = new Vector2(1f, 1f);
        rtFondo.pivot = new Vector2(0.5f, 1f);
        rtFondo.anchoredPosition = Vector2.zero;
        rtFondo.sizeDelta = new Vector2(0f, altura);
        Image imagenFondo = fondo.GetComponent<Image>();
        imagenFondo.color = new Color(0.05f, 0.06f, 0.08f, 1f);
        imagenFondo.raycastTarget = false;
        fondo.transform.SetAsFirstSibling();
        canvasHud = canvas;
        franjaHud = rtFondo;
        alturaFranjaBase = altura;

        // Todo el contenido cuelga de un contenedor que ocupa el lienzo entero: así se puede bajar de una vez.
        GameObject cont = new GameObject("ContenidoHud", typeof(RectTransform));
        cont.transform.SetParent(raiz, false);
        contenidoHud = (RectTransform)cont.transform;
        contenidoHud.anchorMin = Vector2.zero;
        contenidoHud.anchorMax = Vector2.one;
        contenidoHud.offsetMin = Vector2.zero;
        contenidoHud.offsetMax = Vector2.zero;
        raiz = contenidoHud;

        // Deja libre el botón de pausa (24..136 de ancho) y empieza el texto a su derecha.
        float x0 = 170f + insetIzq;
        float linea1 = -(insetArriba + margenSuperiorHud);
        float linea2 = linea1 - (tamanoLetraHud + 28f);
        textoEstado = CrearTexto(raiz, "", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0, linea1),
            new Vector2(1000f, tamanoLetraHud + 22f), TextAnchor.MiddleLeft, tamanoLetraHud, Color.white, FontStyle.Bold);
        textoDestrozo = CrearTexto(raiz, "", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0, linea2),
            new Vector2(230f, 34f), TextAnchor.UpperLeft, 24, new Color(1f, 0.85f, 0.3f, 1f), FontStyle.Bold);

        // Energía del demoledor (celeste), a la derecha de la barra de destrozo.
        float xEnergia = x0 + 240f + AnchoBarra + 40f;
        textoEnergia = CrearTexto(raiz, "", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(xEnergia, linea2),
            new Vector2(230f, 34f), TextAnchor.UpperLeft, 24, ColorEnergia, FontStyle.Bold);
        Image fondoEnergia = CrearImagen(raiz, "EnergiaFondo", new Color(0.02f, 0.02f, 0.03f, 0.8f),
            new Vector2(0f, 1f), new Vector2(xEnergia + 240f, linea2 - 4f), new Vector2(AnchoBarra + 8f, 24f));
        fondoEnergia.rectTransform.pivot = new Vector2(0f, 1f);
        Image rellenoE = CrearImagen(raiz, "EnergiaRelleno", ColorEnergia,
            new Vector2(0f, 1f), new Vector2(xEnergia + 244f, linea2 - 16f), new Vector2(AnchoBarra, 16f));
        rellenoEnergia = rellenoE.rectTransform;
        rellenoEnergia.pivot = new Vector2(0f, 0.5f);

        // Poder activo y su tiempo restante, a la derecha de la barra de energía.
        textoPoder = CrearTexto(raiz, "", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(xEnergia + 240f + AnchoBarra + 36f, linea2),
            new Vector2(500f, 34f), TextAnchor.UpperLeft, 24, new Color(0.5f, 1f, 0.6f, 1f), FontStyle.Bold);

        Image fondoBarra = CrearImagen(raiz, "BarraFondo", new Color(0.02f, 0.02f, 0.03f, 0.8f),
            new Vector2(0f, 1f), new Vector2(x0 + 240f, linea2 - 4f), new Vector2(AnchoBarra + 8f, 24f));
        fondoBarra.rectTransform.pivot = new Vector2(0f, 1f);
        Image relleno = CrearImagen(raiz, "BarraRelleno", new Color(0.95f, 0.75f, 0.2f, 1f),
            new Vector2(0f, 1f), new Vector2(x0 + 244f, linea2 - 16f), new Vector2(0f, 16f));
        rellenoBarra = relleno.rectTransform;
        rellenoBarra.pivot = new Vector2(0f, 0.5f);

        // Vidas: arriba a la derecha, centradas en la franja. En el celular, a la izquierda del botón REINICIAR.
        int totalCorazones = Mathf.Max(1, vidasIniciales);
        corazones = new Image[totalCorazones];
        const float AnchoCorazon = 60f;
        const float SeparacionCorazon = 72f;
        float extra = ControlesTactiles.DebeMostrarse ? 260f : 0f;
        for (int i = 0; i < totalCorazones; i++)
        {
            float x = -(40f + insetDer + extra + AnchoCorazon * 0.5f + (totalCorazones - 1 - i) * SeparacionCorazon);
            Image corazon = CrearImagen(raiz, "Vida" + (i + 1), Color.white, new Vector2(1f, 1f),
                new Vector2(x, -(insetArriba + alturaFranja * 0.5f)), new Vector2(AnchoCorazon, AnchoCorazon * 8f / 9f));
            corazon.sprite = SpriteCorazon();
            corazon.preserveAspect = true;
            corazones[i] = corazon;
        }

        // El mapa se dibuja solo debajo de la franja.
        ReservarCamara(canvas, altura);
        StartCoroutine(VigilarHud());
    }

    /// <summary>La cámara (cenital o la de la fachada) deja libre la franja de arriba para el HUD.</summary>
    private static void ReservarCamara(Canvas canvas, float altura)
    {
        CamaraPisoCenital cenitalCam = Object.FindFirstObjectByType<CamaraPisoCenital>();
        if (cenitalCam != null) cenitalCam.ReservarFranjaSuperior(canvas, altura);
        CamaraSeguimiento lateralCam = Object.FindFirstObjectByType<CamaraSeguimiento>();
        if (lateralCam != null) lateralCam.ReservarFranjaSuperior(canvas, altura);
    }

    /// <summary>Vuelve a medir el HUD cada vez que cambia el tamaño de la ventana.</summary>
    private IEnumerator VigilarHud()
    {
        Vector2Int ultima = new Vector2Int(-1, -1);
        while (true)
        {
            if (Screen.width != ultima.x || Screen.height != ultima.y)
            {
                yield return null;
                yield return null;   // deja que el CanvasScaler aplique el tamaño nuevo
                ultima = new Vector2Int(Screen.width, Screen.height);
                AjustarHud();
            }
            yield return null;
        }
    }

    /// <summary>
    /// Mide en pantalla lo más alto del HUD (textos y corazones). Si queda a menos de 'paddingFijoSuperior'
    /// del borde de arriba, baja el contenido lo que falte y agranda la franja y la reserva de la cámara.
    /// </summary>
    private void AjustarHud()
    {
        if (contenidoHud == null || franjaHud == null || canvasHud == null) return;

        contenidoHud.anchoredPosition = Vector2.zero;
        Canvas.ForceUpdateCanvases();

        float escala = Mathf.Max(0.01f, canvasHud.scaleFactor);
        float tope = float.MinValue;
        Vector3[] esq = new Vector3[4];
        System.Action<RectTransform> medir = rt =>
        {
            if (rt == null) return;
            rt.GetWorldCorners(esq);
            for (int i = 0; i < 4; i++) tope = Mathf.Max(tope, esq[i].y);
        };
        if (textoEstado != null) medir(textoEstado.rectTransform);
        if (textoDestrozo != null) medir(textoDestrozo.rectTransform);
        if (corazones != null) foreach (Image c in corazones) if (c != null) medir(c.rectTransform);

        float limite = Screen.height - paddingFijoSuperior * escala;
        float bajar = tope > limite ? (tope - limite) / escala : 0f;

        contenidoHud.anchoredPosition = new Vector2(0f, -bajar);
        franjaHud.sizeDelta = new Vector2(0f, alturaFranjaBase + bajar);
        ReservarCamara(canvasHud, alturaFranjaBase + bajar);

        Debug.Log(string.Format("[HUD] pantalla {0}x{1}, escala {2:F2}, área segura (y máx) {3:F0}, parte más alta del HUD {4:F0} px, " +
                                "límite {5:F0} px, se baja {6:F1} unidades.",
                                Screen.width, Screen.height, escala, Screen.safeArea.yMax, tope, limite, bajar));
    }

    private static Text CrearTexto(Transform padre, string contenido, Vector2 ancla, Vector2 pivote,
        Vector2 posicion, Vector2 tamano, TextAnchor alineacion, int tamanioFuente, Color color, FontStyle estilo)
    {
        GameObject objeto = new GameObject("Texto", typeof(RectTransform));
        objeto.transform.SetParent(padre, false);

        RectTransform rt = (RectTransform)objeto.transform;
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        Text texto = objeto.AddComponent<Text>();
        texto.font = FuentePredeterminada();
        texto.text = contenido;
        texto.alignment = alineacion;
        texto.fontSize = tamanioFuente;
        texto.fontStyle = estilo;
        texto.color = color;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        texto.raycastTarget = false;

        Outline contorno = objeto.AddComponent<Outline>();
        contorno.effectColor = new Color(0f, 0f, 0f, 0.85f);
        contorno.effectDistance = new Vector2(2f, -2f);

        return texto;
    }

    private static Image CrearImagen(Transform padre, string nombre, Color color, Vector2 ancla,
        Vector2 posicion, Vector2 tamano)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.transform.SetParent(padre, false);

        RectTransform rt = (RectTransform)objeto.transform;
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        Image imagen = objeto.AddComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;
        return imagen;
    }

    /// <summary>Corazón en pixel art (9x8) creado por código: no necesita ningún archivo.</summary>
    private static Sprite SpriteCorazon()
    {
        if (spriteCorazon != null) return spriteCorazon;

        string[] dibujo =
        {
            ".XX...XX.",
            "XwwX.XooX",
            "XwooXoooX",
            "XoooooooX",
            ".XoooooX.",
            "..XoooX..",
            "...XoX...",
            "....X....",
        };

        int ancho = dibujo[0].Length;
        int alto = dibujo.Length;
        Texture2D textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < alto; y++)
        {
            string fila = dibujo[alto - 1 - y];   // la textura empieza abajo
            for (int x = 0; x < ancho; x++)
            {
                Color c;
                switch (fila[x])
                {
                    case 'X': c = new Color(0.12f, 0.08f, 0.1f, 1f); break;
                    case 'w': c = new Color(1f, 1f, 1f, 1f); break;
                    case 'o': c = new Color(0.86f, 0.86f, 0.86f, 1f); break;
                    default: c = new Color(0f, 0f, 0f, 0f); break;
                }
                textura.SetPixel(x, y, c);
            }
        }
        textura.Apply();

        spriteCorazon = Sprite.Create(textura, new Rect(0f, 0f, ancho, alto), new Vector2(0.5f, 0.5f), 100f);
        spriteCorazon.name = "Corazon";
        return spriteCorazon;
    }

    private static Font FuentePredeterminada()
    {
        Font fuente = null;
        try { fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        catch { fuente = null; }

        if (fuente == null)
        {
            try { fuente = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            catch { fuente = null; }
        }
        return fuente;
    }
}