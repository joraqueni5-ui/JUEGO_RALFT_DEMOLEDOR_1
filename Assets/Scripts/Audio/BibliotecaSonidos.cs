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
