using UnityEngine;

/// <summary>
/// Hace que el fondo de la ciudad acompañe a la cámara cuando sube por el edificio,
/// un poco más lento que ella para dar sensación de profundidad.
/// </summary>
[DefaultExecutionOrder(100)]   // después de la cámara, para que no tiemble
public class FondoParalaje : MonoBehaviour
{
    [Tooltip("Cámara a seguir (si se deja vacío usa la cámara principal).")]
    public Transform camara;
    [Tooltip("1 = se mueve igual que la cámara, 0 = no se mueve.")]
    [Range(0f, 1f)]
    public float factorVertical = 0.85f;

    private Vector3 inicio;
    private Vector3 inicioCamara;
    private bool listo;

    private void Start()
    {
        if (camara == null && Camera.main != null) camara = Camera.main.transform;
        if (camara == null) return;

        inicio = transform.position;
        inicioCamara = camara.position;
        listo = true;
    }

    private void LateUpdate()
    {
        if (!listo || camara == null) return;

        Vector3 d = camara.position - inicioCamara;
        transform.position = inicio + new Vector3(d.x, d.y * factorVertical, 0f);
    }
}
