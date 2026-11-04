using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DemoledorUnifranz.Game;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Menú principal de DEMOLEDOR UNIFRANZ.
    /// - Pantalla de inicio "TOCA PARA EMPEZAR" (solo la primera vez que se abre el juego).
    /// - Botones JUGAR, OPCIONES, CÓMO JUGAR, RÉCORDS y SALIR, y CRÉDITOS.
    /// - JUGAR carga la misma escena de siempre (Escenas.Nivel1 → "Universidad").
    /// - Escape cierra la ventana abierta.
    /// </summary>
    public class MenuPrincipalArcade : MonoBehaviour
    {
        [Header("Grupos")]
        public CanvasGroup pantallaInicio;
        public Text textoEmpezar;
        public CanvasGroup grupoMenu;
        public RectTransform panelBotones;

        [Header("Botones")]
        public Button botonJugar;

        [Header("Ventanas")]
        public PanelAnimado panelOpciones;
        public PanelAnimado panelComoJugar;
        public PanelAnimado panelRecords;
        public PanelAnimado panelCreditos;

        [Header("Récords")]
        public GameObject insigniaRecord;
        public Text valorRecord;
        public Text[] filasRecords;
        public Text textoSinRecords;

        [Header("Decoración")]
        public AnimadorImagenUI demoledor;
        public EscombrosDecorativos escombros;
        [Tooltip("Punto (en coordenadas de Escombros) desde donde salen los ladrillos al golpear.")]
        public Vector2 origenEstallido;
        public int ladrillosPorGolpe = 7;

        // La pantalla de inicio se ve una sola vez por sesión (al volver del juego no se repite).
        private static bool inicioVisto;
        private bool enInicio;

        private void OnEnable()
        {
            if (demoledor != null) demoledor.Impacto += AlGolpear;
        }

        private void OnDisable()
        {
            if (demoledor != null) demoledor.Impacto -= AlGolpear;
        }

        private void Start()
        {
            Time.timeScale = 1f;

            PanelAnimado[] ventanas = { panelOpciones, panelComoJugar, panelRecords, panelCreditos };
            foreach (PanelAnimado v in ventanas) if (v != null) v.OcultarInmediato();

            RefrescarRecord();

            if (textoEmpezar != null)
                textoEmpezar.text = PreferenciasMenus.EsCelular ? "TOCA PARA EMPEZAR" : "PRESIONA ENTER O HAZ CLIC";

            if (!inicioVisto && pantallaInicio != null) MostrarInicio();
            else MostrarMenu(false);

            if (!NavegadorEscenas.Disponible(Escenas.Nivel1))
                Debug.LogWarning("[Menus] JUGAR carga la escena '" + Escenas.Nivel1 + "', pero no está en Build Settings.");
        }

        private void Update()
        {
            if (enInicio)
            {
                if (AlgunaTeclaOToque()) Empezar();
                return;
            }

            Keyboard teclado = Keyboard.current;
            if (teclado == null || !teclado.escapeKey.wasPressedThisFrame) return;

            if (panelOpciones != null && panelOpciones.Visible) CerrarOpciones();
            else if (panelComoJugar != null && panelComoJugar.Visible) CerrarComoJugar();
            else if (panelRecords != null && panelRecords.Visible) CerrarRecords();
            else if (panelCreditos != null && panelCreditos.Visible) CerrarCreditos();
        }

        private static bool AlgunaTeclaOToque()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame) return true;
            return false;
        }

        // ------------------------------------------------------------ inicio → menú
        private void MostrarInicio()
        {
            enInicio = true;
            pantallaInicio.gameObject.SetActive(true);
            pantallaInicio.alpha = 1f;
            if (grupoMenu != null)
            {
                grupoMenu.alpha = 0f;
                grupoMenu.interactable = false;
                grupoMenu.blocksRaycasts = false;
            }
        }

        /// <summary>Sale de la pantalla de inicio y muestra los botones.</summary>
        public void Empezar()
        {
            if (!enInicio) return;
            enInicio = false;
            inicioVisto = true;
            ServiciosMenus.Instancia.Golpe();
            StartCoroutine(TransicionInicio());
        }

        private IEnumerator TransicionInicio()
        {
            float t = 0f;
            const float Duracion = 0.35f;
            Vector2 final = panelBotones != null ? panelBotones.anchoredPosition : Vector2.zero;
            if (grupoMenu != null) grupoMenu.alpha = 0f;
            while (t < Duracion)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / Duracion);
                if (pantallaInicio != null) pantallaInicio.alpha = 1f - p;
                if (grupoMenu != null) grupoMenu.alpha = p;
                if (panelBotones != null)
                {
                    float e = 1f - (1f - p) * (1f - p) * (1f - p);
                    panelBotones.anchoredPosition = final + new Vector2(-600f * (1f - e), 0f);
                }
                yield return null;
            }
            if (panelBotones != null) panelBotones.anchoredPosition = final;
            MostrarMenu(true);
        }

        private void MostrarMenu(bool conSonido)
        {
            enInicio = false;
            if (pantallaInicio != null) pantallaInicio.gameObject.SetActive(false);
            if (grupoMenu != null)
            {
                grupoMenu.alpha = 1f;
                grupoMenu.interactable = true;
                grupoMenu.blocksRaycasts = true;
            }
            if (conSonido) ServiciosMenus.Instancia.Panel();
            SeleccionarJugar();
        }

        // ------------------------------------------------------------ botones
        public void Jugar()
        {
            NavegadorEscenas.Cargar(Escenas.Nivel1);
        }

        public void AbrirOpciones() { Abrir(panelOpciones); }
        public void CerrarOpciones() { Cerrar(panelOpciones); }
        public void AbrirComoJugar() { Abrir(panelComoJugar); }
        public void CerrarComoJugar() { Cerrar(panelComoJugar); }
        public void AbrirCreditos() { Abrir(panelCreditos); }
        public void CerrarCreditos() { Cerrar(panelCreditos); }

        public void AbrirRecords()
        {
            RefrescarTablaRecords();
            Abrir(panelRecords);
        }

        public void CerrarRecords() { Cerrar(panelRecords); }

        public void Salir()
        {
            NavegadorEscenas.Salir();
        }

        private void Abrir(PanelAnimado panel)
        {
            if (panel != null) panel.Mostrar();
        }

        private void Cerrar(PanelAnimado panel)
        {
            if (panel != null) panel.Ocultar();
            SeleccionarJugar();
        }

        private void SeleccionarJugar()
        {
            if (botonJugar != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(botonJugar.gameObject);
        }

        // ------------------------------------------------------------ récords
        private void RefrescarRecord()
        {
            int mejor = TablaRecords.Mejor;
            if (insigniaRecord != null) insigniaRecord.SetActive(mejor > 0);
            if (valorRecord != null) valorRecord.text = mejor.ToString();
        }

        private void RefrescarTablaRecords()
        {
            List<TablaRecords.Entrada> lista = TablaRecords.Cargar();
            if (textoSinRecords != null) textoSinRecords.gameObject.SetActive(lista.Count == 0);
            if (filasRecords == null) return;
            for (int i = 0; i < filasRecords.Length; i++)
            {
                if (filasRecords[i] == null) continue;
                filasRecords[i].gameObject.SetActive(i < lista.Count);
                if (i < lista.Count)
                    filasRecords[i].text = (i + 1) + ".   " + lista[i].iniciales + "   " + lista[i].puntos.ToString().PadLeft(6);
            }
        }

        // ------------------------------------------------------------ decoración
        private void AlGolpear()
        {
            if (escombros != null) escombros.Estallido(origenEstallido, ladrillosPorGolpe);
        }
    }
}
