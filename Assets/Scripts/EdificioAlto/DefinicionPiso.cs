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

    [Header("Objetos reparables (4 tipos x 3 estados)")]
    [Tooltip("Hasta 4 tipos de objeto. Un tipo sin sprites usa las ventanas actuales.")]
    public ObjetoPiso[] objetos = new ObjetoPiso[4];

    [Header("Arte del piso (opcional)")]
    [Tooltip("Plataformas en orden: intacta, agrietada, destruida. Lienzo: 40 x 12 px.")]
    public Sprite[] plataformas = new Sprite[3];
    [Tooltip("Fondo del piso. Medida: 704 x 396 px (16:9), 50 px por unidad.")]
    public Sprite fondo;
    [Tooltip("Tinte provisional que se aplica a objetos, plataformas y fondo mientras no haya arte propio.")]
    public Color tinte = Color.white;

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
    public Sprite[] SpritesPlataforma()
    {
        if (plataformas == null || plataformas.Length < 3) return null;
        for (int i = 0; i < 3; i++) if (plataformas[i] == null) return null;
        return plataformas;
    }
}
