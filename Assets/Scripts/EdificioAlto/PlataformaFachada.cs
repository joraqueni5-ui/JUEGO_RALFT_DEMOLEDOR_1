using UnityEngine;

/// <summary>Plataforma agregada a la fachada (tablón de andamio o escalón de la planta baja). piso = -1: planta baja.</summary>
public class PlataformaFachada : MonoBehaviour
{
    public int piso = -1;
    public int seccion;
    [Tooltip("Repisa de al lado: la plataforma se puede pisar solo cuando esa se puede (secciones a las que el demoledor aún no llegó).")]
    public RepisaEdificio vecina;
    public BoxCollider2D caja;
}
