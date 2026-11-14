using UnityEngine;

/// <summary>
/// Sprites que el código necesita en tiempo de juego (efectos, grietas, interfaz y piezas del nivel final).
/// Es un recurso (Assets/Resources/ArteNiveles.asset) ya asignado a los sprites del proyecto; así los PNG originales no se
/// mueven y el juego compilado los incluye. Todo es opcional: si un sprite falta, el juego sigue con su figura provisional.
/// </summary>
public class ArteNiveles : ScriptableObject
{
    public const string NombreRecurso = "ArteNiveles";

    private static ArteNiveles actual;
    private static bool buscado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar() { Recargar(); }

    /// <summary>Vuelve a buscar el recurso (el editor lo llama al crearlo o cambiarlo).</summary>
    public static void Recargar() { actual = null; buscado = false; }

    /// <summary>El recurso cargado (null si todavía no se ejecutó el menú de arte).</summary>
    public static ArteNiveles Actual
    {
        get
        {
            if (actual == null && !buscado)
            {
                actual = Resources.Load<ArteNiveles>(NombreRecurso);
                buscado = true;
            }
            return actual;
        }
    }

    [Header("Figuras provisionales guardadas (cuadrado y círculo blancos)")]
    public Sprite cuadrado;
    public Sprite circulo;

    [Header("Grietas sobre los reparables")]
    public Sprite grietasChica;
    public Sprite grietasMediana;
    public Sprite grietasGrande;

    [Header("Efectos del modo cenital (cuadros en orden)")]
    public Sprite[] escombroLadrillo;
    public Sprite[] trozosEscombro;
    public Sprite[] sombraImpacto;
    public Sprite[] polvoImpacto;
    public Sprite[] golpeEstallido;
    public Sprite[] chispasReparar;
    public Sprite[] estrellasAturdido;
    public Sprite[] zzDormido;
    public Sprite[] aroPoderDorado;
    public Sprite[] aroPoderVerde;
    public Sprite[] destelloPoder;

    [Header("Bloque, lluvia de escombros y avisos (Sprites/Efectos)")]
    public Sprite[] bloqueEscombroGrande;
    public Sprite[] rocaLluvia1;
    public Sprite[] rocaLluvia2;
    public Sprite[] rocaLluvia3;
    public Sprite[] sombraImpactoGrande;
    public Sprite[] polvoImpactoGrande;
    public Sprite[] avisoLluviaEscombros;

    [Header("Nivel final")]
    public Sprite[] polvoDerrumbe;
    public Sprite[] grietaTerremoto;
    public Sprite[] avisoPaloma;
    public Sprite[] palomaVuelo;
    public Sprite vereda;
    public Sprite veredaAgrietada;
    public Sprite[] trozosVereda;
    public Sprite canaletaTrepable;

    [Header("Interfaz")]
    public Sprite puntoInfoPendiente;
    public Sprite puntoInfoLeido;
    public Sprite estrellaLlena16;
    public Sprite estrellaLlena32;
    public Sprite estrellaVacia16;
    public Sprite estrellaVacia32;
    public Sprite corazonLleno;
    public Sprite corazonVacio;
    public Sprite candado;
    public Sprite iconoMartillo;
    public Sprite botonArriba;
    public Sprite botonAbajo;
    public Sprite botonIzquierda;
    public Sprite botonDerecha;
    public Sprite botonReparar;
    public Sprite botonSaltar;
    public Sprite botonPausa;
    [Header("Panel DATO ÚTIL")]
    public Sprite panelDato;
    public Sprite etiquetaArea;
    public Sprite iconoFoco;
    public Sprite libroLeido;
    public Sprite libroPendiente;

    /// <summary>La serie si tiene cuadros válidos; si no, null (así el código usa su figura provisional).</summary>
    public static Sprite[] Serie(Sprite[] cuadros)
    {
        if (cuadros == null || cuadros.Length == 0) return null;
        foreach (Sprite s in cuadros) if (s == null) return null;
        return cuadros;
    }

    /// <summary>Atajo: la serie de un campo del recurso, o null si no hay recurso o está incompleta.</summary>
    public static Sprite[] De(System.Func<ArteNiveles, Sprite[]> campo)
    {
        ArteNiveles a = Actual;
        return a == null ? null : Serie(campo(a));
    }
}
