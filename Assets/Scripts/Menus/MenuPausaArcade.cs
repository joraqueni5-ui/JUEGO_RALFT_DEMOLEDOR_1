using UnityEngine;
using UnityEngine.UI;
using DemoledorUnifranz.Game;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Parte visual de la pausa de la escena Universidad. La lógica (Escape,
    /// Time.timeScale, bloquear al muñeco) sigue en GestorJuego; este script solo
    /// dibuja la ventana y avisa a GestorJuego cuando se pulsa un botón.
    ///
    /// - Ventana PAUSA sobre el juego congelado (el edificio se ve detrás) con
    ///   CONTINUAR, REINICIAR y MENÚ PRINCIPAL, y el estado de la partida.
    /// - Botón ⏸ en pantalla (arriba a la izquierda) para jugar en el celular.
    /// Se construye por código, igual que el HUD de GestorJuego, así que la escena
    /// Universidad no se modifica.
    /// </summary>
    public class MenuPausaArcade : MonoBehaviour
    {
        private GestorJuego gestor;
        private PanelAnimado panel;
        private Text textoEstado;
        private GameObject botonPausa;

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
                textoEstado.text = "PUNTOS " + puntos + "    ·    REPARACIONES " + reparaciones +
                                   "    ·    " + Mathf.RoundToInt(destruido * 100f) + "% DESTRUIDO";
            }
            if (panel != null) panel.Mostrar();
        }

        public void Ocultar()
        {
            if (panel != null) panel.Ocultar();
        }

        /// <summary>Oculta la ventana al instante (al reiniciar o cambiar de escena).</summary>
        public void OcultarInmediato()
        {
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
            KitSpritesArcade kit = KitSpritesArcade.EnMemoria();
            bool hayMenu = NavegadorEscenas.Disponible(Escenas.MenuPrincipal);
            if (!hayMenu)
                Debug.LogWarning("[Menus] MenuPrincipal no está en Build Settings: se oculta el botón MENÚ PRINCIPAL de la pausa.");

            // Por encima del HUD (100) y de los controles táctiles (110).
            Canvas canvas = FabricaUIArcade.CrearCanvas("CanvasPausa", 300);
            canvas.transform.SetParent(transform, false);
            Transform raiz = canvas.transform;

            // Botón de pausa (arriba a la izquierda; ahí no hay controles táctiles).
            Button pausa = FabricaUIArcade.CrearBoton(raiz, "BotonPausa", "", kit.botonAzul, new Vector2(112f, 96f), 10);
            RectTransform rp = (RectTransform)pausa.transform;
            rp.anchorMin = rp.anchorMax = new Vector2(0f, 1f);
            rp.pivot = new Vector2(0f, 1f);
            rp.anchoredPosition = new Vector2(24f, -20f);
            for (int i = 0; i < 2; i++)
            {
                Image barra = FabricaUIArcade.CrearImagen(pausa.transform, "Barra" + i, null, PaletaArcade.Crema);
                FabricaUIArcade.Centrar(barra.rectTransform, new Vector2(i == 0 ? -13f : 13f, 5f), new Vector2(14f, 42f));
                Outline o = barra.gameObject.AddComponent<Outline>();
                o.effectColor = PaletaArcade.Contorno;
                o.effectDistance = new Vector2(3f, -3f);
            }
            pausa.onClick.AddListener(AbrirPausa);
            botonPausa = pausa.gameObject;

            // Ventana de pausa.
            RectTransform caja;
            float alto = hayMenu ? 660f : 540f;
            panel = FabricaUIArcade.CrearVentana(raiz, "PanelPausa", "PAUSA", new Vector2(820f, alto), kit, out caja);

            textoEstado = FabricaUIArcade.CrearTexto(caja, "Estado", "", 30, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, 2f, 2f);
            FabricaUIArcade.Colocar(textoEstado.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(760f, 44f));

            float y = hayMenu ? 70f : 20f;
            Button continuar = FabricaUIArcade.CrearBoton(caja, "BotonContinuar", "CONTINUAR", kit.botonAmarillo, new Vector2(520f, 116f), 52);
            FabricaUIArcade.Centrar((RectTransform)continuar.transform, new Vector2(0f, y), new Vector2(520f, 116f));
            continuar.onClick.AddListener(Continuar);

            Button reiniciar = FabricaUIArcade.CrearBoton(caja, "BotonReiniciar", "REINICIAR", kit.botonNaranja, new Vector2(500f, 100f), 44);
            FabricaUIArcade.Centrar((RectTransform)reiniciar.transform, new Vector2(0f, y - 130f), new Vector2(500f, 100f));
            reiniciar.onClick.AddListener(Reiniciar);

            if (hayMenu)
            {
                Button menu = FabricaUIArcade.CrearBoton(caja, "BotonMenuPrincipal", "MENÚ PRINCIPAL", kit.botonRojo, new Vector2(500f, 100f), 44);
                FabricaUIArcade.Centrar((RectTransform)menu.transform, new Vector2(0f, y - 250f), new Vector2(500f, 100f));
                menu.onClick.AddListener(IrAlMenuPrincipal);
                FabricaUIArcade.NavegacionVertical(continuar, reiniciar, menu);
            }
            else
            {
                FabricaUIArcade.NavegacionVertical(continuar, reiniciar);
            }

            Text ayuda = FabricaUIArcade.CrearTexto(caja, "Ayuda", "ESC  para continuar", 26, new Color(0.8f, 0.85f, 1f), TextAnchor.MiddleCenter, 2f, 0f);
            FabricaUIArcade.Colocar(ayuda.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(600f, 40f));

            panel.seleccionInicial = continuar.gameObject;
            panel.OcultarInmediato();
        }
    }
}
