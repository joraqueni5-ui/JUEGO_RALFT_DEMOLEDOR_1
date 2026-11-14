/// <summary>
/// Carpetas del arte de los niveles. Los sprites están en Assets/Sprites/Pisos/{Comunes, PlantaBaja, GastronomiaEnfermeria, Informatica,
/// Aulas300, Aulas400, Aulas500, TerrazaBiblioteca}, Assets/Sprites/{Poderes, NivelFinal, Efectos, Interfaz}.
/// Las rutas relativas que usan las herramientas de arte (por ejemplo "Pisos/Comunes/grietas/grietas_chica") cuelgan de Assets/Sprites/.
/// </summary>
public static class RutasArte
{
    /// <summary>Base de las rutas relativas de suelos, paredes, puertas, reparables, decoración, poderes y nivel final.</summary>
    public const string Niveles = "Assets/Sprites/";
    /// <summary>Base de las rutas relativas del bloque grande, las rocas de la lluvia y su aviso (carpeta Efectos).</summary>
    public const string EfectosExtra = "Assets/Sprites/";
    /// <summary>Base de las rutas relativas de los efectos animados (Efectos) y la interfaz (Interfaz).</summary>
    public const string Efectos = "Assets/Sprites/";

    /// <summary>Carpetas raíz que se revisan al importar (cualquier PNG dentro es de este arte).</summary>
    public static readonly string[] Raices =
    {
        "Assets/Sprites/Pisos/Comunes",
        "Assets/Sprites/Pisos/PlantaBaja",
        "Assets/Sprites/Pisos/GastronomiaEnfermeria",
        "Assets/Sprites/Pisos/Informatica",
        "Assets/Sprites/Pisos/Aulas300",
        "Assets/Sprites/Pisos/Aulas400",
        "Assets/Sprites/Pisos/Aulas500",
        "Assets/Sprites/Pisos/TerrazaBiblioteca",
        "Assets/Sprites/Poderes",
        "Assets/Sprites/NivelFinal",
        "Assets/Sprites/Efectos",
        "Assets/Sprites/Interfaz",
    };

    /// <summary>Arte derivado (giros, repeticiones y composiciones) que genera el editor; son archivos nuevos.</summary>
    public const string Derivados = "Assets/Arte/PisoCenital/Derivados";
    /// <summary>Paredes ya dibujadas de cada escena.</summary>
    public const string Generado = "Assets/Arte/PisoCenital/Generado";
}
