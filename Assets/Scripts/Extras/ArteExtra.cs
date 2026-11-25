using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sprites extra del juego (mini villanos, potenciadores nuevos, efectos y tema de los menús). Viven en
/// Assets/Resources/Extras (los genera Tools/exportar_extras.py) y se cargan por ruta, así no hace falta
/// tocar ArteNiveles ni las escenas. Todo devuelve null si falta el archivo (el código usa una figura provisional).
/// </summary>
public static class ArteExtra
{
    private const string Raiz = "Extras/";
    private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, Sprite[]> series = new Dictionary<string, Sprite[]>();
    private static readonly Dictionary<Sprite, string> rutas = new Dictionary<Sprite, string>();

    /// <summary>La ruta con que se cargó este sprite (o null si no salió de aquí). La usa el multijugador para pedir
    /// exactamente el mismo sprite en el otro equipo (varios personajes tienen cuadros con el mismo nombre).</summary>
    public static string RutaDe(Sprite s)
    {
        string r;
        return s != null && rutas.TryGetValue(s, out r) ? r : null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        sprites.Clear();
        series.Clear();
        rutas.Clear();
    }

    /// <summary>Un sprite, por ejemplo "Poderes/poder_turbo".</summary>
    public static Sprite Uno(string ruta)
    {
        Sprite s;
        if (sprites.TryGetValue(ruta, out s)) return s;
        s = Resources.Load<Sprite>(Raiz + ruta);
        sprites[ruta] = s;
        if (s != null) rutas[s] = ruta;
        return s;
    }

    /// <summary>Cuadros numerados: Serie("Efectos/humo_poof_", 4) carga humo_poof_0 a humo_poof_3. Null si falta alguno.</summary>
    public static Sprite[] Serie(string prefijo, int cantidad)
    {
        string clave = prefijo + "#" + cantidad;
        Sprite[] r;
        if (series.TryGetValue(clave, out r)) return r;
        r = new Sprite[cantidad];
        for (int i = 0; i < cantidad; i++)
        {
            r[i] = Uno(prefijo + i);
            if (r[i] == null) { r = null; break; }
        }
        series[clave] = r;
        return r;
    }

    /// <summary>Cuadros con nombre dentro de una carpeta: Lista("Mini/termita_cobre/", "camina_0", "camina_1"). Null si falta alguno.</summary>
    public static Sprite[] Lista(string carpeta, params string[] nombres)
    {
        string clave = carpeta + "|" + string.Join(",", nombres);
        Sprite[] r;
        if (series.TryGetValue(clave, out r)) return r;
        r = new Sprite[nombres.Length];
        for (int i = 0; i < nombres.Length; i++)
        {
            r[i] = Uno(carpeta + nombres[i]);
            if (r[i] == null) { r = null; break; }
        }
        series[clave] = r;
        return r;
    }
}
