using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// Construye el menú principal (estilo "cartel de obra") dentro de MenuPrincipal:
/// muro de ladrillo, cartel amarillo con el título DEMOLEDOR UNIFRANZ, el demoledor
/// del equipo golpeando dentro de un hueco del muro y los botones JUGAR, OPCIONES,
/// CÓMO JUGAR y SALIR, con sus ventanas.
/// El canvas anterior (CanvasMenu) queda desactivado, no se borra.
/// </summary>
public static class ConstructorMenuPrincipal
{
    public const string RutaEscena = "Assets/Scenes/MenuPrincipal.unity";
    private const string Raiz = "MenuDemoledorUnifranz";
    private const string CanvasAnterior = "CanvasMenu";
    private const string CuadrosGolpe = "Assets/Arte/Demoledor/golpe_";

    // Distribución (referencia 1920×1080, origen en el centro).
    private static readonly Vector2 PosCartel = new Vector2(-360f, 280f);
    private static readonly Vector2 TamCartel = new Vector2(980f, 360f);
    private const float ColumnaBotones = -360f;
    private static readonly Vector2 PosHueco = new Vector2(480f, -20f);
    private static readonly Vector2 TamHueco = new Vector2(760f, 830f);
    private static readonly Vector2 PosDemoledor = new Vector2(480f, -60f);
    private static readonly Vector2 TamDemoledor = new Vector2(744f, 707f);   // 120×114 px × 6.2

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

        KitSpritesArcade kit = CargarKit();
        HerramientasMenus.PrepararEscena(Raiz, CanvasAnterior);

        Canvas canvas = FabricaUIArcade.CrearCanvas(Raiz, 10);
        Transform raiz = canvas.transform;
        MenuPrincipalArcade menu = canvas.gameObject.AddComponent<MenuPrincipalArcade>();

        // ---------------- Fondo: muro de ladrillo + velo azul + viñeta.
        Image muro = FabricaUIArcade.CrearImagen(raiz, "Muro", kit.muro, Color.white);
        muro.type = Image.Type.Tiled;
        FabricaUIArcade.Estirar(muro.rectTransform);

        Image velo = FabricaUIArcade.CrearImagen(raiz, "VeloAzul", null, new Color(0.04f, 0.06f, 0.14f, 0.35f));
        FabricaUIArcade.Estirar(velo.rectTransform);

        Image vineta = FabricaUIArcade.CrearImagen(raiz, "Vineta", kit.vineta, Color.white);
        FabricaUIArcade.Estirar(vineta.rectTransform);

        // ---------------- Hueco en el muro con el demoledor golpeando.
        Image hueco = FabricaUIArcade.CrearImagen(raiz, "Hueco", kit.hueco, Color.white);
        FabricaUIArcade.Centrar(hueco.rectTransform, PosHueco, TamHueco);

        Sprite[] cuadros = HerramientasMenus.CargarCuadros(CuadrosGolpe, 6);
        Image demoledor = FabricaUIArcade.CrearImagen(raiz, "Demoledor", cuadros.Length > 0 ? cuadros[0] : null, Color.white);
        demoledor.preserveAspect = true;
        FabricaUIArcade.Centrar(demoledor.rectTransform, PosDemoledor, TamDemoledor);
        AnimadorImagenUI animador = demoledor.gameObject.AddComponent<AnimadorImagenUI>();
        animador.cuadros = cuadros;
        animador.cuadrosPorSegundo = 9f;
        animador.pausaEntreVueltas = 0.55f;
        animador.cuadroImpacto = 3;   // golpe_3: los puños tocan el suelo

        RectTransform zonaEscombros = FabricaUIArcade.CrearObjeto("Escombros", raiz);
        FabricaUIArcade.Estirar(zonaEscombros);
        EscombrosDecorativos escombros = zonaEscombros.gameObject.AddComponent<EscombrosDecorativos>();
        escombros.sprite = kit.ladrillo;
        escombros.lluviaAutomatica = false;
        escombros.maximo = 30;
        escombros.opacidad = 1f;

        menu.demoledor = animador;
        menu.escombros = escombros;
        menu.origenEstallido = PosDemoledor + new Vector2(0f, -TamDemoledor.y * 0.5f + 40f);

        // ---------------- Cartel de obra con el título.
        Image cartel = FabricaUIArcade.CrearImagen(raiz, "Cartel", kit.cartel, Color.white);
        RectTransform rc = cartel.rectTransform;
        FabricaUIArcade.Centrar(rc, PosCartel, TamCartel);
        AnimacionTitulo caida = cartel.gameObject.AddComponent<AnimacionTitulo>();
        caida.balanceo = 0.6f;
        caida.pulso = 0.012f;
        caida.flotado = 5f;

        HerramientasMenus.FranjaEnCaja(rc, kit.franja, false, 20f, 32f);

        Text demoledorTxt = FabricaUIArcade.CrearTexto(rc, "TituloDemoledor", "DEMOLEDOR", 138, PaletaArcade.Contorno,
            TextAnchor.MiddleCenter, 0f, 9f);
        demoledorTxt.GetComponent<Shadow>().effectColor = PaletaArcade.Naranja;
        FabricaUIArcade.Centrar(demoledorTxt.rectTransform, new Vector2(0f, 62f), new Vector2(920f, 170f));

        Text unifranz = FabricaUIArcade.CrearTexto(rc, "TituloUnifranz", "UNIFRANZ", 100, PaletaArcade.Naranja,
            TextAnchor.MiddleCenter, 5f, 6f);
        FabricaUIArcade.Centrar(unifranz.rectTransform, new Vector2(0f, -62f), new Vector2(920f, 120f));

        // ---------------- Botones.
        Button jugar = HerramientasMenus.Boton(raiz, "BotonJugar", "JUGAR", kit.botonAmarillo,
            new Vector2(ColumnaBotones, -20f), new Vector2(560f, 130f), 70);
        Button opciones = HerramientasMenus.Boton(raiz, "BotonOpciones", "OPCIONES", kit.botonNaranja,
            new Vector2(ColumnaBotones, -165f), new Vector2(460f, 104f), 48);
        Button comoJugar = HerramientasMenus.Boton(raiz, "BotonComoJugar", "CÓMO JUGAR", kit.botonAzul,
            new Vector2(ColumnaBotones, -285f), new Vector2(460f, 104f), 48);
        Button salir = HerramientasMenus.Boton(raiz, "BotonSalir", "SALIR", kit.botonRojo,
            new Vector2(ColumnaBotones, -405f), new Vector2(460f, 104f), 48);
        FabricaUIArcade.NavegacionVertical(jugar, opciones, comoJugar, salir);

        HerramientasMenus.Conectar(jugar, menu.Jugar);
        HerramientasMenus.Conectar(opciones, menu.AbrirOpciones);
        HerramientasMenus.Conectar(comoJugar, menu.AbrirComoJugar);
        HerramientasMenus.Conectar(salir, menu.Salir);
        menu.botonJugar = jugar;

        // ---------------- Ventanas.
        menu.panelOpciones = CrearVentanaOpciones(raiz, kit, menu);
        menu.panelComoJugar = CrearVentanaComoJugar(raiz, kit, menu);

        Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Crear menú principal");
        Undo.CollapseUndoOperations(grupo);
        Selection.activeGameObject = canvas.gameObject;
    }

    private static KitSpritesArcade CargarKit()
    {
        KitSpritesArcade kit = HerramientasMenus.CargarKit();
        kit.muro = HerramientasMenus.S("muro_ladrillo");
        kit.cartel = HerramientasMenus.S("cartel_obra");
        kit.hueco = HerramientasMenus.S("hueco_muro");
        kit.vineta = HerramientasMenus.S("vineta");
        return kit;
    }

    // ------------------------------------------------------------ ventanas
    private static PanelAnimado CrearVentanaOpciones(Transform raiz, KitSpritesArcade kit, MenuPrincipalArcade menu)
    {
        RectTransform caja;
        PanelAnimado panel = FabricaUIArcade.CrearVentana(raiz, "PanelOpciones", "OPCIONES", new Vector2(940f, 640f), kit, out caja);
        MenuOpcionesArcade opciones = panel.gameObject.AddComponent<MenuOpcionesArcade>();

        Text etiquetaVol = FabricaUIArcade.CrearTexto(caja, "EtiquetaVolumen", "VOLUMEN GENERAL", 40, PaletaArcade.Amarillo, TextAnchor.MiddleLeft, 3f, 3f);
        FabricaUIArcade.Centrar(etiquetaVol.rectTransform, new Vector2(10f, 165f), new Vector2(760f, 56f));

        Slider slider = FabricaUIArcade.CrearSlider(caja, "SliderVolumen", kit, new Vector2(600f, 64f));
        FabricaUIArcade.Centrar((RectTransform)slider.transform, new Vector2(-70f, 90f), new Vector2(600f, 64f));

        Text porcentaje = FabricaUIArcade.CrearTexto(caja, "ValorVolumen", "80%", 42, PaletaArcade.Crema, TextAnchor.MiddleRight, 3f, 3f);
        FabricaUIArcade.Centrar(porcentaje.rectTransform, new Vector2(330f, 90f), new Vector2(150f, 64f));

        Text etiquetaSon = FabricaUIArcade.CrearTexto(caja, "EtiquetaSonido", "SONIDO", 40, PaletaArcade.Amarillo, TextAnchor.MiddleLeft, 3f, 3f);
        FabricaUIArcade.Centrar(etiquetaSon.rectTransform, new Vector2(10f, -5f), new Vector2(760f, 56f));

        Text estado;
        Toggle toggle = FabricaUIArcade.CrearToggle(caja, "ToggleSonido", kit, 64f, out estado);
        FabricaUIArcade.Centrar((RectTransform)toggle.transform, new Vector2(-170f, -80f), new Vector2(404f, 64f));

        Button volver = FabricaUIArcade.CrearBoton(caja, "BotonVolver", "VOLVER", kit.botonAmarillo, new Vector2(420f, 104f), 48);
        FabricaUIArcade.Colocar((RectTransform)volver.transform, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(420f, 104f));
        HerramientasMenus.Conectar(volver, menu.CerrarOpciones);

        slider.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = volver, selectOnDown = toggle };
        toggle.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = slider, selectOnDown = volver };
        volver.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = toggle, selectOnDown = slider };

        opciones.sliderVolumen = slider;
        opciones.textoVolumen = porcentaje;
        opciones.toggleSonido = toggle;
        opciones.textoSonido = estado;

        panel.seleccionInicial = slider.gameObject;
        panel.gameObject.SetActive(false);
        return panel;
    }

    private static PanelAnimado CrearVentanaComoJugar(Transform raiz, KitSpritesArcade kit, MenuPrincipalArcade menu)
    {
        RectTransform caja;
        PanelAnimado panel = FabricaUIArcade.CrearVentana(raiz, "PanelComoJugar", "CÓMO JUGAR", new Vector2(1160f, 760f), kit, out caja);

        const string A = "<color=#FFC21A>", C = "</color>";
        string texto =
            "¡El demoledor no se detiene! Repara los bloques antes de que\n" +
            "el edificio pase el " + A + "70 %" + C + " de destrucción.\n\n" +
            A + "A / D" + C + "   caminar          " + A + "ESPACIO" + C + "   saltar\n" +
            A + "↑ / ↓" + C + "   trepar y bajar por la fachada\n" +
            A + "ENTER" + C + "   martillar y reparar un bloque\n" +
            A + "ESC" + C + "   pausa          " + A + "R" + C + "   reiniciar\n\n" +
            "En el celular usa los botones de la pantalla.";

        Text cuerpo = FabricaUIArcade.CrearTexto(caja, "Texto", texto, 38, PaletaArcade.Crema, TextAnchor.UpperCenter, 2f, 2f);
        cuerpo.lineSpacing = 1.1f;
        FabricaUIArcade.Centrar(cuerpo.rectTransform, new Vector2(0f, 40f), new Vector2(1040f, 470f));

        Button volver = FabricaUIArcade.CrearBoton(caja, "BotonVolver", "VOLVER", kit.botonAmarillo, new Vector2(420f, 104f), 48);
        FabricaUIArcade.Colocar((RectTransform)volver.transform, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(420f, 104f));
        HerramientasMenus.Conectar(volver, menu.CerrarComoJugar);

        panel.seleccionInicial = volver.gameObject;
        panel.gameObject.SetActive(false);
        return panel;
    }
}
