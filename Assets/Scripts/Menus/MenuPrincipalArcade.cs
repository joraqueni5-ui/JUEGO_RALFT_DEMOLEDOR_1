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
    /// - JUGAR empieza en la planta baja, en vista cenital (Escenas.ParaPiso(0) → "PisoCenital").
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
        [Tooltip("Ventana NIVELES: elegir el edificio completo o un piso para probarlo.")]
        public PanelAnimado panelNiveles;

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

            PanelAnimado[] ventanas = { panelOpciones, panelComoJugar, panelRecords, panelCreditos, panelNiveles };
            foreach (PanelAnimado v in ventanas) if (v != null) v.OcultarInmediato();

            RefrescarRecord();
            AgregarBotonMultijugador();
            AgregarPremiosYVersion();

            if (textoEmpezar != null)
                textoEmpezar.text = PreferenciasMenus.EsCelular ? "TOCA PARA EMPEZAR" : "PRESIONA ENTER O HAZ CLIC";

            // Tras el nivel final: directo a los créditos (y luego a la tabla de récords).
            if (GameSession.FlujoFinal) inicioVisto = true;

            if (!inicioVisto && pantallaInicio != null) MostrarInicio();
            else MostrarMenu(false);

            if (GameSession.FlujoFinal) AbrirCreditos();

            if (!NavegadorEscenas.Disponible(Escenas.ParaPiso(0)))
                Debug.LogWarning("[Menus] JUGAR carga la escena '" + Escenas.ParaPiso(0) + "', pero no está en Build Settings.");
        }

        private void Update()
        {
            if (enInicio)
            {
                if (AlgunaTeclaOToque()) Empezar();
                return;
            }

            Keyboard teclado = Keyboard.current;
            if (teclado == null || !teclado.escapeKey.wasPressedThisFrame || SelectorPersonaje.Abierto || LobbyRed.Abierto) return;
            if (GameObject.Find("[PantallaHistoria]") != null || SelectorDificultad.Abierto) return;

            MenuOpcionesArcade opciones = panelOpciones != null ? panelOpciones.GetComponent<MenuOpcionesArcade>() : null;
            if (opciones != null && opciones.ControlesAbiertos) opciones.CerrarControles();
            else if (PanelPremios.Abierto && premios != null) premios.Cerrar();
            else if (panelOpciones != null && panelOpciones.Visible) CerrarOpciones();
            else if (panelComoJugar != null && panelComoJugar.Visible) CerrarComoJugar();
            else if (panelRecords != null && panelRecords.Visible) CerrarRecords();
            else if (panelCreditos != null && panelCreditos.Visible) CerrarCreditos();
            else if (panelNiveles != null && panelNiveles.Visible) CerrarNiveles();
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
            // Primero el personaje (Extras/SelectorPersonaje), luego la dificultad; al confirmar empieza la partida.
            SelectorPersonaje.Mostrar(() => SelectorDificultad.Mostrar(JugarConPersonaje, SeleccionarJugar));
        }

        private void JugarConPersonaje()
        {
            // JUGAR empieza una partida nueva en la planta baja (vista cenital). Antes, la historia del personaje elegido.
            System.Action empezar = () =>
            {
                GameSession.SeleccionarNivel(true, 0);
                NavegadorEscenas.Cargar(Escenas.ParaPiso(0));
            };
            // Jugando solo, siempre la historia del personaje elegido (con SALTAR para ir directo).
            HistoriaJuego.MostrarIntro(empezar);
        }

        /// <summary>MULTIJUGADOR: crear o unirse a una partida por Wi-Fi.</summary>
        public void AbrirMultijugador() { LobbyRed.Mostrar(); }

        /// <summary>
        /// Agrega el botón MULTIJUGADOR debajo de JUGAR (el menú está guardado en la escena con 6 botones): copia el botón
        /// NIVELES y reparte los 7 en el mismo panel.
        /// </summary>
        private void AgregarBotonMultijugador()
        {
            if (panelBotones == null || panelBotones.Find("BotonMultijugador") != null) return;
            Transform molde = panelBotones.Find("BotonNiveles");
            if (molde == null) return;
            GameObject copia = Instantiate(molde.gameObject, panelBotones);
            copia.name = "BotonMultijugador";
            Button b = copia.GetComponent<Button>();
            if (b != null)
            {
                b.onClick = new Button.ButtonClickedEvent();
                b.onClick.AddListener(AbrirMultijugador);
            }
            Text t = copia.GetComponentInChildren<Text>(true);
            if (t != null) t.text = "MULTIJUGADOR";
            KitSpritesArcade kit = KitSpritesArcade.Actual();
            Image fondo = copia.GetComponent<Image>();
            if (fondo != null && kit != null && kit.botonMorado != null && fondo.sprite == kit.botonAzul) fondo.sprite = kit.botonMorado;

            Repartir();
        }

        private static readonly string[] OrdenBotones = { "BotonJugar", "BotonSupervivencia", "BotonMultijugador", "BotonNiveles", "BotonPremios", "BotonOpciones", "BotonComoJugar", "BotonRecords", "BotonSalir" };

        /// <summary>Reparte los botones del menú (8 con MULTIJUGADOR y PREMIOS) en el mismo espacio.</summary>
        private void Repartir()
        {
            List<Selectable> lista = new List<Selectable>();
            int cuantos = 0;
            foreach (string n in OrdenBotones) if (panelBotones.Find(n) != null) cuantos++;
            float paso = cuantos >= 9 ? 69f : cuantos >= 8 ? 78f : 88f, alto = cuantos >= 9 ? 62f : cuantos >= 8 ? 72f : 80f;
            float y = 285f;
            for (int i = 0; i < OrdenBotones.Length; i++)
            {
                Transform h = panelBotones.Find(OrdenBotones[i]);
                if (h == null) continue;
                RectTransform rt = (RectTransform)h;
                bool primero = OrdenBotones[i] == "BotonJugar";
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, primero ? (cuantos >= 9 ? 84f : 92f) : alto);
                y -= primero ? (cuantos >= 9 ? 80f : 90f) : paso;
                Selectable sel = h.GetComponent<Selectable>();
                if (sel != null) lista.Add(sel);
            }
            if (lista.Count > 1) FabricaUIArcade.NavegacionVertical(lista.ToArray());
        }

        // ------------------------------------------------------------ premios, historia y versión
        private PanelPremios premios;

        /// <summary>Botón PREMIOS (accesorios, martillos, logros, récords por piso e historia) y el número de versión.</summary>
        private void AgregarPremiosYVersion()
        {
            if (panelBotones == null) return;
            Canvas lienzo = panelBotones.GetComponentInParent<Canvas>();
            if (lienzo != null) lienzo = lienzo.rootCanvas;
            if (lienzo == null) return;

            if (panelBotones.Find("BotonPremios") == null)
            {
                Transform molde = panelBotones.Find("BotonNiveles");
                if (molde != null)
                {
                    GameObject copia = Instantiate(molde.gameObject, panelBotones);
                    copia.name = "BotonPremios";
                    Button b = copia.GetComponent<Button>();
                    if (b != null)
                    {
                        b.onClick = new Button.ButtonClickedEvent();
                        b.onClick.AddListener(AbrirPremios);
                    }
                    Text t = copia.GetComponentInChildren<Text>(true);
                    if (t != null) t.text = "PREMIOS";
                    Repartir();
                }
            }
            if (panelBotones.Find("BotonSupervivencia") == null)
            {
                Transform molde = panelBotones.Find("BotonNiveles");
                if (molde != null)
                {
                    GameObject copia = Instantiate(molde.gameObject, panelBotones);
                    copia.name = "BotonSupervivencia";
                    Button b = copia.GetComponent<Button>();
                    if (b != null)
                    {
                        b.onClick = new Button.ButtonClickedEvent();
                        b.onClick.AddListener(Supervivencia);
                    }
                    Text t = copia.GetComponentInChildren<Text>(true);
                    if (t != null) t.text = "SUPERVIVENCIA";
                    KitSpritesArcade kit = KitSpritesArcade.Actual();
                    Image fondo = copia.GetComponent<Image>();
                    if (fondo != null && kit != null && kit.botonRojo != null) fondo.sprite = kit.botonRojo;
                    Repartir();
                }
            }
            premios = PanelPremios.Crear(lienzo.transform);
            premios.AlCerrar = SeleccionarJugar;

            Text version = FabricaUIArcade.CrearTexto(lienzo.transform, "Version", "v" + Application.version, 22, new Color(1f, 1f, 1f, 0.55f), TextAnchor.LowerLeft, 2f, 0f);
            FabricaUIArcade.Colocar(version.rectTransform, new Vector2(0f, 0f), new Vector2(110f, 26f), new Vector2(200f, 30f));
        }

        /// <summary>SUPERVIVENCIA: personaje, dificultad y al escenario EL GRAN HALL (oleadas de villanos).</summary>
        public void Supervivencia()
        {
            SelectorPersonaje.Mostrar(() => SelectorDificultad.Mostrar(() =>
            {
                GameSession.SeleccionarNivel(true, 0);
                GameSession.Supervivencia = true;
                NavegadorEscenas.Cargar(Escenas.ParaPiso(0));
            }, SeleccionarJugar, "SUPERVIVENCIA: aguanta todas las oleadas que puedas"));
        }

        public void AbrirPremios()
        {
            if (premios != null) premios.Abrir();
        }

        public void AbrirNiveles() { Abrir(panelNiveles); }
        public void CerrarNiveles() { Cerrar(panelNiveles); }
        public void AbrirOpciones() { Abrir(panelOpciones); }
        public void CerrarOpciones() { Cerrar(panelOpciones); }
        public void AbrirComoJugar() { Abrir(panelComoJugar); }
        public void CerrarComoJugar() { Cerrar(panelComoJugar); }
        public void AbrirCreditos() { Abrir(panelCreditos); }
        public void CerrarCreditos()
        {
            Cerrar(panelCreditos);
            if (GameSession.FlujoFinal)
            {
                GameSession.FlujoFinal = false;
                NavegadorEscenas.Cargar(Escenas.FinJuego);   // pantalla de fin con la tabla de récords
            }
        }

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
