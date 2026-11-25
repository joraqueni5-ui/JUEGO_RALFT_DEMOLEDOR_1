using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// La historia del juego, contada con personajes que hablan en globos (como un cuento animado):
///   Intro  — la noche antes del examen final, un estudiante se duerme en el aula... y sueña. En el sueño, Don Carlos (el
///            guardia, con un cono en la cabeza "por seguridad") le cuenta que viene EL DEMOLEDOR, nacido del estrés de
///            todos los exámenes, y le da un martillo. Cada estudiante se duerme en el lugar de su carrera (la cocinera en
///            la cocina, el de sistemas en el laboratorio de computación...). Se ve al empezar con JUGAR (solo) y desde PREMIOS.
///   Final  — "¡POR FIN LO VENCIMOS!": aparecen todos a festejar... suena el despertador y despierta donde se durmió. Es la mañana del
///            examen; en la puerta de la U, Don Carlos le guiña un ojo... y en un arbusto se mueve un casco amarillo.
/// Toca o ENTER para seguir; SALTAR (o ESC) para saltarla.
/// </summary>
public static class HistoriaJuego
{
    private const string ClaveIntro = "historia.intro.vista";

    public static bool IntroVista
    {
        get { return PlayerPrefs.GetInt(ClaveIntro, 0) == 1; }
        set { PlayerPrefs.SetInt(ClaveIntro, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    /// <summary>Muestra la intro (por ejemplo desde el menú) y llama a 'alTerminar' al cerrarla.</summary>
    public static void MostrarIntro(System.Action alTerminar)
    {
        PantallaHistoria p = PantallaHistoria.Crear();
        p.StartCoroutine(Ejecutar(p, p.Intro(), alTerminar));
    }

    /// <summary>El final, para usar con yield return desde el nivel final.</summary>
    public static IEnumerator ReproducirFinal()
    {
        PantallaHistoria p = PantallaHistoria.Crear();
        yield return p.Final();
        Progreso.Sumar("final_visto");
        p.Cerrar();
    }

    private static IEnumerator Ejecutar(PantallaHistoria p, IEnumerator rutina, System.Action alTerminar)
    {
        yield return rutina;
        IntroVista = true;
        if (alTerminar != null)
        {
            // Primero lo que sigue (por ejemplo cargar el piso) y la pantalla negra tapa el cambio un momento.
            alTerminar();
            float t = 0f;
            while (t < 1.5f && p != null) { t += Time.unscaledDeltaTime; yield return null; }
        }
        if (p != null) p.Cerrar();
    }
}

public class PantallaHistoria : MonoBehaviour
{
    private const float Px = 4f;   // los fondos son de 480 × 270: ×4 llenan 1920 × 1080

    private class Actor
    {
        public RectTransform rt;
        public Image img;
        public Sprite[] cuadros;
        public float fps = 8f;
        public bool saltando, temblando;
        public float fase;
        public Vector2 basePos;
        public Image gorro;
        public bool quieto;   // props: sin respiración
    }

    private Canvas lienzo;
    private RectTransform escenario;
    private Image fondo, frente, velo, cajaDialogo, etiquetaNombre;
    private Text textoNombre, textoDialogo, textoPista, textoGrande;
    private readonly List<Actor> actores = new List<Actor>();
    private bool saltar, avanzar;
    private GameObject seleccionPrevia;
    private bool navegacionPrevia = true;

    public static PantallaHistoria Crear()
    {
        GameObject g = new GameObject("[PantallaHistoria]");
        PantallaHistoria p = g.AddComponent<PantallaHistoria>();
        p.Construir();
        return p;
    }

    private void Construir()
    {
        FabricaUIArcade.AsegurarEventSystem();
        lienzo = FabricaUIArcade.CrearCanvas("LienzoHistoria", 980);
        lienzo.transform.SetParent(transform, false);
        CanvasScaler cs = lienzo.GetComponent<CanvasScaler>();
        cs.matchWidthOrHeight = 1f;   // siempre se ve el alto completo

        Image negro = FabricaUIArcade.CrearImagen(lienzo.transform, "Negro", null, Color.black, true);
        FabricaUIArcade.Estirar(negro.rectTransform);

        escenario = FabricaUIArcade.CrearObjeto("Escenario", lienzo.transform);
        FabricaUIArcade.Centrar(escenario, Vector2.zero, new Vector2(1920f, 1080f));
        fondo = FabricaUIArcade.CrearImagen(escenario, "Fondo", null, Color.white);
        FabricaUIArcade.Estirar(fondo.rectTransform);
        frente = FabricaUIArcade.CrearImagen(escenario, "Frente", null, Color.white);
        FabricaUIArcade.Estirar(frente.rectTransform);
        frente.enabled = false;

        KitSpritesArcade kit = KitSpritesArcade.Actual();
        cajaDialogo = FabricaUIArcade.CrearImagen(lienzo.transform, "Dialogo", kit != null ? kit.panel : null, new Color(1f, 1f, 1f, 0.97f));
        FabricaUIArcade.Colocar(cajaDialogo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1640f, 250f));
        etiquetaNombre = FabricaUIArcade.CrearImagen(cajaDialogo.transform, "Nombre", kit != null ? kit.botonNaranja : null, Color.white);
        FabricaUIArcade.Colocar(etiquetaNombre.rectTransform, new Vector2(0f, 1f), new Vector2(230f, 10f), new Vector2(400f, 76f));
        textoNombre = FabricaUIArcade.CrearTexto(etiquetaNombre.transform, "Texto", "", 34, PaletaArcade.Crema, TextAnchor.MiddleCenter, 3f, 3f);
        FabricaUIArcade.Estirar(textoNombre.rectTransform);
        textoDialogo = FabricaUIArcade.CrearTexto(cajaDialogo.transform, "Texto", "", 40, PaletaArcade.Crema, TextAnchor.UpperLeft, 3f, 0f, true);
        FabricaUIArcade.Estirar(textoDialogo.rectTransform);
        textoDialogo.rectTransform.offsetMin = new Vector2(60f, 40f);
        textoDialogo.rectTransform.offsetMax = new Vector2(-60f, -60f);
        textoDialogo.horizontalOverflow = HorizontalWrapMode.Wrap;

        textoPista = FabricaUIArcade.CrearTexto(lienzo.transform, "Pista", PreferenciasMenus.EsCelular ? "TOCA PARA SEGUIR" : "ENTER O CLIC PARA SEGUIR",
            24, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleRight, 2f, 0f);
        FabricaUIArcade.Colocar(textoPista.rectTransform, new Vector2(1f, 0f), new Vector2(-180f, 40f), new Vector2(600f, 40f));

        textoGrande = FabricaUIArcade.CrearTexto(lienzo.transform, "Grande", "", 120, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, 7f, 7f);
        FabricaUIArcade.Colocar(textoGrande.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(1800f, 220f));

        Button botonSaltar = FabricaUIArcade.CrearBoton(lienzo.transform, "Saltar", "SALTAR >>", kit != null ? kit.botonAzul : null, new Vector2(300f, 80f), 30, null, kit, false);
        FabricaUIArcade.Colocar((RectTransform)botonSaltar.transform, new Vector2(1f, 1f), new Vector2(-180f, -70f), new Vector2(300f, 80f));
        botonSaltar.onClick.AddListener(() => saltar = true);

        velo = FabricaUIArcade.CrearImagen(lienzo.transform, "Velo", null, Color.black);
        FabricaUIArcade.Estirar(velo.rectTransform);
        velo.raycastTarget = false;

        // Que ENTER no apriete el botón del menú que quedó seleccionado debajo.
        if (EventSystem.current != null)
        {
            seleccionPrevia = EventSystem.current.currentSelectedGameObject;
            navegacionPrevia = EventSystem.current.sendNavigationEvents;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.sendNavigationEvents = false;
        }
        MostrarDialogo(false);
    }

    public void Cerrar()
    {
        GestorAudio.ForzarMusica("");
        if (EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = navegacionPrevia;
            if (seleccionPrevia != null) EventSystem.current.SetSelectedGameObject(seleccionPrevia);
        }
        Destroy(gameObject);
    }

    // ------------------------------------------------------------ entrada
    private void Update()
    {
        Keyboard k = Keyboard.current;
        if (k != null && k.escapeKey.wasPressedThisFrame) saltar = true;
        bool toque = (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)) ||
                     (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !SobreBotonSaltar(Mouse.current.position.ReadValue())) ||
                     (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame &&
                      !SobreBotonSaltar(Touchscreen.current.primaryTouch.position.ReadValue()));
        if (toque) avanzar = true;

        float t = Time.unscaledTime;
        foreach (Actor a in actores)
        {
            if (a.rt == null) continue;
            if (a.cuadros != null && a.cuadros.Length > 0)
            {
                a.img.sprite = a.cuadros[(int)((t + a.fase) * a.fps) % a.cuadros.Length];
            }
            Vector2 p = a.basePos;
            if (a.saltando) p.y += Mathf.Abs(Mathf.Sin((t + a.fase) * 7f)) * 50f;
            if (a.temblando) p.x += Mathf.Sin(t * 60f) * 6f;
            a.rt.anchoredPosition = p;
            // Respiran: nadie queda tieso como una foto.
            if (!a.quieto && !a.saltando && a.fps == 0f)
                a.rt.localScale = new Vector3(Mathf.Sign(a.rt.localScale.x), 1f + 0.014f * Mathf.Sin((t + a.fase) * 2.4f), 1f);
        }
        textoPista.color = new Color(1f, 1f, 1f, 0.45f + 0.3f * Mathf.Sin(t * 4f));
    }

    private static bool SobreBotonSaltar(Vector2 pantalla)
    {
        // Esquina de arriba a la derecha (donde está SALTAR): ese toque no cuenta como "seguir".
        return pantalla.x > Screen.width * 0.8f && pantalla.y > Screen.height * 0.82f;
    }

    // ------------------------------------------------------------ ayudas
    private Actor NuevoActor(string nombre, Sprite[] cuadros, Vector2 pos, float escala = Px, bool espejo = false)
    {
        Image img = FabricaUIArcade.CrearImagen(escenario, nombre, cuadros != null && cuadros.Length > 0 ? cuadros[0] : null, Color.white);
        img.preserveAspect = true;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);
        Sprite s = img.sprite;
        rt.sizeDelta = s != null ? s.rect.size * escala : new Vector2(100f, 200f);
        rt.localScale = new Vector3(espejo ? -1f : 1f, 1f, 1f);
        Actor a = new Actor { rt = rt, img = img, cuadros = cuadros, basePos = pos, fase = Random.value * 3f };
        rt.anchoredPosition = pos;
        actores.Add(a);
        return a;
    }

    private Actor Personaje(string id, string animacion, Vector2 pos, bool espejo = false)
    {
        Personajes.Ficha f = id == null ? Personajes.Actual : System.Array.Find(Personajes.Todos, x => x.id == id);
        Sprite[] s = Personajes.SerieFluida(f, animacion);
        Actor a = NuevoActor(f != null ? f.nombre : "Personaje", s, pos, Px, espejo);
        a.fps = 0f;   // quieto (primer cuadro) hasta que camine
        if (f != null && f.escalaAltura != 1f) a.rt.sizeDelta *= f.escalaAltura;
        return a;
    }

    private static string NombreDe(string id)
    {
        if (id == null) return Personajes.Actual.nombre;
        Personajes.Ficha f = System.Array.Find(Personajes.Todos, x => x.id == id);
        return f != null ? f.nombre : id.ToUpperInvariant();
    }

    private static Burbuja.Voz VozDe(string id)
    {
        if (id == null) return Personajes.Actual.Voz;
        Personajes.Ficha f = System.Array.Find(Personajes.Todos, x => x.id == id);
        return f != null ? f.Voz : Burbuja.Voz.Hombre;
    }

    private void PonerCono(Actor a)
    {
        Sprite cono = ArteExtra.Uno("Historia/cono");
        if (cono == null || a == null) return;
        Image g = FabricaUIArcade.CrearImagen(a.rt, "Cono", cono, Color.white);
        RectTransform rt = g.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = cono.rect.size * Px;
        rt.anchoredPosition = new Vector2(0f, -10f * Px);
        a.gorro = g;
    }

    private void Limpiar()
    {
        foreach (Actor a in actores) if (a.rt != null) Destroy(a.rt.gameObject);
        actores.Clear();
        textoGrande.text = "";
    }

    private void Fondo(string ruta, Color color)
    {
        Sprite s = ArteExtra.Uno(ruta);
        fondo.sprite = s;
        fondo.color = s != null ? color : new Color(0.08f, 0.1f, 0.2f);
    }

    private void MostrarDialogo(bool ver)
    {
        if (ver && !cajaDialogo.gameObject.activeSelf && isActiveAndEnabled) StartCoroutine(Aparecer(cajaDialogo.rectTransform));
        cajaDialogo.gameObject.SetActive(ver);
        textoPista.gameObject.SetActive(ver);
    }

    private IEnumerator Aparecer(RectTransform rt)
    {
        float t = 0f;
        while (t < 0.18f && rt != null)
        {
            t += Time.unscaledDeltaTime;
            float k = t / 0.18f;
            float e = k < 0.7f ? Mathf.Lerp(0.85f, 1.04f, k / 0.7f) : Mathf.Lerp(1.04f, 1f, (k - 0.7f) / 0.3f);
            rt.localScale = new Vector3(e, e, 1f);
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
    }

    private IEnumerator Esperar(float s)
    {
        float t = 0f;
        while (t < s && !saltar) { t += Time.unscaledDeltaTime; yield return null; }
    }

    private IEnumerator Fundido(float desde, float hasta, float dur, Color color)
    {
        float t = 0f;
        while (t < dur && !saltar)
        {
            t += Time.unscaledDeltaTime;
            Color c = color; c.a = Mathf.Lerp(desde, hasta, t / dur);
            velo.color = c;
            yield return null;
        }
        Color f = color; f.a = hasta;
        velo.color = f;
    }

    /// <summary>Un globo de diálogo: letra a letra con su balbuceo; toca para completar y otra vez para seguir.</summary>
    private IEnumerator Decir(string idHablante, string texto, Actor quien = null, bool narrador = false)
    {
        if (saltar) yield break;
        MostrarDialogo(true);
        etiquetaNombre.gameObject.SetActive(!narrador);
        textoNombre.text = narrador ? "" : (idHablante == "demoledor" ? "DEMOLEDOR" : NombreDe(idHablante));
        textoDialogo.fontStyle = narrador ? FontStyle.Italic : FontStyle.Normal;
        Burbuja.Voz voz = idHablante == "demoledor" ? Burbuja.Voz.Villano : (narrador ? Burbuja.Voz.Ninguna : VozDe(idHablante));
        avanzar = false;
        float inicio = Time.unscaledTime, proximaSilaba = 0f;
        int visibles = 0;
        while (visibles < texto.Length && !saltar)
        {
            if (avanzar) { avanzar = false; break; }
            int n = Mathf.Min(texto.Length, (int)((Time.unscaledTime - inicio) * 38f));
            if (n != visibles)
            {
                visibles = n;
                textoDialogo.text = texto.Substring(0, visibles);
                char c = texto[visibles - 1];
                if (voz != Burbuja.Voz.Ninguna && char.IsLetter(c) && Time.unscaledTime >= proximaSilaba)
                {
                    proximaSilaba = Time.unscaledTime + 0.075f;
                    GestorAudio.Reproducir(Burbuja.NombreVoz(voz, c), 0.55f, Random.Range(0.94f, 1.08f));
                }
            }
            if (quien != null && quien.rt != null) quien.rt.localScale = new Vector3(Mathf.Sign(quien.rt.localScale.x), 1f + 0.03f * Mathf.Sin(Time.unscaledTime * 30f), 1f);
            yield return null;
        }
        textoDialogo.text = texto;
        if (quien != null && quien.rt != null) quien.rt.localScale = new Vector3(Mathf.Sign(quien.rt.localScale.x), 1f, 1f);
        while (!avanzar && !saltar) yield return null;
        avanzar = false;
        GestorAudio.Reproducir(Snd.MenuMover, 0.5f);
    }

    private IEnumerator Caminar(Actor a, Vector2 hasta, float dur, string id, string animacion)
    {
        if (a == null) yield break;
        Personajes.Ficha f = id == null ? Personajes.Actual : System.Array.Find(Personajes.Todos, x => x.id == id);
        Sprite[] s = f != null ? Personajes.SerieFluida(f, animacion) : null;
        if (s != null) a.cuadros = s;
        a.fps = a.cuadros != null && a.cuadros.Length >= 8 ? 16f : 8f;
        Vector2 desde = a.basePos;
        float t = 0f;
        while (t < dur && !saltar)
        {
            t += Time.unscaledDeltaTime;
            a.basePos = Vector2.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }
        a.basePos = hasta;
        a.fps = 0f;
        if (f != null) { Sprite[] quieto = Personajes.SerieFluida(f, "abajo"); if (quieto != null) { a.cuadros = quieto; } }
    }

    private IEnumerator TituloGrande(string texto, Color color, float dur)
    {
        textoGrande.text = texto;
        textoGrande.color = color;
        float t = 0f;
        while (t < dur && !saltar)
        {
            t += Time.unscaledDeltaTime;
            float e = t < 0.2f ? Mathf.Lerp(2.2f, 0.9f, t / 0.2f) : (t < 0.3f ? Mathf.Lerp(0.9f, 1f, (t - 0.2f) / 0.1f) : 1f + 0.03f * Mathf.Sin(t * 5f));
            textoGrande.rectTransform.localScale = new Vector3(e, e, 1f);
            if (avanzar && t > 0.5f) { avanzar = false; break; }
            yield return null;
        }
    }

    private void Confeti(int n)
    {
        Sprite[] confeti = ArteExtra.Serie("Mejoras/confeti_", 4);
        if (confeti == null) return;
        StartCoroutine(LluviaConfeti(confeti, n));
    }

    private IEnumerator LluviaConfeti(Sprite[] confeti, int n)
    {
        List<RectTransform> piezas = new List<RectTransform>();
        List<Vector2> vel = new List<Vector2>();
        for (int i = 0; i < n; i++)
        {
            Image img = FabricaUIArcade.CrearImagen(escenario, "Confeti", confeti[Random.Range(0, confeti.Length)], Color.white);
            img.rectTransform.sizeDelta = img.sprite.rect.size * 6f;
            img.rectTransform.anchoredPosition = new Vector2(Random.Range(-960f, 960f), Random.Range(560f, 1200f));
            piezas.Add(img.rectTransform);
            vel.Add(new Vector2(Random.Range(-60f, 60f), Random.Range(-380f, -220f)));
        }
        float t = 0f;
        while (t < 6f)
        {
            t += Time.unscaledDeltaTime;
            for (int i = 0; i < piezas.Count; i++)
            {
                if (piezas[i] == null) continue;
                piezas[i].anchoredPosition += vel[i] * Time.unscaledDeltaTime + new Vector2(Mathf.Sin(t * 3f + i) * 40f * Time.unscaledDeltaTime, 0f);
                piezas[i].localRotation = Quaternion.Euler(0f, 0f, t * 200f + i * 37f);
            }
            yield return null;
        }
        foreach (RectTransform r in piezas) if (r != null) Destroy(r.gameObject);
    }

    // ================================================================== LORE POR CARRERA
    /// <summary>Dónde y cómo se queda dormido cada estudiante (y cómo se despierta), según su carrera.</summary>
    private class Lore
    {
        public string id, l1, l2, narr, blanco, martillo, porQue, despierta, examen;
        public Prop[] props = new Prop[0];
    }

    /// <summary>Algo animado del fondo: cuadros en bucle o un emisor de partículas que suben (vapor, notas).</summary>
    private class Prop
    {
        public string serie; public int n; public Vector2 px; public float escala = 4f, fps = 6f; public bool emisor; public Color color = Color.white;
    }

    private static Lore LoreDe(string id)
    {
        switch (id)
        {
            case "sistemas": return new Lore { id = id,
                l1 = "Compila... compila... ¡ERROR en la línea 404! ¿Otra vez?",
                l2 = "Descanso los ojos mientras corren las pruebas... cinco minutitos...",
                narr = "...y Diego se quedó dormido sobre el teclado del laboratorio de computación.",
                blanco = "¡empezando por tus computadoras!",
                martillo = "¿Un martillo? ¿No hay un parche, una actualización... un Ctrl+Z?",
                porQue = "Porque arreglas bugs toda la noche. ¡Esto es lo mismo, pero con ladrillos!",
                despierta = "...Tengo las teclas marcadas en la cara.",
                examen = "¡El examen de programación es a las 8! ¡Voy a llegar tarde!",
                props = new[] {
                    new Prop { serie = "Vida/codigo_", n = 4, px = new Vector2(138f, 115f), escala = 14f, fps = 2.5f },
                    new Prop { serie = "Vida/codigo_", n = 4, px = new Vector2(208f, 115f), escala = 14f, fps = 2f },
                    new Prop { serie = "Vida/codigo_", n = 4, px = new Vector2(318f, 115f), escala = 14f, fps = 3f },
                    new Prop { serie = "Vida/codigo_", n = 4, px = new Vector2(173f, 176f), escala = 11f, fps = 2.5f } } };
            case "medicina": return new Lore { id = id,
                l1 = "Fémur, tibia, peroné... ¿y cómo se llamaba este huesito?",
                l2 = "Repaso el corazón una vez más y me voy... una vez más...",
                narr = "...y Valeria se quedó dormida en el laboratorio de Enfermería, abrazada a su libro de anatomía.",
                blanco = "¡empezando por tu laboratorio!",
                martillo = "Prefiero un bisturí... pero bueno, ¡operemos a este edificio!",
                porQue = "Porque sabes curar. Y esta universidad está muy, MUY enferma.",
                despierta = "...¿Por qué el esqueleto me mira raro?",
                examen = "¡El examen de anatomía es a las 8! ¡Voy a llegar tarde!",
                props = new[] { new Prop { serie = "Vida/ecg_", n = 8, px = new Vector2(305f, 64f), escala = 15f, fps = 9f } } };
            case "administracion": return new Lore { id = id,
                l1 = "Activos, pasivos, patrimonio... ¿por qué este balance no cuadra?",
                l2 = "Hago una tabla más y descanso... solo una celda más...",
                narr = "...y Camila se quedó dormida en la oficina, entre facturas y una calculadora.",
                blanco = "¡empezando por tu oficina!",
                martillo = "¿Cuánto cuesta reparar todo eso? ...No, mejor no lo calculo.",
                porQue = "Porque sabes administrar el caos. ¡Y él es puro caos!",
                despierta = "...Y el balance sigue sin cuadrar.",
                examen = "¡La presentación del examen es a las 8! ¡Voy a llegar tarde!",
                props = new[] { new Prop { serie = "Vida/papel", n = 0, px = new Vector2(168f, 176f), escala = 5f, fps = 0.6f, emisor = true } } };
            case "gastronomia": return new Lore { id = id,
                l1 = "Si mañana el soufflé no sube, el chef me reprueba... ¡una prueba más!",
                l2 = "Mientras se hornea, cierro los ojitos... cinco minutos...",
                narr = "...y Lucía se quedó dormida en la cocina de la U, con el horno encendido.",
                blanco = "¡empezando por tu cocina!",
                martillo = "¿Un martillo? Yo uso un rodillo de amasar... bueno, ¡se parece!",
                porQue = "Porque nadie aguanta el calor como tú. ¡Y esto se va a poner MUY caliente!",
                despierta = "...¡AY, NO! ¡MI SOUFFLÉ!",
                examen = "¡El examen práctico de cocina es a las 8! ¡Voy a llegar tarde!",
                props = new[] {
                    new Prop { serie = "Mejoras/vapor_", n = 4, px = new Vector2(51f, 100f), escala = 7f, fps = 2.2f, emisor = true, color = new Color(1f, 1f, 1f, 0.7f) },
                    new Prop { serie = "Mejoras/vapor_", n = 4, px = new Vector2(95f, 100f), escala = 7f, fps = 1.6f, emisor = true, color = new Color(1f, 1f, 1f, 0.7f) },
                    new Prop { serie = "Martillos/llama_", n = 3, px = new Vector2(52f, 168f), escala = 6f, fps = 10f },
                    new Prop { serie = "Martillos/llama_", n = 3, px = new Vector2(98f, 168f), escala = 6f, fps = 9f } } };
            case "derecho": return new Lore { id = id,
                l1 = "Artículo 1.234, inciso C... ¿o era el inciso D? ¡Objeción, cerebro!",
                l2 = "Leo un código más y cierro la biblioteca... un código más...",
                narr = "...y Mateo se quedó dormido en la biblioteca, sobre el Código Civil.",
                blanco = "¡empezando por tu biblioteca!",
                martillo = "Esto es un mazo de juez, ¿no? ¡Orden en la sala... y en el edificio!",
                porQue = "Porque sabes defender. ¡Y la U necesita un buen abogado!",
                despierta = "...Tengo el Código Civil pegado en la cara.",
                examen = "¡El examen oral es a las 8! ¡Voy a llegar tarde!" };
            case "arquitectura": return new Lore { id = id,
                l1 = "Si la maqueta no queda perfecta, el jurado me la destruye... ¡qué ironía!",
                l2 = "Pego esta columnita y descanso... solo un poquito de pegamento más...",
                narr = "...y Sofía se quedó dormida en el taller, al lado de su maqueta.",
                blanco = "¡empezando por tu taller!",
                martillo = "¡Yo diseño edificios y ese tipo los destruye! ¡Esto es personal!",
                porQue = "Porque conoces cada columna y cada viga. ¡Nadie lo repara mejor que tú!",
                despierta = "...¡Uf! La maqueta sigue entera.",
                examen = "¡La entrega del proyecto es a las 8! ¡Voy a llegar tarde!" };
            case "odontologia": return new Lore { id = id,
                l1 = "Molar, premolar, canino... ¡32 dientes y me olvido de todos!",
                l2 = "Practico una curación más en el modelo y me voy... una más...",
                narr = "...y Ana se quedó dormida en la clínica, con el modelo de dientes en la mano.",
                blanco = "¡empezando por tu clínica!",
                martillo = "¿Un martillo? En odontología no usamos esto... ¡espero!",
                porQue = "Porque tienes mano firme. ¡Y ese demoledor tiene los dientes muy largos!",
                despierta = "...El modelo de dientes me está sonriendo.",
                examen = "¡El examen práctico es a las 8! ¡Voy a llegar tarde!" };
            case "comunicacion": return new Lore { id = id,
                l1 = "Buenas noches, La Paz... no. ¡Buenas noches, BOLIVIA! ...tampoco.",
                l2 = "Grabo la entrada una vez más y edito mañana... una toma más...",
                narr = "...y Bruno se quedó dormido en el estudio de radio, con los audífonos puestos.",
                blanco = "¡empezando por tu estudio de radio!",
                martillo = "¡Exclusiva! Bruno, en vivo, desde la universidad que se derrumba...",
                porQue = "Porque tienes la voz para avisar a todos. ¡Y el coraje para bajarle el volumen a él!",
                despierta = "...¿Seguimos al aire?",
                examen = "¡La presentación del noticiero es a las 8! ¡Voy a llegar tarde!",
                props = new[] { new Prop { serie = "Vida/nota_", n = 2, px = new Vector2(322f, 186f), escala = 6f, fps = 1.2f, emisor = true } } };
            default: return null;
        }
    }

    private static Vector2 Ui(Vector2 px) { return new Vector2((px.x - 240f) * Px, (135f - px.y) * Px); }

    private void Frente(string ruta)
    {
        Sprite s = string.IsNullOrEmpty(ruta) ? null : ArteExtra.Uno(ruta);
        frente.sprite = s;
        frente.enabled = s != null;
        frente.rectTransform.SetAsLastSibling();
    }

    private readonly List<GameObject> extras = new List<GameObject>();
    private bool durmiendo;

    /// <summary>Lo animado del fondo de la carrera (pantallas, vapor, llamas, notas).</summary>
    private void PonerProps(Lore lore)
    {
        if (lore == null) return;
        foreach (Prop p in lore.props)
        {
            Sprite[] s = p.n > 0 ? ArteExtra.Serie(p.serie, p.n) : (ArteExtra.Uno(p.serie) != null ? new[] { ArteExtra.Uno(p.serie) } : null);
            if (s == null) continue;
            if (p.emisor) { StartCoroutine(Emisor(s, p)); continue; }
            Actor a = NuevoActor("Prop", s, Ui(p.px), p.escala);
            a.rt.pivot = new Vector2(0.5f, 0.5f);
            a.fps = p.fps;
            a.img.color = p.color;
            a.quieto = true;
        }
    }

    private IEnumerator Emisor(Sprite[] cuadros, Prop p)
    {
        while (this != null)
        {
            if (!EscenaDeCarrera) yield break;
            Image img = FabricaUIArcade.CrearImagen(escenario, "Particula", cuadros[Random.Range(0, cuadros.Length)], p.color);
            img.rectTransform.sizeDelta = img.sprite.rect.size * p.escala;
            Vector2 desde = Ui(p.px) + new Vector2(Random.Range(-14f, 14f), 0f);
            img.rectTransform.anchoredPosition = desde;
            extras.Add(img.gameObject);
            StartCoroutine(Subir(img, desde, cuadros));
            float t = 0f, espera = 1f / Mathf.Max(0.2f, p.fps);
            while (t < espera) { t += Time.unscaledDeltaTime; yield return null; }
        }
    }

    private bool EscenaDeCarrera;
    private Coroutine acercando;

    private IEnumerator Subir(Image img, Vector2 desde, Sprite[] cuadros)
    {
        float t = 0f, dur = 2.2f, fase = Random.value * 6f;
        Color c0 = img.color;
        while (t < dur && img != null)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            img.rectTransform.anchoredPosition = desde + new Vector2(Mathf.Sin(t * 2.5f + fase) * 18f, k * 170f);
            img.rectTransform.localScale = Vector3.one * (0.7f + 0.6f * k);
            if (cuadros.Length > 1) img.sprite = cuadros[Mathf.Min(cuadros.Length - 1, (int)(k * cuadros.Length))];
            Color c = c0; c.a = c0.a * (k < 0.2f ? k / 0.2f : 1f - (k - 0.2f) / 0.8f); img.color = c;
            yield return null;
        }
        if (img != null) Destroy(img.gameObject);
    }

    /// <summary>Las "Z z z" que suben de la cabeza mientras duerme.</summary>
    private IEnumerator Zetas(Actor a)
    {
        int i = 0;
        while (durmiendo && a != null && a.rt != null)
        {
            Text z = FabricaUIArcade.CrearTexto(escenario, "Z", i % 2 == 0 ? "Z" : "z", 46 + (i % 3) * 8, new Color(0.85f, 0.9f, 1f), TextAnchor.MiddleCenter, 3f, 0f);
            z.rectTransform.sizeDelta = new Vector2(80f, 80f);
            Vector2 desde = a.basePos + new Vector2(60f, a.rt.sizeDelta.y * 0.85f);
            extras.Add(z.gameObject);
            StartCoroutine(SubirTexto(z, desde));
            i++;
            float t = 0f;
            while (t < 0.75f && durmiendo) { t += Time.unscaledDeltaTime; yield return null; }
        }
    }

    private IEnumerator SubirTexto(Text z, Vector2 desde)
    {
        float t = 0f;
        while (t < 2f && z != null)
        {
            t += Time.unscaledDeltaTime;
            float k = t / 2f;
            z.rectTransform.anchoredPosition = desde + new Vector2(Mathf.Sin(t * 3f) * 22f + k * 60f, k * 200f);
            z.rectTransform.localScale = Vector3.one * (0.6f + k);
            z.color = new Color(0.85f, 0.9f, 1f, 1f - k);
            yield return null;
        }
        if (z != null) Destroy(z.gameObject);
    }

    /// <summary>Cabecea dos veces y al final se desploma sobre la mesa.</summary>
    private IEnumerator Cabecear(Actor a)
    {
        if (a == null) yield break;
        Vector2 base0 = a.basePos;
        for (int n = 0; n < 2 && !saltar; n++)
        {
            float t = 0f;
            while (t < 0.9f && !saltar)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Sin(t / 0.9f * Mathf.PI);
                a.rt.localRotation = Quaternion.Euler(0f, 0f, -7f * k);
                a.basePos = base0 + new Vector2(0f, -22f * k);
                yield return null;
            }
            // Se despierta de golpe un instante.
            a.basePos = base0 + new Vector2(0f, 10f);
            yield return Esperar(0.12f);
            a.basePos = base0;
            yield return Esperar(0.55f);
        }
        yield return Desplomar(a, base0, 0.8f);
    }

    private IEnumerator Desplomar(Actor a, Vector2 base0, float dur)
    {
        float t = 0f;
        while (t < dur && !saltar)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            a.rt.localRotation = Quaternion.Euler(0f, 0f, -11f * k);
            a.basePos = base0 + new Vector2(14f * k, -70f * k);
            yield return null;
        }
        a.rt.localRotation = Quaternion.Euler(0f, 0f, -11f);
        a.basePos = base0 + new Vector2(14f, -70f);
    }

    /// <summary>Acercamiento lento de la "cámara" (el escenario crece un poco).</summary>
    private IEnumerator Acercar(float hasta, float dur)
    {
        float desde = escenario.localScale.x, t = 0f;
        while (t < dur && !saltar)
        {
            t += Time.unscaledDeltaTime;
            float e = Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t / dur));
            escenario.localScale = new Vector3(e, e, 1f);
            yield return null;
        }
    }

    private void LimpiarExtras()
    {
        if (acercando != null) StopCoroutine(acercando);
        acercando = null;
        durmiendo = false;
        EscenaDeCarrera = false;
        foreach (GameObject g in extras) if (g != null) Destroy(g);
        extras.Clear();
        Frente(null);
        escenario.localScale = Vector3.one;
    }

    // ================================================================== INTRO
    public IEnumerator Intro()
    {
        GestorAudio.ForzarMusica("musica_historia");
        string yo = null;   // el personaje elegido
        Personajes.Ficha ficha = Personajes.Actual;
        Lore lore = LoreDe(ficha != null ? ficha.id : "");

        // 1) La víspera del examen: cada estudiante en el lugar de su carrera (la cocinera en la cocina, el de sistemas
        //    en el laboratorio de computación...), de noche, estudiando.
        if (lore != null) Fondo("Historia/carrera_" + lore.id, Color.white);
        else Fondo("Historia/fondo_aula", new Color(0.62f, 0.66f, 0.85f));
        Actor heroe = Personaje(yo, "abajo", new Vector2(0f, -400f));
        Frente(lore != null ? "Historia/mesa_" + lore.id : "Historia/mesa_aula");
        EscenaDeCarrera = true;
        PonerProps(lore);
        acercando = StartCoroutine(Acercar(1.08f, 16f));
        yield return Fundido(1f, 0f, 1f, Color.black);
        yield return Decir(null, "Era la noche antes del EXAMEN FINAL en Unifranz...", null, true);
        yield return Decir(yo, lore != null ? lore.l1 : "Una página más... y otra... y otra...", heroe);
        yield return Decir(yo, lore != null ? lore.l2 : "Descanso los ojos un ratito... solo un ratito...", heroe);
        MostrarDialogo(false);
        yield return Cabecear(heroe);
        durmiendo = true;
        StartCoroutine(Zetas(heroe));
        GestorAudio.Reproducir(SndExtra.Ronquido);
        yield return Esperar(0.6f);
        yield return Decir(null, lore != null ? lore.narr : "...y alguien se quedó dormido estudiando en la U.", null, true);
        GestorAudio.Reproducir(SndExtra.Ronquido);

        // 2) Se mete en un sueño: una nube crece desde su cabeza y todo se pone blanco.
        MostrarDialogo(false);
        Sprite nube = ArteExtra.Uno("Historia/nube_sueno");
        Image ns = FabricaUIArcade.CrearImagen(escenario, "Nube", nube, Color.white);
        ns.rectTransform.sizeDelta = new Vector2(120f, 80f) * 2f;
        Vector2 cabeza = heroe.basePos + new Vector2(90f, heroe.rt.sizeDelta.y * 0.9f);
        ns.rectTransform.anchoredPosition = cabeza;
        GestorAudio.Reproducir(SndExtra.PufSueno);
        float t = 0f;
        while (t < 1.6f && !saltar)
        {
            t += Time.unscaledDeltaTime;
            float e = Mathf.Lerp(1f, 14f, Mathf.SmoothStep(0f, 1f, t / 1.6f));
            ns.rectTransform.localScale = new Vector3(e, e, 1f);
            ns.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 40f);
            yield return null;
        }
        yield return Fundido(0f, 1f, 0.35f, Color.white);
        Destroy(ns.gameObject);
        Limpiar();
        LimpiarExtras();

        // 3) El sueño: la U de noche. Aparece Don Carlos, con un cono en la cabeza.
        Fondo("Tema/fondo_menu_noche", Color.white);
        heroe = Personaje(yo, "abajo", new Vector2(-360f, -380f));
        Actor carlos = Personaje("seguridad", "derecha", new Vector2(-1200f, -380f));
        PonerCono(carlos);
        yield return Fundido(1f, 0f, 0.6f, Color.white);
        yield return Caminar(carlos, new Vector2(-640f, -380f), 1.4f, "seguridad", "derecha");
        yield return Decir("seguridad", "¡Psst! ¡Oye, tú! Sí, tú... el que tiene cara de no haber dormido.", carlos);
        yield return Decir("seguridad", "Soy Don Carlos, guardia de esta universidad desde hace... ¡uf! Muchos semestres.", carlos);
        yield return Decir(yo, "¿Por qué tiene un cono en la cabeza?", heroe);
        yield return Decir("seguridad", "¿Esto? Es por SEGURIDAD. ¡Obvio! Ahora escucha bien, que no hay tiempo...", carlos);
        yield return Decir("seguridad", "Esta noche viene EL DEMOLEDOR.", carlos);

        // 4) Llega el demoledor, la pantalla tiembla; ruge y se va de un salto.
        MostrarDialogo(false);
        Actor dem = NuevoActor("Demoledor", ArteExtra.Serie("Menu/demoledor_caminar_", 6), new Vector2(1250f, -400f), 4f, true);
        dem.fps = 10f;
        GestorAudio.Reproducir(SndExtra.RugidoDemoledor);
        float d0 = Time.unscaledTime;
        while (Time.unscaledTime - d0 < 1.6f && !saltar)
        {
            float k = (Time.unscaledTime - d0) / 1.6f;
            dem.basePos = new Vector2(Mathf.Lerp(1250f, 560f, k), -400f + Mathf.Abs(Mathf.Sin(k * Mathf.PI * 5f)) * 18f);
            escenario.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 50f) * 8f, 0f);
            yield return null;
        }
        escenario.anchoredPosition = Vector2.zero;
        dem.cuadros = ArteExtra.Serie("Menu/demoledor_golpe_", 6);
        dem.fps = 10f;
        heroe.temblando = true;
        carlos.temblando = true;
        yield return Decir("demoledor", "¡GRRR! ¡ESTA UNIVERSIDAD VA A CAER... PISO POR PISO!", dem);
        heroe.temblando = false;
        carlos.temblando = false;
        MostrarDialogo(false);
        GestorAudio.Reproducir(SndExtra.RugidoDemoledor, 0.8f, 1.2f);
        d0 = Time.unscaledTime;
        Vector2 desdeDem = dem.basePos;
        while (Time.unscaledTime - d0 < 0.9f && !saltar)
        {
            float k = (Time.unscaledTime - d0) / 0.9f;
            dem.basePos = desdeDem + new Vector2(k * 300f, Mathf.Sin(k * Mathf.PI) * 420f + k * k * 600f);
            dem.rt.localRotation = Quaternion.Euler(0f, 0f, k * 360f);
            yield return null;
        }
        dem.img.enabled = false;
        yield return Decir("seguridad", "Dicen que nació del estrés de todos los exámenes juntos. ¡Y está FURIOSO!", carlos);
        yield return Decir("seguridad", "Va a subir rompiendo todo, piso por piso... " + (lore != null ? lore.blanco : "¡hasta la biblioteca!"), carlos);
        yield return Decir("seguridad", "Toma este martillo. Repara todo lo que él rompa y bloquea sus golpes.", carlos);
        // Prueba el martillo.
        Sprite[] golpe = Personajes.SerieFluida(ficha, "martillo");
        if (golpe != null) { heroe.cuadros = golpe; heroe.fps = golpe.Length >= 8 ? 16f : 8f; }
        GestorAudio.Reproducir(Snd.MenuConfirmar, 0.6f);
        yield return Decir(yo, lore != null ? lore.martillo : "¿Un martillo? ...¿Y esto cómo se usa?", heroe);
        Sprite[] quieto = Personajes.SerieFluida(ficha, "abajo");
        if (quieto != null) { heroe.cuadros = quieto; heroe.fps = 0f; }
        yield return Decir("seguridad", "Si lo cansas, se rinde y escapa al piso de arriba. ¡Persíguelo hasta la terraza!", carlos);
        yield return Decir("seguridad", "Y cada piso tiene sus sorpresas: charcos, cables, apagones... ¡Yo no limpio, eh!", carlos);
        yield return Decir(yo, "¿Y por qué tengo que ser yo?", heroe);
        yield return Decir("seguridad", lore != null ? lore.porQue : "Porque eres el único despierto... más o menos.", carlos);

        // 5) Título.
        MostrarDialogo(false);
        heroe.saltando = true;
        carlos.saltando = true;
        GestorAudio.Reproducir(Snd.MenuConfirmar);
        yield return TituloGrande("¡A REPARAR!", PaletaArcade.Amarillo, 2.2f);
        yield return Fundido(0f, 1f, 0.5f, Color.black);
    }

    // ================================================================== FINAL
    public IEnumerator Final()
    {
        GestorAudio.ForzarMusica("musica_final_sueno");
        string yo = null;
        velo.color = Color.black;

        // 1) ¡Por fin lo vencimos! Todos festejan frente a la U.
        Fondo("Tema/fondo_menu_noche", Color.white);
        Personajes.Ficha[] todos = Personajes.Todos;
        Actor heroe = Personaje(yo, "abajo", new Vector2(0f, -420f));
        heroe.rt.SetAsLastSibling();
        Actor carlos = null;
        int lado = 0;
        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i] == Personajes.Actual) continue;
            float x = (lado % 2 == 0 ? -1f : 1f) * (170f + (lado / 2) * 150f);
            float y = -330f + (lado / 2) * 12f;
            Actor a = Personaje(todos[i].id, "abajo", new Vector2(x, y));
            a.rt.SetSiblingIndex(1);
            a.saltando = true;
            if (todos[i].id == "seguridad") { carlos = a; PonerCono(a); }
            lado++;
        }
        yield return Fundido(1f, 0f, 0.8f, Color.black);
        Confeti(80);
        GestorAudio.Reproducir(SndExtra.Logro);
        heroe.saltando = true;
        yield return TituloGrande("¡POR FIN LO VENCIMOS!", PaletaArcade.Amarillo, 2.6f);
        textoGrande.text = "";
        heroe.saltando = false;
        yield return Decir(yo, "¡POR FIN LO VENCIMOS! ¡La U sigue en pie!", heroe);
        yield return Decir("seguridad", "¡Lo lograste! Sabía que el martillo estaba en buenas manos.", carlos);
        yield return Decir("decana", "Toda la universidad te lo agradece. Eres un orgullo de Unifranz.", null);
        yield return Decir("docente", "Y por eso... ¡tienes un 100 en Reparación de Edificios!", null);
        yield return Decir("mantenimiento", "¡Y yo ya no tengo que barrer escombros! ¡HURRA!", null);
        Confeti(60);

        // 2) Algo raro: todo empieza a temblar y suena... ¿un despertador?
        yield return Decir(yo, "Oigan... ¿escuchan eso?", heroe);
        MostrarDialogo(false);
        GestorAudio.Reproducir(SndExtra.Despertador);
        float t = 0f;
        while (t < 1.6f && !saltar)
        {
            t += Time.unscaledDeltaTime;
            escenario.anchoredPosition = new Vector2(Mathf.Sin(t * 45f) * 10f * t, 0f);
            escenario.localScale = Vector3.one * (1f + t * 0.06f);
            yield return null;
        }
        GestorAudio.Reproducir(SndExtra.PufSueno);
        yield return Fundido(0f, 1f, 0.4f, Color.white);
        escenario.anchoredPosition = Vector2.zero;
        escenario.localScale = Vector3.one;
        Limpiar();

        // 3) De mañana, donde se quedó dormido (la cocina, el laboratorio...): ¡fue un sueño!
        GestorAudio.ForzarMusica("");
        Personajes.Ficha ficha = Personajes.Actual;
        Lore lore = LoreDe(ficha != null ? ficha.id : "");
        if (lore != null && ArteExtra.Uno("Historia/carrera_" + lore.id + "_dia") != null)
        {
            Fondo("Historia/carrera_" + lore.id + "_dia", Color.white);
            Actor dormido = Personaje(yo, "abajo", new Vector2(0f, -400f));
            Vector2 base0 = dormido.basePos;
            dormido.rt.localRotation = Quaternion.Euler(0f, 0f, -11f);
            dormido.basePos = base0 + new Vector2(14f, -70f);
            Frente("Historia/mesa_" + lore.id);
            EscenaDeCarrera = true;
            Actor despertador = NuevoActor("Despertador", ArteExtra.Serie("Historia/despertador_", 2), Ui(new Vector2(352f, 196f)), 4f);
            despertador.fps = 14f;
            despertador.temblando = true;
            despertador.quieto = true;
            durmiendo = true;
            StartCoroutine(Zetas(dormido));
            yield return Fundido(1f, 0f, 0.5f, Color.white);
            GestorAudio.Reproducir(SndExtra.Despertador);
            yield return Esperar(1.2f);
            durmiendo = false;
            // Se despierta de un salto.
            t = 0f;
            Vector2 caido = dormido.basePos;
            while (t < 0.35f && !saltar)
            {
                t += Time.unscaledDeltaTime;
                float k = t / 0.35f;
                dormido.rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-11f, 0f, k));
                dormido.basePos = Vector2.Lerp(caido, base0, k) + new Vector2(0f, Mathf.Sin(k * Mathf.PI) * 90f);
                yield return null;
            }
            dormido.rt.localRotation = Quaternion.identity;
            dormido.basePos = base0;
            despertador.temblando = false;
            despertador.fps = 0f;
            Sprite[] susto = Personajes.SerieFluida(ficha, "abajo_susto");
            if (susto == null) susto = Personajes.Serie(ficha, "abajo_susto");
            if (susto != null) { dormido.cuadros = susto; dormido.fps = 8f; }
            yield return Decir(yo, "¡¿QUÉ?! ¿Fue... un SUEÑO? " + lore.despierta, dormido);
            yield return Decir(null, "7:15 de la mañana. El día del examen final.", null, true);
            dormido.temblando = true;
            yield return Decir(yo, lore.examen, dormido);
            dormido.temblando = false;
            yield return Fundido(0f, 1f, 0.4f, Color.black);
            Limpiar();
            LimpiarExtras();
        }
        else
        {
            Fondo("Historia/fondo_cuarto", Color.white);
            Actor enCama = Personaje(yo, "abajo", new Vector2(-360f, -170f));
            enCama.rt.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Actor reloj = NuevoActor("Despertador", ArteExtra.Serie("Historia/despertador_", 2), new Vector2(164f, -140f), 4f);
            reloj.fps = 14f;
            reloj.temblando = true;
            yield return Fundido(1f, 0f, 0.5f, Color.white);
            GestorAudio.Reproducir(SndExtra.Despertador);
            yield return Esperar(1f);
            // Se despierta de golpe.
            t = 0f;
            while (t < 0.3f && !saltar) { t += Time.unscaledDeltaTime; enCama.rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(90f, 0f, t / 0.3f)); yield return null; }
            enCama.rt.localRotation = Quaternion.identity;
            enCama.basePos = new Vector2(-360f, -230f);
            reloj.temblando = false;
            reloj.fps = 0f;
            yield return Decir(yo, "¡¿QUÉ?! ¿Fue... un SUEÑO?", enCama);
            yield return Decir(null, "7:15 de la mañana. El día del examen final.", null, true);
            yield return Decir(yo, "¡AY, NO! ¡Voy a llegar tarde al examen!", enCama);
            yield return Fundido(0f, 1f, 0.4f, Color.black);
            Limpiar();
        }

        // 4) La U de día. Don Carlos en la puerta... y un casco amarillo en el arbusto.
        GestorAudio.ForzarMusica("musica_historia");
        Fondo("Historia/fondo_u_dia", Color.white);
        Actor carlos2 = Personaje("seguridad", "abajo", new Vector2(420f, -360f));
        PonerCono(carlos2);
        Actor yo2 = Personaje(yo, "derecha", new Vector2(-1100f, -400f));
        yield return Fundido(1f, 0f, 0.5f, Color.black);
        yield return Caminar(yo2, new Vector2(80f, -400f), 1.6f, yo, "derecha");
        Sprite[] lado1 = Personajes.SerieFluida(Personajes.Actual, "derecha");
        if (lado1 != null) { yo2.cuadros = lado1; yo2.fps = 0f; }
        yield return Decir("seguridad", "Buenos días... ¿Dormiste bien, REPARADOR?", carlos2);
        yield return Decir(yo, "¿C-cómo sabe usted que...?", yo2);
        yield return Decir("seguridad", "Ve, ve, que ya empieza tu examen. ¡Suerte! *le guiña un ojo*", carlos2);
        MostrarDialogo(false);
        yield return Caminar(yo2, new Vector2(1150f, -400f), 1.4f, yo, "derecha");

        // El arbusto se mueve...
        GestorAudio.Reproducir(SndExtra.GrunidoCerca);
        float y0 = Time.unscaledTime;
        while (Time.unscaledTime - y0 < 1.2f && !saltar)
        {
            escenario.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 40f) * 3f, 0f);
            yield return null;
        }
        escenario.anchoredPosition = Vector2.zero;
        yield return TituloGrande("¿FIN...?", new Color(1f, 0.85f, 0.3f), 2.5f);
        textoGrande.text = "";
        yield return Decir(null, "Gracias por jugar REPARALOFRANZ. ¡Mucha suerte en tus exámenes!", null, true);
        yield return Fundido(0f, 1f, 0.6f, Color.black);
    }
}
