using System.Collections.Generic;

/// <summary>
/// Mapa de un piso escrito como texto: cada carácter es una baldosa (32 px). La primera línea es la fila de
/// arriba; las coordenadas devueltas tienen la y hacia arriba, igual que el mundo del juego. Un espacio es
/// "vacío" (fuera del edificio). Lógica pura, sin dependencias de Unity.
/// </summary>
public class MapaTexto
{
    /// <summary>Un objeto formado por celdas con el mismo carácter: esquina inferior izquierda y tamaño.</summary>
    public struct Grupo
    {
        public char c;
        public int x, y, w, h;
    }

    public int Ancho { get; private set; }
    public int Alto { get; private set; }

    private char[,] celdas = new char[0, 0];

    /// <summary>Carácter de la celda (x, y), o ' ' si cae fuera del mapa.</summary>
    public char this[int x, int y]
    {
        get { return x < 0 || y < 0 || x >= Ancho || y >= Alto ? ' ' : celdas[x, y]; }
    }

    public static MapaTexto Leer(string texto)
    {
        MapaTexto m = new MapaTexto();
        if (string.IsNullOrEmpty(texto)) return m;

        List<string> lineas = new List<string>(texto.Replace("\r", "").Split('\n'));
        while (lineas.Count > 0 && lineas[0].Trim().Length == 0) lineas.RemoveAt(0);
        while (lineas.Count > 0 && lineas[lineas.Count - 1].Trim().Length == 0) lineas.RemoveAt(lineas.Count - 1);
        if (lineas.Count == 0) return m;

        int ancho = 0;
        foreach (string l in lineas) if (l.Length > ancho) ancho = l.Length;

        m.Ancho = ancho;
        m.Alto = lineas.Count;
        m.celdas = new char[ancho, lineas.Count];
        for (int fila = 0; fila < lineas.Count; fila++)
        {
            int y = lineas.Count - 1 - fila;
            for (int x = 0; x < ancho; x++) m.celdas[x, y] = x < lineas[fila].Length ? lineas[fila][x] : ' ';
        }
        return m;
    }

    /// <summary>Todos los caracteres distintos del mapa (sin el espacio), en orden de aparición.</summary>
    public List<char> Caracteres()
    {
        List<char> lista = new List<char>();
        for (int y = Alto - 1; y >= 0; y--)
            for (int x = 0; x < Ancho; x++)
                if (celdas[x, y] != ' ' && !lista.Contains(celdas[x, y])) lista.Add(celdas[x, y]);
        return lista;
    }

    /// <summary>
    /// Objetos de un carácter, en orden de lectura (de arriba abajo y de izquierda a derecha). Con 'unir', las
    /// celdas contiguas forman rectángulos (una figura que no es un rectángulo se parte en varios); sin 'unir',
    /// cada celda es un objeto.
    /// </summary>
    public List<Grupo> Grupos(char c, bool unir)
    {
        List<Grupo> lista = new List<Grupo>();
        bool[,] usada = new bool[Ancho, Alto];
        for (int y = Alto - 1; y >= 0; y--)
        {
            for (int x = 0; x < Ancho; x++)
            {
                if (celdas[x, y] != c || usada[x, y]) continue;

                int w = 1;
                if (unir) while (x + w < Ancho && celdas[x + w, y] == c && !usada[x + w, y]) w++;

                // Crece hacia abajo mientras toda la fila del tramo sea del mismo carácter.
                int h = 1;
                while (unir && y - h >= 0 && FilaCompleta(c, x, y - h, w, usada)) h++;

                for (int i = 0; i < w; i++)
                    for (int j = 0; j < h; j++) usada[x + i, y - j] = true;
                lista.Add(new Grupo { c = c, x = x, y = y - h + 1, w = w, h = h });
            }
        }
        return lista;
    }

    private bool FilaCompleta(char c, int x, int y, int w, bool[,] usada)
    {
        for (int i = 0; i < w; i++) if (celdas[x + i, y] != c || usada[x + i, y]) return false;
        return true;
    }

    /// <summary>Celdas (x, y) de un carácter, en orden de lectura.</summary>
    public List<int[]> Celdas(char c)
    {
        List<int[]> lista = new List<int[]>();
        for (int y = Alto - 1; y >= 0; y--)
            for (int x = 0; x < Ancho; x++)
                if (celdas[x, y] == c) lista.Add(new[] { x, y });
        return lista;
    }
}
