using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DemoledorUnifranz.Game;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// Ventana PREMIOS del menú principal, con 4 pestañas:
///   ACCESORIOS  gorros que se desbloquean con estrellas (y la corona al vencer al jefe): toca uno para ponértelo.
///   MARTILLOS   efectos para el martillo (chispas doradas, corazones, fuego...).
///   LOGROS      los logros conseguidos y cuánto falta para los demás.
///   RÉCORDS     por piso: estrellas, mejor puntaje y mejor tiempo.
/// Además el botón VER HISTORIA repite la intro.
/// </summary>
public class PanelPremios : MonoBehaviour
{
    public static bool Abierto { get; private set; }

    private PanelAnimado panel;
    private RectTransform contenido;
    private readonly List<Button> pestanas = new List<Button>();
    private int pestana;
    private Text resumen;
    private Button volver;

    private static readonly string[] Pestanas = { "ACCESORIOS", "MARTILLOS", "LOGROS", "RÉCORDS" };

    public static PanelPremios Crear(Transform lienzo)
    {
        KitSpritesArcade kit = KitSpritesArcade.Actual();
        RectTransform caja;
        PanelAnimado pa = FabricaUIArcade.CrearVentana(lienzo, "VentanaPremios", "PREMIOS", new Vector2(1640f, 960f), kit, out caja, null, kit.iconoTrofeo);
        Abierto = false;
        PanelPremios p = pa.gameObject.AddComponent<PanelPremios>();
        p.panel = pa;
        p.Construir(caja, kit);
        pa.OcultarInmediato();
        return p;
    }

    private void Construir(RectTransform caja, KitSpritesArcade kit)
    {
        // Pestañas.
        for (int i = 0; i < Pestanas.Length; i++)
        {
            int k = i;
            Button b = FabricaUIArcade.CrearBoton(caja, "Pestana" + i, Pestanas[i], kit.botonAzul, new Vector2(330f, 76f), 28, null, kit, false);
            FabricaUIArcade.Colocar((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(-540f + i * 360f, -110f), new Vector2(330f, 76f));
            b.onClick.AddListener(() => Mostrar(k));
            pestanas.Add(b);
        }
        FabricaUIArcade.NavegacionHorizontal(pestanas.ToArray());

        contenido = FabricaUIArcade.CrearObjeto("Contenido", caja);
        FabricaUIArcade.Colocar(contenido, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1500f, 620f));

        resumen = FabricaUIArcade.CrearTexto(caja, "Resumen", "", 28, PaletaArcade.AmarilloClaro, TextAnchor.MiddleLeft, 3f, 0f);
        FabricaUIArcade.Colocar(resumen.rectTransform, new Vector2(0f, 0f), new Vector2(420f, 70f), new Vector2(760f, 50f));

        Button historia = FabricaUIArcade.CrearBoton(caja, "BotonHistoria", "VER HISTORIA", kit.botonMorado, new Vector2(340f, 80f), 28, null, kit);
        FabricaUIArcade.Colocar((RectTransform)historia.transform, new Vector2(1f, 0f), new Vector2(-600f, 70f), new Vector2(340f, 80f));
        historia.onClick.AddListener(VerHistoria);

        volver = FabricaUIArcade.CrearBoton(caja, "BotonVolver", "VOLVER", kit.botonRojo, new Vector2(300f, 80f), 30, kit.iconoCasa, kit);
        FabricaUIArcade.Colocar((RectTransform)volver.transform, new Vector2(1f, 0f), new Vector2(-230f, 70f), new Vector2(300f, 80f));
        volver.onClick.AddListener(Cerrar);
        panel.seleccionInicial = pestanas[0].gameObject;
    }

    public void Abrir()
    {
        Abierto = true;
        panel.Mostrar();
        Mostrar(pestana);
    }

    public void Cerrar()
    {
        Abierto = false;
        panel.Ocultar();
        if (AlCerrar != null) AlCerrar();
    }

    public System.Action AlCerrar;

    private void VerHistoria()
    {
        ServiciosMenus.Instancia.Clic();
        HistoriaJuego.MostrarIntro(null);
    }

    // ------------------------------------------------------------ pestañas
    private void Mostrar(int i)
    {
        pestana = i;
        KitSpritesArcade kit = KitSpritesArcade.Actual();
        for (int k = 0; k < pestanas.Count; k++)
        {
            Image img = pestanas[k].GetComponent<Image>();
            if (img != null) img.sprite = k == i ? kit.botonNaranja : kit.botonAzul;
        }
        for (int c = contenido.childCount - 1; c >= 0; c--) Destroy(contenido.GetChild(c).gameObject);
        resumen.text = "ESTRELLAS " + GameSession.EstrellasTotales() + " / 21     LOGROS " + Progreso.LogrosObtenidos() + " / " + Progreso.Logros.Length;
        switch (i)
        {
            case 0: Rejilla(Progreso.Accesorios, Progreso.AccesorioElegido, true); break;
            case 1: Rejilla(Progreso.Martillos, Progreso.MartilloElegido, false); break;
            case 2: Logros(); break;
            default: Records(); break;
        }
    }

    private void Rejilla(Progreso.Premio[] premios, int elegido, bool accesorios)
    {
        KitSpritesArcade kit = KitSpritesArcade.Actual();
        Sprite retrato = null;
        Sprite[] s = Personajes.Serie(Personajes.Actual, "abajo");
        if (s != null) retrato = s[0];
        for (int i = 0; i < premios.Length; i++)
        {
            Progreso.Premio p = premios[i];
            bool libre = Progreso.Desbloqueado(p);
            int col = i % 4, fila = i / 4;
            Vector2 pos = new Vector2(-555f + col * 370f, 150f - fila * 300f);

            Button carta = FabricaUIArcade.CrearBoton(contenido, "Carta" + i, "", i == elegido ? kit.botonAmarillo : kit.botonAzul, new Vector2(340f, 280f), 24, null, kit, false);
            FabricaUIArcade.Centrar((RectTransform)carta.transform, pos, new Vector2(340f, 280f));
            int k = i;
            carta.onClick.AddListener(() => Elegir(premios, k, accesorios));

            // Vista previa: el personaje con el accesorio (o el icono del efecto del martillo).
            if (accesorios && retrato != null)
            {
                Image r = FabricaUIArcade.CrearImagen(carta.transform, "Retrato", retrato, libre ? Color.white : new Color(0.2f, 0.2f, 0.25f));
                r.preserveAspect = true;
                FabricaUIArcade.Colocar(r.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(retrato.rect.width * 2.2f, retrato.rect.height * 2.2f));
                r.rectTransform.pivot = new Vector2(0.5f, 0f);
                Sprite gorro = string.IsNullOrEmpty(p.sprite) ? null : ArteExtra.Uno(p.sprite);
                if (gorro != null)
                {
                    Image g = FabricaUIArcade.CrearImagen(r.transform, "Gorro", gorro, libre ? Color.white : new Color(0.2f, 0.2f, 0.25f));
                    RectTransform gr = g.rectTransform;
                    gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 1f);
                    gr.pivot = new Vector2(0.5f, 0f);
                    gr.sizeDelta = gorro.rect.size * 2.2f;
                    float hundir = p.id == "auriculares" ? 0.3f : p.id == "fiesta" || p.id == "corona" ? 0.1f : p.id == "vikingo" ? 0.2f : p.id == "birrete" ? 0.12f : 0.16f;
                    gr.anchoredPosition = new Vector2(0f, -hundir * retrato.rect.height * 2.2f);
                }
            }
            else
            {
                Sprite ico = string.IsNullOrEmpty(p.sprite) ? null : ArteExtra.Uno(p.sprite);
                if (ico == null) ico = ArteExtra.Uno("Controles/icono_martillo");
                if (ico != null)
                {
                    Image im = FabricaUIArcade.CrearImagen(carta.transform, "Icono", ico, libre ? Color.white : new Color(0.2f, 0.2f, 0.25f));
                    im.preserveAspect = true;
                    FabricaUIArcade.Colocar(im.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(120f, 120f));
                }
            }
            if (!libre)
            {
                Image cand = FabricaUIArcade.CrearImagen(carta.transform, "Candado", Provisional.Candado, new Color(1f, 0.85f, 0.3f));
                FabricaUIArcade.Colocar(cand.rectTransform, new Vector2(1f, 1f), new Vector2(-36f, -36f), new Vector2(44f, 44f));
            }
            string pie = i == elegido ? "¡PUESTO!" : (libre ? p.nombre : p.Requisito);
            Text n = FabricaUIArcade.CrearTexto(carta.transform, "Nombre", libre ? p.nombre : "???", 24, i == elegido ? PaletaArcade.Contorno : PaletaArcade.Crema,
                TextAnchor.MiddleCenter, i == elegido ? 0f : 3f, 0f);
            FabricaUIArcade.Colocar(n.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(320f, 40f));
            n.resizeTextForBestFit = true; n.resizeTextMinSize = 14; n.resizeTextMaxSize = 24;
            Text r2 = FabricaUIArcade.CrearTexto(carta.transform, "Pie", pie == p.nombre ? "TOCA PARA USAR" : pie, 20,
                i == elegido ? PaletaArcade.Contorno : (libre ? PaletaArcade.AmarilloClaro : new Color(1f, 0.6f, 0.5f)), TextAnchor.MiddleCenter, i == elegido ? 0f : 2f, 0f);
            FabricaUIArcade.Colocar(r2.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(320f, 34f));
        }
    }

    private void Elegir(Progreso.Premio[] premios, int i, bool accesorios)
    {
        if (!Progreso.Desbloqueado(premios[i]))
        {
            ServiciosMenus.Instancia.Tic();
            return;
        }
        if (accesorios) Progreso.AccesorioElegido = i; else Progreso.MartilloElegido = i;
        ServiciosMenus.Instancia.Clic();
        Mostrar(pestana);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(pestanas[pestana].gameObject);
    }

    private void Logros()
    {
        KitSpritesArcade kit = KitSpritesArcade.Actual();
        Progreso.Logro[] lista = Progreso.Logros;
        int porColumna = Mathf.CeilToInt(lista.Length / 2f);
        for (int i = 0; i < lista.Length; i++)
        {
            Progreso.Logro l = lista[i];
            bool ok = Progreso.Obtenido(l);
            int col = i / porColumna, fila = i % porColumna;
            Vector2 pos = new Vector2(-370f + col * 750f, 280f - fila * 68f);
            RectTransform fil = FabricaUIArcade.CrearObjeto("Logro" + i, contenido);
            FabricaUIArcade.Centrar(fil, pos, new Vector2(730f, 64f));
            Image ico = FabricaUIArcade.CrearImagen(fil, "Icono", kit.iconoTrofeo, ok ? new Color(1f, 0.8f, 0.2f) : new Color(0.35f, 0.37f, 0.45f));
            FabricaUIArcade.Colocar(ico.rectTransform, new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(48f, 48f));
            Text t = FabricaUIArcade.CrearTexto(fil, "Titulo", l.titulo, 24, ok ? PaletaArcade.Amarillo : new Color(0.75f, 0.78f, 0.88f), TextAnchor.UpperLeft, 2f, 0f);
            FabricaUIArcade.Colocar(t.rectTransform, new Vector2(0f, 1f), new Vector2(70f, -4f), new Vector2(520f, 30f));
            t.rectTransform.pivot = new Vector2(0f, 1f);
            Text d = FabricaUIArcade.CrearTexto(fil, "Descripcion", l.descripcion, 19, PaletaArcade.Crema, TextAnchor.UpperLeft, 0f, 0f, true);
            FabricaUIArcade.Colocar(d.rectTransform, new Vector2(0f, 1f), new Vector2(70f, -33f), new Vector2(520f, 28f));
            d.rectTransform.pivot = new Vector2(0f, 1f);
            string avance = ok ? "LISTO" : Mathf.Min(Progreso.Contador(l.contador), l.meta) + "/" + l.meta;
            Text a = FabricaUIArcade.CrearTexto(fil, "Avance", ok ? "LISTO" : avance, 22, ok ? PaletaArcade.Verde : PaletaArcade.Crema, TextAnchor.MiddleRight, 2f, 0f);
            FabricaUIArcade.Colocar(a.rectTransform, new Vector2(1f, 0.5f), new Vector2(-70f, 0f), new Vector2(140f, 40f));
        }
    }

    private void Records()
    {
        Text cab = FabricaUIArcade.CrearTexto(contenido, "Cabecera", "PISO                                   ESTRELLAS     MEJOR PUNTAJE     MEJOR TIEMPO", 22,
            PaletaArcade.AmarilloClaro, TextAnchor.MiddleLeft, 2f, 0f);
        FabricaUIArcade.Centrar(cab.rectTransform, new Vector2(0f, 290f), new Vector2(1400f, 40f));
        for (int i = 0; i < GameSession.TotalPisosEstrellas; i++)
        {
            float y = 220f - i * 72f;
            Text n = FabricaUIArcade.CrearTexto(contenido, "Piso" + i, DefinicionPiso.NombreAreaDe(i), 24, PaletaArcade.Crema, TextAnchor.MiddleLeft, 2f, 0f);
            FabricaUIArcade.Centrar(n.rectTransform, new Vector2(-330f, y), new Vector2(740f, 44f));
            n.resizeTextForBestFit = true; n.resizeTextMinSize = 14; n.resizeTextMaxSize = 24;
            int estrellas = GameSession.EstrellasDe(i);
            for (int e = 0; e < 3; e++)
            {
                Image est = FabricaUIArcade.CrearImagen(contenido, "Estrella", null, Color.white);
                Provisional.PonerEstrella(est, e < estrellas, PaletaArcade.Amarillo, new Color(0.3f, 0.32f, 0.4f));
                FabricaUIArcade.Centrar(est.rectTransform, new Vector2(110f + e * 46f, y), new Vector2(40f, 40f));
            }
            int pts = Progreso.MejorPuntaje(i);
            Text p = FabricaUIArcade.CrearTexto(contenido, "Puntos", pts > 0 ? pts.ToString() : "---", 26, PaletaArcade.Crema, TextAnchor.MiddleCenter, 2f, 0f);
            FabricaUIArcade.Centrar(p.rectTransform, new Vector2(400f, y), new Vector2(240f, 44f));
            Text t = FabricaUIArcade.CrearTexto(contenido, "Tiempo", Progreso.FormatoTiempo(Progreso.MejorTiempo(i)), 26, PaletaArcade.Crema, TextAnchor.MiddleCenter, 2f, 0f);
            FabricaUIArcade.Centrar(t.rectTransform, new Vector2(620f, y), new Vector2(200f, 44f));
        }
    }
}
