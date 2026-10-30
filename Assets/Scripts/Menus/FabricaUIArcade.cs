using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Piezas de interfaz con el estilo de DEMOLEDOR UNIFRANZ (canvas, textos con
    /// contorno, botones arcade, ventanas, slider y casilla). La usan tanto el
    /// constructor del editor (Tools → Demoledor Unifranz) como el menú de pausa en tiempo de juego,
    /// así todos los menús se ven iguales.
    /// </summary>
    public static class FabricaUIArcade
    {
        public static readonly Vector2 Referencia = new Vector2(1920f, 1080f);

        private static Font fuente;

        /// <summary>Fuente de los textos. Si no se asigna, usa la fuente integrada de Unity.</summary>
        public static Font Fuente
        {
            get
            {
                if (fuente == null) fuente = FuenteIntegrada();
                return fuente;
            }
            set { fuente = value; }
        }

        public static Font FuenteIntegrada()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { f = null; }
            if (f == null)
            {
                try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch { f = null; }
            }
            return f;
        }

        // ------------------------------------------------------------ básicos
        public static RectTransform CrearObjeto(string nombre, Transform padre)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            if (padre != null) objeto.transform.SetParent(padre, false);
            objeto.layer = 5; // UI
            return (RectTransform)objeto.transform;
        }

        public static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Colocar(RectTransform rt, Vector2 ancla, Vector2 posicion, Vector2 tamano)
        {
            rt.anchorMin = ancla;
            rt.anchorMax = ancla;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = posicion;
            rt.sizeDelta = tamano;
        }

        public static void Centrar(RectTransform rt, Vector2 posicion, Vector2 tamano)
        {
            Colocar(rt, new Vector2(0.5f, 0.5f), posicion, tamano);
        }

        /// <summary>Canvas en pantalla con escalado por resolución (1920×1080, mezcla 0.5).</summary>
        public static Canvas CrearCanvas(string nombre, int orden)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            objeto.layer = 5;
            Canvas canvas = objeto.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = orden;

            CanvasScaler escalador = objeto.GetComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = Referencia;
            escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escalador.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        /// <summary>Crea un EventSystem si la escena no tiene uno (los botones lo necesitan).</summary>
        public static EventSystem AsegurarEventSystem()
        {
            EventSystem existente = Object.FindAnyObjectByType<EventSystem>();
            if (existente != null) return existente;

            // Módulo del Input System: funciona con mouse, teclado, mando y pantalla táctil.
            GameObject objeto = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return objeto.GetComponent<EventSystem>();
        }

        public static Image CrearImagen(Transform padre, string nombre, Sprite sprite, Color color, bool recibeClics = false)
        {
            RectTransform rt = CrearObjeto(nombre, padre);
            Image imagen = rt.gameObject.AddComponent<Image>();
            imagen.sprite = sprite;
            imagen.color = color;
            imagen.raycastTarget = recibeClics;
            if (sprite != null && sprite.border != Vector4.zero) imagen.type = Image.Type.Sliced;
            return imagen;
        }

        /// <summary>Texto grande con contorno oscuro grueso y sombra, estilo arcade.</summary>
        public static Text CrearTexto(Transform padre, string nombre, string contenido, int tamano, Color color,
            TextAnchor alineacion = TextAnchor.MiddleCenter, float contorno = 3f, float sombra = 4f)
        {
            RectTransform rt = CrearObjeto(nombre, padre);
            Text texto = rt.gameObject.AddComponent<Text>();
            texto.font = Fuente;
            texto.text = contenido;
            texto.fontSize = tamano;
            texto.fontStyle = FontStyle.Bold;
            texto.color = color;
            texto.alignment = alineacion;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.supportRichText = true;
            texto.raycastTarget = false;

            if (sombra > 0f)
            {
                Shadow s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.55f);
                s.effectDistance = new Vector2(sombra * 0.6f, -sombra);
            }
            if (contorno > 0f)
            {
                Outline o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = PaletaArcade.Contorno;
                o.effectDistance = new Vector2(contorno, -contorno);
            }
            return texto;
        }

        // ------------------------------------------------------------ botones
        /// <summary>Botón arcade con etiqueta, efectos de escala y sonido (BotonArcade).</summary>
        public static Button CrearBoton(Transform padre, string nombre, string etiqueta, Sprite sprite,
            Vector2 tamano, int tamanoFuente)
        {
            Image fondo = CrearImagen(padre, nombre, sprite, Color.white, true);
            fondo.rectTransform.sizeDelta = tamano;

            Button boton = fondo.gameObject.AddComponent<Button>();
            boton.targetGraphic = fondo;
            ColorBlock colores = boton.colors;
            colores.normalColor = Color.white;
            colores.highlightedColor = new Color(1f, 1f, 0.92f);
            colores.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            colores.selectedColor = new Color(1f, 1f, 0.92f);
            colores.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.6f);
            colores.fadeDuration = 0.06f;
            boton.colors = colores;

            Text texto = CrearTexto(fondo.transform, "Etiqueta", etiqueta, tamanoFuente, PaletaArcade.Crema,
                TextAnchor.MiddleCenter, 3f, 0f);
            RectTransform rt = texto.rectTransform;
            Estirar(rt);
            rt.offsetMin = new Vector2(16f, 8f);   // deja ver el canto inferior del botón
            rt.offsetMax = new Vector2(-16f, -2f);

            fondo.gameObject.AddComponent<BotonArcade>();
            return boton;
        }

        /// <summary>Configura la navegación con teclado/mando de una columna de botones.</summary>
        public static void NavegacionVertical(params Selectable[] elementos)
        {
            for (int i = 0; i < elementos.Length; i++)
            {
                Navigation n = new Navigation { mode = Navigation.Mode.Explicit };
                n.selectOnUp = elementos[(i - 1 + elementos.Length) % elementos.Length];
                n.selectOnDown = elementos[(i + 1) % elementos.Length];
                elementos[i].navigation = n;
            }
        }

        // ------------------------------------------------------------ ventanas
        /// <summary>
        /// Ventana modal: velo oscuro a pantalla completa + caja con marco y título.
        /// Devuelve el PanelAnimado (raíz) y la caja donde van los controles.
        /// </summary>
        public static PanelAnimado CrearVentana(Transform padre, string nombre, string titulo, Vector2 tamano,
            KitSpritesArcade kit, out RectTransform caja)
        {
            RectTransform raiz = CrearObjeto(nombre, padre);
            Estirar(raiz);
            raiz.gameObject.AddComponent<CanvasGroup>();

            Image velo = CrearImagen(raiz, "Velo", null, PaletaArcade.Velo, true);
            Estirar(velo.rectTransform);

            Image marco = CrearImagen(raiz, "Caja", kit.panel, Color.white, true);
            caja = marco.rectTransform;
            Centrar(caja, Vector2.zero, tamano);

            // Franja de peligro como cabecera.
            if (kit.franja != null)
            {
                Image franja = CrearImagen(caja, "Franja", kit.franja, Color.white);
                franja.type = Image.Type.Tiled;
                RectTransform rf = franja.rectTransform;
                rf.anchorMin = new Vector2(0f, 1f);
                rf.anchorMax = new Vector2(1f, 1f);
                rf.pivot = new Vector2(0.5f, 1f);
                rf.offsetMin = new Vector2(20f, -52f);
                rf.offsetMax = new Vector2(-20f, -20f);
            }

            // Cartel del título encima del marco.
            Image cartel = CrearImagen(caja, "Cartel", kit.botonNaranja, Color.white);
            Colocar(cartel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 6f), new Vector2(Mathf.Min(620f, tamano.x - 80f), 104f));
            Text textoTitulo = CrearTexto(cartel.transform, "Titulo", titulo, 56, PaletaArcade.Crema, TextAnchor.MiddleCenter, 4f, 4f);
            Estirar(textoTitulo.rectTransform);
            textoTitulo.rectTransform.offsetMin = new Vector2(0f, 10f);

            PanelAnimado panel = raiz.gameObject.AddComponent<PanelAnimado>();
            panel.contenido = caja;
            return panel;
        }

        // ------------------------------------------------------------ controles
        /// <summary>Slider de 0 a 1 con riel, relleno degradado y perilla.</summary>
        public static Slider CrearSlider(Transform padre, string nombre, KitSpritesArcade kit, Vector2 tamano)
        {
            RectTransform raiz = CrearObjeto(nombre, padre);
            raiz.sizeDelta = tamano;
            Slider slider = raiz.gameObject.AddComponent<Slider>();

            Image riel = CrearImagen(raiz, "Riel", kit.barra, Color.white, true);
            Estirar(riel.rectTransform);
            riel.rectTransform.offsetMin = new Vector2(0f, tamano.y * 0.18f);
            riel.rectTransform.offsetMax = new Vector2(0f, -tamano.y * 0.18f);

            RectTransform areaRelleno = CrearObjeto("AreaRelleno", raiz);
            Estirar(areaRelleno);
            areaRelleno.offsetMin = new Vector2(14f, tamano.y * 0.18f);
            areaRelleno.offsetMax = new Vector2(-14f, -tamano.y * 0.18f);

            Image relleno = CrearImagen(areaRelleno, "Relleno", kit.relleno, Color.white);
            RectTransform rr = relleno.rectTransform;
            rr.anchorMin = Vector2.zero;
            rr.anchorMax = new Vector2(0f, 1f);
            rr.pivot = new Vector2(0.5f, 0.5f);
            rr.sizeDelta = new Vector2(28f, 0f);

            RectTransform areaPerilla = CrearObjeto("AreaPerilla", raiz);
            Estirar(areaPerilla);
            areaPerilla.offsetMin = new Vector2(tamano.y * 0.5f, 0f);
            areaPerilla.offsetMax = new Vector2(-tamano.y * 0.5f, 0f);

            Image perilla = CrearImagen(areaPerilla, "Perilla", kit.perilla, Color.white, true);
            RectTransform rp = perilla.rectTransform;
            rp.anchorMin = Vector2.zero;
            rp.anchorMax = new Vector2(0f, 1f);
            rp.pivot = new Vector2(0.5f, 0.5f);
            rp.sizeDelta = new Vector2(tamano.y + 8f, 8f);
            perilla.preserveAspect = true;

            slider.fillRect = rr;
            slider.handleRect = rp;
            slider.targetGraphic = perilla;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 0.8f;

            ColorBlock colores = slider.colors;
            colores.highlightedColor = new Color(1f, 1f, 0.85f);
            colores.selectedColor = new Color(1f, 1f, 0.85f);
            colores.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            slider.colors = colores;
            return slider;
        }

        /// <summary>Casilla de verificación con texto a la derecha.</summary>
        public static Toggle CrearToggle(Transform padre, string nombre, KitSpritesArcade kit, float tamano, out Text etiqueta)
        {
            RectTransform raiz = CrearObjeto(nombre, padre);
            raiz.sizeDelta = new Vector2(tamano + 340f, tamano);
            Toggle toggle = raiz.gameObject.AddComponent<Toggle>();

            Image casilla = CrearImagen(raiz, "Casilla", kit.casilla, Color.white, true);
            RectTransform rc = casilla.rectTransform;
            rc.anchorMin = new Vector2(0f, 0.5f);
            rc.anchorMax = new Vector2(0f, 0.5f);
            rc.pivot = new Vector2(0f, 0.5f);
            rc.anchoredPosition = Vector2.zero;
            rc.sizeDelta = new Vector2(tamano, tamano);

            Image marca = CrearImagen(casilla.transform, "Marca", kit.marca, Color.white);
            Estirar(marca.rectTransform);
            marca.rectTransform.offsetMin = new Vector2(-4f, -2f);
            marca.rectTransform.offsetMax = new Vector2(6f, 8f);

            etiqueta = CrearTexto(raiz, "Estado", "ACTIVADO", 38, PaletaArcade.Crema, TextAnchor.MiddleLeft, 3f, 3f);
            RectTransform re = etiqueta.rectTransform;
            re.anchorMin = new Vector2(0f, 0f);
            re.anchorMax = new Vector2(1f, 1f);
            re.offsetMin = new Vector2(tamano + 22f, 0f);
            re.offsetMax = Vector2.zero;

            toggle.targetGraphic = casilla;
            toggle.graphic = marca;
            toggle.isOn = true;
            return toggle;
        }
    }
}
