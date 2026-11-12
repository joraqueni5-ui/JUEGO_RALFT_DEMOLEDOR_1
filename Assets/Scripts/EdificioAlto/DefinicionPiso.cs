using System;
using UnityEngine;

/// <summary>
/// Un objeto reparable de un piso (horno, monitor, etc.) con sus tres estados.
/// </summary>
[Serializable]
public class ObjetoPiso
{
    [Tooltip("Nombre del objeto, solo para identificarlo (Horno, Monitor...).")]
    public string nombre = "Objeto";
    [Tooltip("Sprites en orden: intacto, roto, destruido. Lienzo recomendado: 75 x 64 px, pivote arriba a la izquierda.")]
    public Sprite[] sprites = new Sprite[3];
}

/// <summary>Un laboratorio del piso (mitad izquierda o derecha) con su color provisional.</summary>
[Serializable]
public class LaboratorioPiso
{
    public string nombre = "Laboratorio";
    [Tooltip("Color provisional de los objetos y el suelo de este laboratorio mientras no haya sprites.")]
    public Color color = Color.white;
}

/// <summary>
/// Un objeto reparable del piso visto desde arriba. El mapa (prefab) coloca el objeto y
/// le pone el mismo 'indice' que tiene aquí; el arte y el tamaño salen de esta ficha.
/// </summary>
[Serializable]
public class ObjetoCenital
{
    public string nombre = "Objeto";
    [Tooltip("0 = laboratorio izquierdo, 1 = laboratorio derecho.")]
    [Range(0, 1)] public int laboratorio;
    [Tooltip("Tamaño en baldosas (1 baldosa = 32 px). Es el tamaño del collider y de la figura provisional.")]
    public Vector2Int tamanoBaldosas = new Vector2Int(2, 2);
    [Tooltip("Sprites en orden: intacto, dañado, roto. Opcional: sin ellos se usa una figura de color.")]
    public Sprite[] sprites = new Sprite[3];

    /// <summary>Los 3 sprites, o null si falta alguno.</summary>
    public Sprite[] SpritesCompletos()
    {
        if (sprites == null || sprites.Length < 3) return null;
        for (int i = 0; i < 3; i++) if (sprites[i] == null) return null;
        return sprites;
    }
}

/// <summary>Cuadros de animación de un personaje visto desde arriba. Todo es opcional.</summary>
[Serializable]
public class AnimacionesCenital
{
    [Header("Caminar / quieto (la izquierda se voltea de la derecha)")]
    public Sprite[] caminarAbajo;
    public Sprite[] caminarArriba;
    public Sprite[] caminarDerecha;
    [Header("Martillo (jugador) o puñetazo (demoledor)")]
    public Sprite[] golpeAbajo;
    public Sprite[] golpeArriba;
    public Sprite[] golpeDerecha;
    [Header("Otros")]
    [Tooltip("Preparación del lanzamiento (demoledor) o cuadros de daño (jugador).")]
    public Sprite[] especial;

    /// <summary>Menor lado (px) que debe tener un cuadro para considerarse un personaje y no una paleta.</summary>
    public const float LadoMinimoPx = 12f;

    /// <summary>Vacía las series que tengan algún sprite diminuto (por ejemplo una tira de paleta de colores).</summary>
    public void QuitarInvalidos()
    {
        caminarAbajo = Validos(caminarAbajo, "caminarAbajo");
        caminarArriba = Validos(caminarArriba, "caminarArriba");
        caminarDerecha = Validos(caminarDerecha, "caminarDerecha");
        golpeAbajo = Validos(golpeAbajo, "golpeAbajo");
        golpeArriba = Validos(golpeArriba, "golpeArriba");
        golpeDerecha = Validos(golpeDerecha, "golpeDerecha");
        especial = Validos(especial, "especial");
    }

    private static Sprite[] Validos(Sprite[] serie, string nombre)
    {
        if (serie == null) return null;
        foreach (Sprite s in serie)
        {
            if (s == null || s.rect.width < LadoMinimoPx || s.rect.height < LadoMinimoPx)
            {
                Debug.LogWarning("[Cenital] La serie '" + nombre + "' tiene un sprite diminuto o vacío" +
                                 (s != null ? " (" + s.name + ", " + s.rect.width + "x" + s.rect.height + " px)" : "") +
                                 ": parece una paleta de colores. Se ignora esa serie.");
                return null;
            }
        }
        return serie;
    }
}

/// <summary>
/// Datos de un piso de Unifranz para el modo de 6 niveles: nombre, carrera, arte,
/// dificultad y ficha informativa. Todo el arte es opcional: si falta, el juego
/// usa las ventanas y repisas actuales con el tinte del piso.
/// </summary>
[CreateAssetMenu(fileName = "Piso", menuName = "Demoledor Unifranz/Definición de piso", order = 0)]
public class DefinicionPiso : ScriptableObject
{
    [Header("Identidad")]
    [Tooltip("Número del piso (1 a 6).")]
    public int numero = 1;
    public string nombre = "Piso";
    [Tooltip("Carrera que se representa en este piso.")]
    public string carrera = "";

    [Header("Mapa (vista desde arriba)")]
    [Tooltip("Prefab del mapa de este piso (Mapa_PisoN): Tilemaps, objetos, ventanal y ascensor. " +
             "El equipo lo edita a mano; el A* lee el mapa tal como quede.")]
    public GameObject prefabMapa;
    [Tooltip("Tamaño de referencia del mapa en baldosas (22 x 12 = 704 x 384 px).")]
    public Vector2Int tamanoMapa = new Vector2Int(22, 12);
    [Tooltip("Los dos laboratorios: izquierda y derecha.")]
    public LaboratorioPiso[] laboratorios = new LaboratorioPiso[2];

    [Header("Objetos reparables (12: 6 por laboratorio)")]
    public ObjetoCenital[] objetosCenital = new ObjetoCenital[0];

    [Header("Baldosas de referencia (32 x 32 px). El mapa se pinta con las baldosas de su Tile Palette")]
    public Sprite sueloLaboratorioA;
    public Sprite sueloLaboratorioB;
    public Sprite sueloPasillo;
    public Sprite pared;
    public Sprite puerta;
    public Sprite ventanalEntero;
    public Sprite ventanalRoto;
    public Sprite ascensorCerrado;
    public Sprite ascensorAbierto;

    [Header("Animaciones (opcionales)")]
    public AnimacionesCenital jugador = new AnimacionesCenital();
    public AnimacionesCenital demoledor = new AnimacionesCenital();
    [Tooltip("Escombro que lanza el demoledor (16 x 16 px).")]
    public Sprite escombro;
    [Tooltip("Sombra de impacto en el suelo (32 x 16 px).")]
    public Sprite sombraImpacto;

    [Header("Ataques del demoledor")]
    [Tooltip("Segundos de preparación visible del puñetazo.")]
    [Min(0.1f)] public float preparacionPunetazo = 0.5f;
    [Tooltip("Radio (baldosas) del puñetazo: el jugador dentro pierde una vida.")]
    [Min(0.2f)] public float radioPunetazo = 1.0f;
    [Tooltip("Segundos de preparación del lanzamiento.")]
    [Min(0.1f)] public float preparacionLanzamiento = 0.8f;
    [Tooltip("Segundos que tarda el escombro en llegar a su destino.")]
    [Min(0.2f)] public float vueloLanzamiento = 0.9f;
    [Tooltip("Radio (baldosas) del impacto del escombro.")]
    [Min(0.2f)] public float radioImpacto = 1.0f;
    [Tooltip("Cada cuántos segundos como mínimo puede lanzar escombros.")]
    [Min(1f)] public float intervaloLanzamiento = 8f;
    [Tooltip("Si el jugador está más lejos que esto (baldosas), prefiere lanzar.")]
    [Min(1f)] public float distanciaJugadorLejos = 7f;
    [Tooltip("Proporción de ataques que son lanzamiento cuando puede elegir (0 = solo puñetazos, 1 = solo lanzamientos).")]
    [Range(0f, 1f)] public float proporcionLanzamiento = 0.3f;
    [Tooltip("Probabilidad de que el lanzamiento apunte al jugador (si no, a un objeto lejano).")]
    [Range(0f, 1f)] public float lanzarAlJugador = 0.5f;

    [Header("MODO LATERAL (obsoleto, se elimina al terminar el modo cenital)")]
    [Tooltip("Hasta 4 tipos de objeto. Un tipo sin sprites usa las ventanas actuales.")]
    public ObjetoPiso[] objetos = new ObjetoPiso[4];

    [Header("Arte del piso (opcional)")]
    [Tooltip("Plataformas en orden: intacta, agrietada, destruida. Lienzo: 40 x 12 px.")]
    public Sprite[] plataformas = new Sprite[3];
    [Tooltip("Fondo del piso. Medida: 704 x 396 px (16:9), 50 px por unidad.")]
    public Sprite fondo;
    [Tooltip("Tinte provisional que se aplica a objetos, plataformas y fondo mientras no haya arte propio.")]
    public Color tinte = Color.white;
    [Tooltip("Color de la cámara fuera del fondo (barras en pantallas más anchas que 16:9). Con alfa 0 se calcula a partir del tinte.")]
    public Color colorFondo = new Color(0f, 0f, 0f, 0f);

    [Header("Dificultad")]
    [Tooltip("Multiplica la velocidad del demoledor (1 = normal).")]
    [Min(0.1f)] public float ritmoDemoledor = 1f;
    [Tooltip("Golpes que debe dar el demoledor antes de que el piso se pueda completar.")]
    [Min(0)] public int golpesMinimos = 8;
    [Tooltip("Si el destrozo del piso pasa de este valor (0 a 1), el edificio se cae.")]
    [Range(0.3f, 1f)] public float limiteDerrumbe = 0.7f;
    [Tooltip("Escombros que suelta el demoledor en cada golpe.")]
    [Min(1)] public int escombrosPorGolpe = 1;

    [Header("Ficha informativa (se muestra al subir a este piso)")]
    public string fichaTitulo = "";
    [TextArea(2, 5)] public string fichaDescripcion = "";
    [TextArea(1, 3)] public string fichaDatoUtil = "";
    [Tooltip("Ilustración opcional de la ficha.")]
    public Sprite fichaIlustracion;

    /// <summary>"PISO 1 – GASTRONOMÍA", para la transición y el HUD.</summary>
    public string TituloCompleto
    {
        get { return "Piso " + numero + " - " + nombre; }
    }

    /// <summary>Sprites del tipo de objeto pedido, o null si ese tipo no tiene arte propio.</summary>
    public Sprite[] SpritesObjeto(int tipo)
    {
        if (objetos == null || objetos.Length == 0) return null;
        ObjetoPiso o = objetos[Mathf.Abs(tipo) % objetos.Length];
        if (o == null || o.sprites == null || o.sprites.Length < 3) return null;
        for (int i = 0; i < 3; i++) if (o.sprites[i] == null) return null;
        return o.sprites;
    }

    /// <summary>Sprites de plataforma, o null si falta alguno.</summary>
    /// <summary>Objeto de la lista cenital por índice, o null si no existe.</summary>
    public ObjetoCenital ObjetoCenitalDe(int indice)
    {
        if (objetosCenital == null || indice < 0 || indice >= objetosCenital.Length) return null;
        return objetosCenital[indice];
    }

    /// <summary>Laboratorio por índice (0 o 1), o null.</summary>
    public LaboratorioPiso LaboratorioDe(int indice)
    {
        if (laboratorios == null || indice < 0 || indice >= laboratorios.Length) return null;
        return laboratorios[indice];
    }

    public Sprite[] SpritesPlataforma()
    {
        if (plataformas == null || plataformas.Length < 3) return null;
        for (int i = 0; i < 3; i++) if (plataformas[i] == null) return null;
        return plataformas;
    }
}
