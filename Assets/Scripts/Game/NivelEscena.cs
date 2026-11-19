using UnityEngine;

/// <summary>
/// Va en cada escena de piso: indica qué DefinicionPiso se juega en ella y cuál es su mapa (el objeto "Mapa" que ya
/// viene armado en la escena). PisoCenital lo busca al empezar; así el piso que se juega lo decide la escena y no
/// una lista compartida.
/// </summary>
public class NivelEscena : MonoBehaviour
{
    [Tooltip("Definición del nivel de esta escena. Guarda el nombre de la escena y el de la siguiente.")]
    public DefinicionPiso definicion;
    [Tooltip("Mapa armado en la escena (el objeto \"Mapa\"). Si está vacío se busca entre los hijos o se crea desde el prefab de la definición.")]
    public MapaCenital mapa;
}
