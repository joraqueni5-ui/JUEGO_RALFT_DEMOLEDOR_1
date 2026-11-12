using UnityEngine;
using DemoledorUnifranz.Game;

/// <summary>
/// Interruptor entre los dos modos de la escena Universidad:
///  - Modo original: el edificio alto de 9 pisos con 3 secciones (EdificioAlto).
///  - Modo pisos: 6 niveles, uno por piso de Unifranz (EdificioPisos).
///
/// Solo activa un edificio y apaga el otro; no toca nada más. Se ejecuta antes que el
/// resto de scripts para que, al despertar la escena, solo exista el edificio elegido.
/// GestorJuego consulta SelectorModoJuego.ModoPisosActivo.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class SelectorModoJuego : MonoBehaviour
{
    [Tooltip("Marcado: juego de 6 pisos. Desmarcado: el edificio alto original de 3 secciones.")]
    public bool modoPisos = true;
    [Tooltip("Objeto EdificioAlto (el original, de 9 pisos).")]
    public GameObject edificioOriginal;
    [Tooltip("Objeto EdificioPisos (el nuevo, de 6 niveles).")]
    public GameObject edificioPisos;

    /// <summary>True durante la partida si se eligió el modo de pisos.</summary>
    public static bool ModoPisosActivo { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstatico()
    {
        ModoPisosActivo = false;
    }

    private void Awake()
    {
        // El menú NIVELES puede forzar un modo para esta partida; si no, manda la casilla.
        bool pisos = modoPisos;
        if (GameSession.ModoSeleccionado == GameSession.ModoElegido.Original) pisos = false;
        else if (GameSession.ModoSeleccionado == GameSession.ModoElegido.Pisos) pisos = true;

        pisos = pisos && edificioPisos != null;   // sin EdificioPisos en la escena, se juega el original
        Aplicar(pisos);
        ModoPisosActivo = pisos;
    }

    private void OnDestroy()
    {
        ModoPisosActivo = false;
    }

    /// <summary>Enciende el edificio del modo de la casilla y apaga el otro.</summary>
    public void Aplicar()
    {
        Aplicar(modoPisos);
    }

    private void Aplicar(bool pisos)
    {
        Activar(edificioOriginal, !pisos);
        Activar(edificioPisos, pisos);
    }

    private static void Activar(GameObject objeto, bool activo)
    {
        if (objeto != null && objeto.activeSelf != activo) objeto.SetActive(activo);
    }

#if UNITY_EDITOR
    // Al marcar o desmarcar la casilla en el Inspector, la jerarquía muestra al instante
    // cuál edificio queda encendido.
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && !Application.isPlaying) Aplicar();
        };
    }
#endif
}
