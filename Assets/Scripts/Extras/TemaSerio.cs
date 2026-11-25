using UnityEngine;
using UnityEngine.UI;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Tema "serio" y uniforme para todos los menús: un solo estilo de botón (acero oscuro con filo ámbar), marco de
    /// selección ámbar, paneles de acero, franja de obra sobria y el logotipo DEMOLEDOR / UNIFRANZ en lugar del cartel amarillo.
    /// En el menú principal cambia el muro por el edificio de noche en demolición (FondoMenuAnimado le da reflectores y polvo).
    ///
    /// No modifica las escenas: InstaladorExtras lo aplica al cargar cada escena (cambia los sprites por nombre) y
    /// KitSpritesArcade.Actual() lo aplica al kit, así el menú de pausa que se crea por código también sale con el tema.
    /// Los sprites están en Assets/Resources/Extras/Tema (Tools/generar_tema_menu.py).
    /// </summary>
    public static class TemaSerio
    {
        /// <summary>Apagar para volver a ver los menús de colores originales.</summary>
        public static bool Activo = true;

        private static Sprite boton, marco, panel, franja, logo, fondo;
        private static bool cargado;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEstatico()
        {
            cargado = false;
            boton = marco = panel = franja = logo = fondo = null;
        }

        private static bool Cargar()
        {
            if (!cargado)
            {
                cargado = true;
                boton = ArteExtra.Uno("Tema/boton_serio");
                marco = ArteExtra.Uno("Tema/marco_serio");
                panel = ArteExtra.Uno("Tema/panel_serio");
                franja = ArteExtra.Uno("Tema/franja_seria");
                logo = ArteExtra.Uno("Tema/logo_demoledor");
                fondo = ArteExtra.Uno("Tema/fondo_menu_noche");
            }
            return Activo && boton != null && panel != null;
        }

        /// <summary>Cambia los sprites del kit por los del tema (una sola vez por kit).</summary>
        public static void Parchear(KitSpritesArcade kit)
        {
            if (kit == null || kit.tematizado || !Cargar()) return;
            kit.tematizado = true;
            kit.botonAmarillo = kit.botonNaranja = kit.botonAzul = kit.botonRojo = kit.botonMorado = boton;
            if (marco != null) kit.marcoSeleccion = marco;
            kit.panel = kit.panelRojo = kit.panelMorado = panel;
            if (franja != null) kit.franja = franja;
        }

        /// <summary>Aplica el tema a todas las imágenes de la escena (también las de ventanas ocultas).</summary>
        public static void RestilizarEscena()
        {
            if (!Cargar()) return;
            Image[] imagenes = Object.FindObjectsByType<Image>(FindObjectsInactive.Include);
            foreach (Image img in imagenes)
            {
                if (img == null || img.sprite == null) continue;
                string n = img.sprite.name;
                if (n.StartsWith("boton_px_")) img.sprite = boton;
                else if (n == "marco_seleccion" && marco != null) img.sprite = marco;
                else if (n.StartsWith("panel_px")) img.sprite = panel;
                else if (n == "franja_px" && franja != null) img.sprite = franja;
                else if (n == "cartel_px" && logo != null) ConvertirEnLogo(img);
                // Solo el muro de fondo del menú: la transición de ladrillos usa el mismo sprite y no se toca.
                else if (n == "muro_px" && img.name == "Muro" && fondo != null) ConvertirEnFondo(img);
            }

            // Menos es más: sin el velo azul, sin la decoración vieja (hueco, demoledor y muñeco fijos) y sin la caja
            // alrededor de los botones; el fondo es una mini partida en vivo (FondoPartidaMenu).
            foreach (Image img in imagenes)
            {
                if (img == null) continue;
                Transform padre = img.transform.parent;
                bool enRespaldo = padre != null && padre.name == "Respaldo";
                if (img.name == "VeloAzul") img.color = new Color(0.02f, 0.03f, 0.08f, 0f);
                else if (enRespaldo && (img.name == "Hueco" || img.name == "Demoledor" || img.name == "Muneco")) img.gameObject.SetActive(false);
                else if (img.name == "PanelBotones")
                {
                    img.color = new Color(1f, 1f, 1f, 0f);
                    foreach (Transform h in img.transform) if (h.name.StartsWith("Franja")) h.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>El cartel amarillo con los textos pasa a ser el logotipo (se ocultan los textos y la franja).</summary>
        private static void ConvertirEnLogo(Image cartel)
        {
            cartel.sprite = logo;
            cartel.type = Image.Type.Simple;
            cartel.preserveAspect = true;
            cartel.color = Color.white;
            foreach (Transform hijo in cartel.transform)
            {
                if (hijo.GetComponent<Text>() != null || hijo.name.StartsWith("Franja")) hijo.gameObject.SetActive(false);
            }
        }

        private static void ConvertirEnFondo(Image muro)
        {
            Sprite piso = ArteExtra.Uno("Menu/piso_menu");
            muro.sprite = piso != null ? piso : fondo;
            muro.type = Image.Type.Simple;
            muro.preserveAspect = false;
            muro.color = Color.white;
            AspectRatioFitter ajuste = muro.GetComponent<AspectRatioFitter>();
            if (ajuste == null) ajuste = muro.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            ajuste.aspectRatio = 16f / 9f;
            Transform respaldo = muro.transform.parent;
            if (piso != null)
            {
                if (muro.GetComponent<FondoPartidaMenu>() == null) muro.gameObject.AddComponent<FondoPartidaMenu>();
            }
            else if (respaldo != null && respaldo.GetComponent<FondoMenuAnimado>() == null)
            {
                FondoMenuAnimado anim = respaldo.gameObject.AddComponent<FondoMenuAnimado>();
                anim.despuesDe = muro.transform;
            }
        }
    }
}
