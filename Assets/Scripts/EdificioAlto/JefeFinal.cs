using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// El demoledor como jefe final en la fachada, con 3 fases (una por sección del edificio):
///   Fase 1  el demoledor de siempre.
///   Fase 2  "¡SE ENOJÓ!": un poco más grande y anaranjado; te lanza escombros en arco (una marca roja avisa dónde caen).
///   Fase 3  "¡FORMA FINAL!": se transforma en el DEMOLEDOR SUPREMO (grande, rojo, echando vapor), suena la música del jefe,
///           lanza escombros más seguido y rompe una fila entera de ventanas con una onda de choque.
/// Al ganar se desinfla y vuelve a su tamaño antes de que los estudiantes lo lancen del edificio.
/// InstaladorExtras lo agrega al nivel final.
/// </summary>
public class JefeFinal : MonoBehaviour
{
    private EdificioAlto edificio;
    private DemoledorAutomatico dem;
    private Muneco muneco;
    private TerremotoFachada terremoto;
    private UltiFachada ulti;
    private GestorJuego gestor;
    private SpriteRenderer dibujo;
    private Vector3 escalaBase = Vector3.one;

    private int fase = 1;
    private float escalaObjetivo = 1f, escalaActual = 1f;
    private Color tinteObjetivo = Color.white;
    private bool ocupado, salvado;
    private float proximoLanzamiento, proximaFila, proximoVapor;
    private int filaAviso = -1;
    private Color[] coloresAviso;

    // Interfaz.
    private Canvas lienzo;
    private Text textoNombre, textoGrande, textoSub;
    private Image[] pips;
    private float finTextoGrande;

    private IEnumerator Start()
    {
        while (EdificioAlto.Instancia == null) yield return null;
        edificio = EdificioAlto.Instancia;
        while ((dem = FindAnyObjectByType<DemoledorAutomatico>()) == null) yield return null;
        muneco = FindAnyObjectByType<Muneco>();
        terremoto = FindAnyObjectByType<TerremotoFachada>();
        ulti = FindAnyObjectByType<UltiFachada>();
        gestor = FindAnyObjectByType<GestorJuego>();
        dibujo = dem.GetComponentInChildren<SpriteRenderer>();
        escalaBase = dem.transform.localScale;
        edificio.SeccionCompletada += AlCompletarSeccion;
        edificio.Salvado += AlSalvar;
        CrearInterfaz();
        ActualizarPips();
    }

    private void OnDestroy()
    {
        if (edificio != null)
        {
            edificio.SeccionCompletada -= AlCompletarSeccion;
            edificio.Salvado -= AlSalvar;
        }
        if (lienzo != null) Destroy(lienzo.gameObject);
    }

    private bool Ocupado
    {
        get
        {
            return ocupado || salvado || edificio == null || edificio.Terminado || (terremoto != null && terremoto.EnCurso) ||
                   (ulti != null && ulti.EnCurso) || (gestor != null && (gestor.PartidaTerminada || gestor.EstaEnPausa));
        }
    }

    // ------------------------------------------------------------ fases
    private void AlCompletarSeccion(int nueva)
    {
        if (salvado || edificio == null || nueva >= edificio.secciones) return;
        if (nueva == 1) StartCoroutine(Fase2());
        else if (nueva >= 2) StartCoroutine(Fase3());
    }

    private IEnumerator Fase2()
    {
        fase = 2;
        ActualizarPips();
        yield return new WaitForSeconds(1.5f);   // termina de subir a la cornisa nueva
        escalaObjetivo = 1.12f;
        tinteObjetivo = new Color(1f, 0.82f, 0.7f);
        GestorAudio.Reproducir(SndExtra.RugidoDemoledor);
        CamaraSeguimiento.Temblar(0.12f, 0.6f);
        Anunciar("¡SE ENOJÓ!", "¡AHORA TE LANZA ESCOMBROS!", new Color(1f, 0.6f, 0.25f));
        proximoLanzamiento = Time.time + 3f;
    }

    private IEnumerator Fase3()
    {
        fase = 3;
        ActualizarPips();
        yield return new WaitForSeconds(1.5f);
        while (Ocupado && !salvado) yield return null;
        if (salvado) yield break;

        // Transformación: se detiene, tiembla todo, crece parpadeando entre blanco y rojo.
        ocupado = true;
        dem.Pausar();
        GestorAudio.Reproducir(SndExtra.JefeTransforma);
        GestorAudio.ForzarMusica("musica_jefe_final");
        Anunciar("¡FORMA FINAL!", "DEMOLEDOR SUPREMO", new Color(1f, 0.3f, 0.25f));
        float t = 0f;
        float desde = escalaActual;
        while (t < 2.2f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 2.2f);
            escalaActual = Mathf.Lerp(desde, 1.35f, k) + Mathf.Sin(t * 40f) * 0.04f * (1f - k);
            if (dibujo != null) dibujo.color = Mathf.Repeat(t * 8f, 1f) < 0.5f ? Color.white : new Color(1f, 0.35f, 0.3f);
            if (Time.frameCount % 6 == 0) CamaraSeguimiento.Temblar(0.1f, 0.15f);
            if (Time.time >= proximoVapor) Vapor(4);
            AplicarEscala();
            yield return null;
        }
        escalaObjetivo = 1.35f;
        tinteObjetivo = new Color(1f, 0.55f, 0.5f);
        // Vuelve a su cornisa por si algo lo movió y sigue atacando.
        Vector3 p = dem.transform.position;
        dem.transform.position = new Vector3(p.x, edificio.AlturaCornisa(Mathf.Min(edificio.SeccionActual, edificio.secciones - 1)), p.z);
        if (!TerremotoOUlti()) dem.Reanudar();
        ocupado = false;
        proximoLanzamiento = Time.time + 2.5f;
        proximaFila = Time.time + 7f;
    }

    private bool TerremotoOUlti() { return (terremoto != null && terremoto.EnCurso) || (ulti != null && ulti.EnCurso); }

    private void AlSalvar()
    {
        if (salvado) return;
        salvado = true;
        StopAllCoroutines();
        RestaurarFila();
        GestorAudio.ForzarMusica("");
        if (fase >= 2) StartCoroutine(Desinflar());
        if (lienzo != null) lienzo.gameObject.SetActive(false);
    }

    private void RestaurarFila()
    {
        if (filaAviso < 0 || coloresAviso == null || edificio == null) return;
        for (int c = 0; c < edificio.columnas && c < coloresAviso.Length; c++)
        {
            VentanaEdificio v = edificio.VentanaEn(filaAviso, c);
            if (v != null && v.dibujo != null) v.dibujo.color = coloresAviso[c];
        }
        filaAviso = -1;
    }

    private IEnumerator Desinflar()
    {
        GestorAudio.Reproducir(SndExtra.Desinflar);
        float t = 0f, desde = escalaActual;
        while (t < 1.2f)
        {
            t += Time.deltaTime;
            float k = t / 1.2f;
            escalaActual = Mathf.Lerp(desde, 1f, k) + Mathf.Sin(t * 30f) * 0.05f * (1f - k);
            if (Time.frameCount % 3 == 0) Vapor(1);
            AplicarEscala();
            yield return null;
        }
        escalaActual = escalaObjetivo = 1f;
        tinteObjetivo = Color.white;
        AplicarEscala();
        if (dibujo != null) dibujo.color = Color.white;
    }

    // ------------------------------------------------------------ ciclo
    private void Update()
    {
        if (edificio == null || dem == null) return;

        // Reinicio del nivel (R o reintentar): vuelve a la fase 1.
        if (!salvado && edificio.SeccionActual == 0 && fase > 1)
        {
            StopAllCoroutines();
            RestaurarFila();
            ocupado = false;
            fase = 1;
            escalaObjetivo = 1f;
            tinteObjetivo = Color.white;
            GestorAudio.ForzarMusica("");
            ActualizarPips();
        }

        if (!ocupado && !salvado)
        {
            escalaActual = Mathf.MoveTowards(escalaActual, escalaObjetivo, Time.deltaTime * 0.6f);
            AplicarEscala();
            if (dibujo != null)
            {
                Color c = tinteObjetivo;
                if (fase == 3) c = Color.Lerp(tinteObjetivo, Color.white, 0.25f + 0.25f * Mathf.Sin(Time.time * 5f));
                dibujo.color = Color.Lerp(dibujo.color, c, 1f - Mathf.Exp(-5f * Time.deltaTime));
            }
            if (fase == 3 && Time.time >= proximoVapor) Vapor(1);
        }

        if (Ocupado || fase < 2) { AnimarTexto(); return; }
        if (Time.time >= proximoLanzamiento)
        {
            proximoLanzamiento = Time.time + (fase == 3 ? Random.Range(4.5f, 6f) : Random.Range(7f, 9f));
            StartCoroutine(LanzarEscombro());
        }
        if (fase == 3 && Time.time >= proximaFila)
        {
            proximaFila = Time.time + Random.Range(13f, 16f);
            StartCoroutine(RomperFila());
        }
        AnimarTexto();
    }

    private void AplicarEscala()
    {
        if (dem != null) dem.transform.localScale = escalaBase * escalaActual;
    }

    private void Vapor(int n)
    {
        proximoVapor = Time.time + 0.18f;
        Sprite[] vapor = ArteExtra.Serie("Mejoras/vapor_", 4);
        if (vapor == null || dem == null) return;
        Vector2 cabeza = (Vector2)dem.transform.position + Vector2.up * (1.05f * escalaActual);
        for (int i = 0; i < n; i++)
            ParticulaSimple.Crear(vapor, cabeza + new Vector2(Random.Range(-0.25f, 0.25f), 0f), new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(0.6f, 1.1f)),
                0.8f, 12, new Color(1f, 0.85f, 0.85f, 0.85f), 0.5f, 0.9f, 0f);
    }

    // ------------------------------------------------------------ ataques
    /// <summary>Lanza un escombro en arco hacia donde está el muñeco; una marca roja avisa dónde va a caer.</summary>
    private IEnumerator LanzarEscombro()
    {
        if (muneco == null) muneco = FindAnyObjectByType<Muneco>();
        if (muneco == null || dem == null) yield break;
        Vector2 desde = (Vector2)dem.transform.position + Vector2.up * (0.9f * escalaActual);
        Vector2 hacia = muneco.CentroCuerpo + new Vector2(Random.Range(-0.35f, 0.35f), -0.2f);
        GestorAudio.Reproducir(SndExtra.JefeLanza);
        if (dem != null) dem.GolpeAlTecho(0.12f);

        SpriteRenderer marca = Provisional.Crear(null, "MarcaJefe", Provisional.Aro, new Color(1f, 0.2f, 0.15f, 0.8f), 40);
        marca.transform.position = hacia;
        Sprite roca = dem.spriteEscombro != null ? dem.spriteEscombro : Provisional.Circulo;
        SpriteRenderer sr = Provisional.Crear(null, "EscombroJefe", roca, Color.white, 41);
        sr.transform.localScale = Vector3.one * 1.6f;

        float dur = 1.1f, t = 0f;
        float alto = Mathf.Max(1.2f, (hacia.y - desde.y) * 0.5f + 1.6f);
        while (t < dur && sr != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            Vector2 p = Vector2.Lerp(desde, hacia, k) + Vector2.up * (Mathf.Sin(k * Mathf.PI) * alto);
            sr.transform.position = p;
            sr.transform.Rotate(0f, 0f, 540f * Time.deltaTime);
            float pulso = 0.45f + 0.15f * Mathf.Sin(Time.time * 18f);
            if (marca != null) marca.transform.localScale = Vector3.one * (pulso * (0.6f + 0.4f * k));
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
        if (marca != null) Destroy(marca.gameObject);
        if (salvado) yield break;

        EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), hacia, 42, 1f, 12f);
        GestorAudio.Reproducir(Snd.ImpactoEscombro);
        CamaraSeguimiento.Temblar(0.06f, 0.15f);
        if (muneco != null && Vector2.Distance(muneco.CentroCuerpo, hacia) < 0.6f) muneco.RecibirGolpe();
        else DanarVentanaCerca(hacia, 0.7f);
    }

    private void DanarVentanaCerca(Vector2 punto, float radio)
    {
        VentanaEdificio mejor = null;
        float md = radio;
        for (int p = 0; p < edificio.Pisos; p++)
            for (int c = 0; c < edificio.columnas; c++)
            {
                VentanaEdificio v = edificio.VentanaEn(p, c);
                if (v == null || v.seccion != edificio.SeccionActual) continue;
                float d = Vector2.Distance(v.Centro, punto);
                if (d < md) { md = d; mejor = v; }
            }
        if (mejor != null) mejor.Danar();
    }

    /// <summary>Golpea la cornisa y una onda de choque recorre una fila de ventanas rompiéndolas una por una.</summary>
    private IEnumerator RomperFila()
    {
        int seccion = edificio.SeccionActual;
        int primero = edificio.PrimerPisoDeSeccion(seccion), cuantos = edificio.PisosDeSeccion(seccion);
        if (cuantos <= 0) yield break;
        int fila = primero + Random.Range(0, cuantos);

        ocupado = true;
        dem.Pausar();
        Anunciar("¡ONDA DE CHOQUE!", "¡SAL DE LA FILA QUE BRILLA!", new Color(1f, 0.45f, 0.3f));

        // Aviso: la fila parpadea en rojo mientras golpea la cornisa (se guarda el tinte propio de cada ventana).
        Color[] originales = new Color[edificio.columnas];
        for (int c = 0; c < edificio.columnas; c++)
        {
            VentanaEdificio v = edificio.VentanaEn(fila, c);
            originales[c] = v != null && v.dibujo != null ? v.dibujo.color : Color.white;
        }
        filaAviso = fila;
        coloresAviso = originales;
        float t = 0f;
        while (t < 1.4f)
        {
            t += Time.deltaTime;
            bool rojo = Mathf.Repeat(t * 6f, 1f) < 0.5f;
            for (int c = 0; c < edificio.columnas; c++)
            {
                VentanaEdificio v = edificio.VentanaEn(fila, c);
                if (v != null && v.dibujo != null) v.dibujo.color = rojo ? new Color(1f, 0.45f, 0.4f) : originales[c];
            }
            if (Mathf.Repeat(t, 0.45f) < Time.deltaTime) dem.GolpeAlTecho(0.25f);
            yield return null;
        }

        // La onda: de un lado al otro, ventana por ventana.
        bool izquierda = Random.value < 0.5f;
        for (int i = 0; i < edificio.columnas; i++)
        {
            int c = izquierda ? i : edificio.columnas - 1 - i;
            VentanaEdificio v = edificio.VentanaEn(fila, c);
            if (v == null) continue;
            if (v.dibujo != null) v.dibujo.color = originales[c];
            if (!salvado) v.Danar();
            EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), v.Centro, 42, 1.1f, 14f);
            GestorAudio.Reproducir(Snd.GolpeObjeto, 0.8f, 0.9f + i * 0.05f);
            CamaraSeguimiento.Temblar(0.09f, 0.12f);
            if (muneco != null && Mathf.Abs(muneco.CentroCuerpo.y - v.Centro.y) < 0.5f && Mathf.Abs(muneco.CentroCuerpo.x - v.Centro.x) < 0.7f)
                muneco.RecibirGolpe();
            yield return new WaitForSeconds(0.14f);
        }
        filaAviso = -1;
        dem.TerminarGolpes();
        if (!TerremotoOUlti() && !salvado) dem.Reanudar();
        ocupado = false;
    }

    // ------------------------------------------------------------ interfaz
    private void CrearInterfaz()
    {
        lienzo = FabricaUIArcade.CrearCanvas("LienzoJefe", 120);
        textoNombre = FabricaUIArcade.CrearTexto(lienzo.transform, "NombreJefe", "DEMOLEDOR · FASE 1", 30, PaletaArcade.Crema, TextAnchor.MiddleCenter, 3f, 3f);
        FabricaUIArcade.Colocar(textoNombre.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -175f), new Vector2(700f, 44f));
        pips = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            pips[i] = FabricaUIArcade.CrearImagen(lienzo.transform, "Fase" + i, Provisional.Circulo, Color.white);
            FabricaUIArcade.Colocar(pips[i].rectTransform, new Vector2(0.5f, 1f), new Vector2(-40f + i * 40f, -212f), new Vector2(24f, 24f));
        }
        textoGrande = FabricaUIArcade.CrearTexto(lienzo.transform, "Anuncio", "", 110, Color.white, TextAnchor.MiddleCenter, 6f, 6f);
        FabricaUIArcade.Colocar(textoGrande.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1600f, 160f));
        textoSub = FabricaUIArcade.CrearTexto(lienzo.transform, "Sub", "", 44, PaletaArcade.Crema, TextAnchor.MiddleCenter, 4f, 4f);
        FabricaUIArcade.Colocar(textoSub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1600f, 70f));
    }

    private void ActualizarPips()
    {
        if (textoNombre == null) return;
        textoNombre.text = (fase == 3 ? "DEMOLEDOR SUPREMO" : "DEMOLEDOR") + "  ·  FASE " + fase + " DE 3";
        textoNombre.color = fase == 3 ? new Color(1f, 0.45f, 0.4f) : fase == 2 ? new Color(1f, 0.75f, 0.4f) : PaletaArcade.Crema;
        for (int i = 0; i < pips.Length; i++)
            if (pips[i] != null) pips[i].color = i < fase ? (fase == 3 ? new Color(1f, 0.3f, 0.25f) : new Color(1f, 0.7f, 0.25f)) : new Color(1f, 1f, 1f, 0.25f);
    }

    private void Anunciar(string grande, string sub, Color color)
    {
        if (textoGrande == null) return;
        textoGrande.text = grande;
        textoGrande.color = color;
        textoSub.text = sub;
        finTextoGrande = Time.time + 2.4f;
    }

    private void AnimarTexto()
    {
        if (textoGrande == null) return;
        float resto = finTextoGrande - Time.time;
        bool ver = resto > 0f;
        textoGrande.enabled = textoSub.enabled = ver;
        if (!ver) return;
        float t = 2.4f - resto;
        float e = t < 0.15f ? Mathf.Lerp(2f, 0.9f, t / 0.15f) : (t < 0.25f ? Mathf.Lerp(0.9f, 1f, (t - 0.15f) / 0.1f) : 1f);
        textoGrande.rectTransform.localScale = new Vector3(e, e, 1f);
        float a = Mathf.Clamp01(resto / 0.4f);
        Color c = textoGrande.color; c.a = a; textoGrande.color = c;
        c = textoSub.color; c.a = a; textoSub.color = c;
    }
}
