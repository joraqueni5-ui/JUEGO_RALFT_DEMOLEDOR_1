using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Efecto de botón arcade: crece al pasar el cursor (o al seleccionarlo con teclado),
    /// muestra un marco blanco y una flecha que se mueve, se aplasta al presionar,
    /// suena y vibra en el celular. Funciona con mouse, teclado, mando y toques.
    /// Usa tiempo sin escala para seguir animando con el juego en pausa.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BotonArcade : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [Tooltip("Escala al pasar el cursor o al estar seleccionado.")]
        public float escalaEncima = 1.07f;
        [Tooltip("Escala mientras se mantiene presionado.")]
        public float escalaPresionado = 0.93f;
        [Tooltip("Velocidad de la animación.")]
        public float suavizado = 16f;
        [Tooltip("Inclinación (grados) al pasar el cursor (0 = sin inclinación, mejor para pixel art).")]
        public float inclinacion = 0f;

        [Header("Selección")]
        public GameObject marcoSeleccion;
        public RectTransform flecha;

        private Vector2 posicionFlecha;
        private bool flechaGuardada;

        private Button boton;
        private Vector3 escalaBase = Vector3.one;
        private bool encima;
        private bool seleccionado;
        private bool presionado;

        private void Awake()
        {
            boton = GetComponent<Button>();
            escalaBase = transform.localScale;
            boton.onClick.AddListener(AlHacerClic);
        }

        private void OnDisable()
        {
            encima = false;
            seleccionado = false;
            presionado = false;
            transform.localScale = escalaBase;
            transform.localRotation = Quaternion.identity;
            if (marcoSeleccion != null) marcoSeleccion.SetActive(false);
            if (flecha != null) flecha.gameObject.SetActive(false);
        }

        private void Update()
        {
            bool activo = boton.IsInteractable();
            float objetivo = 1f;
            if (activo && (encima || seleccionado)) objetivo = escalaEncima;
            if (activo && presionado) objetivo = escalaPresionado;

            float k = 1f - Mathf.Exp(-suavizado * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, escalaBase * objetivo, k);

            float angulo = activo && (encima || seleccionado) && !presionado
                ? Mathf.Sin(Time.unscaledTime * 6f) * inclinacion
                : 0f;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(0f, 0f, angulo), k);

            bool marcado = activo && (encima || seleccionado);
            if (marcoSeleccion != null && marcoSeleccion.activeSelf != marcado) marcoSeleccion.SetActive(marcado);
            if (flecha != null)
            {
                if (!flechaGuardada) { posicionFlecha = flecha.anchoredPosition; flechaGuardada = true; }
                if (flecha.gameObject.activeSelf != marcado) flecha.gameObject.SetActive(marcado);
                // La flecha "late" de 4 en 4 unidades, como un cursor de arcade.
                float paso = Mathf.Floor(Mathf.PingPong(Time.unscaledTime * 16f, 2f)) * 4f;
                flecha.anchoredPosition = posicionFlecha + new Vector2(-paso, 0f);
            }
        }

        private void AlHacerClic()
        {
            ServiciosMenus.Instancia.Clic();
            PreferenciasMenus.VibrarCorto();
        }

        public void OnPointerEnter(PointerEventData datos)
        {
            encima = true;
            if (boton.IsInteractable()) ServiciosMenus.Instancia.Hover();
        }

        public void OnPointerExit(PointerEventData datos)
        {
            encima = false;
            presionado = false;
        }

        public void OnPointerDown(PointerEventData datos) { presionado = true; }

        public void OnPointerUp(PointerEventData datos) { presionado = false; }

        public void OnSelect(BaseEventData datos)
        {
            // Solo suena si la selección viene del teclado o mando (con mouse ya sonó al entrar).
            if (!seleccionado && !encima && boton.IsInteractable()) ServiciosMenus.Instancia.Hover();
            seleccionado = true;
        }

        public void OnDeselect(BaseEventData datos) { seleccionado = false; }
    }
}
