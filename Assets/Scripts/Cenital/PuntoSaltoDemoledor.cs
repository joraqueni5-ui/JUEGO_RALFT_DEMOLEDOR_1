using UnityEngine;

/// <summary>
/// Marca el punto "!" del mapa desde el que el demoledor salta al rendirse en un piso sin ascensor.
/// Solo se dibuja en el editor (con los Gizmos); en el juego no se ve.
/// </summary>
public class PuntoSaltoDemoledor : MonoBehaviour
{
    [Tooltip("Hacia dónde salta (hacia fuera del edificio).")]
    public Vector2 direccion = Vector2.right;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);
        Vector3 fin = transform.position + (Vector3)(direccion.normalized * 0.9f);
        Gizmos.DrawLine(transform.position, fin);
        Gizmos.DrawWireSphere(fin, 0.12f);
    }
}
