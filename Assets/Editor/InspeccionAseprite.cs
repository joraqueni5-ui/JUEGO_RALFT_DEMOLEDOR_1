using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Herramienta de diagnostico: lista los recursos que el importador de
/// Aseprite genera para cada archivo .aseprite del proyecto (sprites,
/// recortes, clips de animacion y controladores).
/// </summary>
public static class InspeccionAseprite
{
    static readonly string[] Rutas =
    {
        "Assets/Edificio/edificio.aseprite",
        "Assets/Edificio/ventanas.aseprite",
        "Assets/personaje/Movimiento Abajo.aseprite",
        "Assets/personaje/Movimiento Espalda1.aseprite",
        "Assets/personaje/Movimiento martillo.aseprite",
        "Assets/personaje/Movimiento Arriba11.aseprite",
        "Assets/personaje/Movimiento Derecha.aseprite",
        "Assets/Animaciones/Enemigo/animacion_caminata_derecha.aseprite",
        "Assets/Animaciones/Enemigo/animaciion_caminata_izquierda.aseprite",
        "Assets/Animaciones/Enemigo/demoledor_golpe.aseprite",
        "Assets/Animaciones/Enemigo/animacion_salto.aseprite"
    };

    [MenuItem("Ralft Demoledor/Inspeccionar recursos de Aseprite")]
    public static void Ejecutar()
    {
        foreach (string ruta in Rutas)
        {
            Debug.Log("### " + ruta + " existe=" + File.Exists(ruta));

            if (!File.Exists(ruta))
            {
                continue;
            }

            Object[] recursos = AssetDatabase.LoadAllAssetsAtPath(ruta);

            foreach (Object recurso in recursos)
            {
                Sprite sprite = recurso as Sprite;

                if (sprite != null)
                {
                    Debug.Log("    SPRITE nombre=" + sprite.name
                              + " rect=" + sprite.rect
                              + " textura=" + sprite.texture.width + "x" + sprite.texture.height
                              + " ppu=" + sprite.pixelsPerUnit);
                }
                else
                {
                    Debug.Log("    OTRO tipo=" + recurso.GetType().Name + " nombre=" + recurso.name);
                }
            }
        }

        Debug.Log("=== FIN DE LA INSPECCION ===");
        EditorApplication.Exit(0);
    }
}
