using UnityEditor;
using UnityEngine;

/// <summary>Lectura de sprites dentro de archivos .aseprite (los usan los constructores de menús).</summary>
public static class AsignarArtePiso1
{
    /// <summary>Un sprite de un .aseprite por nombre (o el primero si no hay uno con ese nombre).</summary>
    internal static Sprite Cargar(string ruta, string nombre)
    {
        Object[] todo = AssetDatabase.LoadAllAssetsAtPath(ruta);
        Sprite primero = null;
        foreach (Object o in todo)
        {
            Sprite s = o as Sprite;
            if (s == null) continue;
            if (s.name == nombre) return s;
            if (primero == null) primero = s;
        }
        return primero;
    }
}
