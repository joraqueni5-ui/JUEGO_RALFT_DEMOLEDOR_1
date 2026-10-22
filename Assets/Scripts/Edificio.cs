using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Agrupa los bloques del edificio en una rejilla (fila, columna) y ofrece
/// las consultas que necesitan el demoledor automático y el muñeco:
/// qué bloque hay en cada celda, a qué altura está un piso, dónde está una
/// columna y qué bloque se repara de un martillazo.
/// </summary>
public class Edificio : MonoBehaviour
{
    public static Edificio Instancia { get; private set; }

    [Tooltip("Golpes que aguantan TODOS los bloques al iniciar (1 = se rompen de un golpe).")]
    [Range(0, 6)]
    public int golpesParaRomper = 2;

    /// <summary>Se dispara con cualquier cambio de bloque (roto, agrietado o reparado).</summary>
    public event Action Cambio;

    /// <summary>Se dispara solo cuando el muñeco consigue reparar un bloque.</summary>
    public event Action Reparacion;

    public int Columnas { get; private set; }
    public int Filas { get; private set; }
    public int Total { get; private set; }
    public int Demolidos { get; private set; }
    public int Agrietados { get; private set; }
    public int Intactos { get { return Total - Demolidos - Agrietados; } }

    /// <summary>Recuadro que ocupa todo el edificio en el mundo.</summary>
    public Rect Limites { get; private set; }

    /// <summary>0 = todo entero, 1 = todo destruido (un bloque agrietado cuenta medio).</summary>
    public float PorcentajeDestruido
    {
        get { return Total == 0 ? 0f : (Demolidos + Agrietados * 0.5f) / Total; }
    }

    private readonly List<BloqueEdificio> bloques = new List<BloqueEdificio>();
    private BloqueEdificio[,] rejilla;
    private bool reiniciando;

    private void Awake()
    {
        Instancia = this;
        ConstruirRejilla();
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private void ConstruirRejilla()
    {
        bloques.Clear();
        bloques.AddRange(GetComponentsInChildren<BloqueEdificio>(true));

        Columnas = 0;
        Filas = 0;
        foreach (BloqueEdificio bloque in bloques)
        {
            if (bloque == null) continue;
            Columnas = Mathf.Max(Columnas, bloque.columna + 1);
            Filas = Mathf.Max(Filas, bloque.fila + 1);
        }

        rejilla = new BloqueEdificio[Mathf.Max(1, Filas), Mathf.Max(1, Columnas)];

        bool primero = true;
        foreach (BloqueEdificio bloque in bloques)
        {
            if (bloque == null) continue;

            bloque.Preparar();   // asegura su recuadro antes de medir el edificio
            if (golpesParaRomper > 0) bloque.golpesParaRomper = golpesParaRomper;
            bloque.CambioEstado += AlCambiarBloque;

            if (rejilla[bloque.fila, bloque.columna] == null) rejilla[bloque.fila, bloque.columna] = bloque;

            if (primero)
            {
                Limites = bloque.Area;
                primero = false;
            }
            else
            {
                Limites = Rect.MinMaxRect(
                    Mathf.Min(Limites.xMin, bloque.Area.xMin),
                    Mathf.Min(Limites.yMin, bloque.Area.yMin),
                    Mathf.Max(Limites.xMax, bloque.Area.xMax),
                    Mathf.Max(Limites.yMax, bloque.Area.yMax));
            }
        }

        Recontar();
    }

    /// <summary>Bloque que ocupa esa celda (null si la celda está vacía, por ejemplo sobre el techo).</summary>
    public BloqueEdificio BloqueEn(int fila, int columna)
    {
        if (rejilla == null) return null;
        if (fila < 0 || columna < 0 || fila >= rejilla.GetLength(0) || columna >= rejilla.GetLength(1)) return null;
        return rejilla[fila, columna];
    }

    /// <summary>Altura del piso (borde de abajo de los bloques de esa fila).</summary>
    public float AlturaPiso(int fila)
    {
        for (int c = 0; c < Columnas; c++)
        {
            BloqueEdificio bloque = BloqueEn(fila, c);
            if (bloque != null) return bloque.Area.yMin;
        }
        return 0f;
    }

    /// <summary>Centro horizontal de una columna.</summary>
    public float CentroColumna(int columna)
    {
        for (int f = 0; f < Filas; f++)
        {
            BloqueEdificio bloque = BloqueEn(f, columna);
            if (bloque != null) return bloque.Area.center.x;
        }
        return Limites.xMin;
    }

    /// <summary>Golpea un bloque concreto (el demoledor automático usa esto).</summary>
    public bool Golpear(BloqueEdificio bloque, int danio = 1)
    {
        if (bloque == null || bloque.Estado == EstadoBloque.Destruido) return false;
        return bloque.RecibirGolpe(danio);
    }

    /// <summary>
    /// Repara el bloque agrietado/destruido más cercano al punto (uno solo por martillazo).
    /// Devuelve el bloque reparado o null si no había nada a mano.
    /// </summary>
    public BloqueEdificio RepararMasCercano(Vector2 punto, float radio)
    {
        BloqueEdificio mejor = null;
        float mejorDistancia = radio;

        for (int i = 0; i < bloques.Count; i++)
        {
            BloqueEdificio bloque = bloques[i];
            if (bloque == null || bloque.Estado == EstadoBloque.Intacto) continue;

            float distancia = bloque.Area.Contains(punto) ? 0f : Vector2.Distance(punto, bloque.Area.center);
            if (distancia > mejorDistancia) continue;

            mejorDistancia = distancia;
            mejor = bloque;
        }

        if (mejor == null) return null;

        mejor.Reparar();
        if (Reparacion != null) Reparacion();
        return mejor;
    }

    /// <summary>Devuelve el edificio a su estado inicial (avisa una sola vez al final).</summary>
    public void Reiniciar()
    {
        reiniciando = true;
        for (int i = 0; i < bloques.Count; i++)
        {
            if (bloques[i] != null) bloques[i].Reiniciar();
        }
        reiniciando = false;

        Recontar();
        if (Cambio != null) Cambio();
    }

    private void AlCambiarBloque(BloqueEdificio bloque)
    {
        if (reiniciando) return;

        Recontar();
        if (Cambio != null) Cambio();
    }

    private void Recontar()
    {
        Total = 0;
        Demolidos = 0;
        Agrietados = 0;

        for (int i = 0; i < bloques.Count; i++)
        {
            BloqueEdificio bloque = bloques[i];
            if (bloque == null) continue;

            Total++;
            switch (bloque.Estado)
            {
                case EstadoBloque.Destruido: Demolidos++; break;
                case EstadoBloque.Agrietado: Agrietados++; break;
            }
        }
    }
}
