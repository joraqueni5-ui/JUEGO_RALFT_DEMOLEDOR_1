using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// Dificultad de la partida (se elige antes de empezar; en multijugador la elige quien crea la partida):
///   FÁCIL    5 vidas, demoledores más lentos, cada reparación los cansa más y llegan menos mini villanos.
///   NORMAL   3 vidas, como está pensado el juego.
///   DIFÍCIL  2 vidas, demoledores más rápidos, se cansan menos y llegan más mini villanos.
/// </summary>
public static class Dificultad
{
    public enum Nivel { Facil = 0, Normal = 1, Dificil = 2 }

    private const string Clave = "dificultad.elegida";

    public static Nivel Actual
    {
        get { return (Nivel)Mathf.Clamp(PlayerPrefs.GetInt(Clave, 1), 0, 2); }
        set { PlayerPrefs.SetInt(Clave, (int)value); PlayerPrefs.Save(); }
    }

    public static string Nombre(Nivel n) { return n == Nivel.Facil ? "FÁCIL" : n == Nivel.Dificil ? "DIFÍCIL" : "NORMAL"; }
    public static string NombreActual { get { return Nombre(Actual); } }

    /// <summary>Vidas al empezar (sobre las 3 normales).</summary>
    public static int Vidas(int normales)
    {
        switch (Actual)
        {
            case Nivel.Facil: return normales + 2;
            case Nivel.Dificil: return Mathf.Max(1, normales - 1);
            default: return normales;
        }
    }

    /// <summary>Multiplica el ritmo de los demoledores (velocidad, preparación de golpes, pausas).</summary>
    public static float Ritmo { get { return Actual == Nivel.Facil ? 0.8f : Actual == Nivel.Dificil ? 1.22f : 1f; } }

    /// <summary>Multiplica cuánta energía pierde el demoledor con cada reparación o bloqueo.</summary>
    public static float Cansancio { get { return Actual == Nivel.Facil ? 1.3f : Actual == Nivel.Dificil ? 0.82f : 1f; } }

    /// <summary>Multiplica el intervalo entre mini villanos (más alto = llegan menos).</summary>
    public static float IntervaloMinis { get { return Actual == Nivel.Facil ? 1.4f : Actual == Nivel.Dificil ? 0.72f : 1f; } }

    public static Color ColorDe(Nivel n)
    {
        return n == Nivel.Facil ? new Color(0.45f, 0.95f, 0.5f) : n == Nivel.Dificil ? new Color(1f, 0.42f, 0.35f) : new Color(1f, 0.85f, 0.3f);
    }
}

/// <summary>Ventana "ELIGE LA DIFICULTAD" con tres tarjetas. Llama a 'alElegir' al confirmar (o a 'alCancelar' con VOLVER / ESC).</summary>
public class SelectorDificultad : MonoBehaviour
{
    public static bool Abierto { get { return actual != null; } }
    private static SelectorDificultad actual;

    private System.Action alElegir, alCancelar;
    private readonly Button[] tarjetas = new Button[3];

    private static readonly Color[] ColoresTarjeta =
    {
        new Color(0.35f, 0.85f, 0.45f), new Color(1f, 0.75f, 0.2f), new Color(0.95f, 0.3f, 0.3f),
    };

    private static readonly string[] Detalles =
    {
        "5 VIDAS\nDemoledores lentos\nSe cansan rápido\nPocos mini villanos",
        "3 VIDAS\nEl juego como fue\npensado",
        "2 VIDAS\nDemoledores veloces\nAguantan más\n¡Muchos mini villanos!",
    };

    public static void Mostrar(System.Action alElegir, System.Action alCancelar = null, string subtitulo = null)
    {
        if (actual != null) Destroy(actual.gameObject);
        FabricaUIArcade.AsegurarEventSystem();
        Canvas c = FabricaUIArcade.CrearCanvas("SelectorDificultad", 900);
        actual = c.gameObject.AddComponent<SelectorDificultad>();
        actual.alElegir = alElegir;
        actual.alCancelar = alCancelar;
        actual.Construir(subtitulo);
    }

    private void OnDestroy() { if (actual == this) actual = null; }

    private void Construir(string subtitulo)
    {
        KitSpritesArcade kit = KitSpritesArcade.Actual();
        Image velo = FabricaUIArcade.CrearImagen(transform, "Velo", null, new Color(0.02f, 0.03f, 0.08f, 0.9f), true);
        FabricaUIArcade.Estirar(velo.rectTransform);
        Text t = FabricaUIArcade.CrearTexto(transform, "Titulo", "ELIGE LA DIFICULTAD", 72, PaletaArcade.Amarillo);
        FabricaUIArcade.Centrar(t.rectTransform, new Vector2(0f, 400f), new Vector2(1700f, 100f));
        Text s = FabricaUIArcade.CrearTexto(transform, "Sub", subtitulo ?? "Puedes cambiarla cada vez que empiezas", 30, PaletaArcade.Crema, TextAnchor.MiddleCenter, 3f, 0f, true);
        FabricaUIArcade.Centrar(s.rectTransform, new Vector2(0f, 330f), new Vector2(1700f, 44f));

        Sprite[] fondos = { kit.botonAzul, kit.botonAmarillo, kit.botonRojo };
        for (int i = 0; i < 3; i++)
        {
            int k = i;
            Dificultad.Nivel n = (Dificultad.Nivel)i;
            Button b = FabricaUIArcade.CrearBoton(transform, "Tarjeta" + i, "", fondos[i], new Vector2(480f, 520f), 30, null, kit, false);
            FabricaUIArcade.Centrar((RectTransform)b.transform, new Vector2(-540f + i * 540f, -10f), new Vector2(480f, 520f));
            b.onClick.AddListener(() => Elegir(k));
            // Seleccionada no se oscurece (antes NORMAL parecía bloqueada).
            ColorBlock cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.pressedColor = new Color(0.92f, 0.92f, 0.92f);
            b.colors = cb;
            // Franja de color arriba (verde, ámbar, rojo): el tema serio deja las tres tarjetas del mismo azul.
            Image franja = FabricaUIArcade.CrearImagen(b.transform, "Franja", null, ColoresTarjeta[i]);
            RectTransform rfr = franja.rectTransform;
            rfr.anchorMin = new Vector2(0f, 1f);
            rfr.anchorMax = new Vector2(1f, 1f);
            rfr.pivot = new Vector2(0.5f, 1f);
            rfr.offsetMin = new Vector2(14f, -30f);
            rfr.offsetMax = new Vector2(-14f, -14f);
            Text nombre = FabricaUIArcade.CrearTexto(b.transform, "Nombre", Dificultad.Nombre(n), 60, PaletaArcade.Crema,
                TextAnchor.MiddleCenter, 4f, 0f);
            FabricaUIArcade.Colocar(nombre.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(460f, 90f));
            Text det = FabricaUIArcade.CrearTexto(b.transform, "Detalle", Detalles[i], 30, PaletaArcade.Crema,
                TextAnchor.UpperCenter, 2f, 0f, true);
            if (i == 1)
            {
                Text rec = FabricaUIArcade.CrearTexto(b.transform, "Recomendado", "RECOMENDADO", 26, ColoresTarjeta[1], TextAnchor.MiddleCenter, 3f, 0f);
                FabricaUIArcade.Colocar(rec.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(440f, 40f));
            }
            FabricaUIArcade.Colocar(det.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(440f, 300f));
            det.rectTransform.pivot = new Vector2(0.5f, 1f);
            tarjetas[i] = b;
        }
        FabricaUIArcade.NavegacionHorizontal(tarjetas);
        Button volver = FabricaUIArcade.CrearBoton(transform, "Volver", "VOLVER", kit.botonRojo, new Vector2(380f, 86f), 34, kit.iconoCasa, kit);
        FabricaUIArcade.Centrar((RectTransform)volver.transform, new Vector2(0f, -380f), new Vector2(380f, 86f));
        volver.onClick.AddListener(Cancelar);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(tarjetas[(int)Dificultad.Actual].gameObject);
    }

    private void Update()
    {
        Keyboard k = Keyboard.current;
        if (k != null && k.escapeKey.wasPressedThisFrame) Cancelar();
        float t = Time.unscaledTime;
        for (int i = 0; i < 3; i++)
        {
            if (tarjetas[i] == null) continue;
            bool sel = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == tarjetas[i].gameObject;
            float e = sel ? 1.06f + 0.02f * Mathf.Sin(t * 6f) : 1f;
            tarjetas[i].transform.localScale = new Vector3(e, e, 1f);
        }
    }

    private void Elegir(int i)
    {
        Dificultad.Actual = (Dificultad.Nivel)i;
        GestorAudio.Reproducir(Snd.MenuConfirmar);
        System.Action a = alElegir;
        Destroy(gameObject);
        if (a != null) a();
    }

    private void Cancelar()
    {
        GestorAudio.Reproducir(Snd.MenuVolver);
        System.Action a = alCancelar;
        Destroy(gameObject);
        if (a != null) a();
    }
}
