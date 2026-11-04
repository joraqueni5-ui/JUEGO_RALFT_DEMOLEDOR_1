using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DemoledorUnifranz.Game;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Parte visual de la pausa de la escena Universidad. La lógica (Escape,
    /// Time.timeScale, bloquear al muñeco) sigue en GestorJuego; este script solo
    /// dibuja las ventanas y avisa a GestorJuego cuando se pulsa un botón.
    ///
    /// - Ventana PAUSA sobre el juego congelado con CONTINUAR, REINICIAR y MENÚ
    ///   PRINCIPAL, el estado de la partida y los controles.
    /// - MENÚ PRINCIPAL pide confirmación ("¿SALIR AL MENÚ?") para no perder la partida por error.
    /// - Botón de pausa en pantalla (arriba a la izquierda) para jugar en el celular.
    /// Se construye por código, igual que el HUD de GestorJuego, así que la escena
    /// Universidad no se modifica. Usa la fuente y los sprites que dejó RecursosMenus.
    /// </summary>
    public class MenuPausaArcade : MonoBehaviour
    {
        private const float Px = FabricaUIArcade.Px;

        private GestorJuego gestor;
        private PanelAnimado panel;
        private PanelAnimado confirmacion;
        private Text textoEstado;
        private GameObject botonPausa;
        private GameObject botonMenu;

        /// <summary>Crea el menú de pausa como hijo de GestorJuego.</summary>
        public static MenuPausaArcade Crear(GestorJuego gestor)
        {
            GameObject objeto = new GameObject("MenuPausa");
            objeto.transform.SetParent(gestor.transform, false);
            MenuPausaArcade menu = objeto.AddComponent<MenuPausaArcade>();
            menu.gestor = gestor;
            FabricaUIArcade.AsegurarEventSystem();
            menu.Construir();
            return menu;
        }

        private void Update()
        {
            if (botonPausa != null && gestor != null)
                botonPausa.SetActive(!gestor.PartidaTerminada && !gestor.EstaEnPausa);
        }

        // ------------------------------------------------------------ mostrar / ocultar
        /// <summary>Abre la ventana mostrando cómo va la partida.</summary>
        public void Mostrar(int puntos, int reparaciones, float destruido)
        {
            if (textoEstado != null)
            {
                textoEstado.text = "PUNTOS " + puntos + "   ·   REPARACIONES " + reparaciones +
                                   "   ·   DESTROZO " + Mathf.RoundToInt(destruido * 100f) + "%";
            }
            if (confirmacion != null) confirmacion.OcultarInmediato();
            if (panel != null) panel.Mostrar();
        }

        public void Ocultar()
        {
            if (confirmacion != null && confirmacion.Visible) confirmacion.Ocultar();
            if (panel != null) panel.Ocultar();
        }

        /// <summary>Oculta las ventanas al instante (al reiniciar o cambiar de escena).</summary>
        public void OcultarInmediato()
        {
            if (confirmacion != null) confirmacion.OcultarInmediato();
            if (panel != null) panel.OcultarInmediato();
        }

        // ------------------------------------------------------------ botones
        private void Continuar()
        {
            if (gestor != null) gestor.Reanudar();
        }

        private void Reiniciar()
        {
            if (gestor != null) gestor.ReiniciarPartida();
        }

        private void PreguntarSalir()
        {
            if (confirmacion != null) confirmacion.Mostrar();
            else IrAlMenuPrincipal();
        }

        private void NoSalir()
        {
            if (confirmacion != null) confirmacion.Ocultar();
            if (botonMenu != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(botonMenu);
        }

        private void IrAlMenuPrincipal()
        {
            Time.timeScale = 1f;
            NavegadorEscenas.Cargar(Escenas.MenuPrincipal);
        }

        private void AbrirPausa()
        {
            if (gestor != null) gestor.Pausar();
        }

        // ------------------------------------------------------------ construcción
        private void Construir()
        {
            KitSpritesArcade kit = KitSpritesArcade.Actual();
            bool hayMenu = NavegadorEscenas.Disponible(Escenas.MenuPrincipal);
            if (!hayMenu)
                Debug.LogWarning("[Menus] MenuPrincipal no está en Build Settings: se oculta el botón MENÚ PRINCIPAL de la pausa.");

            // Por encima del HUD (100) y de los controles táctiles (110).
            Canvas canvas = FabricaUIArcade.CrearCanvas("CanvasPausa", 300);
            canvas.transform.SetParent(transform, false);
            Transform raiz = canvas.transform;

            // Botón de pausa: arriba a la izquierda, en el lugar que dejó libre el HUD (24, -20; 112×96).
            Button pausa = FabricaUIArcade.CrearBotonIcono(raiz, "BotonPausa", kit.botonAzul, kit.iconoPausa, 96f, kit);
            RectTransform rp = (RectTransform)pausa.transform;
            rp.anchorMin = rp.anchorMax = new Vector2(0f, 1f);
            rp.pivot = new Vector2(0f, 1f);
            rp.anchoredPosition = new Vector2(24f, -20f);
            rp.sizeDelta = new Vector2(112f, 96f);
            pausa.onClick.AddListener(AbrirPausa);
            botonPausa = pausa.gameObject;

            // Ventana de pausa.
            RectTransform caja;
            panel = FabricaUIArcade.CrearVentana(raiz, "PanelPausa", "PAUSA", new Vector2(960f, 820f), kit, out caja, null, kit.iconoPausa);

            textoEstado = FabricaUIArcade.CrearTexto(caja, "Estado", "", 26, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, Px * 0.75f, 0f);
            FabricaUIArcade.Colocar(textoEstado.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(880f, 40f));

            Vector2 tam = new Vector2(600f, 100f);
            Button continuar = FabricaUIArcade.CrearBoton(caja, "BotonContinuar", "CONTINUAR", kit.botonAmarillo, new Vector2(620f, 108f), 44, kit.iconoJugar, kit);
            FabricaUIArcade.Centrar((RectTransform)continuar.transform, new Vector2(0f, 175f), new Vector2(620f, 108f));
            continuar.onClick.AddListener(Continuar);

            Button reiniciar = FabricaUIArcade.CrearBoton(caja, "BotonReiniciar", "REINICIAR", kit.botonNaranja, tam, 40, kit.iconoReiniciar, kit);
            FabricaUIArcade.Centrar((RectTransform)reiniciar.transform, new Vector2(0f, 50f), tam);
            reiniciar.onClick.AddListener(Reiniciar);

            if (hayMenu)
            {
                Button menu = FabricaUIArcade.CrearBoton(caja, "BotonMenuPrincipal", "MENÚ PRINCIPAL", kit.botonRojo, tam, 40, kit.iconoCasa, kit);
                FabricaUIArcade.Centrar((RectTransform)menu.transform, new Vector2(0f, -70f), tam);
                menu.onClick.AddListener(PreguntarSalir);
                botonMenu = menu.gameObject;
                FabricaUIArcade.NavegacionVertical(continuar, reiniciar, menu);
            }
            else
            {
                FabricaUIArcade.NavegacionVertical(continuar, reiniciar);
            }

            // Controles a la vista.
            Text tituloControles = FabricaUIArcade.CrearTexto(caja, "TituloControles", "CONTROLES", 32, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, Px, 0f);
            FabricaUIArcade.Centrar(tituloControles.rectTransform, new Vector2(0f, -170f), new Vector2(800f, 44f));
            string controles = PreferenciasMenus.EsCelular
                ? "USA LOS BOTONES DE LA PANTALLA\nPARA CAMINAR, SALTAR Y REPARAR"
                : "A / D  CAMINAR    ESPACIO  SALTAR    S  BAJAR\nENTER  REPARAR    ESC  PAUSA    R  REINICIAR";
            Text textoControles = FabricaUIArcade.CrearTexto(caja, "Controles", controles, 24, PaletaArcade.Crema, TextAnchor.MiddleCenter, Px * 0.5f, 0f, true);
            textoControles.lineSpacing = 1.3f;
            FabricaUIArcade.Centrar(textoControles.rectTransform, new Vector2(0f, -238f), new Vector2(880f, 80f));

            Text ayuda = FabricaUIArcade.CrearTexto(caja, "Ayuda", PreferenciasMenus.EsCelular ? "" : "ESC  PARA CONTINUAR", 24,
                new Color(0.8f, 0.85f, 1f), TextAnchor.MiddleCenter, Px * 0.5f, 0f);
            FabricaUIArcade.Colocar(ayuda.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(700f, 36f));

            panel.seleccionInicial = continuar.gameObject;
            panel.OcultarInmediato();

            if (hayMenu) ConstruirConfirmacion(raiz, kit);
        }

        private void ConstruirConfirmacion(Transform raiz, KitSpritesArcade kit)
        {
            RectTransform caja;
            confirmacion = FabricaUIArcade.CrearVentana(raiz, "ConfirmarSalida", "¿SALIR AL MENÚ?", new Vector2(900f, 460f), kit, out caja, kit.panelRojo);

            Text aviso = FabricaUIArcade.CrearTexto(caja, "Aviso", "PERDERÁS LA PARTIDA ACTUAL", 32, PaletaArcade.Crema, TextAnchor.MiddleCenter, Px, 0f);
            FabricaUIArcade.Centrar(aviso.rectTransform, new Vector2(0f, 40f), new Vector2(820f, 50f));

            Vector2 tam = new Vector2(320f, 100f);
            Button si = FabricaUIArcade.CrearBoton(caja, "BotonSi", "SÍ", kit.botonRojo, tam, 44, null, kit);
            FabricaUIArcade.Centrar((RectTransform)si.transform, new Vector2(-190f, -100f), tam);
            si.onClick.AddListener(IrAlMenuPrincipal);

            Button no = FabricaUIArcade.CrearBoton(caja, "BotonNo", "NO", kit.botonAmarillo, tam, 44, null, kit);
            FabricaUIArcade.Centrar((RectTransform)no.transform, new Vector2(190f, -100f), tam);
            no.onClick.AddListener(NoSalir);

            FabricaUIArcade.NavegacionHorizontal(si, no);
            confirmacion.seleccionInicial = no.gameObject;   // por seguridad, NO queda elegido
            confirmacion.OcultarInmediato();
        }
    }
}
