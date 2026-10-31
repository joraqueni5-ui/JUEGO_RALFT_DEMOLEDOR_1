using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// El edificio alto de la Unifranz: 9 pisos x 5 columnas de ventanas, cada una con
/// su repisa pisable debajo, repartidos en 3 secciones de 3 pisos.
/// Todas las ventanas y repisas empiezan intactas.
///
/// El demoledor no deja de golpear. Una sección se completa cuando, después de un
/// mínimo de golpes, las 15 ventanas de esa sección están intactas al mismo tiempo.
/// Entonces el demoledor sube a la siguiente. Al completar la última, el edificio
/// queda salvado. Las repisas de secciones más arriba que la del demoledor no se
/// pueden pisar: así el muñeco no lo pasa.
/// </summary>
public class EdificioAlto : MonoBehaviour
{
    public static EdificioAlto Instancia { get; private set; }

    [Header("Rejilla")]
    public int columnas = 5;
    public int pisosPorSeccion = 3;
    public int secciones = 3;

    [Header("Geometría (en unidades, relativa a este objeto)")]
    [Tooltip("Zona de la fachada del edificio.")]
    public Rect fachada = new Rect(0f, 0f, 7.22f, 13.18f);
    [Tooltip("Altura donde se para el demoledor en cada sección (cornisas y, la última, el techo).")]
    public float[] alturaCornisas = { 5.9f, 9.54f, 13.18f };
    [Tooltip("Centro horizontal de cada columna de ventanas.")]
    public float[] centroColumnas = { 0.89f, 2.13f, 3.57f, 4.81f, 6.05f };
    [Tooltip("Inicio y fin de la terraza verde del techo.")]
    public Vector2 terraza = new Vector2(4.82f, 6.8f);

    [Header("Reglas")]
    [Tooltip("Golpes que da el demoledor en cada sección antes de que se pueda completar.")]
    public int[] golpesMinimosPorSeccion = { 8, 10, 12 };

    /// <summary>Cualquier cambio de ventana, repisa, golpe o sección.</summary>
    public event Action Cambio;
    /// <summary>El muñeco reparó algo con un martillazo.</summary>
    public event Action Reparacion;
    /// <summary>Se completó una sección; el número es la NUEVA sección (igual a 'secciones' si ya terminó).</summary>
    public event Action<int> SeccionCompletada;
    /// <summary>Se completaron todas las secciones.</summary>
    public event Action Salvado;

    public int SeccionActual { get; private set; }
    public bool Terminado { get { return SeccionActual >= secciones; } }
    public int GolpesDados { get; private set; }
    public int Pisos { get { return pisosPorSeccion * secciones; } }
    public int VentanasPorSeccion { get { return pisosPorSeccion * columnas; } }

    /// <summary>Zona de la fachada en coordenadas de mundo.</summary>
    public Rect Limites
    {
        get
        {
            Vector2 p = transform.position;
            return new Rect(p + fachada.position, fachada.size);
        }
    }

    private readonly List<VentanaEdificio> ventanas = new List<VentanaEdificio>();
    private readonly List<RepisaEdificio> repisas = new List<RepisaEdificio>();
    private VentanaEdificio[,] rejilla;
    private RepisaEdificio[,] rejillaRepisas;
    private bool reiniciando;

    private void Awake()
    {
        Instancia = this;
        ConstruirRejilla();
        EmpezarSeccion(0);
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private void Update()
    {
        if (Terminado) return;
        if (GolpesDados < GolpesMinimos(SeccionActual)) return;
        if (DanadasEnSeccion(SeccionActual) > 0) return;

        Avanzar();
    }

    private void ConstruirRejilla()
    {
        ventanas.Clear();
        ventanas.AddRange(GetComponentsInChildren<VentanaEdificio>(true));
        rejilla = new VentanaEdificio[Mathf.Max(1, Pisos), Mathf.Max(1, columnas)];
        foreach (VentanaEdificio v in ventanas)
        {
            if (v == null) continue;
            v.Preparar();
            v.CambioEstado += AlCambiarVentana;
            if (v.piso >= 0 && v.piso < Pisos && v.columna >= 0 && v.columna < columnas)
                rejilla[v.piso, v.columna] = v;
        }

        repisas.Clear();
        repisas.AddRange(GetComponentsInChildren<RepisaEdificio>(true));
        rejillaRepisas = new RepisaEdificio[Mathf.Max(1, Pisos), Mathf.Max(1, columnas)];
        foreach (RepisaEdificio r in repisas)
        {
            if (r == null) continue;
            r.Preparar();
            r.CambioEstado += AlCambiarRepisa;
            if (r.piso >= 0 && r.piso < Pisos && r.columna >= 0 && r.columna < columnas)
                rejillaRepisas[r.piso, r.columna] = r;
        }
    }

    // ------------------------------------------------------------ consultas
    public VentanaEdificio VentanaEn(int piso, int columna)
    {
        if (rejilla == null) return null;
        if (piso < 0 || columna < 0 || piso >= rejilla.GetLength(0) || columna >= rejilla.GetLength(1)) return null;
        return rejilla[piso, columna];
    }

    public RepisaEdificio RepisaEn(int piso, int columna)
    {
        if (rejillaRepisas == null) return null;
        if (piso < 0 || columna < 0 || piso >= rejillaRepisas.GetLength(0) || columna >= rejillaRepisas.GetLength(1)) return null;
        return rejillaRepisas[piso, columna];
    }

    /// <summary>Ventanas de la sección que no están intactas.</summary>
    public int DanadasEnSeccion(int seccion)
    {
        int cuenta = 0;
        foreach (VentanaEdificio v in ventanas)
        {
            if (v != null && v.seccion == seccion && v.Estado != EstadoVentana.Intacta) cuenta++;
        }
        return cuenta;
    }

    /// <summary>
    /// Destrozo de la sección actual (0 a 1), solo con las ventanas: una destruida
    /// cuenta entera y una rota cuenta la mitad.
    /// </summary>
    public float DestrozoSeccionActual
    {
        get
        {
            if (Terminado) return 0f;
            float suma = 0f;
            int total = 0;
            foreach (VentanaEdificio v in ventanas)
            {
                if (v == null || v.seccion != SeccionActual) continue;
                total++;
                if (v.Estado == EstadoVentana.Destruida) suma += 1f;
                else if (v.Estado == EstadoVentana.Rota) suma += 0.5f;
            }
            return total == 0 ? 0f : suma / total;
        }
    }

    /// <summary>Altura (mundo) donde se paran los pies del demoledor en esa sección.</summary>
    public float AlturaCornisa(int seccion)
    {
        if (alturaCornisas == null || alturaCornisas.Length == 0) return transform.position.y;
        int i = Mathf.Clamp(seccion, 0, alturaCornisas.Length - 1);
        return transform.position.y + alturaCornisas[i];
    }

    /// <summary>Centro horizontal (mundo) de una columna.</summary>
    public float CentroColumna(int columna)
    {
        if (centroColumnas == null || centroColumnas.Length == 0) return transform.position.x;
        int i = Mathf.Clamp(columna, 0, centroColumnas.Length - 1);
        return transform.position.x + centroColumnas[i];
    }

    public float CentroTerraza { get { return transform.position.x + (terraza.x + terraza.y) * 0.5f; } }

    /// <summary>Altura (mundo) de la primera fila de repisas de una sección: es su "piso".</summary>
    public float PisoSeccion(int seccion)
    {
        int s = Mathf.Clamp(seccion, 0, secciones - 1);
        RepisaEdificio r = RepisaEn(s * pisosPorSeccion, 0);
        return r != null ? r.Superficie : transform.position.y;
    }

    /// <summary>
    /// Repisa donde reaparecer en una sección: la de la primera fila más cercana a x
    /// que todavía se pueda pisar (o cualquiera de la sección si esa fila está rota).
    /// </summary>
    public RepisaEdificio RepisaSegura(int seccion, float x)
    {
        int s = Mathf.Clamp(seccion, 0, secciones - 1);
        for (int p = s * pisosPorSeccion; p < (s + 1) * pisosPorSeccion; p++)
        {
            RepisaEdificio mejor = null;
            float distancia = float.MaxValue;
            for (int c = 0; c < columnas; c++)
            {
                RepisaEdificio r = RepisaEn(p, c);
                if (r == null || !r.SePuedePisar) continue;
                float d = Mathf.Abs(r.CentroX - x);
                if (d < distancia) { distancia = d; mejor = r; }
            }
            if (mejor != null) return mejor;
        }
        return null;
    }

    private int GolpesMinimos(int seccion)
    {
        if (golpesMinimosPorSeccion == null || golpesMinimosPorSeccion.Length == 0) return 0;
        return golpesMinimosPorSeccion[Mathf.Clamp(seccion, 0, golpesMinimosPorSeccion.Length - 1)];
    }

    // ------------------------------------------------------------ acciones
    /// <summary>El demoledor avisa que dio un golpe en la sección actual.</summary>
    public void RegistrarGolpe()
    {
        if (Terminado) return;
        GolpesDados++;
    }

    /// <summary>
    /// Un martillazo: sube un estado la ventana dañada más cercana al pecho; si no hay
    /// ninguna a mano, repara la repisa agrietada más cercana. Devuelve true si reparó algo.
    /// </summary>
    public bool RepararMasCercano(Vector2 pecho, float radio)
    {
        VentanaEdificio mejorVentana = null;
        float mejorDistancia = radio;
        foreach (VentanaEdificio v in ventanas)
        {
            if (v == null || v.Estado == EstadoVentana.Intacta) continue;
            Rect area = v.Area;
            float d = area.Contains(pecho) ? 0f : Vector2.Distance(pecho, area.center);
            if (d > mejorDistancia) continue;
            mejorDistancia = d;
            mejorVentana = v;
        }

        if (mejorVentana != null)
        {
            mejorVentana.Reparar();
            if (Reparacion != null) Reparacion();
            return true;
        }

        RepisaEdificio mejorRepisa = null;
        mejorDistancia = radio + 0.15f;
        foreach (RepisaEdificio r in repisas)
        {
            if (r == null || r.Estado != EstadoRepisa.Agrietada) continue;
            float d = Vector2.Distance(pecho, r.Area.center);
            if (d > mejorDistancia) continue;
            mejorDistancia = d;
            mejorRepisa = r;
        }

        if (mejorRepisa != null)
        {
            mejorRepisa.Reparar();
            if (Reparacion != null) Reparacion();
            return true;
        }
        return false;
    }

    /// <summary>Todo intacto, primera sección y golpes a cero.</summary>
    public void Reiniciar()
    {
        reiniciando = true;
        foreach (VentanaEdificio v in ventanas) if (v != null) v.Reiniciar();
        foreach (RepisaEdificio r in repisas) if (r != null) r.Reiniciar();
        reiniciando = false;

        EmpezarSeccion(0);
    }

    private void EmpezarSeccion(int seccion)
    {
        SeccionActual = seccion;
        GolpesDados = 0;
        foreach (RepisaEdificio r in repisas)
        {
            if (r != null) r.Habilitada = r.seccion <= seccion;
        }
        if (Cambio != null) Cambio();
    }

    private void Avanzar()
    {
        EmpezarSeccion(SeccionActual + 1);
        if (SeccionCompletada != null) SeccionCompletada(SeccionActual);
        if (Terminado && Salvado != null) Salvado();
    }

    private void AlCambiarVentana(VentanaEdificio ventana)
    {
        if (reiniciando) return;
        if (Cambio != null) Cambio();
    }

    private void AlCambiarRepisa(RepisaEdificio repisa)
    {
        if (reiniciando) return;
        if (Cambio != null) Cambio();
    }
}
