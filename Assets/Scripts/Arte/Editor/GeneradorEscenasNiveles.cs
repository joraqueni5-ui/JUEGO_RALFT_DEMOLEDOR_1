using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// "Regenerar mapa de esta escena": rehace solo el objeto "Mapa" de la escena de nivel abierta a partir del mapa de texto de su
/// DefinicionPiso. Lo que esté fuera de "Mapa" se conserva.
/// </summary>
public static class GeneradorEscenasNiveles
{
    public static void RegenerarEscenaAbierta()
    {
        if (Application.isPlaying) { Debug.LogError("[Niveles] Sal del modo Play antes de regenerar el mapa."); return; }

        NivelEscena nivel = Object.FindAnyObjectByType<NivelEscena>();
        if (nivel == null || nivel.definicion == null)
        {
            EditorUtility.DisplayDialog("Regenerar mapa", "La escena abierta no tiene un objeto con el componente NivelEscena y una DefinicionPiso. Abre una escena de Assets/Scenes/Niveles.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Regenerar mapa",
                "Se vuelve a construir el objeto \"Mapa\" de esta escena desde '" + nivel.definicion.name + "'.\n\n" +
                "Lo que hayas agregado FUERA de \"Mapa\" se conserva; lo que hayas movido o agregado dentro de \"Mapa\" se pierde.\n\n¿Continuar?",
                "Regenerar", "Cancelar")) return;

        // Se borra solo el contenedor del mapa.
        List<GameObject> viejos = new List<GameObject>();
        if (nivel.mapa != null) viejos.Add(nivel.mapa.gameObject);
        foreach (MapaCenital m in Object.FindObjectsByType<MapaCenital>(FindObjectsInactive.Include))
            if (m != null && !viejos.Contains(m.gameObject)) viejos.Add(m.gameObject);
        foreach (GameObject g in viejos) Object.DestroyImmediate(g);

        MapaCenital nuevo = ConstructorPisoDesdeMapa.ConstruirEnEscena(nivel.definicion);
        if (nuevo == null) return;
        nivel.mapa = nuevo;
        EditorUtility.SetDirty(nivel);

        Scene escena = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        if (!string.IsNullOrEmpty(escena.path)) EditorSceneManager.SaveScene(escena);
        Debug.Log("[Niveles] Mapa de '" + escena.name + "' regenerado desde " + nivel.definicion.name + ".");
    }
}
