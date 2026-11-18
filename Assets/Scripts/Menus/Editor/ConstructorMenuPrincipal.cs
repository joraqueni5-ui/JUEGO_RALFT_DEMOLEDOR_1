using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// Construye el menú principal pixel art dentro de MenuPrincipal:
/// - Fondo: el video de una partida (Assets/Sprites/Menus/Video) o, si no hay video,
///   el muro de ladrillo con el demoledor del equipo golpeando dentro de un hueco.
/// - Pantalla de inicio "TOCA PARA EMPEZAR".
/// - Cartel DEMOLEDOR UNIFRANZ y botones JUGAR, OPCIONES, CÓMO JUGAR, RÉCORDS, SALIR y CRÉDITOS.
/// - Ventanas de Opciones, Cómo jugar, Récords y Créditos.
/// El canvas anterior queda desactivado o se reemplaza; nunca se toca la escena del juego.
/// </summary>
public static class ConstructorMenuPrincipal
{
    public const string RutaEscena = "Assets/Scenes/MenuPrincipal.unity";
    public const string DirVideo = HerramientasMenus.DirSprites + "/Video";
    private const string Raiz = "MenuDemoledorUnifranz";
    private const string CanvasAnterior = "CanvasMenu";
    private const string CuadrosGolpe = "Assets/Arte/Demoledor/golpe_";
    private const string CuadrosMartillo = "Assets/Arte/Muneco/martillo_";

    private const float Px = FabricaUIArcade.Px;

    [MenuItem(HerramientasMenus.RaizMenuTools + "Construir Menú Principal", false, 1)]
    public static void MenuConstruir()
    {
        if (!HerramientasMenus.AbrirEscena(RutaEscena, true)) return;
        Construir();
        HerramientasMenus.Terminar("Menú principal construido en " + RutaEscena + ".", false, true);
    }

    public static void Construir()
    {
        Undo.SetCurrentGroupName("Construir menú principal");
        int grupo = Undo.GetCurrentGroup();

        KitSpritesArcade kit = HerramientasMenus.CargarKit();
        HerramientasMenus.PrepararEscena(Raiz, CanvasAnterior);

        Canvas canvas = FabricaUIArcade.CrearCanvas(Raiz, 10);
        Transform raiz = canvas.transform;
        MenuPrincipalArcade menu = canvas.gameObject.AddComponent<MenuPrincipalArcade>();
        HerramientasMenus.AgregarRecursos(raiz, kit, FabricaUIArcade.Fuente, FabricaUIArcade.FuenteCuerpo);

        CrearFondo(raiz, kit, menu);
        CrearGrupoMenu(raiz, kit, menu);
        CrearPantallaInicio(raiz, kit, menu);

        menu.panelOpciones = CrearVentanaOpciones(raiz, kit, menu);
        menu.panelComoJugar = CrearVentanaComoJugar(raiz, kit, menu);
        menu.panelRecords = CrearVentanaRecords(raiz, kit, menu);
        menu.panelCreditos = CrearVentanaCreditos(raiz, kit, menu);
        menu.panelNiveles = CrearVentanaNiveles(raiz, kit, menu);

        Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Crear menú principal");
        Undo.CollapseUndoOperations(grupo);
        Selection.activeGameObject = canvas.gameObject;
    }

    // ------------------------------------------------------------ fondo (video o respaldo)
    private static void CrearFondo(Transform raiz, KitSpritesArcade kit, MenuPrincipalArcade menu)
    {
        RectTransform fondo = FabricaUIArcade.CrearObjeto("Fondo", raiz);
        FabricaUIArcade.Estirar(fondo);
        FondoVideoMenu video = fondo.gameObject.AddComponent<FondoVideoMenu>();

        // Respaldo: muro de ladrillo con el demoledor golpeando en un hueco.
        RectTransform respaldo = FabricaUIArcade.CrearObjeto("Respaldo", fondo);
        FabricaUIArcade.Estirar(respaldo);

        Image muro = FabricaUIArcade.CrearImagen(respaldo, "Muro", kit.muro, Color.white);
        FabricaUIArcade.Estirar(muro.rectTransform);
        Image velo = FabricaUIArcade.CrearImagen(respaldo, "VeloAzul", null, new Color(0.04f, 0.06f, 0.14f, 0.30f));
        FabricaUIArcade.Estirar(velo.rectTransform);
        Image vineta = FabricaUIArcade.CrearImagen(respaldo, "Vineta", kit.vineta, Color.white);
        FabricaUIArcade.Estirar(vineta.rectTransform);

        Image hueco = FabricaUIArcade.CrearImagen(respaldo, "Hueco", kit.hueco, Color.white);
        FabricaUIArcade.Centrar(hueco.rectTransform, new Vector2(500f, -30f), new Vector2(192f * Px, 208f * Px));

        Sprite[] golpe = HerramientasMenus.CargarCuadros(CuadrosGolpe, 6);
        Image demoledor = FabricaUIArcade.CrearImagen(respaldo, "Demoledor", golpe.Length > 0 ? golpe[0] : null, Color.white);
        demoledor.preserveAspect = true;
        FabricaUIArcade.Centrar(demoledor.rectTransform, new Vector2(500f, -150f), new Vector2(120f * Px, 114f * Px));
        AnimadorImagenUI animador = demoledor.gameObject.AddComponent<AnimadorImagenUI>();
        animador.cuadros = golpe;
        animador.cuadrosPorSegundo = 9f;
        animador.pausaEntreVueltas = 0.55f;
        animador.cuadroImpacto = 3;   // golpe_3: los puños tocan el suelo

        Sprite[] martillo = HerramientasMenus.CargarCuadros(CuadrosMartillo, 4);
        Image muneco = FabricaUIArcade.CrearImagen(respaldo, "Muneco", martillo.Length > 0 ? martillo[0] : null, Color.white);
        muneco.preserveAspect = true;
        RectTransform rm = muneco.rectTransform;
        // Anclado abajo a la derecha y volteado (escala X negativa) para que mire al demoledor;
        // queda un poco cortado por el borde, como si se asomara.
        rm.anchorMin = rm.anchorMax = new Vector2(1f, 0f);
        rm.pivot = new Vector2(0f, 0f);
        rm.anchoredPosition = new Vector2(24f, 0f);
        rm.sizeDelta = new Vector2(52f * Px, 87f * Px);
        rm.localScale = new Vector3(-1f, 1f, 1f);
        AnimadorImagenUI animMuneco = muneco.gameObject.AddComponent<AnimadorImagenUI>();
        animMuneco.cuadros = martillo;
        animMuneco.cuadrosPorSegundo = 7f;
        animMuneco.pausaEntreVueltas = 0.9f;

        RectTransform zonaEscombros = FabricaUIArcade.CrearObjeto("Escombros", respaldo);
        FabricaUIArcade.Estirar(zonaEscombros);
        EscombrosDecorativos escombros = zonaEscombros.gameObject.AddComponent<EscombrosDecorativos>();
        escombros.sprite = kit.ladrillo;
        escombros.lluviaAutomatica = false;
        escombros.maximo = 30;
        escombros.opacidad = 1f;
        escombros.tamano = new Vector2(8f * Px, 4f * Px);

        menu.demoledor = animador;
        menu.escombros = escombros;
        menu.origenEstallido = new Vector2(500f, -150f - 114f * Px * 0.5f + 40f);

        // Video (se enciende solo si hay un video y se puede reproducir).
        RawImage pantalla = FabricaUIArcade.CrearObjeto("Video", fondo).gameObject.AddComponent<RawImage>();
        FabricaUIArcade.Estirar(pantalla.rectTransform);
        pantalla.raycastTarget = false;
        AspectRatioFitter ajuste = pantalla.gameObject.AddComponent<AspectRatioFitter>();
        ajuste.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        ajuste.aspectRatio = 16f / 9f;

        video.clip = BuscarVideo();
        video.pantalla = pantalla;
        video.respaldo = respaldo.gameObject;

        // Sombra a la izquierda para leer los botones sobre cualquier fondo.
        Image sombra = FabricaUIArcade.CrearImagen(fondo, "SombraIzquierda", kit.sombraLateral, Color.white);
        RectTransform rs = sombra.rectTransform;
        rs.anchorMin = Vector2.zero;
        rs.anchorMax = new Vector2(0.62f, 1f);
        rs.offsetMin = rs.offsetMax = Vector2.zero;
        sombra.type = Image.Type.Simple;
    }

    private static VideoClip BuscarVideo()
    {
        if (!AssetDatabase.IsValidFolder(DirVideo)) return null;
        string[] guids = AssetDatabase.FindAssets("t:VideoClip", new[] { DirVideo });
        if (guids.Length == 0) return null;
        VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (clip != null) Debug.Log("[Menus] Video del menú: " + clip.name);
        return clip;
    }

    // ------------------------------------------------------------ cartel y botones
    private static void CrearGrupoMenu(Transform raiz, KitSpritesArcade kit, MenuPrincipalArcade menu)
    {
        RectTransform grupo = FabricaUIArcade.CrearObjeto("GrupoMenu", raiz);
        FabricaUIArcade.Estirar(grupo);
        menu.grupoMenu = grupo.gameObject.AddComponent<CanvasGroup>();

        CrearCartel(grupo, kit, new Vector2(-392f, 372f), new Vector2(960f, 240f), 96, 64);

        // Panel con los botones.
        RectTransform panel = FabricaUIArcade.CrearCaja(grupo, "PanelBotones", kit.panel, kit.franja,
            new Vector2(-420f, -150f), new Vector2(840f, 720f), true);
        menu.panelBotones = panel;

        // Seis botones (con NIVELES) en el mismo panel: un poco más bajos y juntos.
        Vector2 tam = new Vector2(640f, 92f);
        Button jugar = HerramientasMenus.Boton(panel, "BotonJugar", "JUGAR", kit.botonAmarillo, new Vector2(0f, 270f), new Vector2(660f, 104f), 44, kit.iconoJugar);
        Button niveles = HerramientasMenus.Boton(panel, "BotonNiveles", "NIVELES", kit.botonAzul, new Vector2(0f, 162f), tam, 38, kit.iconoArriba);
        Button opciones = HerramientasMenus.Boton(panel, "BotonOpciones", "OPCIONES", kit.botonNaranja, new Vector2(0f, 58f), tam, 38, kit.iconoOpciones);
        Button ayuda = HerramientasMenus.Boton(panel, "BotonComoJugar", "CÓMO JUGAR", kit.botonAzul, new Vector2(0f, -46f), tam, 38, kit.iconoAyuda);
        Button records = HerramientasMenus.Boton(panel, "BotonRecords", "RÉCORDS", kit.botonMorado, new Vector2(0f, -150f), tam, 38, kit.iconoTrofeo);
        Button salir = HerramientasMenus.Boton(panel, "BotonSalir", "SALIR", kit.botonRojo, new Vector2(0f, -254f), tam, 38, kit.iconoSalir);
        FabricaUIArcade.NavegacionVertical(jugar, niveles, opciones, ayuda, records, salir);

        HerramientasMenus.Conectar(jugar, menu.Jugar);
        HerramientasMenus.Conectar(niveles, menu.AbrirNiveles);
        HerramientasMenus.Conectar(opciones, menu.AbrirOpciones);
        HerramientasMenus.Conectar(ayuda, menu.AbrirComoJugar);
        HerramientasMenus.Conectar(records, menu.AbrirRecords);
        HerramientasMenus.Conectar(salir, menu.Salir);
        menu.botonJugar = jugar;

        // Insignia del récord (arriba a la derecha).
        RectTransform insignia = FabricaUIArcade.CrearCaja(grupo, "InsigniaRecord", kit.panelMorado, null, Vector2.zero, new Vector2(320f, 100f));
        insignia.anchorMin = insignia.anchorMax = new Vector2(1f, 1f);
        insignia.anchoredPosition = new Vector2(-190f, -70f);
        Image trofeo = FabricaUIArcade.CrearIcono(insignia, "Trofeo", kit.iconoTrofeo, PaletaArcade.Amarillo, 40f);
        trofeo.rectTransform.anchoredPosition = new Vector2(-104f, 0f);
        Text etiqueta = FabricaUIArcade.CrearTexto(insignia, "Etiqueta", "RÉCORD", 32, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, 4f, 0f);
        FabricaUIArcade.Centrar(etiqueta.rectTransform, new Vector2(30f, 18f), new Vector2(220f, 40f));
        Text valor = FabricaUIArcade.CrearTexto(insignia, "Valor", "0", 32, PaletaArcade.Crema, TextAnchor.MiddleCenter, 4f, 0f);
        FabricaUIArcade.Centrar(valor.rectTransform, new Vector2(30f, -20f), new Vector2(220f, 40f));
        menu.insigniaRecord = insignia.gameObject;
        menu.valorRecord = valor;

        // CRÉDITOS (abajo a la derecha).
        Button creditos = FabricaUIArcade.CrearBoton(grupo, "BotonCreditos", "CRÉDITOS", kit.botonAzul, new Vector2(300f, 80f), 32);
        RectTransform rc = (RectTransform)creditos.transform;
        rc.anchorMin = rc.anchorMax = new Vector2(1f, 0f);
        rc.anchoredPosition = new Vector2(-180f, 60f);
        HerramientasMenus.Conectar(creditos, menu.AbrirCreditos);

        // Etiqueta "DEMO EN VIVO" (solo con video).
        RectTransform demo = FabricaUIArcade.CrearCaja(grupo, "EtiquetaDemo", kit.panelRojo, null, Vector2.zero, new Vector2(340f, 72f));
        demo.anchorMin = demo.anchorMax = new Vector2(1f, 0f);
        demo.anchoredPosition = new Vector2(-200f, 160f);
        Image punto = FabricaUIArcade.CrearImagen(demo, "Punto", null, PaletaArcade.Rojo);
        FabricaUIArcade.Centrar(punto.rectTransform, new Vector2(-126f, 0f), new Vector2(16f, 16f));
        punto.gameObject.AddComponent<CanvasGroup>();
        punto.gameObject.AddComponent<Parpadeo>();
        Text textoDemo = FabricaUIArcade.CrearTexto(demo, "Texto", "DEMO EN VIVO", 28, PaletaArcade.Crema, TextAnchor.MiddleCenter, 4f, 0f);
        FabricaUIArcade.Centrar(textoDemo.rectTransform, new Vector2(16f, 0f), new Vector2(280f, 40f));
        demo.gameObject.SetActive(false);
        raiz.GetComponentInChildren<FondoVideoMenu>(true).etiquetaDemo = demo.gameObject;
    }

    /// <summary>Cartel amarillo de obra con DEMOLEDOR / UNIFRANZ.</summary>
    private static RectTransform CrearCartel(Transform padre, KitSpritesArcade kit, Vector2 posicion, Vector2 tamano, int tam1, int tam2)
    {
        Image cartel = FabricaUIArcade.CrearImagen(padre, "Cartel", kit.cartel, Color.white);
        RectTransform rc = cartel.rectTransform;
        FabricaUIArcade.Centrar(rc, posicion, tamano);
        AnimacionTitulo caida = cartel.gameObject.AddComponent<AnimacionTitulo>();
        caida.balanceo = 0f;
        caida.pulso = 0f;
        caida.flotado = 4f;
        FabricaUIArcade.Franja(rc, kit.franja, false, 4f * Px, 6f * Px);

        Text demoledor = FabricaUIArcade.CrearTexto(rc, "TituloDemoledor", "DEMOLEDOR", tam1, PaletaArcade.Contorno,
            TextAnchor.MiddleCenter, 0f, Px * 1.5f);
        demoledor.GetComponent<Shadow>().effectColor = PaletaArcade.Naranja;
        FabricaUIArcade.Centrar(demoledor.rectTransform, new Vector2(0f, tamano.y * 0.2f), new Vector2(tamano.x - 40f, tam1 * 1.2f));

        Text unifranz = FabricaUIArcade.CrearTexto(rc, "TituloUnifranz", "UNIFRANZ", tam2, PaletaArcade.Naranja,
            TextAnchor.MiddleCenter, Px, Px);
        FabricaUIArcade.Centrar(unifranz.rectTransform, new Vector2(0f, -tamano.y * 0.12f), new Vector2(tamano.x - 40f, tam2 * 1.2f));
        return rc;
    }

    // ------------------------------------------------------------ pantalla de inicio
    private static void CrearPantallaInicio(Transform raiz, KitSpritesArcade kit, MenuPrincipalArcade menu)
    {
        RectTransform inicio = FabricaUIArcade.CrearObjeto("PantallaInicio", raiz);
        FabricaUIArcade.Estirar(inicio);
        menu.pantallaInicio = inicio.gameObject.AddComponent<CanvasGroup>();

        Image velo = FabricaUIArcade.CrearImagen(inicio, "Velo", null, new Color(0.03f, 0.05f, 0.12f, 0.55f), true);
        FabricaUIArcade.Estirar(velo.rectTransform);

        CrearCartel(inicio, kit, new Vector2(0f, 220f), new Vector2(1100f, 280f), 112, 72);

        Image barra = FabricaUIArcade.CrearImagen(inicio, "Barra", null, new Color(0.04f, 0.06f, 0.14f, 0.92f));
        RectTransform rb = barra.rectTransform;
        rb.anchorMin = new Vector2(0f, 0f);
        rb.anchorMax = new Vector2(1f, 0f);
        rb.pivot = new Vector2(0.5f, 0f);
        rb.offsetMin = Vector2.zero;
        rb.offsetMax = new Vector2(0f, 180f);
        Image linea = FabricaUIArcade.CrearImagen(barra.transform, "Linea", null, PaletaArcade.Amarillo);
        RectTransform rl = linea.rectTransform;
        rl.anchorMin = new Vector2(0f, 1f);
        rl.anchorMax = new Vector2(1f, 1f);
        rl.pivot = new Vector2(0.5f, 1f);
        rl.offsetMin = new Vector2(0f, -Px);
        rl.offsetMax = Vector2.zero;

        RectTransform aviso = FabricaUIArcade.CrearObjeto("Aviso", barra.transform);
        FabricaUIArcade.Estirar(aviso);
        aviso.gameObject.AddComponent<CanvasGroup>();
        aviso.gameObject.AddComponent<Parpadeo>();
        Text texto = FabricaUIArcade.CrearTexto(aviso, "Texto", "TOCA PARA EMPEZAR", 48, PaletaArcade.Crema, TextAnchor.MiddleCenter, Px, Px);
        FabricaUIArcade.Centrar(texto.rectTransform, new Vector2(0f, 14f), new Vector2(1600f, 64f));
        Image flecha = FabricaUIArcade.CrearIcono(aviso, "Flecha", kit.iconoAbajo, PaletaArcade.Amarillo, 28f);
        flecha.rectTransform.anchoredPosition = new Vector2(0f, -44f);
        menu.textoEmpezar = texto;
    }

    // ------------------------------------------------------------ ventanas
    private static Button BotonVolver(RectTransform caja, KitSpritesArcade kit, UnityEngine.Events.UnityAction accion)
    {
        Button volver = FabricaUIArcade.CrearBoton(caja, "BotonVolver", "VOLVER", kit.botonAmarillo, new Vector2(400f, 100f), 40, kit.iconoCasa);
        FabricaUIArcade.Colocar((RectTransform)volver.transform, new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(400f, 100f));
        HerramientasMenus.Conectar(volver, accion);
        return volver;
    }

    private static Text Etiqueta(RectTransform caja, string nombre, string texto, Vector2 pos)
    {
        Text t = FabricaUIArcade.CrearTexto(caja, nombre, texto, 32, PaletaArcade.Amarillo, TextAnchor.MiddleLeft, Px, 0f);
        FabricaUIArcade.Centrar(t.rectTransform, pos, new Vector2(700f, 48f));
        return t;
    }

    private static PanelAnimado CrearVentanaOpciones(Transform raiz, KitSpritesArcade kit, MenuPrincipalArcade menu)
    {
        RectTransform caja;
        PanelAnimado panel = FabricaUIArcade.CrearVentana(raiz, "PanelOpciones", "OPCIONES", new Vector2(980f, 800f), kit, out caja, null, kit.iconoOpciones);
        MenuOpcionesArcade opciones = panel.gameObject.AddComponent<MenuOpcionesArcade>();

        // Música (el control de volumen que ya existía) y Efectos.
        Etiqueta(caja, "EtiquetaVolumen", "MÚSICA", new Vector2(-60f, 270f));
        Slider slider = FabricaUIArcade.CrearSlider(caja, "SliderVolumen", kit, new Vector2(600f, 56f));
        FabricaUIArcade.Centrar((RectTransform)slider.transform, new Vector2(-80f, 206f), new Vector2(600f, 56f));
        Text porcentaje = FabricaUIArcade.CrearTexto(caja, "ValorVolumen", "80%", 32, PaletaArcade.Crema, TextAnchor.MiddleRight, Px, 0f);
        FabricaUIArcade.Centrar(porcentaje.rectTransform, new Vector2(330f, 206f), new Vector2(160f, 56f));

        Etiqueta(caja, "EtiquetaEfectos", "EFECTOS", new Vector2(-60f, 130f));
        Slider sliderEfectos = FabricaUIArcade.CrearSlider(caja, "SliderEfectos", kit, new Vector2(600f, 56f));
        FabricaUIArcade.Centrar((RectTransform)sliderEfectos.transform, new Vector2(-80f, 66f), new Vector2(600f, 56f));
        Text porcentajeEfectos = FabricaUIArcade.CrearTexto(caja, "ValorEfectos", "80%", 32, PaletaArcade.Crema, TextAnchor.MiddleRight, Px, 0f);
        FabricaUIArcade.Centrar(porcentajeEfectos.rectTransform, new Vector2(330f, 66f), new Vector2(160f, 56f));

        Text estadoSonido;
        Toggle sonido = Fila(caja, kit, "Sonido", "SONIDO", -30f, out estadoSonido);
        Text estadoVibracion;
        Toggle vibracion = Fila(caja, kit, "Vibracion", "VIBRACIÓN", -110f, out estadoVibracion);
        Text estadoPantalla;
        Toggle pantalla = Fila(caja, kit, "PantallaCompleta", "PANTALLA COMPLETA", -110f, out estadoPantalla);   // misma altura: nunca se ven juntas

        Button volver = BotonVolver(caja, kit, menu.CerrarOpciones);

        slider.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = volver, selectOnDown = sliderEfectos };
        sliderEfectos.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = slider, selectOnDown = sonido };
        sonido.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = sliderEfectos, selectOnDown = volver };
        volver.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = sonido, selectOnDown = slider };
        vibracion.navigation = pantalla.navigation = new Navigation { mode = Navigation.Mode.Automatic };

        opciones.sliderVolumen = slider;
        opciones.textoVolumen = porcentaje;
        opciones.sliderEfectos = sliderEfectos;
        opciones.textoEfectos = porcentajeEfectos;
        opciones.toggleSonido = sonido;
        opciones.textoSonido = estadoSonido;
        opciones.filaVibracion = vibracion.transform.parent.gameObject;
        opciones.toggleVibracion = vibracion;
        opciones.textoVibracion = estadoVibracion;
        opciones.filaPantallaCompleta = pantalla.transform.parent.gameObject;
        opciones.togglePantallaCompleta = pantalla;
        opciones.textoPantallaCompleta = estadoPantalla;

        panel.seleccionInicial = slider.gameObject;
        panel.gameObject.SetActive(false);
        return panel;
    }

    /// <summary>Fila "ETIQUETA ............ [x] SÍ" para las opciones.</summary>
    private static Toggle Fila(RectTransform caja, KitSpritesArcade kit, string nombre, string texto, float y, out Text estado)
    {
        RectTransform fila = FabricaUIArcade.CrearObjeto("Fila" + nombre, caja);
        FabricaUIArcade.Centrar(fila, new Vector2(0f, y), new Vector2(820f, 64f));
        Text t = FabricaUIArcade.CrearTexto(fila, "Etiqueta", texto, 32, PaletaArcade.Amarillo, TextAnchor.MiddleLeft, Px, 0f);
        FabricaUIArcade.Estirar(t.rectTransform);
        Toggle toggle = FabricaUIArcade.CrearToggle(fila, "Toggle" + nombre, kit, 56f, out estado);
        RectTransform rt = (RectTransform)toggle.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(200f, 56f);
        return toggle;
    }

    /// <summary>
    /// Ventana "ELIGE UN NIVEL": el edificio de Unifranz (Assets/Sprites/edificio_unifranz, 576 x 321 px, un solo
    /// sprite con filtro Point) con una franja transparente por nivel que se resalta al elegirlo, una lista con los
    /// 7 niveles y el nivel final, y debajo del edificio la ficha del nivel. Los nombres salen de la DefinicionPiso
    /// (nombreArea). Si cambias los pisos, vuelve a ejecutar "Construir Menú Principal".
    /// </summary>
    private static PanelAnimado CrearVentanaNiveles(Transform raiz, KitSpritesArcade kit, MenuPrincipalArcade menu)
    {
        const int Niveles = 8;                       // planta baja, pisos 1 a 6 y el nivel final
        const float K = 1.4f;                        // ampliación del edificio (sin deformarlo)
        const float ColumnaIzq = -400f, ColumnaDer = 560f;

        // Franjas de cada piso sobre el dibujo (px desde arriba del sprite recortado de 576 x 321):
        // planta baja, cinco filas de ventanas (pisos 1 a 5) y la azotea (piso 6, terraza y biblioteca).
        float[] franjaArriba = { 229f, 193f, 163f, 133f, 104f, 77f, 0f };
        float[] franjaAlto = { 92f, 36f, 30f, 30f, 29f, 27f, 77f };

        // Textos de cada nivel: el nombre del área y la ficha salen de los assets DefinicionPiso.
        string[] titulos = new string[Niveles];
        string[] descripciones = new string[Niveles];
        string[] datos = new string[Niveles];
        bool[] disponible = new bool[Niveles];
        for (int i = 0; i < Niveles - 1; i++)
        {
            titulos[i] = DefinicionPiso.NombreAreaDe(i);
            descripciones[i] = "ESTE NIVEL TODAVÍA NO ESTÁ LISTO.\nPRONTO PODRÁS JUGARLO.";
            datos[i] = "";
        }
        titulos[Niveles - 1] = DefinicionPiso.NombreNivelFinal;
        descripciones[Niveles - 1] = "El demoledor escapó a la fachada. Sube reparando las ventanas, esquiva las palomas y sobrevive al terremoto.";
        datos[Niveles - 1] = "Es el nivel más difícil: tienes 3 vidas.";

        if (AssetDatabase.IsValidFolder("Assets/Data/Pisos"))
        {
            foreach (string guid in AssetDatabase.FindAssets("t:DefinicionPiso", new[] { "Assets/Data/Pisos" }))
            {
                DefinicionPiso d = AssetDatabase.LoadAssetAtPath<DefinicionPiso>(AssetDatabase.GUIDToAssetPath(guid));
                if (d == null || d.numero < 0 || d.numero >= Niveles - 1) continue;
                int i = d.numero;
                disponible[i] = d.disponible;
                titulos[i] = d.NombreArea;
                if (disponible[i])
                {
                    descripciones[i] = d.fichaDescripcion;
                    datos[i] = d.fichaDatoUtil;
                }
            }
        }

        RectTransform caja;
        PanelAnimado panel = FabricaUIArcade.CrearVentana(raiz, "PanelNiveles", "ELIGE UN NIVEL", new Vector2(1760f, 960f), kit, out caja, null, kit.iconoArriba);
        MenuNivelesArcade niveles = panel.gameObject.AddComponent<MenuNivelesArcade>();

        // --- el edificio de Unifranz (proporción original, filtro Point del importador)
        Sprite dibujo = AsignarArtePiso1.Cargar("Assets/Sprites/edificio_unifranz.aseprite", "edificio_unifranz");
        if (dibujo == null) Debug.LogWarning("[Menus] No se encontró Assets/Sprites/edificio_unifranz.aseprite: el edificio se verá vacío.");
        Vector2 tam = dibujo != null ? dibujo.rect.size : new Vector2(576f, 321f);
        Image edificio = FabricaUIArcade.CrearImagen(caja, "Edificio", dibujo, dibujo != null ? Color.white : new Color(0.3f, 0.32f, 0.4f));
        edificio.preserveAspect = true;
        FabricaUIArcade.Centrar(edificio.rectTransform, new Vector2(ColumnaIzq, 160f), tam * K);

        // --- una franja transparente por piso sobre el edificio (se resalta al elegir el nivel)
        Button[] botonesBanda = new Button[Niveles - 1];
        Image[] bandas = new Image[Niveles - 1];
        for (int i = 0; i < Niveles - 1; i++)
        {
            Image banda = FabricaUIArcade.CrearImagen(edificio.transform, "Banda" + i, null, new Color(0f, 0f, 0f, 0f), true);
            RectTransform rb = banda.rectTransform;
            rb.anchorMin = rb.anchorMax = new Vector2(0f, 1f);
            rb.pivot = new Vector2(0f, 1f);
            rb.anchoredPosition = new Vector2(0f, -franjaArriba[i] * K);
            rb.sizeDelta = new Vector2(tam.x * K, franjaAlto[i] * K);
            Button boton = banda.gameObject.AddComponent<Button>();
            boton.targetGraphic = banda;
            boton.transition = Selectable.Transition.None;
            botonesBanda[i] = boton;
            bandas[i] = banda;
        }

        // --- marco dorado alrededor de todo el edificio (nivel final)
        GameObject marco = new GameObject("MarcoFinal", typeof(RectTransform));
        marco.transform.SetParent(edificio.transform, false);
        RectTransform rm = (RectTransform)marco.transform;
        FabricaUIArcade.Estirar(rm);
        Color oro = new Color(1f, 0.82f, 0.15f, 1f);
        float g = 5f;
        string[] lados = { "Arriba", "Abajo", "Izquierda", "Derecha" };
        for (int l = 0; l < 4; l++)
        {
            Image linea = FabricaUIArcade.CrearImagen(rm, "Marco" + lados[l], null, oro);
            RectTransform rl = linea.rectTransform;
            if (l < 2)
            {
                rl.anchorMin = new Vector2(0f, l == 0 ? 1f : 0f); rl.anchorMax = new Vector2(1f, l == 0 ? 1f : 0f);
                rl.pivot = new Vector2(0.5f, l == 0 ? 1f : 0f); rl.sizeDelta = new Vector2(0f, g);
            }
            else
            {
                rl.anchorMin = new Vector2(l == 2 ? 0f : 1f, 0f); rl.anchorMax = new Vector2(l == 2 ? 0f : 1f, 1f);
                rl.pivot = new Vector2(l == 2 ? 0f : 1f, 0.5f); rl.sizeDelta = new Vector2(g, 0f);
            }
            rl.anchoredPosition = Vector2.zero;
        }
        marco.SetActive(false);

        // --- lista de niveles (a la derecha)
        Button[] lista = new Button[Niveles];
        Text[] etiquetas = new Text[Niveles];
        for (int i = 0; i < Niveles; i++)
        {
            Sprite fondoBoton = i == Niveles - 1 ? kit.botonMorado : kit.botonAzul;
            Button b = HerramientasMenus.Boton(caja, "Nivel" + i, titulos[i], fondoBoton, new Vector2(ColumnaDer, 330f - 72f * i), new Vector2(540f, 64f), 18);
            Text etiqueta = b.GetComponentInChildren<Text>();
            if (etiqueta != null)
            {
                etiqueta.alignment = TextAnchor.MiddleLeft;
                etiqueta.rectTransform.offsetMin = new Vector2(26f, etiqueta.rectTransform.offsetMin.y);
                etiqueta.rectTransform.offsetMax = new Vector2(-118f, etiqueta.rectTransform.offsetMax.y);
                etiquetas[i] = etiqueta;
            }
            lista[i] = b;
        }

        // --- ficha del nivel (debajo del edificio)
        Text titulo = FabricaUIArcade.CrearTexto(caja, "FichaTitulo", "", 40, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, Px, Px);
        FabricaUIArcade.Centrar(titulo.rectTransform, new Vector2(ColumnaIzq, -100f), new Vector2(800f, 56f));
        Text estado = FabricaUIArcade.CrearTexto(caja, "FichaEstado", "", 28, PaletaArcade.Verde, TextAnchor.MiddleCenter, Px * 0.75f, 0f);
        FabricaUIArcade.Centrar(estado.rectTransform, new Vector2(ColumnaIzq, -158f), new Vector2(800f, 40f));
        Text descripcion = FabricaUIArcade.CrearTexto(caja, "FichaDescripcion", "", 26, PaletaArcade.Crema, TextAnchor.UpperCenter, Px * 0.5f, 0f, true);
        descripcion.horizontalOverflow = HorizontalWrapMode.Wrap;
        descripcion.lineSpacing = 1.25f;
        FabricaUIArcade.Centrar(descripcion.rectTransform, new Vector2(ColumnaIzq, -250f), new Vector2(780f, 130f));
        Text datoTitulo = FabricaUIArcade.CrearTexto(caja, "FichaDatoTitulo", "DATO ÚTIL", 24, PaletaArcade.Naranja, TextAnchor.MiddleCenter, Px * 0.75f, 0f);
        FabricaUIArcade.Centrar(datoTitulo.rectTransform, new Vector2(ColumnaIzq, -335f), new Vector2(780f, 34f));
        Text dato = FabricaUIArcade.CrearTexto(caja, "FichaDato", "", 22, PaletaArcade.Crema, TextAnchor.UpperCenter, Px * 0.5f, 0f, true);
        dato.horizontalOverflow = HorizontalWrapMode.Wrap;
        dato.lineSpacing = 1.2f;
        FabricaUIArcade.Centrar(dato.rectTransform, new Vector2(ColumnaIzq, -392f), new Vector2(780f, 70f));

        // --- botones JUGAR y VOLVER, y la pista
        Button jugar = HerramientasMenus.Boton(caja, "BotonJugarNivel", "JUGAR", kit.botonAmarillo, new Vector2(ColumnaDer, -255f), new Vector2(540f, 80f), 40, kit.iconoJugar);
        Button volver = HerramientasMenus.Boton(caja, "BotonVolver", "VOLVER", kit.botonRojo, new Vector2(ColumnaDer, -350f), new Vector2(420f, 70f), 34, kit.iconoCasa);
        HerramientasMenus.Conectar(volver, menu.CerrarNiveles);

        Text pista = FabricaUIArcade.CrearTexto(caja, "Pista", PreferenciasMenus.EsCelular ? "TOCA UN NIVEL PARA VER SU FICHA" : "MOUSE O FLECHAS PARA ELEGIR · ENTER PARA JUGAR", 18,
            new Color(0.8f, 0.85f, 1f), TextAnchor.MiddleCenter, Px * 0.5f, 0f);
        FabricaUIArcade.Centrar(pista.rectTransform, new Vector2(ColumnaDer, -425f), new Vector2(560f, 28f));

        // --- navegación con teclado: la lista sube y baja; a la derecha de la lista está JUGAR
        for (int i = 0; i < Niveles; i++)
        {
            lista[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = i > 0 ? lista[i - 1] : volver,
                selectOnDown = i + 1 < Niveles ? lista[i + 1] : jugar,
                selectOnRight = jugar,
            };
        }
        jugar.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = lista[Niveles - 1], selectOnDown = volver, selectOnLeft = lista[0] };
        volver.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = jugar, selectOnDown = lista[0], selectOnLeft = lista[0] };

        niveles.botonesBanda = botonesBanda;
        niveles.bandas = bandas;
        niveles.marcoFinal = marco;
        niveles.botonesLista = lista;
        niveles.etiquetasLista = etiquetas;
        niveles.titulos = titulos;
        niveles.descripciones = descripciones;
        niveles.datos = datos;
        niveles.disponible = disponible;
        niveles.textoTitulo = titulo;
        niveles.textoEstado = estado;
        niveles.textoDescripcion = descripcion;
        niveles.textoDatoTitulo = datoTitulo;
        niveles.textoDato = dato;
        niveles.botonJugar = jugar;

        panel.seleccionInicial = lista[0].gameObject;
        panel.gameObject.SetActive(false);
        return panel;
    }

    private static PanelAnimado CrearVentanaComoJugar(Transform raiz, KitSpritesArcade kit, MenuPrincipalArcade menu)
    {
        RectTransform caja;
        PanelAnimado panel = FabricaUIArcade.CrearVentana(raiz, "PanelComoJugar", "CÓMO JUGAR", new Vector2(1320f, 800f), kit, out caja, null, kit.iconoAyuda);

        const string A = "<color=#FFC21A>", C = "</color>";
        string texto =
            "¡EL DEMOLEDOR NO SE DETIENE!\n" +
            "REPARA LAS VENTANAS Y REPISAS ANTES DE QUE\n" +
            "LA SECCIÓN SE VENGA ABAJO. TIENES 3 VIDAS.\n\n" +
            A + "A / D" + C + "  CAMINAR        " + A + "ESPACIO / ARRIBA" + C + "  SALTAR\n" +
            A + "S" + C + "  BAJAR DE LA REPISA     " + A + "ENTER" + C + "  REPARAR\n" +
            A + "ESC" + C + "  PAUSA        " + A + "R" + C + "  REINICIAR\n\n" +
            "EN EL CELULAR USA LOS BOTONES DE LA PANTALLA.";

        Text cuerpo = FabricaUIArcade.CrearTexto(caja, "Texto", texto, 28, PaletaArcade.Crema, TextAnchor.UpperCenter, Px * 0.75f, 0f, true);
        cuerpo.lineSpacing = 1.3f;
        FabricaUIArcade.Centrar(cuerpo.rectTransform, new Vector2(0f, 40f), new Vector2(1200f, 520f));

        Button volver = BotonVolver(caja, kit, menu.CerrarComoJugar);
        panel.seleccionInicial = volver.gameObject;
        panel.gameObject.SetActive(false);
        return panel;
    }

    private static PanelAnimado CrearVentanaRecords(Transform raiz, KitSpritesArcade kit, MenuPrincipalArcade menu)
    {
        RectTransform caja;
        PanelAnimado panel = FabricaUIArcade.CrearVentana(raiz, "PanelRecords", "RÉCORDS", new Vector2(820f, 760f), kit, out caja, kit.panelMorado, kit.iconoTrofeo);

        Text[] filas = new Text[5];
        for (int i = 0; i < filas.Length; i++)
        {
            filas[i] = FabricaUIArcade.CrearTexto(caja, "Fila" + (i + 1), (i + 1) + ".   ---      0", 40,
                i == 0 ? PaletaArcade.Amarillo : PaletaArcade.Crema, TextAnchor.MiddleCenter, Px, 0f);
            FabricaUIArcade.Centrar(filas[i].rectTransform, new Vector2(0f, 210f - i * 76f), new Vector2(680f, 60f));
        }
        Text vacio = FabricaUIArcade.CrearTexto(caja, "SinRecords", "AÚN NO HAY RÉCORDS.\n¡JUEGA Y ENTRA A LA TABLA!", 32,
            PaletaArcade.Crema, TextAnchor.MiddleCenter, Px, 0f);
        vacio.lineSpacing = 1.3f;
        FabricaUIArcade.Centrar(vacio.rectTransform, new Vector2(0f, 60f), new Vector2(720f, 140f));

        Button volver = BotonVolver(caja, kit, menu.CerrarRecords);
        menu.filasRecords = filas;
        menu.textoSinRecords = vacio;
        panel.seleccionInicial = volver.gameObject;
        panel.gameObject.SetActive(false);
        return panel;
    }

    private static PanelAnimado CrearVentanaCreditos(Transform raiz, KitSpritesArcade kit, MenuPrincipalArcade menu)
    {
        RectTransform caja;
        PanelAnimado panel = FabricaUIArcade.CrearVentana(raiz, "PanelCreditos", "CRÉDITOS", new Vector2(980f, 720f), kit, out caja, kit.panelRojo);

        const string A = "<color=#FFC21A>", C = "</color>";
        string texto =
            A + "DEMOLEDOR UNIFRANZ" + C + "\n" +
            "JUEGO 2D · UNIFRANZ\n\n" +
            A + "EQUIPO" + C + "\n" +
            "SHADEN · JHONATAN\nALEJANDRO · JORGE\n\n" +
            "FUENTE: SILKSCREEN (SIL OFL)";
        Text cuerpo = FabricaUIArcade.CrearTexto(caja, "Texto", texto, 32, PaletaArcade.Crema, TextAnchor.UpperCenter, Px * 0.75f, 0f, true);
        cuerpo.lineSpacing = 1.3f;
        FabricaUIArcade.Centrar(cuerpo.rectTransform, new Vector2(0f, 40f), new Vector2(880f, 460f));

        Button volver = BotonVolver(caja, kit, menu.CerrarCreditos);
        panel.seleccionInicial = volver.gameObject;
        panel.gameObject.SetActive(false);
        return panel;
    }
}
