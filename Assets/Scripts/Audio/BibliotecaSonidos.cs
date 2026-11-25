using System;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>A qué grupo del mezclador va un sonido.</summary>
public enum BusAudio { Efectos, Interfaz, Musica }

/// <summary>Un sonido de la biblioteca: se busca por nombre, así se cambia el clip o el volumen sin tocar el código.</summary>
[Serializable]
public class EntradaSonido
{
    [Tooltip("Nombre con el que lo pide el juego (el mismo del archivo, por ejemplo paso_1).")]
    public string nombre = "";
    public AudioClip clip;
    [Tooltip("Volumen propio de este sonido (se multiplica por el volumen de Música o Efectos del menú de Opciones).")]
    [Range(0f, 1f)] public float volumen = 1f;
    [Tooltip("Cambia un poco el tono en cada repetición (para pasos, martillazos, golpes, impactos y rocas).")]
    public bool variarTono;
    public BusAudio bus = BusAudio.Efectos;
    [Tooltip("Cuántas copias pueden sonar a la vez.")]
    [Min(1)] public int maximoSimultaneos = 3;
}

/// <summary>
/// Todos los sonidos y la música del juego, asignados por nombre. Vive en Assets/Resources/BibliotecaSonidos.asset (la arma el menú
/// "Demoledor Unifranz / Audio / Armar la biblioteca de sonidos") y GestorAudio la carga sola.
/// </summary>
public class BibliotecaSonidos : ScriptableObject
{
    public const string NombreRecurso = "BibliotecaSonidos";

    [Header("Mezclador (grupos Musica, Efectos e Interfaz)")]
    public AudioMixer mezcla;

    [Header("Ajustes generales")]
    [Tooltip("Variación de tono de los sonidos repetitivos (0,06 = ±6 %).")]
    [Range(0f, 0.2f)] public float variacionTono = 0.06f;
    [Tooltip("Segundos mínimos entre dos repeticiones del mismo sonido.")]
    [Min(0f)] public float separacionMinima = 0.05f;
    [Tooltip("Cuántas fuentes de audio reutilizables hay para efectos.")]
    [Range(4, 48)] public int fuentesDeEfectos = 20;
    [Tooltip("Segundos del fundido al cambiar de pista.")]
    [Min(0.05f)] public float fundidoMusica = 0.8f;
    [Tooltip("Fracción del volumen de la música durante la pausa.")]
    [Range(0f, 1f)] public float musicaEnPausa = 0.4f;

    [Header("Efectos (WAV)")]
    public EntradaSonido[] efectos = new EntradaSonido[0];
    [Header("Música (OGG)")]
    public EntradaSonido[] musica = new EntradaSonido[0];

    public EntradaSonido Buscar(string nombre)
    {
        EntradaSonido e = Buscar(efectos, nombre);
        return e != null ? e : Buscar(musica, nombre);
    }

    private static EntradaSonido Buscar(EntradaSonido[] lista, string nombre)
    {
        if (lista == null) return null;
        foreach (EntradaSonido e in lista) if (e != null && e.nombre == nombre) return e;
        return null;
    }
}

/// <summary>Nombres de los sonidos (los mismos que los archivos), para no escribirlos a mano en cada lugar.</summary>
public static class Snd
{
    // Música
    public const string MusicaMenu = "musica_menu_bucle";
    public const string MusicaPisos = "musica_pisos_bucle";
    public const string MusicaTension = "musica_tension_bucle";
    public const string MusicaNivelFinal = "musica_nivel_final_bucle";
    public const string JingleVictoria = "jingle_victoria";
    public const string JingleDerrota = "jingle_derrota";
    // Jugador
    public const string Paso1 = "paso_1", Paso2 = "paso_2", Martillazo = "martillazo_reparar", ObjetoReparado = "objeto_reparado";
    public const string RecibirDano = "recibir_dano", AturdidoBucle = "aturdido_bucle", Trepar = "trepar", Salto = "salto", CaidaVacio = "caida_vacio";
    // Demoledor
    public const string Punetazo = "punetazo", GolpeObjeto = "golpe_objeto", LevantarBloque = "levantar_bloque", Lanzar = "lanzar";
    public const string ImpactoEscombro = "impacto_escombro", MeRindo = "me_rindo", Agotado = "agotado", Furia = "furia";
    public const string SaltoTerraza = "salto_terraza", CaidaFinal = "caida_final";
    // Ataques y nivel final
    public const string VidrioRoto = "vidrio_roto", AvisoLluvia = "aviso_lluvia_escombros", RocaCayendo = "roca_cayendo";
    public const string Terremoto = "terremoto", DerrumbeVereda = "derrumbe_vereda", Paloma = "paloma";
    // Juego
    public const string PoderAparece = "poder_aparece", PoderRecoger = "poder_recoger", PoderActivoBucle = "poder_activo_bucle";
    public const string LeerPuntoInfo = "leer_punto_info", BloqueoSalvado = "bloqueo_salvado", AscensorAbre = "ascensor_abre";
    public const string Estrella1 = "estrella_1", Estrella2 = "estrella_2", Estrella3 = "estrella_3", ObjetoRotoAlerta = "objeto_roto_alerta";
    // Interfaz
    public const string MenuMover = "menu_mover", MenuConfirmar = "menu_confirmar", MenuVolver = "menu_volver";
    public const string Pausa = "pausa", TransicionLadrillos = "transicion_ladrillos";
}

/// <summary>
/// Sonidos extra (Assets/Resources/SonidosExtra, generados con Tools/generar_sonidos_extra.py). No hace falta agregarlos a la
/// BibliotecaSonidos: si un nombre no está en la biblioteca, GestorAudio lo busca en Resources/SonidosExtra con el volumen de
/// SndExtra.Volumen. Si luego se agregan a la biblioteca, se usan los de la biblioteca.
/// </summary>
public static class SndExtra
{
    public const string Carpeta = "SonidosExtra/";
    // Demoledor
    public const string AlarmaAparicion = "alarma_aparicion", RugidoDemoledor = "rugido_demoledor", PasoDemoledor = "paso_demoledor";
    public const string GrunidoCerca = "grunido_cerca", EmpujonDemoledor = "empujon_demoledor", GolpeFuerteJugador = "golpe_fuerte_jugador";
    // Mini villanos
    public const string MiniLlamada = "mini_llamada", MiniGolpe = "mini_golpe", MiniMordida = "mini_mordida", MiniVencido = "mini_vencido";
    public const string TopadoraMotor = "topadora_motor_bucle";
    // Potenciadores
    public const string PoderTurbo = "poder_turbo", PoderMartillo = "poder_martillo", PoderCongelar = "poder_congelar";
    public const string PoderCasco = "poder_casco", CascoRoto = "casco_roto", Congelado = "congelado";
    // Ambiente (bucles)
    public const string AmbienteUniversidad = "ambiente_universidad", AmbienteTerraza = "ambiente_terraza";
    public const string AmbienteFachada = "ambiente_fachada", AmbienteMenu = "ambiente_menu";
    // Música adaptativa: capa de amenaza que sube cuando el demoledor está cerca
    public const string CapaAmenaza = "capa_amenaza_bucle";
    // Gritos
    public const string GritoMultitud = "grito_multitud", GritoSusto = "grito_susto";
    // Furia del estudiante
    public const string IraLista = "ira_lista", IraActivar = "ira_activar", IraBucle = "ira_bucle", IraOnda = "ira_onda";
    // Movimientos especiales del demoledor
    public const string EmbestidaCarga = "embestida_carga", EmbestidaCorrer = "embestida_correr", ChoqueMuro = "choque_muro";
    public const string PisotonSalto = "pisoton_salto", PisotonImpacto = "pisoton_impacto";
    // Multijugador
    public const string RedConectado = "red_conectado";
    // Peligros de cada piso
    public const string GotaCae = "gota_cae", Resbalon = "resbalon", Chispazo = "chispazo", Apagon = "apagon", LuzVuelve = "luz_vuelve";
    public const string Rafaga = "rafaga";
    // Jefe final
    public const string JefeTransforma = "jefe_transforma", JefeLanza = "jefe_lanza", Desinflar = "desinflar";
    // Villanos nuevos y supervivencia
    public const string Torbellino = "torbellino", LaserRobot = "laser_robot", Oleada = "oleada";
    // Escenarios con vida
    public const string Ding = "ding", Latido = "latido", LibrosCaen = "libros_caen", Reparado = "reparado";
    // Logros e historia
    public const string Logro = "logro", Desbloqueo = "desbloqueo", Despertador = "despertador", PufSueno = "puf_sueno", Ronquido = "ronquido";

    /// <summary>Volumen propio de cada sonido extra (se multiplica por el volumen de Efectos de Opciones).</summary>
    public static float Volumen(string nombre)
    {
        switch (nombre)
        {
            case AlarmaAparicion: return 0.5f;
            case RugidoDemoledor: return 0.9f;
            case PasoDemoledor: return 0.55f;
            case GrunidoCerca: return 0.65f;
            case EmpujonDemoledor: return 0.95f;
            case GolpeFuerteJugador: return 0.8f;
            case MiniLlamada: return 0.45f;
            case MiniGolpe: return 0.5f;
            case MiniMordida: return 0.4f;
            case MiniVencido: return 0.7f;
            case TopadoraMotor: return 0.3f;
            case Congelado: return 0.6f;
            case AmbienteMenu: return 0.45f;
            case AmbienteUniversidad: return 0.75f;
            case AmbienteTerraza: return 0.6f;
            case AmbienteFachada: return 0.6f;
            case CapaAmenaza: return 0.9f;
            case GritoMultitud: return 0.85f;
            case IraBucle: return 0.45f;
            case IraActivar: return 1f;
            case PisotonImpacto: return 1f;
            case ChoqueMuro: return 0.95f;
            case EmbestidaCorrer: return 0.75f;
            case RedConectado: return 0.6f;
            case GotaCae: return 0.45f;
            case Rafaga: return 0.6f;
            case Apagon: return 0.8f;
            case Chispazo: return 0.75f;
            case JefeTransforma: return 1f;
            case Logro: return 0.7f;
            case Desbloqueo: return 0.7f;
            case Despertador: return 0.65f;
            case Ronquido: return 0.55f;
            default: return 0.8f;
        }
    }

    /// <summary>Sonidos repetitivos a los que conviene variar un poco el tono.</summary>
    public static bool VariarTono(string nombre)
    {
        return nombre == PasoDemoledor || nombre == MiniGolpe || nombre == MiniMordida || nombre == GrunidoCerca;
    }
}
