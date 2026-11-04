using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Piezas de interfaz pixel art de DEMOLEDOR UNIFRANZ (canvas, textos con
    /// contorno, botones con icono, ventanas, slider y casilla). La usan tanto los
    /// constructores del editor (Tools → Demoledor Unifranz) como el menú de pausa
    /// en tiempo de juego, así todos los menús se ven iguales.
    /// </summary>
    public static class FabricaUIArcade
    {
        public static readonly Vector2 Referencia = new Vector2(1920f, 1080f);

        /// <summary>Un pixel del arte ampliado (×4) en unidades de UI.</summary>
        public const float Px = GeneradorPixelArt.Escala;

        private static Font fuente;
        private static Font fuenteCuerpo;

        /// <summary>Fuente de títulos y botones. Sin asignar: la de RecursosMenus o la integrada.</summary>
        public static Font Fuente
        {
            get
            {
                if (fuente == null) fuente = RecursosMenus.FuenteTitulos != null ? RecursosMenus.FuenteTitulos : FuenteIntegrada();
                return fuente;
            }
            set { fuente = value; }
        }

        /// <summary>Fuente de textos largos. Sin asignar: la de títulos.</summary>
        public static Font FuenteCuerpo
        {
            get
            {
                if (fuenteCuerpo == null) fuenteCuerpo = RecursosMenus.FuenteCuerpo;
                return fuenteCuerpo != null ? fuenteCuerpo : Fuente;
            }
            set { fuenteCuerpo = value; }
        }

        /// <summary>True si la fuente actual es la pixel (cambia el estilo: sin negrita sintética).</summary>
        public static bool FuentePixel { get { return Fuente != null && Fuente.name.Contains("Silkscreen"); } }

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEstatico()
        {
            fuente = null;
            fuenteCuerpo = null;
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
            canvas.pixelPerfect = false;

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

        /// <summary>
        /// Image con el sprite indicado. Los sprites pixel art (filtro Point) se amplían ×4
        /// también en sus bordes 9-slice; los que se repiten (franja, muro) usan Tiled.
        /// </summary>
        public static Image CrearImagen(Transform padre, string nombre, Sprite sprite, Color color, bool recibeClics = false)
        {
            RectTransform rt = CrearObjeto(nombre, padre);
            Image imagen = rt.gameObject.AddComponent<Image>();
            imagen.sprite = sprite;
            imagen.color = color;
            imagen.raycastTarget = recibeClics;
            if (sprite != null)
            {
                bool pixel = sprite.texture != null && sprite.texture.filterMode == FilterMode.Point;
                bool repite = sprite.texture != null && sprite.texture.wrapMode == TextureWrapMode.Repeat;
                if (repite) imagen.type = Image.Type.Tiled;
                else if (sprite.border != Vector4.zero) imagen.type = Image.Type.Sliced;
                if (pixel) imagen.pixelsPerUnitMultiplier = KitSpritesArcade.MultiplicadorPixel;
            }
            return imagen;
        }

        /// <summary>Icono pixel 7×7 ampliado (28×28 por defecto).</summary>
        public static Image CrearIcono(Transform padre, string nombre, Sprite icono, Color color, float tamano = 28f)
        {
            Image img = CrearImagen(padre, nombre, icono, color);
            img.rectTransform.sizeDelta = new Vector2(tamano, tamano);
            img.preserveAspect = true;
            return img;
        }

        /// <summary>Texto con contorno oscuro y sombra, estilo arcade.</summary>
        public static Text CrearTexto(Transform padre, string nombre, string contenido, int tamano, Color color,
            TextAnchor alineacion = TextAnchor.MiddleCenter, float contorno = 4f, float sombra = 4f, bool cuerpo = false)
        {
            RectTransform rt = CrearObjeto(nombre, padre);
            Text texto = rt.gameObject.AddComponent<Text>();
            texto.font = cuerpo ? FuenteCuerpo : Fuente;
            texto.text = contenido;
            texto.fontSize = tamano;
            texto.fontStyle = FuentePixel ? FontStyle.Normal : FontStyle.Bold;
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
                s.effectDistance = new Vector2(sombra, -sombra);
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
        /// <summary>
        /// Botón pixel con icono opcional a la izquierda, marco blanco y flecha al estar
        /// seleccionado, efecto de escala, sonido y vibración (BotonArcade).
        /// </summary>
        public static Button CrearBoton(Transform padre, string nombre, string etiqueta, Sprite sprite,
            Vector2 tamano, int tamanoFuente, Sprite icono = null, KitSpritesArcade kit = null, bool conFlecha = true)
        {
            if (kit == null) kit = KitSpritesArcade.Actual();

            Image fondo = CrearImagen(padre, nombre, sprite, Color.white, true);
            fondo.rectTransform.sizeDelta = tamano;

            Button boton = fondo.gameObject.AddComponent<Button>();
            boton.targetGraphic = fondo;
            ColorBlock colores = boton.colors;
            colores.normalColor = Color.white;
            colores.highlightedColor = new Color(1f, 1f, 0.9f);
            colores.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colores.selectedColor = new Color(1f, 1f, 0.9f);
            colores.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.6f);
            colores.fadeDuration = 0.05f;
            boton.colors = colores;

            // El canto inferior del botón ocupa 5 px del arte: el contenido se centra en la cara.
            float caraAbajo = 5f * Px, caraArriba = 3f * Px;
            float izquierda = 16f;

            if (icono != null)
            {
                Image ico = CrearIcono(fondo.transform, "Icono", icono, PaletaArcade.Crema, Mathf.Min(36f, tamano.y - 40f));
                RectTransform ri = ico.rectTransform;
                ri.anchorMin = ri.anchorMax = new Vector2(0f, 0.5f);
                ri.pivot = new Vector2(0f, 0.5f);
                ri.anchoredPosition = new Vector2(24f, (caraAbajo - caraArriba) * 0.5f);
                izquierda = 24f + ri.sizeDelta.x + 8f;
            }

            if (!string.IsNullOrEmpty(etiqueta))
            {
                Text texto = CrearTexto(fondo.transform, "Etiqueta", etiqueta, tamanoFuente, PaletaArcade.Crema,
                    TextAnchor.MiddleCenter, 4f, 0f);
                RectTransform rt = texto.rectTransform;
                Estirar(rt);
                rt.offsetMin = new Vector2(icono != null ? izquierda * 0.5f : 12f, caraAbajo);
                rt.offsetMax = new Vector2(-12f, -caraArriba);
            }

            // Marco blanco y flecha de selección (se encienden con el mouse o el teclado).
            BotonArcade efecto = fondo.gameObject.AddComponent<BotonArcade>();
            if (kit.marcoSeleccion != null)
            {
                Image marco = CrearImagen(fondo.transform, "MarcoSeleccion", kit.marcoSeleccion, Color.white);
                Estirar(marco.rectTransform);
                marco.rectTransform.offsetMin = new Vector2(-Px * 2f, -Px * 2f);
                marco.rectTransform.offsetMax = new Vector2(Px * 2f, Px * 2f);
                marco.gameObject.SetActive(false);
                efecto.marcoSeleccion = marco.gameObject;
            }
            if (conFlecha && kit.iconoFlecha != null)
            {
                Image flecha = CrearIcono(fondo.transform, "Flecha", kit.iconoFlecha, Color.white, 28f);
                RectTransform rf = flecha.rectTransform;
                rf.anchorMin = rf.anchorMax = new Vector2(0f, 0.5f);
                rf.pivot = new Vector2(1f, 0.5f);
                rf.anchoredPosition = new Vector2(-14f, 0f);
                Outline o = flecha.gameObject.AddComponent<Outline>();
                o.effectColor = PaletaArcade.Contorno;
                o.effectDistance = new Vector2(Px, -Px);
                flecha.gameObject.SetActive(false);
                efecto.flecha = rf;
            }
            return boton;
        }

        /// <summary>Botón cuadrado solo con icono (por ejemplo, la pausa en el celular).</summary>
        public static Button CrearBotonIcono(Transform padre, string nombre, Sprite sprite, Sprite icono, float tamano, KitSpritesArcade kit = null)
        {
            Button b = CrearBoton(padre, nombre, "", sprite, new Vector2(tamano, tamano), 10, null, kit, false);
            Image ico = CrearIcono(b.transform, "Icono", icono, PaletaArcade.Crema, tamano * 0.42f);
            ico.rectTransform.anchoredPosition = new Vector2(0f, Px);
            ico.transform.SetSiblingIndex(0);
            return b;
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

        /// <summary>Configura la navegación con teclado/mando de una fila de botones.</summary>
        public static void NavegacionHorizontal(params Selectable[] elementos)
        {
            for (int i = 0; i < elementos.Length; i++)
            {
                Navigation n = new Navigation { mode = Navigation.Mode.Explicit };
                n.selectOnLeft = elementos[(i - 1 + elementos.Length) % elementos.Length];
                n.selectOnRight = elementos[(i + 1) % elementos.Length];
                elementos[i].navigation = n;
            }
        }

        // ------------------------------------------------------------ marcos y ventanas
        /// <summary>Franja de peligro horizontal dentro de una caja (pegada arriba o abajo).</summary>
        public static Image Franja(RectTransform caja, Sprite franja, bool arriba, float margen, float alto)
        {
            Image f = CrearImagen(caja, arriba ? "FranjaSuperior" : "FranjaInferior", franja, Color.white);
            f.type = Image.Type.Tiled;
            RectTransform rt = f.rectTransform;
            float y = arriba ? 1f : 0f;
            rt.anchorMin = new Vector2(0f, y);
            rt.anchorMax = new Vector2(1f, y);
            rt.pivot = new Vector2(0.5f, y);
            rt.offsetMin = new Vector2(margen, arriba ? -margen - alto : margen);
            rt.offsetMax = new Vector2(-margen, arriba ? -margen : margen + alto);
            return f;
        }

        /// <summary>Caja con marco pixel (panel) y franjas de peligro arriba y, si se pide, abajo.</summary>
        public static RectTransform CrearCaja(Transform padre, string nombre, Sprite panel, Sprite franja,
            Vector2 posicion, Vector2 tamano, bool franjaAbajo = false)
        {
            Image marco = CrearImagen(padre, nombre, panel, Color.white, true);
            RectTransform caja = marco.rectTransform;
            Centrar(caja, posicion, tamano);
            if (franja != null)
            {
                Franja(caja, franja, true, 4f * Px, 4f * Px);
                if (franjaAbajo) Franja(caja, franja, false, 4f * Px, 4f * Px);
            }
            return caja;
        }

        /// <summary>
        /// Ventana modal: velo oscuro a pantalla completa + caja con marco, franja y
        /// cartel con el título (con icono opcional). Devuelve el PanelAnimado (raíz).
        /// </summary>
        public static PanelAnimado CrearVentana(Transform padre, string nombre, string titulo, Vector2 tamano,
            KitSpritesArcade kit, out RectTransform caja, Sprite panel = null, Sprite icono = null)
        {
            RectTransform raiz = CrearObjeto(nombre, padre);
            Estirar(raiz);
            raiz.gameObject.AddComponent<CanvasGroup>();

            Image velo = CrearImagen(raiz, "Velo", null, PaletaArcade.Velo, true);
            Estirar(velo.rectTransform);

            caja = CrearCaja(raiz, "Caja", panel != null ? panel : kit.panel, kit.franja, Vector2.zero, tamano);

            // Cartel del título encima del marco.
            float ancho = Mathf.Min(640f, tamano.x - 120f);
            Image cartel = CrearImagen(caja, "Cartel", kit.botonNaranja, Color.white);
            Colocar(cartel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 8f), new Vector2(ancho, 96f));
            float desplaza = 0f;
            if (icono != null)
            {
                Image ico = CrearIcono(cartel.transform, "Icono", icono, PaletaArcade.Crema, 36f);
                ico.rectTransform.anchorMin = ico.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                ico.rectTransform.pivot = new Vector2(0f, 0.5f);
                ico.rectTransform.anchoredPosition = new Vector2(28f, Px);
                desplaza = 24f;
            }
            Text textoTitulo = CrearTexto(cartel.transform, "Titulo", titulo, 48, PaletaArcade.Crema, TextAnchor.MiddleCenter, 4f, 4f);
            Estirar(textoTitulo.rectTransform);
            textoTitulo.rectTransform.offsetMin = new Vector2(desplaza, 5f * Px);
            textoTitulo.rectTransform.offsetMax = new Vector2(0f, -3f * Px);

            PanelAnimado animado = raiz.gameObject.AddComponent<PanelAnimado>();
            animado.contenido = caja;
            return animado;
        }

        // ------------------------------------------------------------ controles
        /// <summary>Slider de 0 a 1 con riel, relleno y perilla pixel.</summary>
        public static Slider CrearSlider(Transform padre, string nombre, KitSpritesArcade kit, Vector2 tamano)
        {
            RectTransform raiz = CrearObjeto(nombre, padre);
            raiz.sizeDelta = tamano;
            Slider slider = raiz.gameObject.AddComponent<Slider>();

            Image riel = CrearImagen(raiz, "Riel", kit.barra, Color.white, true);
            Estirar(riel.rectTransform);
            riel.rectTransform.offsetMin = new Vector2(0f, tamano.y * 0.22f);
            riel.rectTransform.offsetMax = new Vector2(0f, -tamano.y * 0.22f);

            RectTransform areaRelleno = CrearObjeto("AreaRelleno", raiz);
            Estirar(areaRelleno);
            areaRelleno.offsetMin = new Vector2(0f, tamano.y * 0.22f);
            areaRelleno.offsetMax = new Vector2(0f, -tamano.y * 0.22f);

            Image relleno = CrearImagen(areaRelleno, "Relleno", kit.relleno, Color.white);
            RectTransform rr = relleno.rectTransform;
            rr.anchorMin = Vector2.zero;
            rr.anchorMax = new Vector2(0f, 1f);
            rr.pivot = new Vector2(0.5f, 0.5f);
            rr.sizeDelta = Vector2.zero;

            RectTransform areaPerilla = CrearObjeto("AreaPerilla", raiz);
            Estirar(areaPerilla);
            areaPerilla.offsetMin = new Vector2(12f, 0f);
            areaPerilla.offsetMax = new Vector2(-12f, 0f);

            Image perilla = CrearImagen(areaPerilla, "Perilla", kit.perilla, Color.white, true);
            RectTransform rp = perilla.rectTransform;
            rp.anchorMin = Vector2.zero;
            rp.anchorMax = new Vector2(0f, 1f);
            rp.pivot = new Vector2(0.5f, 0.5f);
            rp.sizeDelta = new Vector2(6f * Px, 0f);

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

        /// <summary>Casilla de verificación pixel con texto a la derecha.</summary>
        public static Toggle CrearToggle(Transform padre, string nombre, KitSpritesArcade kit, float tamano, out Text etiqueta)
        {
            RectTransform raiz = CrearObjeto(nombre, padre);
            raiz.sizeDelta = new Vector2(tamano + 300f, tamano);
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
            marca.rectTransform.offsetMin = new Vector2(Px * 2f, Px * 2f);
            marca.rectTransform.offsetMax = new Vector2(-Px * 2f, -Px * 2f);

            etiqueta = CrearTexto(raiz, "Estado", "SÍ", 32, PaletaArcade.Crema, TextAnchor.MiddleLeft, 4f, 0f);
            RectTransform re = etiqueta.rectTransform;
            re.anchorMin = new Vector2(0f, 0f);
            re.anchorMax = new Vector2(1f, 1f);
            re.offsetMin = new Vector2(tamano + 20f, 0f);
            re.offsetMax = Vector2.zero;

            toggle.targetGraphic = casilla;
            toggle.graphic = marca;
            toggle.isOn = true;
            return toggle;
        }
    }
}
