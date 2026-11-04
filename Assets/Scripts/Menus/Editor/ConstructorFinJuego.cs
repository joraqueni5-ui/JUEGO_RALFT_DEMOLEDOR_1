using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// Construye la pantalla de fin de juego pixel art dentro de FinJuego: el edificio
/// alto del equipo de fondo, el título del final (VICTORIA / GAME OVER), los datos
/// reales de la partida con medalla y frase, la tabla de récords con iniciales y
/// los botones VOLVER A JUGAR y MENÚ PRINCIPAL.
/// El canvas anterior (CanvasFinJuego) queda desactivado, no se borra.
/// </summary>
public static class ConstructorFinJuego
{
    public const string RutaEscena = "Assets/Scenes/FinJuego.unity";
    private const string Raiz = "FinJuegoDemoledorUnifranz";
    private const string CanvasAnterior = "CanvasFinJuego";
    private const string FondoEdificio = "Assets/Sprites/Edificio/edificio_alto_fondo.png";

    private const float Px = FabricaUIArcade.Px;

    [MenuItem(HerramientasMenus.RaizMenuTools + "Construir Pantalla Fin de Juego", false, 2)]
    public static void MenuConstruir()
    {
        if (!HerramientasMenus.AbrirEscena(RutaEscena, true)) return;
        Construir();
        HerramientasMenus.Terminar("Pantalla de fin de juego construida en " + RutaEscena + ".", false, true);
    }

    public static void Construir()
    {
        Undo.SetCurrentGroupName("Construir fin de juego");
        int grupo = Undo.GetCurrentGroup();

        KitSpritesArcade kit = HerramientasMenus.CargarKit();
        Sprite edificio = SpriteEdificio();
        HerramientasMenus.PrepararEscena(Raiz, CanvasAnterior);

        Canvas canvas = FabricaUIArcade.CrearCanvas(Raiz, 10);
        Transform raiz = canvas.transform;
        FinJuegoArcade fin = canvas.gameObject.AddComponent<FinJuegoArcade>();
        HerramientasMenus.AgregarRecursos(raiz, kit, FabricaUIArcade.Fuente, FabricaUIArcade.FuenteCuerpo);
        fin.medallaBronce = kit.medallaBronce;
        fin.medallaPlata = kit.medallaPlata;
        fin.medallaOro = kit.medallaOro;

        // ---------------- Fondo: el edificio alto, oscurecido, con ladrillos cayendo.
        Image fondo = FabricaUIArcade.CrearImagen(raiz, "FondoEdificio", edificio, new Color(0.5f, 0.52f, 0.62f, 1f));
        FabricaUIArcade.Estirar(fondo.rectTransform);
        if (edificio != null)
        {
            AspectRatioFitter ajuste = fondo.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            ajuste.aspectRatio = edificio.rect.width / edificio.rect.height;
        }
        else
        {
            fondo.color = PaletaArcade.AzulOscuro;
            Debug.LogWarning("[Menus] No se encontró el fondo del edificio; se usa un fondo azul.");
        }
        Image velo = FabricaUIArcade.CrearImagen(raiz, "VeloAzul", null, new Color(0.03f, 0.05f, 0.13f, 0.40f));
        FabricaUIArcade.Estirar(velo.rectTransform);
        Image vineta = FabricaUIArcade.CrearImagen(raiz, "Vineta", kit.vineta, Color.white);
        FabricaUIArcade.Estirar(vineta.rectTransform);

        RectTransform zonaEscombros = FabricaUIArcade.CrearObjeto("Escombros", raiz);
        FabricaUIArcade.Estirar(zonaEscombros);
        EscombrosDecorativos escombros = zonaEscombros.gameObject.AddComponent<EscombrosDecorativos>();
        escombros.sprite = kit.ladrillo;
        escombros.opacidad = 0.8f;
        escombros.maximo = 10;
        escombros.tamano = new Vector2(8f * Px, 4f * Px);

        // ---------------- Títulos.
        RectTransform grupoTitulo = FabricaUIArcade.CrearObjeto("Titulo", raiz);
        FabricaUIArcade.Centrar(grupoTitulo, new Vector2(0f, 450f), new Vector2(1600f, 130f));
        AnimacionTitulo caida = grupoTitulo.gameObject.AddComponent<AnimacionTitulo>();
        caida.balanceo = 0f;
        caida.pulso = 0.01f;
        Text titulo = FabricaUIArcade.CrearTexto(grupoTitulo, "TextoTitulo", "GAME OVER", 96, PaletaArcade.Rojo, TextAnchor.MiddleCenter, Px * 1.5f, Px * 2f);
        FabricaUIArcade.Estirar(titulo.rectTransform);

        Text subtitulo = FabricaUIArcade.CrearTexto(raiz, "TextoSubtitulo", "¡EL EDIFICIO SE CAYÓ!", 40, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, Px, Px);
        FabricaUIArcade.Centrar(subtitulo.rectTransform, new Vector2(0f, 354f), new Vector2(1600f, 56f));

        // ---------------- Caja de resultados (izquierda).
        RectTransform caja = FabricaUIArcade.CrearCaja(raiz, "CajaResultado", kit.panel, kit.franja, new Vector2(-330f, -100f), new Vector2(1064f, 780f));

        Image medalla = FabricaUIArcade.CrearImagen(caja, "Medalla", kit.medallaOro, Color.white);
        medalla.preserveAspect = true;
        FabricaUIArcade.Centrar(medalla.rectTransform, new Vector2(-390f, 210f), new Vector2(16f * Px * 1.5f, 20f * Px * 1.5f));
        Text textoMedalla = FabricaUIArcade.CrearTexto(caja, "TextoMedalla", "¡MEDALLA DE ORO!", 36, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, Px, 0f);
        FabricaUIArcade.Centrar(textoMedalla.rectTransform, new Vector2(80f, 250f), new Vector2(780f, 50f));
        Text frase = FabricaUIArcade.CrearTexto(caja, "TextoFrase", "\"...\"", 24, PaletaArcade.Crema, TextAnchor.MiddleCenter, Px * 0.5f, 0f, true);
        frase.horizontalOverflow = HorizontalWrapMode.Wrap;
        frase.lineSpacing = 1.2f;
        FabricaUIArcade.Centrar(frase.rectTransform, new Vector2(80f, 176f), new Vector2(760f, 80f));

        Text valorPuntos;
        Fila(caja, "FilaPuntos", "PUNTOS", 64f, out valorPuntos);
        Text valorReparaciones;
        Fila(caja, "FilaReparaciones", "REPARACIONES", -4f, out valorReparaciones);
        Text valorDestrozo;
        GameObject filaDestrozo = Fila(caja, "FilaDestrozo", "DESTROZO", -72f, out valorDestrozo);

        Button otraVez = HerramientasMenus.Boton(caja, "BotonVolverAJugar", "VOLVER A JUGAR", kit.botonAmarillo,
            new Vector2(0f, -196f), new Vector2(820f, 104f), 40, kit.iconoReiniciar);
        Button menuPrincipal = HerramientasMenus.Boton(caja, "BotonMenuPrincipal", "MENÚ PRINCIPAL", kit.botonRojo,
            new Vector2(0f, -310f), new Vector2(820f, 92f), 36, kit.iconoCasa);
        HerramientasMenus.Conectar(otraVez, fin.VolverAJugar);
        HerramientasMenus.Conectar(menuPrincipal, fin.IrAlMenuPrincipal);

        // ---------------- Récords (derecha).
        RectTransform cajaRecords = FabricaUIArcade.CrearCaja(raiz, "CajaRecords", kit.panelMorado, null, new Vector2(564f, -100f), new Vector2(616f, 780f));
        Text tituloRecords = FabricaUIArcade.CrearTexto(cajaRecords, "Titulo", "RÉCORDS", 40, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, Px, Px);
        FabricaUIArcade.Centrar(tituloRecords.rectTransform, new Vector2(0f, 320f), new Vector2(560f, 56f));
        Image trofeoL = FabricaUIArcade.CrearIcono(cajaRecords, "TrofeoIzq", kit.iconoTrofeo, PaletaArcade.Amarillo, 36f);
        trofeoL.rectTransform.anchoredPosition = new Vector2(-190f, 320f);
        Image trofeoR = FabricaUIArcade.CrearIcono(cajaRecords, "TrofeoDer", kit.iconoTrofeo, PaletaArcade.Amarillo, 36f);
        trofeoR.rectTransform.anchoredPosition = new Vector2(190f, 320f);

        RectTransform tabla = FabricaUIArcade.CrearObjeto("Tabla", cajaRecords);
        FabricaUIArcade.Estirar(tabla);
        Text[] posiciones = new Text[5], iniciales = new Text[5], puntajes = new Text[5];
        for (int i = 0; i < 5; i++)
        {
            float y = 220f - i * 84f;
            posiciones[i] = Celda(tabla, "Pos" + (i + 1), (i + 1) + ".", new Vector2(-200f, y), TextAnchor.MiddleLeft);
            iniciales[i] = Celda(tabla, "Iniciales" + (i + 1), "---", new Vector2(-60f, y), TextAnchor.MiddleLeft);
            puntajes[i] = Celda(tabla, "Puntos" + (i + 1), "0", new Vector2(150f, y), TextAnchor.MiddleRight);
        }
        Text nuevo = FabricaUIArcade.CrearTexto(tabla, "Nuevo", "¡NUEVO RÉCORD!", 32, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, Px, 0f);
        FabricaUIArcade.Centrar(nuevo.rectTransform, new Vector2(0f, -300f), new Vector2(560f, 48f));
        nuevo.gameObject.AddComponent<CanvasGroup>();
        nuevo.gameObject.AddComponent<Parpadeo>();

        // ---------------- Panel de iniciales (reemplaza a la tabla mientras se escriben).
        RectTransform panelIni = FabricaUIArcade.CrearObjeto("PanelIniciales", cajaRecords);
        FabricaUIArcade.Estirar(panelIni);
        Text tituloIni = FabricaUIArcade.CrearTexto(panelIni, "Titulo", "¡NUEVO RÉCORD!\nESCRIBE TUS INICIALES", 28, PaletaArcade.Crema, TextAnchor.MiddleCenter, Px, 0f);
        tituloIni.lineSpacing = 1.3f;
        FabricaUIArcade.Centrar(tituloIni.rectTransform, new Vector2(0f, 220f), new Vector2(560f, 100f));
        Text[] letras = new Text[3];
        Button[] arriba = new Button[3], abajo = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            float x = -150f + i * 150f;
            arriba[i] = FabricaUIArcade.CrearBotonIcono(panelIni, "Arriba" + (i + 1), kit.botonAzul, kit.iconoArriba, 80f, kit);
            FabricaUIArcade.Centrar((RectTransform)arriba[i].transform, new Vector2(x, 100f), new Vector2(96f, 72f));
            letras[i] = FabricaUIArcade.CrearTexto(panelIni, "Letra" + (i + 1), "A", 88, PaletaArcade.Crema, TextAnchor.MiddleCenter, Px * 1.5f, Px);
            FabricaUIArcade.Centrar(letras[i].rectTransform, new Vector2(x, 4f), new Vector2(120f, 110f));
            abajo[i] = FabricaUIArcade.CrearBotonIcono(panelIni, "Abajo" + (i + 1), kit.botonAzul, kit.iconoAbajo, 80f, kit);
            FabricaUIArcade.Centrar((RectTransform)abajo[i].transform, new Vector2(x, -92f), new Vector2(96f, 72f));
        }
        Button listo = HerramientasMenus.Boton(panelIni, "BotonListo", "LISTO", kit.botonAmarillo, new Vector2(0f, -230f), new Vector2(400f, 100f), 40, kit.iconoJugar);
        Text pista = FabricaUIArcade.CrearTexto(panelIni, "Pista", "O ESCRÍBELAS CON EL TECLADO", 20, new Color(0.8f, 0.85f, 1f), TextAnchor.MiddleCenter, Px * 0.5f, 0f, true);
        FabricaUIArcade.Centrar(pista.rectTransform, new Vector2(0f, -320f), new Vector2(560f, 32f));
        pista.gameObject.SetActive(true);
        panelIni.gameObject.SetActive(false);

        // Navegación: flechas ↑ arriba de cada letra, ↓ abajo, LISTO al final.
        for (int i = 0; i < 3; i++)
        {
            arriba[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = arriba[(i + 2) % 3], selectOnRight = arriba[(i + 1) % 3], selectOnDown = abajo[i],
            };
            abajo[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = abajo[(i + 2) % 3], selectOnRight = abajo[(i + 1) % 3], selectOnUp = arriba[i], selectOnDown = listo,
            };
        }
        listo.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = abajo[1], selectOnDown = otraVez };
        otraVez.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = menuPrincipal, selectOnDown = menuPrincipal, selectOnRight = listo };
        menuPrincipal.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = otraVez, selectOnDown = otraVez, selectOnRight = listo };

        // ---------------- Conexiones.
        fin.textoTitulo = titulo;
        fin.textoSubtitulo = subtitulo;
        fin.imagenMedalla = medalla;
        fin.textoMedalla = textoMedalla;
        fin.textoFrase = frase;
        fin.valorPuntos = valorPuntos;
        fin.valorReparaciones = valorReparaciones;
        fin.valorDestrozo = valorDestrozo;
        fin.filaDestrozo = filaDestrozo;
        fin.tablaRecords = tabla.gameObject;
        fin.posiciones = posiciones;
        fin.iniciales = iniciales;
        fin.puntajes = puntajes;
        fin.marcaNuevo = nuevo.gameObject;
        fin.panelIniciales = panelIni.gameObject;
        fin.letras = letras;
        fin.botonesArriba = arriba;
        fin.botonesAbajo = abajo;
        fin.botonListo = listo;
        fin.botonVolverAJugar = otraVez;
        fin.botonMenuPrincipal = menuPrincipal;

        Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Crear fin de juego");
        Undo.CollapseUndoOperations(grupo);
        Selection.activeGameObject = canvas.gameObject;
    }

    /// <summary>Fila "ETIQUETA ........ valor" con una línea suave debajo.</summary>
    private static GameObject Fila(RectTransform caja, string nombre, string etiqueta, float y, out Text valor)
    {
        RectTransform fila = FabricaUIArcade.CrearObjeto(nombre, caja);
        FabricaUIArcade.Centrar(fila, new Vector2(0f, y), new Vector2(920f, 56f));

        Text texto = FabricaUIArcade.CrearTexto(fila, "Etiqueta", etiqueta, 36, PaletaArcade.Amarillo, TextAnchor.MiddleLeft, Px, 0f);
        FabricaUIArcade.Estirar(texto.rectTransform);
        valor = FabricaUIArcade.CrearTexto(fila, "Valor", "0", 40, PaletaArcade.Crema, TextAnchor.MiddleRight, Px, 0f);
        FabricaUIArcade.Estirar(valor.rectTransform);

        Image linea = FabricaUIArcade.CrearImagen(fila, "Linea", null, new Color(1f, 1f, 1f, 0.14f));
        RectTransform rl = linea.rectTransform;
        rl.anchorMin = new Vector2(0f, 0f);
        rl.anchorMax = new Vector2(1f, 0f);
        rl.pivot = new Vector2(0.5f, 0.5f);
        rl.anchoredPosition = new Vector2(0f, -6f);
        rl.sizeDelta = new Vector2(0f, Px);
        return fila.gameObject;
    }

    private static Text Celda(RectTransform padre, string nombre, string texto, Vector2 pos, TextAnchor alineacion)
    {
        Text t = FabricaUIArcade.CrearTexto(padre, nombre, texto, 36, PaletaArcade.Crema, alineacion, Px, 0f);
        FabricaUIArcade.Centrar(t.rectTransform, pos, new Vector2(alineacion == TextAnchor.MiddleRight ? 240f : 160f, 56f));
        return t;
    }

    /// <summary>Fondo del edificio alto del equipo; si no está, el que ya usaba la escena.</summary>
    private static Sprite SpriteEdificio()
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(FondoEdificio);
        if (s != null) return s;
        GameObject anterior = HerramientasMenus.BuscarRaiz(CanvasAnterior);
        if (anterior == null) return null;
        foreach (Image img in anterior.GetComponentsInChildren<Image>(true))
            if (img.name == "Fondo" && img.sprite != null) return img.sprite;
        return null;
    }
}
