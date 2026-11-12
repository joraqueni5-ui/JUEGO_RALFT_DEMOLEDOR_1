using System;
using UnityEngine;

/// <summary>
/// Modo de 6 pisos: extiende al edificio alto original. Cada nivel es el interior de un
/// piso de Unifranz y usa la geometría de UNA sección (3 filas x 5 columnas), por eso
/// este objeto se construye con secciones = 1.
///
/// Al completar un piso se carga la DefinicionPiso siguiente: cambia los sprites (o el
/// tinte provisional), la dificultad y los golpes mínimos. Al completar el último, el
/// edificio queda salvado.
///
/// Como hereda de EdificioAlto, el muñeco, el demoledor, los escombros y la cámara lo
/// usan a través de EdificioAlto.Instancia sin cambios. Solo está activo en el modo
/// pisos; en el modo original este objeto está apagado y no se ejecuta nada de aquí.
/// </summary>
[DefaultExecutionOrder(-500)]
public class EdificioPisos : EdificioAlto
{
    [Header("Pisos")]
    [Tooltip("Los 6 pisos en orden (Assets/Data/Pisos).")]
    public DefinicionPiso[] pisos = new DefinicionPiso[6];
    [Tooltip("Piso con el que arranca (0 = Piso 1). Sirve para probar un piso concreto.")]
    public int pisoInicial;
    [Tooltip("Si está activo, al completar un piso carga el siguiente de inmediato. " +
             "Se apaga cuando la transición entre pisos se encarga de cargarlo.")]
    public bool cargarSiguienteSolo = true;

    /// <summary>Se cargó un piso; el número es el índice (0 = Piso 1).</summary>
    public event Action<int> PisoCargado;
    /// <summary>Se completó un piso; el número es el índice del piso terminado.</summary>
    public event Action<int> PisoCompletado;

    public int PisoActual { get; private set; }
    public int TotalPisos { get { return pisos == null ? 0 : pisos.Length; } }

    private bool finalizado;
    private bool esperandoSiguiente;

    public override bool Terminado { get { return finalizado; } }

    /// <summary>Definición del piso en curso (null si no hay ninguna asignada).</summary>
    public DefinicionPiso DefinicionActual
    {
        get
        {
            if (pisos == null || pisos.Length == 0) return null;
            return pisos[Mathf.Clamp(PisoActual, 0, pisos.Length - 1)];
        }
    }

    public DefinicionPiso DefinicionDe(int indice)
    {
        if (pisos == null || indice < 0 || indice >= pisos.Length) return null;
        return pisos[indice];
    }

    /// <summary>Multiplicador de velocidad del demoledor en este piso.</summary>
    public float RitmoDemoledor
    {
        get { DefinicionPiso d = DefinicionActual; return d != null ? d.ritmoDemoledor : 1f; }
    }

    public int EscombrosPorGolpe
    {
        get { DefinicionPiso d = DefinicionActual; return d != null ? d.escombrosPorGolpe : 1; }
    }

    /// <summary>Destrozo (0 a 1) a partir del cual el edificio se cae en este piso.</summary>
    public float LimiteDerrumbe
    {
        get { DefinicionPiso d = DefinicionActual; return d != null ? d.limiteDerrumbe : 0.7f; }
    }

    protected override void Awake()
    {
        PisoActual = Mathf.Clamp(pisoInicial, 0, Mathf.Max(0, TotalPisos - 1));
        base.Awake();
        AplicarPiso();
    }

    protected override int GolpesMinimos(int seccion)
    {
        DefinicionPiso d = DefinicionActual;
        return d != null ? d.golpesMinimos : base.GolpesMinimos(seccion);
    }

    /// <summary>
    /// Carga un piso: aplica sus sprites o tinte, deja todo intacto y los golpes en cero.
    /// </summary>
    public void CargarPiso(int indice)
    {
        if (TotalPisos == 0) return;

        PisoActual = Mathf.Clamp(indice, 0, TotalPisos - 1);
        finalizado = false;
        esperandoSiguiente = false;
        AplicarPiso();
        Reiniciar();
        if (PisoCargado != null) PisoCargado(PisoActual);
    }

    /// <summary>Vuelve a empezar el piso en curso.</summary>
    public void ReiniciarPiso()
    {
        CargarPiso(PisoActual);
    }

    protected override void Avanzar()
    {
        if (esperandoSiguiente) return;

        int completado = PisoActual;
        if (completado >= TotalPisos - 1)
        {
            finalizado = true;
            AvisarCambio();
            if (PisoCompletado != null) PisoCompletado(completado);
            AvisarSalvado();
            return;
        }

        esperandoSiguiente = true;
        if (PisoCompletado != null) PisoCompletado(completado);
        if (cargarSiguienteSolo) CargarPiso(completado + 1);
    }

    /// <summary>Aplica a objetos y repisas el arte (o el tinte provisional) del piso en curso.</summary>
    private void AplicarPiso()
    {
        DefinicionPiso d = DefinicionActual;
        if (d == null) return;

        Sprite[] plataformas = d.SpritesPlataforma();
        foreach (VentanaEdificio v in ventanas)
        {
            if (v == null) continue;
            Sprite[] s = d.SpritesObjeto(v.tipo);
            v.AplicarSprites(s, s != null ? Color.white : d.tinte);
        }
        foreach (RepisaEdificio r in repisas)
        {
            if (r == null) continue;
            r.AplicarSprites(plataformas, plataformas != null ? Color.white : d.tinte);
        }
    }
}
