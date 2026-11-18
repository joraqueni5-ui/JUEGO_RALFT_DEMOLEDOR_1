using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Pantalla "SUBIENDO AL PISO N - NOMBRE" que aparece al completar un piso: muestra la
    /// ficha informativa del piso siguiente (título, descripción, dato útil e ilustración
    /// opcional). Se cierra con el botón CONTINUAR, ENTER o ESPACIO. Funciona con el juego
    /// congelado (usa tiempo sin escala). Se construye por código, igual que MenuPausaArcade,
    /// así que no modifica ninguna escena.
    /// </summary>
    public class TransicionPiso : MonoBehaviour
    {
        private const float Px = FabricaUIArcade.Px;
        // Para que el ENTER del último martillazo no cierre la ficha sin querer.
        private const float EsperaAntesDeAceptarTecla = 0.6f;

        private PanelAnimado panel;
        private Text tituloVentana, textoPiso, textoNombre, textoFichaTitulo, textoDescripcion, textoDatoTitulo, textoDato;
        private Image ilustracion;
        private float horaApertura;

        public bool Abierta { get; private set; }

        /// <summary>Se dispara al cerrar la ficha (el juego puede continuar).</summary>
        public event Action Cerrada;

        public static TransicionPiso Crear(Transform padre)
        {
            GameObject objeto = new GameObject("TransicionPiso");
            objeto.transform.SetParent(padre, false);
            TransicionPiso t = objeto.AddComponent<TransicionPiso>();
            FabricaUIArcade.AsegurarEventSystem();
            t.Construir();
            return t;
        }

        /// <summary>Muestra la ficha del piso al que se va a subir.</summary>
        public void Mostrar(DefinicionPiso piso, string titulo = null, string cabecera = null)
        {
            if (piso == null) return;

            if (tituloVentana != null) tituloVentana.text = string.IsNullOrEmpty(titulo) ? "¡SUBIENDO!" : titulo;
            // Se usa el nombre del área, no el número del piso ("SUBIENDO A LABORATORIOS DE INFORMÁTICA").
            textoPiso.text = !string.IsNullOrEmpty(cabecera) ? cabecera : piso.numero == 0 ? "BIENVENIDO A" : "SUBIENDO A";
            textoNombre.text = piso.NombreArea;
            textoNombre.fontSize = piso.NombreArea.Length > 22 ? 38 : piso.NombreArea.Length > 14 ? 46 : 52;
            textoFichaTitulo.text = piso.numero >= 1 && piso.numero <= 6 ? "Subiendo a " + piso.NombreArea
                : string.IsNullOrEmpty(piso.fichaTitulo) ? piso.NombreArea : piso.fichaTitulo;
            textoDescripcion.text = piso.fichaDescripcion;
            bool hayDato = !string.IsNullOrEmpty(piso.fichaDatoUtil);
            textoDatoTitulo.gameObject.SetActive(hayDato);
            textoDato.gameObject.SetActive(hayDato);
            textoDato.text = piso.fichaDatoUtil;

            // Con ilustración, el texto se corre a la derecha; sin ella, ocupa todo el ancho.
            bool hayImagen = piso.fichaIlustracion != null;
            ilustracion.gameObject.SetActive(hayImagen);
            ilustracion.sprite = piso.fichaIlustracion;
            float centro = hayImagen ? 170f : 0f;
            float ancho = hayImagen ? 640f : 940f;
            Colocar(textoFichaTitulo, centro, 90f, ancho, 48f);
            Colocar(textoDescripcion, centro, -25f, ancho, 150f);
            Colocar(textoDatoTitulo, centro, -130f, ancho, 36f);
            Colocar(textoDato, centro, -190f, ancho, 90f);

            Abierta = true;
            horaApertura = Time.unscaledTime;
            panel.Mostrar();
        }

        public void Cerrar()
        {
            if (!Abierta) return;
            Abierta = false;
            panel.Ocultar();
            if (Cerrada != null) Cerrada();
        }

        private void Update()
        {
            if (!Abierta || Time.unscaledTime < horaApertura + EsperaAntesDeAceptarTecla) return;

            Keyboard teclado = Keyboard.current;
            if (teclado == null) return;
            if (teclado[Key.Enter].wasPressedThisFrame || teclado[Key.NumpadEnter].wasPressedThisFrame ||
                teclado[Key.Space].wasPressedThisFrame)
            {
                Cerrar();
            }
        }

        // ------------------------------------------------------------ construcción
        private void Construir()
        {
            KitSpritesArcade kit = KitSpritesArcade.Actual();

            // Por encima de la pausa (300) y de los controles táctiles (110).
            Canvas canvas = FabricaUIArcade.CrearCanvas("CanvasTransicionPiso", 320);
            canvas.transform.SetParent(transform, false);

            RectTransform caja;
            panel = FabricaUIArcade.CrearVentana(canvas.transform, "PanelTransicionPiso", "¡SUBIENDO!",
                new Vector2(1100f, 860f), kit, out caja, null, kit.iconoArriba);

            textoPiso = FabricaUIArcade.CrearTexto(caja, "Piso", "", 76, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, Px, Px);
            FabricaUIArcade.Colocar(textoPiso.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 285f), new Vector2(1000f, 90f));

            textoNombre = FabricaUIArcade.CrearTexto(caja, "Nombre", "", 52, PaletaArcade.Crema, TextAnchor.MiddleCenter, Px, Px);
            FabricaUIArcade.Colocar(textoNombre.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 195f), new Vector2(1000f, 64f));

            ilustracion = FabricaUIArcade.CrearImagen(caja, "Ilustracion", null, Color.white);
            ilustracion.preserveAspect = true;
            FabricaUIArcade.Colocar(ilustracion.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-340f, -40f), new Vector2(280f, 280f));
            ilustracion.gameObject.SetActive(false);

            textoFichaTitulo = FabricaUIArcade.CrearTexto(caja, "FichaTitulo", "", 34, PaletaArcade.AmarilloClaro, TextAnchor.MiddleCenter, Px * 0.75f, 0f);
            textoDescripcion = FabricaUIArcade.CrearTexto(caja, "Descripcion", "", 26, PaletaArcade.Crema, TextAnchor.UpperCenter, Px * 0.5f, 0f, true);
            textoDatoTitulo = FabricaUIArcade.CrearTexto(caja, "DatoTitulo", "DATO ÚTIL", 26, PaletaArcade.Naranja, TextAnchor.MiddleCenter, Px * 0.75f, 0f);
            textoDato = FabricaUIArcade.CrearTexto(caja, "Dato", "", 24, PaletaArcade.Crema, TextAnchor.UpperCenter, Px * 0.5f, 0f, true);
            textoDescripcion.lineSpacing = 1.25f;
            textoDato.lineSpacing = 1.25f;
            Ajustar(textoDescripcion);
            Ajustar(textoDato);

            Button continuar = FabricaUIArcade.CrearBoton(caja, "BotonContinuar", "CONTINUAR", kit.botonAmarillo,
                new Vector2(620f, 108f), 44, kit.iconoJugar, kit);
            FabricaUIArcade.Centrar((RectTransform)continuar.transform, new Vector2(0f, -315f), new Vector2(620f, 108f));
            continuar.onClick.AddListener(Cerrar);
            panel.seleccionInicial = continuar.gameObject;

            Text ayuda = FabricaUIArcade.CrearTexto(caja, "Ayuda", PreferenciasMenus.EsCelular ? "" : "ENTER  PARA CONTINUAR", 22,
                new Color(0.8f, 0.85f, 1f), TextAnchor.MiddleCenter, Px * 0.5f, 0f);
            FabricaUIArcade.Colocar(ayuda.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(700f, 32f));

            foreach (Text t in GetComponentsInChildren<Text>(true))
                if (t.text == "¡SUBIENDO!") { tituloVentana = t; break; }

            panel.OcultarInmediato();
        }

        /// <summary>Los textos largos saltan de línea dentro de su caja.</summary>
        private static void Ajustar(Text texto)
        {
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void Colocar(Text texto, float centroX, float y, float ancho, float alto)
        {
            FabricaUIArcade.Colocar(texto.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(centroX, y), new Vector2(ancho, alto));
        }
    }
}
