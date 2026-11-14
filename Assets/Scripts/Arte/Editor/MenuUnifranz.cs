using UnityEditor;

/// <summary>Único menú de herramientas del equipo: Herramientas / Unifranz.</summary>
public static class MenuUnifranz
{
    [MenuItem("Herramientas/Unifranz/Regenerar mapa de esta escena", false, 0)]
    private static void RegenerarMapa() { GeneradorEscenasNiveles.RegenerarEscenaAbierta(); }

    [MenuItem("Herramientas/Unifranz/Revisar Build Settings", false, 1)]
    private static void RevisarBuildSettings() { HerramientasMenus.MenuRevisarBuildSettings(); }
}
