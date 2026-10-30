using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// Construye la pantalla de fin de juego dentro de FinJuego: el edificio del equipo
/// de fondo (el mismo sprite que ya usaba la escena), GAME OVER, "¡EL EDIFICIO SE
/// CAYÓ!", los datos reales de la partida y los botones VOLVER A JUGAR y MENÚ PRINCIPAL.
/// El canvas anterior (CanvasFinJuego) queda desactivado, no se borra.
/// </summary>
public static class ConstructorFinJuego
{
    public const string RutaEscena = "Assets/Scenes/FinJuego.unity";
    private const string Raiz = "FinJuegoDemoledorUnifranz";
    private const string CanvasAnterior = "CanvasFinJuego";

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
        kit.vineta = HerramientasMenus.S("vineta");
        Sprite edificio = SpriteEdificio();
        HerramientasMenus.PrepararEscena(Raiz, CanvasAnterior);

        Canvas canvas = FabricaUIArcade.CrearCanvas(Raiz, 10);
        Transform raiz = canvas.transform;
        FinJuegoArcade fin = canvas.gameObject.AddComponent<FinJuegoArcade>();

        // ---------------- Fondo: el edificio de la universidad, oscurecido.
        Image fondo = FabricaUIArcade.CrearImagen(raiz, "FondoEdificio", edificio, new Color(0.55f, 0.56f, 0.66f, 1f));
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
            Debug.LogWarning("[Menus] No se encontró el sprite del edificio en CanvasFinJuego/Fondo; se usa un fondo azul.");
        }

        Image velo = FabricaUIArcade.CrearImagen(raiz, "VeloAzul", null, new Color(0.03f, 0.05f, 0.13f, 0.45f));
        FabricaUIArcade.Estirar(velo.rectTransform);
        Image vineta = FabricaUIArcade.CrearImagen(raiz, "Vineta", kit.vineta, Color.white);
        FabricaUIArcade.Estirar(vineta.rectTransform);

        // Ladrillos que siguen cayendo (el edificio se vino abajo).
        RectTransform zonaEscombros = FabricaUIArcade.CrearObjeto("Escombros", raiz);
        FabricaUIArcade.Estirar(zonaEscombros);
        EscombrosDecorativos escombros = zonaEscombros.gameObject.AddComponent<EscombrosDecorativos>();
        escombros.sprite = kit.ladrillo;
        escombros.opacidad = 0.7f;
        escombros.maximo = 10;

        // ---------------- Títulos.
        RectTransform grupoTitulo = FabricaUIArcade.CrearObjeto("Titulo", raiz);
        FabricaUIArcade.Centrar(grupoTitulo, new Vector2(0f, 375f), new Vector2(1400f, 190f));
        grupoTitulo.gameObject.AddComponent<AnimacionTitulo>();
        Text titulo = FabricaUIArcade.CrearTexto(grupoTitulo, "TextoTitulo", "GAME OVER", 170, PaletaArcade.Rojo, TextAnchor.MiddleCenter, 8f, 12f);
        FabricaUIArcade.Estirar(titulo.rectTransform);

        Text subtitulo = FabricaUIArcade.CrearTexto(raiz, "TextoSubtitulo", "¡EL EDIFICIO SE CAYÓ!", 58, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, 5f, 5f);
        FabricaUIArcade.Centrar(subtitulo.rectTransform, new Vector2(0f, 262f), new Vector2(1400f, 80f));

        // ---------------- Caja de resultados.
        Image marco = FabricaUIArcade.CrearImagen(raiz, "CajaResultado", kit.panel, Color.white);
        RectTransform caja = marco.rectTransform;
        FabricaUIArcade.Centrar(caja, new Vector2(0f, -130f), new Vector2(1000f, 600f));
        HerramientasMenus.FranjaEnCaja(caja, kit.franja, true, 20f, 32f);

        Text valorPuntos;
        Fila(caja, "FilaPuntos", "PUNTOS", 205f, out valorPuntos);
        Text valorReparaciones;
        GameObject filaReparaciones = Fila(caja, "FilaReparaciones", "REPARACIONES", 140f, out valorReparaciones);
        Text valorDestruido;
        GameObject filaDestruido = Fila(caja, "FilaDestruido", "ESTRUCTURA DESTRUIDA", 75f, out valorDestruido);

        Text record = FabricaUIArcade.CrearTexto(caja, "TextoRecord", "MEJOR PUNTAJE: 0", 36, PaletaArcade.Crema, TextAnchor.MiddleCenter, 3f, 3f);
        FabricaUIArcade.Centrar(record.rectTransform, new Vector2(0f, 12f), new Vector2(860f, 50f));

        Button otraVez = HerramientasMenus.Boton(caja, "BotonVolverAJugar", "VOLVER A JUGAR", kit.botonAmarillo,
            new Vector2(0f, -95f), new Vector2(580f, 120f), 56);
        Button menuPrincipal = HerramientasMenus.Boton(caja, "BotonMenuPrincipal", "MENÚ PRINCIPAL", kit.botonRojo,
            new Vector2(0f, -225f), new Vector2(520f, 100f), 46);
        FabricaUIArcade.NavegacionVertical(otraVez, menuPrincipal);
        HerramientasMenus.Conectar(otraVez, fin.VolverAJugar);
        HerramientasMenus.Conectar(menuPrincipal, fin.IrAlMenuPrincipal);

        fin.textoTitulo = titulo;
        fin.textoSubtitulo = subtitulo;
        fin.valorPuntos = valorPuntos;
        fin.valorReparaciones = valorReparaciones;
        fin.valorDestruido = valorDestruido;
        fin.filaReparaciones = filaReparaciones;
        fin.filaDestruido = filaDestruido;
        fin.textoRecord = record;
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
        FabricaUIArcade.Centrar(fila, new Vector2(0f, y), new Vector2(800f, 60f));

        Text texto = FabricaUIArcade.CrearTexto(fila, "Etiqueta", etiqueta, 40, PaletaArcade.Amarillo, TextAnchor.MiddleLeft, 3f, 3f);
        FabricaUIArcade.Estirar(texto.rectTransform);

        valor = FabricaUIArcade.CrearTexto(fila, "Valor", "0", 46, PaletaArcade.Crema, TextAnchor.MiddleRight, 3f, 3f);
        FabricaUIArcade.Estirar(valor.rectTransform);

        Image linea = FabricaUIArcade.CrearImagen(fila, "Linea", null, new Color(1f, 1f, 1f, 0.12f));
        RectTransform rl = linea.rectTransform;
        rl.anchorMin = new Vector2(0f, 0f);
        rl.anchorMax = new Vector2(1f, 0f);
        rl.pivot = new Vector2(0.5f, 0.5f);
        rl.anchoredPosition = new Vector2(0f, -4f);
        rl.sizeDelta = new Vector2(0f, 3f);
        return fila.gameObject;
    }

    /// <summary>Sprite del edificio que ya usaba la escena (CanvasFinJuego/Fondo).</summary>
    private static Sprite SpriteEdificio()
    {
        GameObject anterior = HerramientasMenus.BuscarRaiz(CanvasAnterior);
        if (anterior == null) return null;
        foreach (Image img in anterior.GetComponentsInChildren<Image>(true))
            if (img.name == "Fondo" && img.sprite != null) return img.sprite;
        return null;
    }
}
