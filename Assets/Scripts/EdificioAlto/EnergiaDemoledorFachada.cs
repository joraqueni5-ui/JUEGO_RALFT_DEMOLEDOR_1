using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Barra de energía del demoledor en el nivel final (la misma "DEMOLEDOR" de los pisos cenitales). Empieza en
/// 100 % y baja con cada reparación del muñeco (y con cada bloqueo, si el modo lateral llega a tenerlo). No
/// llega a 0 por sí sola: tiene un mínimo mientras no se complete la última sección. La victoria sigue siendo
/// completar las 3 secciones; al ganar, la barra queda en 0.
/// </summary>
[RequireComponent(typeof(EdificioAlto))]
public class EnergiaDemoledorFachada : MonoBehaviour
{
    [Tooltip("Energía con la que empieza el demoledor, en %.")]
    [Range(1f, 100f)] public float energiaInicial = 100f;
    [Tooltip("% que pierde con cada reparación del muñeco.")]
    [Min(0f)] public float energiaPorReparacion = 2f;
    [Tooltip("% que pierde con cada bloqueo (el modo lateral todavía no tiene bloqueos: queda listo para usarse).")]
    [Min(0f)] public float energiaPorBloqueo = 8f;
    [Tooltip("Mínimo (%) al que llega mientras no se complete la última sección.")]
    [Range(0f, 50f)] public float energiaMinima = 5f;

    private EdificioAlto edificio;
    private float energia = 100f;

    /// <summary>Energía en % (0 a 100).</summary>
    public float Porcentaje { get { return energia; } }
    /// <summary>Energía de 0 a 1.</summary>
    public float Energia { get { return Mathf.Clamp01(energia / 100f); } }

    private void Awake()
    {
        edificio = GetComponent<EdificioAlto>();
        energia = energiaInicial;
    }

    private void OnEnable()
    {
        if (edificio == null) edificio = GetComponent<EdificioAlto>();
        edificio.Reparacion += AlReparar;
    }

    private void OnDisable()
    {
        if (edificio != null) edificio.Reparacion -= AlReparar;
    }

    private void Start()
    {
        // La ulti del demoledor (lluvia de escombros) se agrega sola si la escena no la trae.
        if (GetComponent<UltiFachada>() == null) gameObject.AddComponent<UltiFachada>();
    }

    private void Update()
    {
#if UNITY_EDITOR
        // Solo en el editor: F9 deja al demoledor sin energía y completa el nivel; F8 baja la energía a 16 % (la ulti salta a 15 %).
        Keyboard teclado = Keyboard.current;
        if (teclado != null && edificio != null && !edificio.Terminado)
        {
            if (teclado[Key.F9].wasPressedThisFrame) { energia = 0f; edificio.DepurarTerminar(); }
            else if (teclado[Key.F8].wasPressedThisFrame) energia = Mathf.Max(energiaMinima, 16f);
        }
#endif
        // Al completar la última sección el demoledor queda sin energía.
        if (edificio != null && edificio.Terminado && energia > 0f) energia = 0f;
    }

    private void AlReparar()
    {
        Bajar(energiaPorReparacion);
    }

    /// <summary>Para cuando el modo lateral tenga bloqueos.</summary>
    public void RegistrarBloqueo()
    {
        Bajar(energiaPorBloqueo);
    }

    private void Bajar(float porcentaje)
    {
        if (edificio == null || edificio.Terminado) return;
        energia = Mathf.Max(energiaMinima, energia - Mathf.Max(0f, porcentaje));
    }

    /// <summary>Vuelve a 100 % (al reiniciar el nivel).</summary>
    public void Reiniciar()
    {
        energia = energiaInicial;
    }
}
