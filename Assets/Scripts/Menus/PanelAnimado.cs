using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Ventana emergente (opciones, pausa, cómo jugar…): aparece con un fundido y un
    /// pequeño rebote, y desaparece al revés. Funciona con el juego en pausa.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PanelAnimado : MonoBehaviour
    {
        [Tooltip("Caja que rebota al aparecer (el velo de fondo solo se funde).")]
        public RectTransform contenido;
        [Tooltip("Elemento que queda seleccionado al abrir (navegación con teclado o mando).")]
        public GameObject seleccionInicial;
        public float duracion = 0.22f;

        private CanvasGroup grupo;
        private Coroutine rutina;

        public bool Visible { get; private set; }

        private void Awake()
        {
            grupo = GetComponent<CanvasGroup>();
        }

        public void Mostrar()
        {
            if (grupo == null) grupo = GetComponent<CanvasGroup>();
            gameObject.SetActive(true);
            Visible = true;
            grupo.interactable = true;
            grupo.blocksRaycasts = true;
            ServiciosMenus.Instancia.Panel();

            if (rutina != null) StopCoroutine(rutina);
            rutina = StartCoroutine(Animar(true));

            if (seleccionInicial != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(seleccionInicial);
        }

        public void Ocultar()
        {
            if (!gameObject.activeInHierarchy)
            {
                Visible = false;
                return;
            }
            Visible = false;
            if (grupo == null) grupo = GetComponent<CanvasGroup>();
            grupo.interactable = false;
            grupo.blocksRaycasts = false;

            if (rutina != null) StopCoroutine(rutina);
            rutina = StartCoroutine(Animar(false));
        }

        /// <summary>Deja el panel oculto al instante (al iniciar la escena).</summary>
        public void OcultarInmediato()
        {
            if (grupo == null) grupo = GetComponent<CanvasGroup>();
            Visible = false;
            grupo.alpha = 0f;
            grupo.interactable = false;
            grupo.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private IEnumerator Animar(bool mostrar)
        {
            float t = 0f;
            float desde = grupo.alpha;
            float hasta = mostrar ? 1f : 0f;

            while (t < duracion)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duracion);
                grupo.alpha = Mathf.Lerp(desde, hasta, p);

                if (contenido != null)
                {
                    float e = mostrar ? SalidaConRebote(p) : 1f - p * p * 0.15f;
                    float escala = mostrar ? Mathf.LerpUnclamped(0.82f, 1f, e) : e;
                    contenido.localScale = new Vector3(escala, escala, 1f);
                }
                yield return null;
            }

            grupo.alpha = hasta;
            if (contenido != null) contenido.localScale = Vector3.one;
            if (!mostrar) gameObject.SetActive(false);
            rutina = null;
        }

        private static float SalidaConRebote(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
