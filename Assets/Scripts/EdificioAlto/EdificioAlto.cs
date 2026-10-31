using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// El edificio alto de la Unifranz: 9 pisos x 5 columnas de ventanas repartidos
/// en 3 secciones de 3 pisos. Todas las ventanas empiezan intactas.
///
/// En cada sección el demoledor tiene una cantidad de golpes. Cuando ya los usó
/// todos, no queda ningún escombro cayendo y todas las ventanas de la sección
/// están intactas, la sección se completa y el demoledor sube a la siguiente.
/// Al completar la última sección el edificio queda salvado.
/// </summary>
public class EdificioAlto : MonoBehaviour
{
    public static EdificioAlto Instancia { get; private set; }

    [Header("Rejilla")]
    public int columnas = 5;
    public int pisosPorSeccion = 3;
    public int secciones = 3;

    [Header("Geometría (en unidades, relativa a este objeto)")]
    [Tooltip("Zona de la fachada por donde puede trepar el muñeco.")]
    public Rect fachada = new Rect(0f, 0f, 7.22f, 13.18f);
    [Tooltip("Altura donde se para el demoledor en cada sección (cornisas y, la última, el techo).")]
    public float[] alturaCornisas = { 5.9f, 9.54f, 13.18f };
    [Tooltip("Centro horizontal de cada columna de ventanas.")]
    public float[] centroColumnas = { 0.89f, 2.13f, 3.57f, 4.81f, 6.05f };
    [Tooltip("Inicio y fin de la terraza verde del techo.")]
    public Vector2 terraza = new Vector2(4.82f, 6.8f);

    [Header("Reglas")]
    [Tooltip("Golpes que da el demoledor en cada sección antes de rendirse.")]
    public int[] golpesPorSeccion = { 12, 16, 20 };

    /// <summary>Cualquier cambio de ventana, golpe o sección.</summary>
    public event Action Cambio;
    /// <summary>El muñeco subió el estado de una ventana con un martillazo.</summary>
    public event Action Reparacion;
    /// <summary>Se completó una sección; el número es la NUEVA sección (igual a 'secciones' si ya terminó).</summary>
    public event Action<int> SeccionCompletada;
    /// <summary>Se completaron todas las secciones.</summary>
    public event Action Salvado;

    public int SeccionActual { get; private set; }
    public bool Terminado { get { return SeccionActual >= secciones; } }
    public int GolpesRestantes { get; private set; }
    public int Pisos { get { return pisosPorSeccion * secciones; } }

    /// <summary>Escombros que todavía están cayendo (los cuenta EscombroCayendo).</summary>
    public int EscombrosEnElAire { get; set; }

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
    private VentanaEdificio[,] rejilla;
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
        if (GolpesRestantes > 0 || EscombrosEnElAire > 0) return;
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
    }

    // ------------------------------------------------------------ consultas
    public VentanaEdificio VentanaEn(int piso, int columna)
    {
        if (rejilla == null) return null;
        if (piso < 0 || columna < 0 || piso >= rejilla.GetLength(0) || columna >= rejilla.GetLength(1)) return null;
        return rejilla[piso, columna];
    }

    /// <summary>Ventanas de una columna dentro de una sección, de arriba hacia abajo.</summary>
    public List<VentanaEdificio> ColumnaDeSeccion(int seccion, int columna)
    {
        List<VentanaEdificio> lista = new List<VentanaEdificio>();
        int primero = seccion * pisosPorSeccion;
        for (int p = primero + pisosPorSeccion - 1; p >= primero; p--)
        {
            VentanaEdificio v = VentanaEn(p, columna);
            if (v != null) lista.Add(v);
        }
        return lista;
    }

    public int VentanasPorSeccion { get { return pisosPorSeccion * columnas; } }

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
    /// Destrozo de la sección actual (0 a 1): una ventana destruida cuenta entera
    /// y una rota cuenta la mitad.
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

    /// <summary>Columna más cercana a una x de mundo.</summary>
    public int ColumnaMasCercana(float x)
    {
        int mejor = 0;
        float distancia = float.MaxValue;
        for (int c = 0; c < columnas; c++)
        {
            float d = Mathf.Abs(CentroColumna(c) - x);
            if (d < distancia) { distancia = d; mejor = c; }
        }
        return mejor;
    }

    public float CentroTerraza { get { return transform.position.x + (terraza.x + terraza.y) * 0.5f; } }

    // ------------------------------------------------------------ acciones
    /// <summary>El demoledor pide permiso para dar un golpe (descuenta uno).</summary>
    public bool ConsumirGolpe()
    {
        if (Terminado || GolpesRestantes <= 0) return false;
        GolpesRestantes--;
        if (Cambio != null) Cambio();
        return true;
    }

    /// <summary>
    /// Repara (sube un estado) la ventana dañada más cercana al punto.
    /// Un solo martillazo = una sola ventana. Devuelve la ventana o null.
    /// </summary>
    public VentanaEdificio RepararMasCercano(Vector2 punto, float radio)
    {
        VentanaEdificio mejor = null;
        float mejorDistancia = radio;

        foreach (VentanaEdificio v in ventanas)
        {
            if (v == null || v.Estado == EstadoVentana.Intacta) continue;

            Rect area = v.Area;
            float distancia = area.Contains(punto) ? 0f : Vector2.Distance(punto, area.center);
            if (distancia > mejorDistancia) continue;

            mejorDistancia = distancia;
            mejor = v;
        }

        if (mejor == null) return null;

        mejor.Reparar();
        if (Reparacion != null) Reparacion();
        return mejor;
    }

    /// <summary>Todas las ventanas intactas, primera sección y golpes al inicio.</summary>
    public void Reiniciar()
    {
        reiniciando = true;
        foreach (VentanaEdificio v in ventanas)
        {
            if (v != null) v.Reiniciar();
        }
        reiniciando = false;

        // Los escombros en el aire los borra el gestor antes de llamar aquí.
        EscombrosEnElAire = 0;
        EmpezarSeccion(0);
    }

    private void EmpezarSeccion(int seccion)
    {
        SeccionActual = seccion;
        int i = Mathf.Clamp(seccion, 0, Mathf.Max(0, golpesPorSeccion.Length - 1));
        GolpesRestantes = (Terminado || golpesPorSeccion == null || golpesPorSeccion.Length == 0) ? 0 : golpesPorSeccion[i];
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
}
