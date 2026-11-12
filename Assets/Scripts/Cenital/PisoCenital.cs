using UnityEngine;

/// <summary>
/// Núcleo del piso visto desde arriba. Hereda de EdificioAlto para que GestorJuego (HUD, vidas,
/// pausa, fin de partida) lo use sin cambios: expone los mismos eventos y cifras.
///
/// Reglas: el piso se completa cuando, tras un mínimo de golpes del demoledor, los 12 objetos
/// están intactos a la vez. Se pierde si el destrozo pasa del límite o el jugador se queda sin vidas.
/// El mapa (Mapa_PisoN) se toma del prefab de la DefinicionPiso, o del que ya esté en la escena.
/// </summary>
[DefaultExecutionOrder(-500)]
public class PisoCenital : EdificioAlto
{
    [Tooltip("Piso que se juega (Assets/Data/Pisos/Piso1_Gastronomia).")]
    public DefinicionPiso definicion;
    [Tooltip("Mapa en la escena. Si está vacío se busca en los hijos o se crea desde el prefab de la definición.")]
    public MapaCenital mapa;
    public CamaraPisoCenital camara;

    private bool finalizado;

    public static PisoCenital Actual { get { return Instancia as PisoCenital; } }

    public DefinicionPiso Definicion { get { return definicion; } }
    public MapaCenital Mapa { get { return mapa; } }
    public override bool Terminado { get { return finalizado; } }
    public override int VentanasPorSeccion { get { return mapa != null ? mapa.objetos.Count : 0; } }
    public float LimiteDerrumbe { get { return definicion != null ? definicion.limiteDerrumbe : 0.7f; } }
    public float RitmoDemoledor { get { return definicion != null ? definicion.ritmoDemoledor : 1f; } }

    protected override void Awake()
    {
        secciones = 1;
        pisosPorSeccion = 1;
        columnas = 1;

        if (mapa == null) mapa = GetComponentInChildren<MapaCenital>();
        if (mapa == null && definicion != null && definicion.prefabMapa != null)
        {
            GameObject m = Instantiate(definicion.prefabMapa, transform);
            m.name = definicion.prefabMapa.name;
            mapa = m.GetComponent<MapaCenital>();
        }
        if (mapa == null)
        {
            Debug.LogError("[PisoCenital] No hay mapa: asigna 'Prefab Mapa' en la DefinicionPiso.");
            base.Awake();
            return;
        }

        base.Awake();   // Instancia = this, y avisa 'Cambio'
    }

    // El mapa se lee en Start (no en Awake): así los colliders ya tienen sus límites físicos.
    // Este script corre antes que los demás (orden -500), así que el jugador, el demoledor y
    // GestorJuego encuentran el mapa listo cuando empiezan.
    private void Start()
    {
        if (mapa == null) return;

        mapa.Preparar(definicion);
        foreach (ObjetoReparable o in mapa.objetos) o.CambioEstado += AlCambiarObjeto;

        if (camara == null) camara = FindFirstObjectByType<CamaraPisoCenital>();
        if (camara != null) camara.Encuadrar(mapa.Limites);
        AvisarCambio();
    }

    protected override int GolpesMinimos(int seccion)
    {
        return definicion != null ? definicion.golpesMinimos : 8;
    }

    private void AlCambiarObjeto(ObjetoReparable o)
    {
        AvisarCambio();
    }

    // ------------------------------------------------------------ consultas
    public override int DanadasEnSeccion(int seccion)
    {
        if (mapa == null) return 0;
        int n = 0;
        foreach (ObjetoReparable o in mapa.objetos) if (o != null && o.Estado != EstadoObjeto.Intacto) n++;
        return n;
    }

    /// <summary>Destrozo (0 a 1): un objeto roto cuenta entero y uno dañado la mitad.</summary>
    public override float DestrozoSeccionActual
    {
        get
        {
            if (Terminado || mapa == null || mapa.objetos.Count == 0) return 0f;
            float suma = 0f;
            foreach (ObjetoReparable o in mapa.objetos)
            {
                if (o == null) continue;
                if (o.Estado == EstadoObjeto.Roto) suma += 1f;
                else if (o.Estado == EstadoObjeto.Danado) suma += 0.5f;
            }
            return suma / mapa.objetos.Count;
        }
    }

    // ------------------------------------------------------------ acciones
    /// <summary>El jugador reparó un objeto: suma puntos en GestorJuego.</summary>
    public void RegistrarReparacion()
    {
        AvisarReparacion();
    }

    public override void Reiniciar()
    {
        finalizado = false;
        if (mapa != null)
        {
            foreach (ObjetoReparable o in mapa.objetos) if (o != null) o.Reiniciar();
        }
        EscombroCenital.EliminarTodos();
        base.Reiniciar();
    }

    protected override void Avanzar()
    {
        // Único piso de esta prueba: completarlo salva el edificio.
        finalizado = true;
        AvisarCambio();
        AvisarSalvado();
    }
}
