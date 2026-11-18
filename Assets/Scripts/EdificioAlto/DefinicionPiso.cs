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
    [Tooltip("Color de la figura provisional de este objeto. Con alfa 0 se usa el color de su laboratorio.")]
    public Color colorProvisional = new Color(0f, 0f, 0f, 0f);

    /// <summary>Los 3 sprites, o null si falta alguno.</summary>
    public Sprite[] SpritesCompletos()
    {
        if (sprites == null || sprites.Length < 3) return null;
        for (int i = 0; i < 3; i++) if (sprites[i] == null) return null;
        return sprites;
    }
}

/// <summary>Qué hace un poder al recogerlo.</summary>
public enum TipoPoder { Invulnerabilidad, ReparacionTotal, VidaExtra }

/// <summary>Dónde aparece un poder.</summary>
public enum OrigenPoder
{
    /// <summary>Junto al chef (NPC) del laboratorio de gastronomía, a 'desplazamiento' baldosas de él.</summary>
    JuntoAlChef,
    /// <summary>Frente a un objeto reparable (índice en objetosCenital), a 'desplazamiento' baldosas de su centro.</summary>
    FrenteAlObjeto,
    /// <summary>En una posición fija del mapa: 'desplazamiento' son las coordenadas en baldosas.</summary>
    PuntoFijo,
    /// <summary>En una de las celdas 'P' del mapa de texto.</summary>
    PuntoP,
    /// <summary>En una de las celdas 'Q' del mapa de texto.</summary>
    PuntoQ,
}

/// <summary>
/// Un poder que puede aparecer en el piso (salteña, empanada, botiquín...). Cada piso define los suyos en
/// DefinicionPiso.poderes: qué hace, cuánto dura, dónde aparece y qué mensajes muestra.
/// </summary>
/// <summary>Cuándo aparece un paso del tutorial.</summary>
public enum AparicionTutorial { Siempre, HayObjetoDanado, HayObjetoAmenazado, DemoledorSeFue }

/// <summary>Qué debe hacer el jugador para completar un paso del tutorial.</summary>
public enum CumplirTutorial { Moverse, Reparar, Bloquear, LeerPuntoInfo, EntrarAlAscensor }

/// <summary>Un mensaje del tutorial: aparece una vez y desaparece cuando se cumple.</summary>
[Serializable]
public class PasoTutorial
{
    [TextArea(1, 3)] public string texto = "";
    public AparicionTutorial aparece = AparicionTutorial.Siempre;
    public CumplirTutorial cumple = CumplirTutorial.Moverse;
}

/// <summary>Un punto "?" del piso: dónde está y qué texto muestra.</summary>
[Serializable]
public class PuntoInfoPiso
{
    public string nombre = "Punto";
    [Tooltip("Posición en el mundo (el mapa empieza en 0,0; cada baldosa mide 1).")]
    public Vector2 posicion;
    [Tooltip("Nombre del área en mayúsculas (pestaña del panel DATO ÚTIL).")]
    public string area = "";
    [TextArea(2, 5)] public string texto = "";
    [Tooltip("Línea \"Consejo:\" en dorado, debajo del texto (incluye la palabra Consejo).")]
    [TextArea(1, 3)] public string consejo = "";
}

[Serializable]
public class PoderPiso
{
    public string nombre = "Poder";
    public TipoPoder tipo;
    [Tooltip("Texto del HUD mientras el poder está activo (por ejemplo INVENCIBLE).")]
    public string etiquetaHud = "PODER";
    [Tooltip("Segundos que dura el efecto (no se usa en VidaExtra).")]
    [Min(0f)] public float duracion = 5f;
    [Tooltip("Vidas que da (solo VidaExtra).")]
    [Min(1)] public int valor = 1;
    [Tooltip("Probabilidad relativa de que aparezca este poder frente a los demás.")]
    [Min(0f)] public float peso = 1f;

    [Header("Dónde aparece")]
    public OrigenPoder origen;
    [Tooltip("Reparable de referencia (índice en 'objetosCenital') si el origen es FrenteAlObjeto.")]
    public int indiceObjeto;
    [Tooltip("Nombre del reparable de referencia (FrenteAlObjeto). Si se escribe, tiene prioridad sobre el índice.")]
    public string objetoReferencia = "";
    public Vector2 desplazamiento = new Vector2(0.9f, 0f);

    [Header("Mensajes")]
    public string mensajeAparece = "";
    public string mensajeRecogido = "";

    [Header("Aspecto (provisional si faltan los sprites)")]
    [Tooltip("Letra o símbolo de la figura provisional.")]
    public string simbolo = "?";
    public Color color = Color.white;
    [Tooltip("Color del aro alrededor del jugador mientras el poder está activo.")]
    public Color colorAro = Color.white;
    [Tooltip("Sprite del poder en el suelo (32 x 32 px).")]
    public Sprite sprite;
    [Tooltip("Aro alrededor del jugador (48 x 64 px). Si está vacío se dibuja un aro provisional.")]
    public Sprite spriteAro;
}

/// <summary>Si una pieza de decoración bloquea el paso.</summary>
public enum ColisionDecoracion { Ninguna, Simple }

/// <summary>
/// Un tipo de decoración del piso (banco, planta, ventana, alfombrilla...). No se rompe ni cuenta para el
/// destrozo. Cada pieza del mapa (DecoracionCenital) apunta a un tipo por su número en la lista.
/// </summary>
[Serializable]
public class FichaDecoracion
{
    public string nombre = "Decoración";
    [Tooltip("Tamaño habitual en baldosas (1 baldosa = 32 px). Cada pieza del mapa puede usar otro tamaño.")]
    public Vector2Int tamanoBaldosas = new Vector2Int(1, 1);
    [Tooltip("Arte de la pieza. Si está vacío se dibuja una figura del color provisional.")]
    public Sprite sprite;
    [Tooltip("Color de la figura provisional mientras no haya arte.")]
    public Color colorProvisional = Color.gray;
    [Tooltip("Simple = bloquea el paso (el jugador y el A* lo rodean). Ninguna = solo se ve.")]
    public ColisionDecoracion colision = ColisionDecoracion.Ninguna;
    [Tooltip("Dibuja una sombra suave bajo la pieza.")]
    public bool sombra = true;
    [Tooltip("Se dibuja según su altura en el mapa (delante o detrás del jugador) aunque no tenga colisión.")]
    public bool ordenarPorAltura;
    [Tooltip("La figura provisional es un círculo en vez de un cuadrado.")]
    public bool provisionalCircular;
    [Tooltip("Tamaño de la figura provisional respecto al área de la pieza (0,1 a 1).")]
    [Range(0.1f, 1f)] public float escalaProvisional = 1f;
    [Tooltip("Pieza sobre un muro: se dibuja del grosor de la franja de la pared (por ejemplo una ventana).")]
    public bool ajustarAFranja;
    [Header("Piezas largas (carteles, pizarras)")]
    [Tooltip("Extremo izquierdo, centro (se repite) y extremo derecho para armar una pieza de varias baldosas de largo.")]
    public Sprite spriteIzquierda;
    public Sprite spriteCentro;
    public Sprite spriteDerecha;
    [Tooltip("Arte que se usa solo si la pieza mide exactamente lo que mide el sprite (por ejemplo una banca de 1 x 3 o un cartel de 4 x 1).")]
    public Sprite[] spritesPorTamano = new Sprite[0];
}

/// <summary>Qué representa un carácter del mapa de texto.</summary>
public enum TipoCelda
{
    Pared, ParedDecorada, Puerta, Suelo, Entrada, Ascensor, Jugador, PuntoInfo, Npc, PoderP, PoderQ, Reparable, Decoracion,
    /// <summary>Punto de salto del demoledor en la baranda (solo se ve en el editor).</summary>
    PuntoSalto
}

/// <summary>Cómo se genera la baldosa de un suelo cuando no tiene sprite propio.</summary>
public enum EstiloSuelo { Tablero, Borde, Liso, Pasto }

/// <summary>
/// Un carácter de la leyenda del mapa de texto: qué es, cómo se llama, su arte, su tamaño y si bloquea el paso.
/// Cada piso define la suya; los reparables y la decoración comparten estos campos.
/// </summary>
[Serializable]
public class EntradaLeyenda
{
    [Tooltip("Un solo carácter del mapa.")]
    public string caracter = "";
    public TipoCelda tipo = TipoCelda.Decoracion;
    [Tooltip("Nombre del objeto, zona o NPC (también nombra las baldosas de un suelo).")]
    public string nombre = "";
    [Tooltip("Reparable: intacto, dañado, roto. Decoración o suelo: el primero es el arte. Entrada: entera y rota. NPC: cuadros de caminata.")]
    public Sprite[] sprites = new Sprite[0];
    [Tooltip("Letras iguales y contiguas forman UN solo objeto (por ejemplo UUU = fila de 3 x 1).")]
    public bool unirContiguas = true;
    [Tooltip("Simple = bloquea el paso (el jugador y el A* lo rodean).")]
    public bool colision;
    [Tooltip("Color de la figura provisional mientras no haya arte (en un suelo, el primer color del tablero).")]
    public Color colorProvisional = Color.gray;
    [Tooltip("Suelo: segundo color del tablero o del borde.")]
    public Color colorSecundario = Color.gray;
    [Tooltip("Suelo: cómo se dibuja la baldosa sin sprite.")]
    public EstiloSuelo estilo = EstiloSuelo.Tablero;
    public bool sombra = true;
    [Tooltip("Se dibuja según su altura en el mapa (delante o detrás del jugador) aunque no tenga colisión.")]
    public bool ordenarPorAltura;
    public bool provisionalCircular;
    [Range(0.1f, 1f)] public float escalaProvisional = 1f;
    [Tooltip("Desplazamiento (baldosas) de la pieza respecto a su celda.")]
    public Vector2 desplazamiento;
    [Tooltip("Reparable: laboratorio (0 o 1) para el color provisional. NPC: zona a la que pertenece.")]
    [Range(0, 1)] public int zona;
    [Tooltip("NPC: es el chef (deja los poderes JuntoAlChef).")]
    public bool esChef;
    [Tooltip("Pieza sobre un muro: se dibuja del grosor de la franja de la pared.")]
    public bool ajustarAFranja;
    [Tooltip("Suelo: el borde exterior del mapa junto a esta zona se dibuja como baranda en vez de pared (una terraza).")]
    public bool barandaExterior;
    [Tooltip("Entrada: se rompe al entrar el demoledor (una puerta de vidrio sí, unas escaleras no).")]
    public bool rompible;
    [Header("Piezas largas (carteles, pizarras)")]
    public Sprite spriteIzquierda;
    public Sprite spriteCentro;
    public Sprite spriteDerecha;
    [Tooltip("Decoración: arte que se usa solo si la pieza mide exactamente lo que mide el sprite. " +
             "Reparable: variantes del arte intacto para cada tamaño en baldosas (la que mida lo mismo que el objeto del mapa).")]
    public Sprite[] spritesPorTamano = new Sprite[0];

    public char Letra { get { return string.IsNullOrEmpty(caracter) ? ' ' : caracter[0]; } }
}

/// <summary>Una pieza que se coloca por coordenadas porque no cabe en el mapa de texto (encima de otra, carteles...).</summary>
[Serializable]
public class PiezaExtra
{
    [Tooltip("Carácter de la leyenda (tipo Decoración) que da el aspecto.")]
    public string caracter = "";
    [Tooltip("Esquina inferior izquierda, en baldosas.")]
    public Vector2Int posicion;
    public Vector2Int tamano = new Vector2Int(1, 1);
    public string texto = "";
    public bool enPared;
    public bool encima;
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

    /// <summary>
    /// Mezcla dos juegos de animaciones: gana la serie de 'definicion' si es válida (no vacía y sin sprites
    /// diminutos); si no, se conserva la de 'escena'. Así una serie inválida no borra una buena.
    /// </summary>
    public static AnimacionesCenital Combinar(AnimacionesCenital escena, AnimacionesCenital definicion)
    {
        if (definicion == null) return escena;
        return new AnimacionesCenital
        {
            caminarAbajo = Elegir(definicion.caminarAbajo, escena.caminarAbajo),
            caminarArriba = Elegir(definicion.caminarArriba, escena.caminarArriba),
            caminarDerecha = Elegir(definicion.caminarDerecha, escena.caminarDerecha),
            golpeAbajo = Elegir(definicion.golpeAbajo, escena.golpeAbajo),
            golpeArriba = Elegir(definicion.golpeArriba, escena.golpeArriba),
            golpeDerecha = Elegir(definicion.golpeDerecha, escena.golpeDerecha),
            especial = Elegir(definicion.especial, escena.especial),
        };
    }

    private static Sprite[] Elegir(Sprite[] nuevo, Sprite[] actual)
    {
        if (nuevo == null || nuevo.Length == 0) return actual;
        foreach (Sprite s in nuevo)
            if (s == null || s.rect.width < LadoMinimoPx || s.rect.height < LadoMinimoPx) return actual;
        return nuevo;
    }

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
    [Header("Nombre del área (lo que ve el jugador)")]
    [Tooltip("Nombre del área en mayúsculas para menús, HUD, fichas y pantallas (por ejemplo LABORATORIOS DE INFORMÁTICA). " +
             "Si está vacío se usa el predeterminado de su número.")]
    public string nombreArea = "";

    /// <summary>Nombre del nivel final: la fachada.</summary>
    public const string NombreNivelFinal = "NIVEL FINAL: LA FACHADA";

    private static readonly string[] NombresPorDefecto =
    {
        "PLANTA BAJA", "GASTRONOMÍA Y ENFERMERÍA", "LABORATORIOS DE INFORMÁTICA", "AULAS 300", "AULAS 400", "AULAS 500", "TERRAZA Y BIBLIOTECA",
    };

    /// <summary>Nombre del área de un nivel por su número (0 a 6; 7 = nivel final).</summary>
    public static string NombreAreaDe(int numero)
    {
        if (numero >= 7) return NombreNivelFinal;
        return numero >= 0 ? NombresPorDefecto[numero] : "";
    }

    /// <summary>El nombre del área que ve el jugador (el campo, o el predeterminado de su número).</summary>
    public string NombreArea { get { return string.IsNullOrWhiteSpace(nombreArea) ? NombreAreaDe(numero) : nombreArea.ToUpperInvariant(); } }

    [Header("Escenas")]
    [Tooltip("Nombre de la escena de este nivel (Assets/Scenes/Niveles). Vacío = la predeterminada de su número.")]
    public string nombreEscena = "";
    [Tooltip("Nombre de la escena que sigue al superar este nivel. Vacío = la predeterminada (el último piso sigue con el nivel final).")]
    public string escenaSiguiente = "";

    /// <summary>Escenas de los niveles, por número (0 a 6 = pisos; 7 = nivel final).</summary>
    public static readonly string[] EscenasPorDefecto =
    {
        "Piso0_PlantaBaja", "Piso1_GastronomiaEnfermeria", "Piso2_Informatica", "Piso3_Aulas300", "Piso4_Aulas400",
        "Piso5_Aulas500", "Piso6_TerrazaBiblioteca", "NivelFinal_Fachada",
    };

    public static string NombreEscenaDe(int numero)
    {
        return EscenasPorDefecto[Mathf.Clamp(numero, 0, EscenasPorDefecto.Length - 1)];
    }

    /// <summary>La escena de este nivel (el campo, o la predeterminada de su número).</summary>
    public string NombreEscena { get { return string.IsNullOrWhiteSpace(nombreEscena) ? NombreEscenaDe(numero) : nombreEscena.Trim(); } }

    /// <summary>La escena que sigue a este nivel (el campo, o la predeterminada).</summary>
    public string EscenaSiguiente { get { return string.IsNullOrWhiteSpace(escenaSiguiente) ? NombreEscenaDe(numero + 1) : escenaSiguiente.Trim(); } }

    [Header("Identidad")]
    [Tooltip("Número del piso (0 = planta baja, 1 a 6).")]
    public int numero = 1;
    [Tooltip("Si el piso se puede jugar. Un piso no disponible se muestra como PRÓXIMAMENTE.")]
    public bool disponible = true;
    public string nombre = "Piso";
    [Tooltip("Carrera que se representa en este piso.")]
    public string carrera = "";

    [Header("Mapa (vista desde arriba)")]
    [Tooltip("Prefab del mapa de este piso (Mapa_PisoN): Tilemaps, objetos, ventanal y ascensor. " +
             "El equipo lo edita a mano; el A* lee el mapa tal como quede.")]
    public GameObject prefabMapa;
    [Tooltip("Tamaño del mapa en baldosas. Lo calcula el constructor desde el mapa de texto.")]
    public Vector2Int tamanoMapa = new Vector2Int(22, 12);

    [Header("Mapa de texto (1 carácter = 1 baldosa de 32 px)")]
    [TextArea(14, 40)] public string mapa = "";
    [Tooltip("Qué es cada carácter del mapa. Los reparables de 'objetosCenital' y la decoración de 'decoracion' salen de aquí.")]
    public EntradaLeyenda[] leyenda = new EntradaLeyenda[0];
    [Tooltip("Piezas que no caben en el mapa de texto (encima de otras, carteles...).")]
    public PiezaExtra[] piezasExtra = new PiezaExtra[0];
    [Tooltip("Textura (32 x 32 px, legible) con que se pintan las paredes delgadas. Vacío = el color liso de abajo.")]
    public Sprite texturaPared;
    [Tooltip("Marco de las puertas (32 x 32 px, legible). Vacío = marco liso del color de abajo.")]
    public Sprite spriteMarcoPuerta;
    [Tooltip("Color de las paredes delgadas (si no hay textura).")]
    public Color colorPared = new Color(0.34f, 0.35f, 0.42f, 1f);
    [Tooltip("Color del marco fino de las puertas.")]
    public Color colorMarcoPuerta = new Color(0.45f, 0.30f, 0.18f, 1f);
    [Tooltip("Color de lo que hay fuera del edificio.")]
    public Color colorExterior = new Color(0.07f, 0.07f, 0.1f, 1f);
    [Tooltip("Grosor (px de 32) de la franja de las paredes.")]
    [Range(4, 20)] public int grosorPared = 10;
    [Tooltip("Los dos laboratorios: izquierda y derecha.")]
    public LaboratorioPiso[] laboratorios = new LaboratorioPiso[2];

    [Header("Objetos reparables (12: 6 por laboratorio)")]
    public ObjetoCenital[] objetosCenital = new ObjetoCenital[0];

    [Header("Decoración (no se rompe)")]
    [Tooltip("Tipos de decoración. Cada pieza del mapa (DecoracionCenital) usa uno por su número.")]
    public FichaDecoracion[] decoracion = new FichaDecoracion[0];
    [Tooltip("Caminata del estudiante del laboratorio de Gastronomía (mirando a la derecha). Opcional.")]
    public Sprite[] estudianteGastronomia;
    [Tooltip("Caminata del estudiante del laboratorio de Medicina (mirando a la derecha). Opcional.")]
    public Sprite[] estudianteMedicina;
    [Tooltip("Caminata del chef (mirando a la derecha). Opcional: si falta se usa la del estudiante de Gastronomía.")]
    public Sprite[] chef;

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
    [Min(0.1f)] public float preparacionPunetazo = 0.6f;
    [Tooltip("Radio (baldosas) del puñetazo: el jugador dentro pierde una vida.")]
    [Min(0.2f)] public float radioPunetazo = 1.0f;
    [Tooltip("Segundos de preparación del lanzamiento.")]
    [Min(0.1f)] public float preparacionLanzamiento = 0.8f;
    [Tooltip("Segundos que tarda el escombro en llegar a su destino.")]
    [Min(0.2f)] public float vueloLanzamiento = 0.9f;
    [Tooltip("Radio (baldosas) del impacto del escombro.")]
    [Min(0.2f)] public float radioImpacto = 1.0f;
    [Tooltip("Cada cuántos segundos como mínimo puede lanzar escombros.")]
    [Min(1f)] public float intervaloLanzamiento = 6f;
    [Tooltip("Si el jugador está más lejos que esto (baldosas), prefiere lanzar.")]
    [Min(1f)] public float distanciaJugadorLejos = 7f;
    [Tooltip("Proporción de ataques que son lanzamiento cuando puede elegir (0 = solo puñetazos, 1 = solo lanzamientos).")]
    [Range(0f, 1f)] public float proporcionLanzamiento = 0.3f;
    [Tooltip("Probabilidad de que el lanzamiento apunte al jugador (si no, a un objeto lejano).")]
    [Range(0f, 1f)] public float lanzarAlJugador = 0.5f;

    [Header("Lanzamiento de bloque (con el sprite del bloque grande)")]
    [Tooltip("Segundos de preparación: se agacha y levanta el bloque sobre su cabeza (sustituye a 'preparacionLanzamiento' cuando hay arte del bloque).")]
    [Min(0.1f)] public float preparacionBloque = 0.5f;
    [Tooltip("Altura máxima (baldosas) del arco del bloque.")]
    [Min(0.5f)] public float alturaArcoBloque = 2.5f;
    [Tooltip("Escala del bloque en la parte alta del arco (1 abajo, esta arriba).")]
    [Min(1f)] public float escalaMaximaBloque = 1.3f;
    [Tooltip("Segundos finales en que la sombra de impacto se marca con borde rojo.")]
    [Min(0f)] public float segundosBordeRojo = 0.3f;
    [Tooltip("Fuerza y duración del temblor de cámara al caer un bloque o una roca.")]
    [Min(0f)] public float fuerzaTemblorImpacto = 0.06f;
    [Min(0.05f)] public float duracionTemblorImpacto = 0.15f;

    [Header("Ulti del demoledor: ¡LLUVIA DE ESCOMBROS!")]
    public bool ultiActiva = true;
    [Tooltip("Se activa una vez por nivel cuando la energía del demoledor baja de este valor (0,20 = 20 %).")]
    [Range(0.01f, 0.9f)] public float umbralUlti = 0.20f;
    [Tooltip("Rocas que caen en la lluvia. 0 = según el piso (6 + número de piso: 6 en la planta baja ... 12 en el piso 6).")]
    [Min(0)] public int cantidadRocasUlti = 0;
    [Tooltip("OBSOLETO: el aviso dura 'golpesAvisoUlti' x 'intervaloGolpeUlti' (4 x 0,5 = 2 s).")]
    [Min(0.2f)] public float duracionAvisoUlti = 2f;
    [Tooltip("Golpes al suelo con los dos puños durante el aviso.")]
    [Min(1)] public int golpesAvisoUlti = 4;
    [Tooltip("Segundos entre un golpe al suelo y el siguiente (aviso y lluvia).")]
    [Min(0.2f)] public float intervaloGolpeUlti = 0.5f;
    [Tooltip("Segundos de lluvia: sigue golpeando el suelo cada 'intervaloGolpeUlti' (4 / 0,5 = 8 golpes) y cada golpe suelta la siguiente tanda de rocas.")]
    [Min(1f)] public float duracionLluvia = 4f;
    [Tooltip("Aplastamiento del sprite en cada golpe al suelo (0 = ninguno, 0,2 = se achata un 20 %).")]
    [Range(0f, 0.5f)] public float aplastamientoGolpeUlti = 0.2f;

    /// <summary>Segundos del aviso de la ulti.</summary>
    public float SegundosAvisoUlti { get { return Mathf.Max(1, golpesAvisoUlti) * Mathf.Max(0.2f, intervaloGolpeUlti); } }
    /// <summary>Golpes al suelo durante la lluvia.</summary>
    public int GolpesLluvia { get { return Mathf.Max(1, Mathf.RoundToInt(duracionLluvia / Mathf.Max(0.2f, intervaloGolpeUlti))); } }
    [Tooltip("Segundos que queda agotado después de la lluvia: no ataca y se puede bloquear o rodear.")]
    [Min(0.5f)] public float duracionAgotado = 3f;
    [Tooltip("Segundos que la sombra avisa antes de que caiga cada roca.")]
    [Min(0.3f)] public float avisoCaidaRoca = 1f;
    [Tooltip("Radio (baldosas) del daño de una roca.")]
    [Min(0.2f)] public float radioImpactoRoca = 0.8f;
    [Tooltip("Las rocas \"cerca del jugador\" caen a esta distancia mínima y máxima de él (baldosas).")]
    public Vector2 distanciaRocaJugador = new Vector2(1.5f, 3f);
    [Tooltip("Máximo de sombras activas a menos de 'radioCercaJugador' del jugador (para que siempre pueda esquivar).")]
    [Min(1)] public int maxSombrasCercaJugador = 3;
    [Min(0.5f)] public float radioCercaJugador = 2f;

    /// <summary>Rocas de la lluvia de este piso.</summary>
    public int RocasUlti { get { return cantidadRocasUlti > 0 ? cantidadRocasUlti : 6 + Mathf.Clamp(numero, 0, 6); } }

    [Header("Bloqueo y furia del demoledor")]
    [Tooltip("Segundos que queda aturdido el demoledor cuando el jugador repara el objeto que iba a golpear.")]
    [Min(0.1f)] public float aturdimientoBloqueo = 2f;
    [Tooltip("Puntos que gana el jugador por bloquear un golpe.")]
    [Min(0)] public int puntosBloqueo = 50;
    [Tooltip("Furia: cuando el destrozo del piso (0 a 1) pasa de este valor, el demoledor se enfurece.")]
    [Range(0.05f, 1f)] public float umbralFuria = 0.5f;
    [Tooltip("Furia: multiplica la velocidad al caminar (1,4 = 40 % más rápido).")]
    [Min(1f)] public float velocidadFuria = 1.4f;
    [Tooltip("Furia: cada cuántos segundos puede lanzar escombros (sin furia se usa 'intervaloLanzamiento').")]
    [Min(0.5f)] public float intervaloLanzamientoFuria = 3.5f;

    [Header("Personajes de ambiente")]
    [Tooltip("Muestra a los personajes que caminan por el piso (estudiantes, chef, barista, recepcionista, bibliotecaria). Desactivado: no se ven ni se mueven, pero el punto del chef sigue existiendo para los poderes.")]
    public bool mostrarNPC = false;

    [Header("Poderes")]
    [Tooltip("Poderes que pueden aparecer en este piso. Vacío = sin poderes.")]
    public PoderPiso[] poderes = new PoderPiso[0];
    [Tooltip("Máximo de poderes en el suelo a la vez.")]
    [Min(1)] public int maxPoderesEnPiso = 2;
    [Tooltip("Cada cuántos segundos puede aparecer un poder.")]
    [Min(1f)] public float intervaloPoderes = 8f;
    [Tooltip("Variación aleatoria del intervalo (0,25 = entre 75 % y 125 %).")]
    [Range(0f, 0.9f)] public float variacionIntervaloPoderes = 0.25f;
    [Tooltip("Segundos que un poder espera en el suelo antes de desaparecer.")]
    [Min(1f)] public float duracionPoderEnSuelo = 8f;
    [Tooltip("Los últimos segundos antes de desaparecer el poder parpadea.")]
    [Min(0f)] public float parpadeoFinalPoder = 2f;
    [Tooltip("Radio (baldosas) en el que el jugador recoge el poder.")]
    [Min(0.2f)] public float radioRecogidaPoder = 0.6f;
    [Tooltip("Vidas máximas que se pueden tener (un botiquín no pasa de aquí).")]
    [Min(1)] public int vidasMaximas = 3;

    [Header("Choque con el demoledor")]
    [Tooltip("Distancia (baldosas) que el choque empuja al jugador, en dirección contraria al demoledor.")]
    [Min(0f)] public float empujeChoque = 1f;
    [Tooltip("Segundos que el jugador queda aturdido (sin moverse ni reparar) y sin poder recibir daño.")]
    [Min(0.1f)] public float aturdimientoChoque = 1.5f;
    [Tooltip("Segundos, contados desde el choque, durante los que otro choque no vuelve a aturdirlo.")]
    [Min(0f)] public float esperaChoque = 3f;

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
    [Tooltip("OBSOLETO en el modo cenital: ya no condiciona la victoria (se supera con la energía del demoledor). " +
             "Solo lo usa el modo lateral por pisos.")]
    [Min(0)] public int golpesMinimos = 8;

    [Header("Energía del demoledor (condición para superar el piso)")]
    [Tooltip("Energía con la que empieza el demoledor, en %.")]
    [Range(1f, 100f)] public float energiaInicial = 100f;
    [Tooltip("% de energía que pierde el demoledor con cada reparación del jugador.")]
    [Min(0f)] public float energiaPorReparacion = 4f;
    [Tooltip("% de energía que pierde el demoledor cada vez que el jugador bloquea un golpe.")]
    [Min(0f)] public float energiaPorBloqueo = 12f;

    [Header("Rendición y ascensor")]
    [Tooltip("Segundos que el demoledor muestra '¡ME RINDO!' antes de irse.")]
    [Min(0f)] public float pausaRendicion = 1.5f;
    [Tooltip("Multiplica la velocidad del demoledor mientras sale caminando hacia el ventanal.")]
    [Min(0.2f)] public float velocidadSalida = 1.3f;
    [Tooltip("Tiempo máximo (s) que se espera a que salga caminando; pasado ese tiempo sale en línea recta.")]
    [Min(2f)] public float tiempoMaximoSalida = 12f;
    [Tooltip("Color del parpadeo del ascensor cuando se abre (si no hay sprite de ascensor abierto, es el color de todo).")]
    public Color colorAscensorAbierto = new Color(0.3f, 1f, 0.45f, 1f);
    [Tooltip("Mensaje al abrirse el ascensor.")]
    public string mensajeAscensor = "¡El ascensor está abierto!";
    [Tooltip("Piso al que se sube desde el ascensor (para mostrar su ficha 'Subiendo al piso N'). Vacío = termina la partida.")]
    public DefinicionPiso siguientePiso;

    [Header("Tutorial (solo los pisos que lo definan)")]
    [Tooltip("Mensajes que aparecen una vez y desaparecen al cumplirse.")]
    public PasoTutorial[] pasosTutorial = new PasoTutorial[0];
    [Tooltip("Muestra la ficha del piso al empezar (con CONTINUAR), por ejemplo la bienvenida de la planta baja.")]
    public bool mostrarFichaAlEmpezar;
    [Tooltip("Título de la ventana de la ficha (si está vacío, '¡SUBIENDO!').")]
    public string tituloVentanaFicha = "";

    [Header("Puntos de información y estrellas")]
    [Tooltip("Puntos '?' del piso: uno en cada laboratorio y uno en el pasillo.")]
    public PuntoInfoPiso[] puntosInfo =
    {
        new PuntoInfoPiso { nombre = "Laboratorio de Gastronomía", posicion = new Vector2(2.5f, 6.5f),
            texto = "Texto provisional 1: aquí irá un dato sobre la carrera de Gastronomía." },
        new PuntoInfoPiso { nombre = "Pasillo", posicion = new Vector2(12.5f, 3.5f),
            texto = "Texto provisional 2: aquí irá un dato sobre el piso y la universidad." },
        new PuntoInfoPiso { nombre = "Laboratorio de Medicina", posicion = new Vector2(15.5f, 6.5f),
            texto = "Texto provisional 3: aquí irá un dato sobre la carrera de Medicina." },
    };
    [Tooltip("Puntos que da cada punto '?' la primera vez que se pisa.")]
    [Min(0)] public int puntosPorInfo = 20;
    [Tooltip("Segundos que se muestra el texto de un punto '?'.")]
    [Min(0.5f)] public float duracionTextoInfo = 3.5f;
    [Tooltip("Distancia (baldosas) a la que el jugador activa un punto '?'.")]
    [Range(0.3f, 2f)] public float radioInfo = 0.8f;
    [Header("Panel DATO ÚTIL")]
    [Tooltip("Tiempo de lectura = base + segundosPorCaracter x (caracteres del texto y del consejo), entre el mínimo y el máximo.")]
    [Min(0f)] public float segundosBaseDato = 4f;
    [Min(0f)] public float segundosPorCaracterDato = 0.06f;
    [Min(1f)] public float minimoSegundosDato = 8f;
    [Min(1f)] public float maximoSegundosDato = 14f;
    [Tooltip("Mientras el jugador esté a esta distancia (casillas) del \"?\", el tiempo no corre y el panel sigue abierto.")]
    [Min(0.5f)] public float distanciaMantenerPanelDato = 1.5f;
    [Tooltip("Si el jugador se aleja más de esta distancia (casillas), el panel se cierra tras \"esperaCerrarPanelDato\" segundos.")]
    [Min(0.5f)] public float distanciaCerrarPanelDato = 2f;
    [Min(0f)] public float esperaCerrarPanelDato = 1.5f;
    [Tooltip("Opacidad del panel mientras el jugador o el demoledor quedan detrás de él.")]
    [Range(0.1f, 1f)] public float opacidadPanelTapando = 0.45f;
    [Tooltip("Datos de reserva (área, texto, consejo) para \"?\" que sobren en el mapa. Se usan en orden; si sobran, se conservan.")]
    public PuntoInfoPiso[] datosReserva = new PuntoInfoPiso[0];
    [Tooltip("Color de la pestaña con el nombre del área. Con alfa 0 se usa el color del piso (0 azul, 1 naranja, 2 verde, 3 morado, 4 rojo, 5 celeste, 6 verde claro).")]
    public Color colorPestanaDato = new Color(0f, 0f, 0f, 0f);

    private static readonly string[] ColoresPestana = { "#2F80ED", "#F2994A", "#27AE60", "#9B51E0", "#EB5757", "#2D9CDB", "#6FCF97" };

    /// <summary>Color de la pestaña del panel DATO ÚTIL en este piso.</summary>
    public Color ColorPestana
    {
        get
        {
            if (colorPestanaDato.a > 0.01f) return colorPestanaDato;
            Color c;
            return ColorUtility.TryParseHtmlString(ColoresPestana[Mathf.Clamp(numero, 0, ColoresPestana.Length - 1)], out c) ? c : Color.white;
        }
    }
    [Tooltip("Segunda estrella: el destrozo (0 a 1) al rendirse el demoledor debe ser menor que este valor.")]
    [Range(0.05f, 1f)] public float destrozoMaximoEstrella = 0.3f;
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

    /// <summary>Tipo de decoración por índice, o null si no existe.</summary>
    public FichaDecoracion DecoracionDe(int indice)
    {
        if (decoracion == null || indice < 0 || indice >= decoracion.Length) return null;
        return decoracion[indice];
    }

    /// <summary>Entrada de la leyenda para un carácter, o null.</summary>
    public EntradaLeyenda LeyendaDe(char c)
    {
        if (leyenda == null) return null;
        foreach (EntradaLeyenda e in leyenda) if (e != null && e.Letra == c) return e;
        return null;
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
