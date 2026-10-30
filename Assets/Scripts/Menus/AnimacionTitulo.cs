using UnityEngine;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Título que cae desde arriba con rebote (como un bloque que se desploma) y
    /// luego flota y "respira" suavemente.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class AnimacionTitulo : MonoBehaviour
    {
        [Tooltip("Segundos de espera antes de caer.")]
        public float retraso = 0.1f;
        [Tooltip("Altura (px) desde la que cae.")]
        public float alturaCaida = 520f;
        public float duracionCaida = 0.9f;
        [Tooltip("Amplitud (px) del flotado.")]
        public float flotado = 8f;
        [Tooltip("Amplitud del pulso de escala.")]
        public float pulso = 0.025f;
        [Tooltip("Inclinación (grados) del balanceo.")]
        public float balanceo = 1.2f;

        private RectTransform rt;
        private Vector2 posicionBase;
        private float inicio;

        private void Awake()
        {
            rt = (RectTransform)transform;
            posicionBase = rt.anchoredPosition;
        }

        private void OnEnable()
        {
            inicio = Time.unscaledTime + retraso;
            if (rt != null) rt.anchoredPosition = posicionBase + new Vector2(0f, alturaCaida);
        }

        private void Update()
        {
            float t = Time.unscaledTime - inicio;
            if (t < 0f)
            {
                rt.anchoredPosition = posicionBase + new Vector2(0f, alturaCaida);
                return;
            }

            if (t < duracionCaida)
            {
                float p = SalidaRebote(t / duracionCaida);
                rt.anchoredPosition = posicionBase + new Vector2(0f, alturaCaida * (1f - p));
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
                return;
            }

            float s = t - duracionCaida;
            rt.anchoredPosition = posicionBase + new Vector2(0f, Mathf.Sin(s * 2.4f) * flotado);
            float escala = 1f + Mathf.Sin(s * 3.1f) * pulso;
            rt.localScale = new Vector3(escala, escala, 1f);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(s * 1.3f) * balanceo);
        }

        private static float SalidaRebote(float x)
        {
            const float n1 = 7.5625f, d1 = 2.75f;
            if (x < 1f / d1) return n1 * x * x;
            if (x < 2f / d1) { x -= 1.5f / d1; return n1 * x * x + 0.75f; }
            if (x < 2.5f / d1) { x -= 2.25f / d1; return n1 * x * x + 0.9375f; }
            x -= 2.625f / d1;
            return n1 * x * x + 0.984375f;
        }
    }
}
