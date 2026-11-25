using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// El paso de un piso a otro en ascensor:
///   1) EntrarAlAscensor: después del festejo, los estudiantes caminan dentro del ascensor, se dan vuelta, las puertas
///      se cierran y suena el "¡ding!".
///   2) Viaje: pantalla del interior del ascensor (espejo, pasamanos, panel de botones), las puertas se abren, el visor
///      cuenta los pisos mientras la cabina vibra con su musiquita, "¡ding!" y las puertas se cierran: hace de pantalla
///      de carga antes de que aparezca el piso siguiente.
/// Todo en tiempo real (funciona con el juego en pausa).
/// </summary>
public static class ViajeAscensor
{
    // ================================================================== 1) entrar al ascensor
    public static IEnumerator EntrarAlAscensor(PisoCenital piso)
    {
        if (piso == null || piso.Mapa == null || piso.Mapa.ascensor == null) yield break;
        AscensorCenital asc = piso.Mapa.ascensor;
        Collider2D col = asc.GetComponent<Collider2D>();
        Bounds caja = col != null ? col.bounds : new Bounds(asc.transform.position, new Vector3(1f, 1f, 0f));
        if (!asc.Abierto) asc.Abrir(piso.Definicion);

        // Cada estudiante camina hasta adentro (si son dos, uno al lado del otro).
        List<JugadorCenital> jugadores = new List<JugadorCenital>();
        foreach (JugadorCenital j in JugadorCenital.Todos) if (j != null && j.gameObject.activeInHierarchy) jugadores.Add(j);
        if (jugadores.Count == 0) yield break;
        // Lejos del ascensor (por ejemplo en la terraza): no hay paseo.
        foreach (JugadorCenital j in jugadores) if (Vector2.Distance(j.transform.position, caja.center) > 4f) yield break;

        List<Vector2> desde = new List<Vector2>(), hasta = new List<Vector2>();
        List<SpriteRenderer> dibujos = new List<SpriteRenderer>();
        for (int i = 0; i < jugadores.Count; i++)
        {
            JugadorCenital j = jugadores[i];
            Rigidbody2D rb = j.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
            desde.Add(j.transform.position);
            float dx = jugadores.Count > 1 ? (i == 0 ? -0.22f : 0.22f) * caja.size.x : 0f;
            hasta.Add(new Vector2(caja.center.x + dx, caja.center.y - caja.extents.y * 0.15f));
            dibujos.Add(j.GetComponentInChildren<SpriteRenderer>());
        }

        Vector2 foco = caja.center;
        float inicio = Time.unscaledTime;
        const float caminar = 0.9f;
        while (Time.unscaledTime - inicio < caminar)
        {
            float k = Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - inicio) / caminar);
            for (int i = 0; i < jugadores.Count; i++)
            {
                if (jugadores[i] == null) continue;
                jugadores[i].transform.position = Vector2.Lerp(desde[i], hasta[i], k);
                Sprite[] serie = SerieHacia(jugadores[i], hasta[i] - desde[i]);
                if (dibujos[i] != null && serie != null && serie.Length > 0)
                {
                    dibujos[i].sprite = serie[(int)((Time.unscaledTime - inicio) * 10f * Personajes.FactorCuadros(serie)) % serie.Length];
                    dibujos[i].flipX = (hasta[i] - desde[i]).x < -0.05f && Mathf.Abs((hasta[i] - desde[i]).x) > Mathf.Abs((hasta[i] - desde[i]).y);
                }
            }
            CamaraPisoCenital.Enfocar(foco, Mathf.Lerp(0.85f, 0.6f, k));
            yield return null;
        }

        // Se dan vuelta hacia la cámara y saludan (un saltito).
        for (int i = 0; i < jugadores.Count; i++)
        {
            if (dibujos[i] == null) continue;
            Sprite[] abajo = jugadores[i].animaciones != null ? jugadores[i].animaciones.caminarAbajo : null;
            if (abajo != null && abajo.Length > 0) { dibujos[i].sprite = abajo[0]; dibujos[i].flipX = false; }
        }
        float saludo = Time.unscaledTime;
        while (Time.unscaledTime - saludo < 0.45f)
        {
            float k = (Time.unscaledTime - saludo) / 0.45f;
            for (int i = 0; i < jugadores.Count; i++)
                if (jugadores[i] != null) jugadores[i].transform.position = hasta[i] + Vector2.up * Mathf.Sin(k * Mathf.PI) * 0.18f;
            CamaraPisoCenital.Enfocar(foco, 0.6f);
            yield return null;
        }

        // Las puertas se cierran desde los costados.
        int orden = 0;
        foreach (SpriteRenderer d in dibujos) if (d != null) orden = Mathf.Max(orden, d.sortingOrder);
        yield return CerrarPuertas(piso.Mapa.transform, caja, orden + 6, foco);
        for (int i = 0; i < jugadores.Count; i++) if (dibujos[i] != null) dibujos[i].enabled = false;
        GestorAudio.Reproducir(SndExtra.Ding, 0.9f);
        CamaraPisoCenital.Temblar(0.05f, 0.25f);
        float fin = Time.unscaledTime;
        while (Time.unscaledTime - fin < 0.6f) { CamaraPisoCenital.Enfocar(foco, 0.6f); yield return null; }
        CamaraPisoCenital.SoltarEnfoque();
    }

    /// <summary>Las dos hojas de la puerta se cierran sobre el ascensor (cuelgan del mapa: cada equipo las dibuja).</summary>
    private static IEnumerator CerrarPuertas(Transform padre, Bounds caja, int orden, Vector2 foco)
    {
        Sprite puerta = ArteExtra.Uno("Ascensor/puerta");
        SpriteRenderer izq = Provisional.Crear(padre, "PuertaAscensorIzq", puerta != null ? puerta : Provisional.Cuadrado, puerta != null ? Color.white : new Color(0.7f, 0.72f, 0.78f), orden);
        SpriteRenderer der = Provisional.Crear(padre, "PuertaAscensorDer", puerta != null ? puerta : Provisional.Cuadrado, puerta != null ? Color.white : new Color(0.7f, 0.72f, 0.78f), orden);
        der.flipX = true;
        float anchoMitad = caja.size.x * 0.5f, alto = caja.size.y + 0.6f;
        Vector2 tam = izq.sprite.bounds.size;
        Vector3 escala = new Vector3(anchoMitad / Mathf.Max(0.01f, tam.x), alto / Mathf.Max(0.01f, tam.y), 1f);
        izq.transform.localScale = escala;
        der.transform.localScale = escala;
        float yCentro = caja.center.y + 0.3f;
        float cierre = Time.unscaledTime;
        GestorAudio.Reproducir(Snd.AscensorAbre, 0.8f, 0.8f);
        while (Time.unscaledTime - cierre < 0.55f)
        {
            float k = Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - cierre) / 0.55f);
            if (izq != null) izq.transform.position = new Vector2(Mathf.Lerp(caja.min.x - anchoMitad * 0.5f, caja.center.x - anchoMitad * 0.5f, k), yCentro);
            if (der != null) der.transform.position = new Vector2(Mathf.Lerp(caja.max.x + anchoMitad * 0.5f, caja.center.x + anchoMitad * 0.5f, k), yCentro);
            CamaraPisoCenital.Enfocar(foco, 0.6f);
            yield return null;
        }
    }

    /// <summary>
    /// Multijugador, en el invitado: el anfitrión avisó que los estudiantes entran al ascensor. Los muñecos llegan por el
    /// espejo; aquí se abre el ascensor, la cámara se acerca y, cuando ya están adentro, se cierran las puertas.
    /// </summary>
    public static IEnumerator PuertasRemotas(PisoCenital piso)
    {
        if (piso == null || piso.Mapa == null || piso.Mapa.ascensor == null) yield break;
        AscensorCenital asc = piso.Mapa.ascensor;
        Collider2D col = asc.GetComponent<Collider2D>();
        Bounds caja = col != null ? col.bounds : new Bounds(asc.transform.position, new Vector3(1f, 1f, 0f));
        if (!asc.Abierto) asc.Abrir(piso.Definicion);
        Vector2 foco = caja.center;
        float inicio = Time.unscaledTime;
        while (Time.unscaledTime - inicio < 1.35f)   // lo que tardan en caminar adentro y saludar en el anfitrión
        {
            CamaraPisoCenital.Enfocar(foco, Mathf.Lerp(0.85f, 0.6f, (Time.unscaledTime - inicio) / 1.35f));
            yield return null;
        }
        yield return CerrarPuertas(piso.Mapa.transform, caja, Provisional.OrdenY(caja.min.y - 1f) + 30, foco);
        float fin = Time.unscaledTime;
        while (Time.unscaledTime - fin < 0.6f) { CamaraPisoCenital.Enfocar(foco, 0.6f); yield return null; }
        CamaraPisoCenital.SoltarEnfoque();
    }

    private static Sprite[] SerieHacia(JugadorCenital j, Vector2 dir)
    {
        AnimacionesCenital a = j.animaciones;
        if (a == null) return null;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) return a.caminarDerecha;
        return dir.y > 0f ? a.caminarArriba : a.caminarAbajo;
    }

    // ================================================================== 2) el viaje (pantalla de carga)
    public static IEnumerator Viaje(int desde, int hasta, string area)
    {
        Canvas canvas = FabricaUIArcade.CrearCanvas("ViajeAscensor", 950);
        Object.DontDestroyOnLoad(canvas.gameObject);
        Transform raiz = canvas.transform;

        Image negro = FabricaUIArcade.CrearImagen(raiz, "Negro", null, Color.black);
        FabricaUIArcade.Estirar(negro.rectTransform);

        // La cabina (todo lo de adentro vibra junto).
        RectTransform cabina = FabricaUIArcade.CrearObjeto("Cabina", raiz);
        FabricaUIArcade.Estirar(cabina);
        Sprite spInterior = ArteExtra.Uno("Ascensor/interior");
        Image interior = FabricaUIArcade.CrearImagen(cabina, "Interior", spInterior, spInterior != null ? Color.white : new Color(0.55f, 0.58f, 0.64f));
        FabricaUIArcade.Estirar(interior.rectTransform);
        interior.type = Image.Type.Simple;

        // Visor del número (sobre el recuadro del arte: x 380-470, y 14-44 de 480 × 270).
        RectTransform visor = FabricaUIArcade.CrearObjeto("Visor", cabina);
        visor.anchorMin = new Vector2(380f / 480f, 1f - 44f / 270f);
        visor.anchorMax = new Vector2(470f / 480f, 1f - 14f / 270f);
        visor.offsetMin = Vector2.zero;
        visor.offsetMax = Vector2.zero;
        Text numero = FabricaUIArcade.CrearTexto(visor, "Numero", Etiqueta(desde), 64, new Color(1f, 0.55f, 0.15f), TextAnchor.MiddleCenter, 0f, 0f);
        FabricaUIArcade.Estirar(numero.rectTransform);
        numero.resizeTextMaxSize = 70;
        numero.resizeTextMinSize = 10;
        numero.resizeTextForBestFit = true;

        // Los estudiantes, parados en el piso de la cabina.
        List<Image> gente = new List<Image>();
        List<Sprite[]> series = new List<Sprite[]>();
        List<Personajes.Ficha> fichas = new List<Personajes.Ficha>();
        foreach (JugadorCenital j in JugadorCenital.Todos) if (j != null && j.Ficha != null && !fichas.Contains(j.Ficha)) fichas.Add(j.Ficha);
        if (fichas.Count == 0) fichas.Add(Personajes.Actual);
        for (int i = 0; i < fichas.Count; i++)
        {
            Sprite[] s = Personajes.SerieFluida(fichas[i], "abajo");
            Image im = FabricaUIArcade.CrearImagen(cabina, "Estudiante", s != null ? s[0] : null, s != null ? Color.white : new Color(0.3f, 0.55f, 1f));
            im.preserveAspect = true;
            float x = fichas.Count > 1 ? (i == 0 ? -150f : 150f) : 0f;
            RectTransform rt = im.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.06f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(x - 120f, 0f);
            rt.sizeDelta = new Vector2(360f, 520f);
            gente.Add(im);
            series.Add(s);
        }

        // Cartel de abajo: "SUBIENDO AL PISO 3 · AULAS".
        Text cartel = FabricaUIArcade.CrearTexto(raiz, "Cartel", "SUBIENDO A " + Etiqueta(hasta, true) + (string.IsNullOrEmpty(area) ? "" : "\n" + area.ToUpperInvariant()),
            44, Color.white, TextAnchor.MiddleCenter, 5f, 5f);
        RectTransform rc = cartel.rectTransform;
        rc.anchorMin = new Vector2(0f, 0.8f);
        rc.anchorMax = new Vector2(0.75f, 1f);
        rc.offsetMin = Vector2.zero;
        rc.offsetMax = Vector2.zero;

        // Puertas grandes (cerradas al empezar).
        Sprite spPuerta = ArteExtra.Uno("Ascensor/puerta_grande");
        Color cPuerta = spPuerta != null ? Color.white : new Color(0.75f, 0.78f, 0.84f);
        Image pi = FabricaUIArcade.CrearImagen(raiz, "PuertaIzq", spPuerta, cPuerta);
        Image pd = FabricaUIArcade.CrearImagen(raiz, "PuertaDer", spPuerta, cPuerta);
        pi.type = Image.Type.Simple;
        pd.type = Image.Type.Simple;
        pd.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        Puertas(pi.rectTransform, pd.rectTransform, 0f);

        Image velo = FabricaUIArcade.CrearImagen(raiz, "Velo", null, Color.black);
        FabricaUIArcade.Estirar(velo.rectTransform);

        string musicaAntes = null;
        bool hayMusica = GestorAudio.ExisteMusica("musica_ascensor");
        if (hayMusica) GestorAudio.ForzarMusica("musica_ascensor");

        // Aparece (fundido desde negro) y se abren las puertas.
        float t0 = Time.unscaledTime;
        while (Time.unscaledTime - t0 < 0.35f) { velo.color = new Color(0f, 0f, 0f, 1f - (Time.unscaledTime - t0) / 0.35f); yield return null; }
        velo.color = Color.clear;
        GestorAudio.Reproducir(Snd.AscensorAbre, 0.8f, 1.1f);
        t0 = Time.unscaledTime;
        while (Time.unscaledTime - t0 < 0.5f) { Puertas(pi.rectTransform, pd.rectTransform, Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - t0) / 0.5f)); yield return null; }
        Puertas(pi.rectTransform, pd.rectTransform, 1f);

        // Sube: vibra, el visor cuenta los pisos y los estudiantes se balancean.
        int pasos = Mathf.Max(1, Mathf.Abs(hasta - desde));
        const float viaje = 2.6f;
        t0 = Time.unscaledTime;
        int mostrado = desde;
        while (Time.unscaledTime - t0 < viaje)
        {
            float t = Time.unscaledTime - t0, k = t / viaje;
            cabina.anchoredPosition = new Vector2(Mathf.Sin(t * 41f) * 3f, Mathf.Sin(t * 29f) * 4f + Mathf.Sin(k * Mathf.PI) * 10f);
            int n = desde + Mathf.Clamp(Mathf.FloorToInt(k * 1.35f * pasos), 0, pasos) * (hasta >= desde ? 1 : -1);
            if (n != mostrado) { mostrado = n; numero.text = Etiqueta(n); GestorAudio.Reproducir(SndExtra.Ding, 0.35f, 1.4f); }
            // El número parpadea mientras sube.
            numero.color = new Color(1f, 0.55f, 0.15f, mostrado == hasta ? 1f : (Mathf.Repeat(t * 3f, 1f) < 0.75f ? 1f : 0.35f));
            for (int i = 0; i < gente.Count; i++)
            {
                RectTransform rt = gente[i].rectTransform;
                float x = (gente.Count > 1 ? (i == 0 ? -150f : 150f) : 0f) - 120f;
                rt.anchoredPosition = new Vector2(x, Mathf.Abs(Mathf.Sin(t * 5f + i)) * 14f);
                if (series[i] != null && series[i].Length > 0 && k > 0.6f)
                    gente[i].sprite = series[i][(int)(t * 8f * Personajes.FactorCuadros(series[i])) % series[i].Length];   // bailan con la musiquita
            }
            yield return null;
        }
        cabina.anchoredPosition = Vector2.zero;
        numero.text = Etiqueta(hasta);
        numero.color = new Color(1f, 0.55f, 0.15f);
        GestorAudio.Reproducir(SndExtra.Ding, 1f);

        // Las puertas se cierran y fundido a negro (el piso siguiente se carga detrás).
        yield return EsperarReal(0.35f);
        GestorAudio.Reproducir(Snd.AscensorAbre, 0.8f, 0.9f);
        t0 = Time.unscaledTime;
        while (Time.unscaledTime - t0 < 0.45f) { Puertas(pi.rectTransform, pd.rectTransform, 1f - Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - t0) / 0.45f)); yield return null; }
        Puertas(pi.rectTransform, pd.rectTransform, 0f);
        t0 = Time.unscaledTime;
        while (Time.unscaledTime - t0 < 0.3f) { velo.color = new Color(0f, 0f, 0f, (Time.unscaledTime - t0) / 0.3f); yield return null; }
        velo.color = Color.black;
        if (hayMusica) GestorAudio.ForzarMusica(musicaAntes);

        // Se queda en negro hasta que cargue la escena nueva y después se desvanece solo.
        canvas.gameObject.AddComponent<DesvanecerAlCargar>().velo = velo;
    }

    private static IEnumerator EsperarReal(float s)
    {
        float t = Time.unscaledTime;
        while (Time.unscaledTime - t < s) yield return null;
    }

    /// <summary>abierto = 0: cerradas (cada una tapa media pantalla); 1: escondidas a los costados.</summary>
    private static void Puertas(RectTransform izq, RectTransform der, float abierto)
    {
        float d = 0.5f * abierto;
        izq.anchorMin = new Vector2(0f - d, 0f);
        izq.anchorMax = new Vector2(0.5f - d, 1f);
        izq.offsetMin = Vector2.zero;
        izq.offsetMax = Vector2.zero;
        der.anchorMin = new Vector2(0.5f + d, 0f);
        der.anchorMax = new Vector2(1f + d, 1f);
        der.offsetMin = Vector2.zero;
        der.offsetMax = Vector2.zero;
    }

    private static string Etiqueta(int piso, bool larga = false)
    {
        if (piso <= 0) return larga ? "LA PLANTA BAJA" : "PB";
        if (piso >= 7) return larga ? "LA TERRAZA" : "T";
        return larga ? "EL PISO " + piso : piso.ToString();
    }

    /// <summary>Negro sobre la escena nueva unos instantes y luego se desvanece (sobrevive a la carga).</summary>
    private class DesvanecerAlCargar : MonoBehaviour
    {
        public Image velo;
        private float creado, desdeCarga = -1f;

        private void Awake()
        {
            creado = Time.unscaledTime;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += AlCargar;
        }

        private void OnDestroy() { UnityEngine.SceneManagement.SceneManager.sceneLoaded -= AlCargar; }

        private void AlCargar(UnityEngine.SceneManagement.Scene escena, UnityEngine.SceneManagement.LoadSceneMode modo)
        {
            if (desdeCarga < 0f) desdeCarga = Time.unscaledTime;
        }

        private void Start()
        {
            // Oculta todo menos el velo.
            foreach (Transform t in transform) if (velo == null || t != velo.transform) t.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (desdeCarga < 0f)
            {
                // Seguro: si en 8 s no cargó nada, se quita igual.
                if (Time.unscaledTime - creado > 8f) desdeCarga = Time.unscaledTime;
                return;
            }
            float k = (Time.unscaledTime - desdeCarga - 0.15f) / 0.4f;
            if (velo != null) velo.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(k));
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
