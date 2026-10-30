using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DemoledorUnifranz.Game;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Menú principal de DEMOLEDOR UNIFRANZ: JUGAR, OPCIONES, CÓMO JUGAR y SALIR.
    /// JUGAR carga la misma escena que el menú anterior (Escenas.Nivel1, que el
    /// proyecto redirige a "Universidad"). Escape cierra la ventana abierta.
    /// Cada vez que el demoledor golpea, salen ladrillos volando del hueco del muro.
    /// </summary>
    public class MenuPrincipalArcade : MonoBehaviour
    {
        [Header("Botones")]
        public Button botonJugar;

        [Header("Ventanas")]
        public PanelAnimado panelOpciones;
        public PanelAnimado panelComoJugar;

        [Header("Decoración")]
        public AnimadorImagenUI demoledor;
        public EscombrosDecorativos escombros;
        [Tooltip("Punto (en coordenadas de Escombros) desde donde salen los ladrillos al golpear.")]
        public Vector2 origenEstallido;
        public int ladrillosPorGolpe = 7;

        private void OnEnable()
        {
            if (demoledor != null) demoledor.Impacto += AlGolpear;
        }

        private void OnDisable()
        {
            if (demoledor != null) demoledor.Impacto -= AlGolpear;
        }

        private void AlGolpear()
        {
            if (escombros != null) escombros.Estallido(origenEstallido, ladrillosPorGolpe);
        }

        private void Start()
        {
            Time.timeScale = 1f;

            if (panelOpciones != null) panelOpciones.OcultarInmediato();
            if (panelComoJugar != null) panelComoJugar.OcultarInmediato();

            SeleccionarJugar();

            if (!NavegadorEscenas.Disponible(Escenas.Nivel1))
            {
                Debug.LogWarning("[Menus] JUGAR carga la escena '" + Escenas.Nivel1 +
                                 "', pero no está en Build Settings. Agrégala para que el botón funcione.");
            }
        }

        private void Update()
        {
            Keyboard teclado = Keyboard.current;
            if (teclado == null || !teclado.escapeKey.wasPressedThisFrame) return;

            if (panelOpciones != null && panelOpciones.Visible) CerrarOpciones();
            else if (panelComoJugar != null && panelComoJugar.Visible) CerrarComoJugar();
        }

        // ------------------------------------------------------------ botones
        public void Jugar()
        {
            NavegadorEscenas.Cargar(Escenas.Nivel1);
        }

        public void AbrirOpciones()
        {
            if (panelOpciones != null) panelOpciones.Mostrar();
        }

        public void CerrarOpciones()
        {
            if (panelOpciones != null) panelOpciones.Ocultar();
            SeleccionarJugar();
        }

        public void AbrirComoJugar()
        {
            if (panelComoJugar != null) panelComoJugar.Mostrar();
        }

        public void CerrarComoJugar()
        {
            if (panelComoJugar != null) panelComoJugar.Ocultar();
            SeleccionarJugar();
        }

        public void Salir()
        {
            NavegadorEscenas.Salir();
        }

        private void SeleccionarJugar()
        {
            if (botonJugar != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(botonJugar.gameObject);
        }
    }
}
