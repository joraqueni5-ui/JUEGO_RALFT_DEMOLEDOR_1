using UnityEngine;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Hace parpadear un elemento de UI (por ejemplo "TOCA PARA EMPEZAR"), como en
    /// las máquinas arcade. Usa tiempo sin escala.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class Parpadeo : MonoBehaviour
    {
        [Tooltip("Segundos visibles y ocultos en cada ciclo.")]
        public float periodo = 0.55f;

        private CanvasGroup grupo;

        private void Awake()
        {
            grupo = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            grupo.alpha = Mathf.Repeat(Time.unscaledTime, periodo * 2f) < periodo ? 1f : 0f;
        }
    }
}
