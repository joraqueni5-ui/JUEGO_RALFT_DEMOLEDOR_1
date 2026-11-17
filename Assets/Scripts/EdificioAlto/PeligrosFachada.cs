using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Peligros y ayudas del nivel final (la fachada):
/// - PALOMAS: cada cierto tiempo (más seguido en las secciones altas) una paloma cruza volando la fachada de
///   lado a lado, a la altura de un piso de la sección actual. Antes de entrar, un aviso parpadeante en el borde
///   de la pantalla marca por dónde viene. Si toca al muñeco, pierde una vida (con su invulnerabilidad breve).
/// - PODERES: de vez en cuando un estudiante se asoma por una ventana intacta y deja un poder en su repisa:
///   salteña (invulnerable unos segundos) o botiquín (+1 vida). El poder desaparece si no se recoge.
/// Todos los tiempos y cantidades son campos del Inspector. Las figuras son provisionales.
/// </summary>
public class PeligrosFachada : MonoBehaviour
{
    [Header("Palomas (una por sección, de abajo hacia arriba)")]
    public float[] intervaloMinimo = { 8f, 7f, 6f };
    public float[] intervaloMaximo = { 9f, 8f, 7f };
    [Tooltip("Segundos que parpadea el aviso antes de que entre la paloma.")]
    public float segundosDeAviso = 1.6f;
    [Tooltip("Unidades por segundo.")]
    public float velocidadPaloma = 5.5f;
    [Tooltip("Distancia al centro del muñeco a la que lo golpea.")]
    public float radioContacto = 0.5f;
    [Tooltip("Primera paloma: segundos desde que empieza el nivel.")]
    public float primeraPaloma = 5f;

    [Header("Poderes")]
    public float intervaloPoderMinimo = 12f;
    public float intervaloPoderMaximo = 18f;
    [Tooltip("Segundos que el poder espera en la repisa antes de desaparecer.")]
    public float duracionEnRepisa = 8f;
    public float parpadeoFinal = 2f;
    [Tooltip("Segundos de invulnerabilidad de la salteña.")]
    public float duracionInvulnerable = 5f;
    public int vidasMaximas = 3;
    [Tooltip("Distancia al centro del muñeco a la que recoge el poder.")]
    public float radioRecogida = 0.6f;
    public Sprite spriteBotiquin;
    [Tooltip("Opcional: si está vacío, la salteña es una figura provisional.")]
    public Sprite spriteSalteña;

    private EdificioAlto edificio;
    private Muneco muneco;
    private GestorJuego gestor;
    private Camera camara;
    private float proximaPaloma, proximoPoder;
    private readonly List<GameObject> activos = new List<GameObject>();
    private bool poderEnCurso;

    private bool Quieto
    {
        get { return edificio == null || edificio.Terminado || gestor == null || gestor.PartidaTerminada || gestor.EstaEnPausa; }
    }

    private void Start()
    {
        edificio = GetComponent<EdificioAlto>();
        if (edificio == null) edificio = EdificioAlto.Instancia;
        muneco = FindFirstObjectByType<Muneco>();
        gestor = FindFirstObjectByType<GestorJuego>();
        camara = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        Reiniciar();
    }

    /// <summary>Quita lo que esté volando o esperando y vuelve a empezar los tiempos (al reiniciar la partida).</summary>
    public void Reiniciar()
    {
        StopAllCoroutines();
        foreach (GameObject g in activos) if (g != null) Destroy(g);
        activos.Clear();
        poderEnCurso = false;

        // También reinicia la energía del demoledor y el terremoto (la vereda vuelve a estar firme).
        EnergiaDemoledorFachada energia = GetComponent<EnergiaDemoledorFachada>();
        if (energia != null) energia.Reiniciar();
        TerremotoFachada terremoto = GetComponent<TerremotoFachada>();
        if (terremoto != null) terremoto.Reiniciar();
        UltiFachada ulti = GetComponent<UltiFachada>();
        if (ulti != null) ulti.Reiniciar();

        proximaPaloma = Time.time + primeraPaloma;
        proximoPoder = Time.time + Random.Range(intervaloPoderMinimo, intervaloPoderMaximo);
    }

    private void Update()
    {
        if (Quieto) return;

        if (Time.time >= proximaPaloma)
        {
            StartCoroutine(RutinaPaloma());
            int s = Mathf.Clamp(edificio.SeccionActual, 0, Mathf.Max(0, intervaloMinimo.Length - 1));
            float min = intervaloMinimo.Length > 0 ? intervaloMinimo[Mathf.Min(s, intervaloMinimo.Length - 1)] : 8f;
            float max = intervaloMaximo.Length > 0 ? intervaloMaximo[Mathf.Min(s, intervaloMaximo.Length - 1)] : 9f;
            proximaPaloma = Time.time + segundosDeAviso + Random.Range(min, Mathf.Max(min, max));
        }

        if (Time.time >= proximoPoder && !poderEnCurso)
        {
            proximoPoder = Time.time + Random.Range(intervaloPoderMinimo, intervaloPoderMaximo);
            StartCoroutine(RutinaPoder());
        }
    }

    // ------------------------------------------------------------ palomas
    private IEnumerator RutinaPaloma()
    {
        if (camara == null) yield break;

        // Piso de la sección actual, de preferencia uno que se vea en pantalla.
        int primero = edificio.PrimerPisoDeSeccion(edificio.SeccionActual);
        int cantidad = Mathf.Max(1, edificio.PisosDeSeccion(edificio.SeccionActual));
        float mitadAlto = camara.orthographicSize;
        List<int> visibles = new List<int>();
        for (int p = primero; p < primero + cantidad; p++)
        {
            float y = AlturaDePiso(p);
            if (Mathf.Abs(y - camara.transform.position.y) < mitadAlto - 0.4f) visibles.Add(p);
        }
        int piso = visibles.Count > 0 ? visibles[Random.Range(0, visibles.Count)] : primero + Random.Range(0, cantidad);
        float altura = AlturaDePiso(piso);

        float mitadAncho = mitadAlto * camara.aspect;
        float dir = Random.value < 0.5f ? 1f : -1f;
        float xCentro = camara.transform.position.x;

        // Aviso parpadeante en el borde por donde entra (queda dentro de la pantalla).
        float yAviso = Mathf.Clamp(altura, camara.transform.position.y - mitadAlto + 0.5f, camara.transform.position.y + mitadAlto - 0.5f);
        GameObject aviso = CrearAviso(new Vector3(xCentro - dir * (mitadAncho - 0.45f), yAviso, 0f));
        activos.Add(aviso);
        float fin = Time.time + segundosDeAviso;
        SpriteRenderer[] marcas = aviso.GetComponentsInChildren<SpriteRenderer>();
        if (aviso.transform.Find("AvisoArte") != null) marcas = new SpriteRenderer[0];   // el arte ya parpadea solo
        while (Time.time < fin)
        {
            if (!Quieto)
            {
                bool visible = ((int)(Time.time * 8f) % 2) == 0;
                foreach (SpriteRenderer m in marcas) m.enabled = visible;
            }
            yield return null;
        }
        activos.Remove(aviso);
        Destroy(aviso);

        // La paloma cruza de un borde al otro.
        GameObject paloma = CrearPaloma(dir);
        GestorAudio.Reproducir(Snd.Paloma);
        activos.Add(paloma);
        float x = xCentro - dir * (mitadAncho + 1.2f);
        float meta = xCentro + dir * (mitadAncho + 1.2f);
        float t = 0f;
        while ((dir > 0f && x < meta) || (dir < 0f && x > meta))
        {
            if (!Quieto)
            {
                t += Time.deltaTime;
                x += dir * velocidadPaloma * Time.deltaTime;
                float y = altura + Mathf.Sin(t * 5f) * 0.08f;
                paloma.transform.position = new Vector3(x, y, 0f);
                Aletear(paloma, t);

                if (muneco != null && muneco.enabled && muneco.PuedeRecibirGolpe &&
                    Vector2.Distance(paloma.transform.position, muneco.CentroCuerpo) < radioContacto)
                {
                    muneco.RecibirGolpe();
                }
            }
            yield return null;
        }
        activos.Remove(paloma);
        Destroy(paloma);
    }

    private float AlturaDePiso(int piso)
    {
        RepisaEdificio r = edificio.RepisaEn(piso, 0);
        return (r != null ? r.Superficie : edificio.PisoSeccion(edificio.SeccionActual)) + 0.55f;
    }

    private GameObject CrearAviso(Vector3 posicion)
    {
        GameObject g = new GameObject("AvisoPaloma");
        g.transform.position = posicion;
        Sprite[] arteAviso = ArteNiveles.De(a => a.avisoPaloma);
        if (arteAviso != null)
        {
            SpriteRenderer sr = Provisional.Crear(g.transform, "AvisoArte", arteAviso[0], Color.white, 40);
            EfectoSprite.EnBucle(sr, arteAviso, 4f);
            return g;
        }
        SpriteRenderer disco = Provisional.Crear(g.transform, "Disco", Provisional.Circulo, new Color(1f, 0.2f, 0.15f, 0.9f), 40);
        disco.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        SpriteRenderer marca = Provisional.Crear(g.transform, "Marca", Provisional.Cuadrado, Color.white, 41);
        marca.transform.localScale = new Vector3(0.09f, 0.26f, 1f);
        marca.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        SpriteRenderer punto = Provisional.Crear(g.transform, "Punto", Provisional.Cuadrado, Color.white, 41);
        punto.transform.localScale = new Vector3(0.09f, 0.09f, 1f);
        punto.transform.localPosition = new Vector3(0f, -0.15f, 0f);
        return g;
    }

    /// <summary>Paloma provisional: cuerpo, cabeza, pico y dos alas que aletean.</summary>
    private GameObject CrearPaloma(float dir)
    {
        GameObject g = new GameObject("Paloma");
        g.transform.localScale = new Vector3(dir, 1f, 1f);
        Sprite[] vuelo = ArteNiveles.De(a => a.palomaVuelo);
        if (vuelo != null)
        {
            // Con arte: 2 cuadros de vuelo (el sprite mira a la derecha; se voltea con la escala según la dirección).
            SpriteRenderer sr = Provisional.Crear(g.transform, "Cuerpo", vuelo[0], Color.white, 45);
            EfectoSprite.EnBucle(sr, vuelo, 8f);
            return g;
        }
        SpriteRenderer cuerpo = Provisional.Crear(g.transform, "Cuerpo", Provisional.Circulo, new Color(0.62f, 0.64f, 0.70f), 45);
        cuerpo.transform.localScale = new Vector3(0.7f, 0.38f, 1f);
        SpriteRenderer cabeza = Provisional.Crear(g.transform, "Cabeza", Provisional.Circulo, new Color(0.5f, 0.52f, 0.6f), 46);
        cabeza.transform.localPosition = new Vector3(0.34f, 0.1f, 0f);
        cabeza.transform.localScale = new Vector3(0.24f, 0.24f, 1f);
        SpriteRenderer pico = Provisional.Crear(g.transform, "Pico", Provisional.Cuadrado, new Color(0.95f, 0.65f, 0.2f), 47);
        pico.transform.localPosition = new Vector3(0.5f, 0.09f, 0f);
        pico.transform.localScale = new Vector3(0.12f, 0.06f, 1f);
        SpriteRenderer ala = Provisional.Crear(g.transform, "Ala", Provisional.Circulo, new Color(0.45f, 0.47f, 0.55f), 47);
        ala.transform.localPosition = new Vector3(-0.05f, 0.12f, 0f);
        ala.transform.localScale = new Vector3(0.42f, 0.3f, 1f);
        return g;
    }

    private static void Aletear(GameObject paloma, float t)
    {
        Transform ala = paloma.transform.Find("Ala");
        if (ala == null) return;
        float k = Mathf.Sin(t * 22f);
        ala.localScale = new Vector3(0.42f, 0.12f + 0.3f * Mathf.Abs(k), 1f);
        ala.localPosition = new Vector3(-0.05f, 0.12f * Mathf.Sign(k == 0f ? 1f : k) + 0.02f, 0f);
    }

    // ------------------------------------------------------------ poderes
    private IEnumerator RutinaPoder()
    {
        poderEnCurso = true;

        // Ventana intacta de la sección actual cuya repisa se pueda pisar (de preferencia a la vista).
        List<VentanaEdificio> candidatas = new List<VentanaEdificio>();
        int primero = edificio.PrimerPisoDeSeccion(edificio.SeccionActual);
        int cantidad = edificio.PisosDeSeccion(edificio.SeccionActual);
        for (int p = primero; p < primero + cantidad; p++)
        {
            for (int c = 0; c < edificio.columnas; c++)
            {
                VentanaEdificio v = edificio.VentanaEn(p, c);
                RepisaEdificio r = edificio.RepisaEn(p, c);
                if (v == null || r == null || v.Estado != EstadoVentana.Intacta || !r.SePuedePisar) continue;
                if (camara != null && Mathf.Abs(r.Superficie - camara.transform.position.y) > camara.orthographicSize - 0.5f) continue;
                candidatas.Add(v);
            }
        }
        if (candidatas.Count == 0) { poderEnCurso = false; yield break; }

        VentanaEdificio ventana = candidatas[Random.Range(0, candidatas.Count)];
        RepisaEdificio repisa = edificio.RepisaEn(ventana.piso, ventana.columna);
        bool vida = Random.value < 0.4f;

        // El estudiante se asoma por la ventana.
        GameObject estudiante = CrearEstudiante(ventana.Centro);
        activos.Add(estudiante);
        float t = 0f;
        const float Asomado = 1.5f;
        while (t < Asomado)
        {
            if (!Quieto)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((Asomado - t) / 0.3f);
                estudiante.transform.localScale = Vector3.one * Mathf.Max(0.01f, k);
                estudiante.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 9f) * 6f);
            }
            yield return null;
        }
        activos.Remove(estudiante);
        Destroy(estudiante);

        // Deja el poder en la repisa.
        if (ventana.Estado != EstadoVentana.Intacta || !repisa.SePuedePisar) { poderEnCurso = false; yield break; }
        Vector3 donde = new Vector3(repisa.CentroX, repisa.Superficie + 0.32f, 0f);
        TextoFlotante.Mostrar((Vector2)donde + Vector2.up * 0.9f, vida ? "¡Un estudiante dejó un botiquín!" : "¡Un estudiante dejó una salteña!",
            vida ? new Color(1f, 0.5f, 0.55f) : new Color(1f, 0.85f, 0.2f));

        GameObject poder = CrearPoder(donde, vida);
        GestorAudio.Reproducir(Snd.PoderAparece);
        activos.Add(poder);
        float vive = 0f;
        while (vive < duracionEnRepisa)
        {
            if (!Quieto)
            {
                vive += Time.deltaTime;
                poder.transform.position = donde + Vector3.up * (Mathf.Sin(Time.time * 4f) * 0.06f);
                bool parpadea = duracionEnRepisa - vive < parpadeoFinal;
                SpriteRenderer[] rs = poder.GetComponentsInChildren<SpriteRenderer>();
                bool visible = !parpadea || ((int)(Time.time * 10f) % 2) == 0;
                foreach (SpriteRenderer r in rs) r.enabled = visible;

                if (muneco != null && muneco.enabled && Vector2.Distance(poder.transform.position, muneco.CentroCuerpo) < radioRecogida)
                {
                    Recoger(vida, donde);
                    break;
                }
            }
            yield return null;
        }
        activos.Remove(poder);
        Destroy(poder);
        poderEnCurso = false;
    }

    private void Recoger(bool vida, Vector3 donde)
    {
        GestorAudio.Reproducir(Snd.PoderRecoger);
        if (vida)
        {
            bool gano = gestor != null && gestor.GanarVida(1, vidasMaximas);
            TextoFlotante.Mostrar((Vector2)donde + Vector2.up * 0.9f, gano ? "+1 VIDA" : "VIDAS AL MÁXIMO", new Color(1f, 0.45f, 0.5f));
        }
        else
        {
            muneco.DarInvulnerabilidad(duracionInvulnerable);
            TextoFlotante.Mostrar((Vector2)donde + Vector2.up * 0.9f, "¡INVENCIBLE!", new Color(1f, 0.85f, 0.2f));
        }
    }

    private GameObject CrearPoder(Vector3 posicion, bool vida)
    {
        GameObject g = new GameObject(vida ? "Botiquin" : "Salteña");
        g.transform.position = posicion;
        Sprite arte = vida ? spriteBotiquin : spriteSalteña;
        if (arte != null)
        {
            SpriteRenderer sr = Provisional.Crear(g.transform, "Dibujo", arte, Color.white, 48);
            Provisional.AjustarAlArea(sr, arte, new Bounds(posicion, new Vector3(0.6f, 0.6f, 0f)));
            return g;
        }

        if (vida)
        {
            SpriteRenderer caja = Provisional.Crear(g.transform, "Caja", Provisional.Cuadrado, new Color(0.95f, 0.95f, 0.95f), 48);
            caja.transform.localScale = new Vector3(0.45f, 0.36f, 1f);
            SpriteRenderer v = Provisional.Crear(g.transform, "CruzV", Provisional.Cuadrado, new Color(0.9f, 0.15f, 0.2f), 49);
            v.transform.localScale = new Vector3(0.1f, 0.28f, 1f);
            SpriteRenderer h = Provisional.Crear(g.transform, "CruzH", Provisional.Cuadrado, new Color(0.9f, 0.15f, 0.2f), 49);
            h.transform.localScale = new Vector3(0.28f, 0.1f, 1f);
        }
        else
        {
            SpriteRenderer masa = Provisional.Crear(g.transform, "Masa", Provisional.Circulo, new Color(0.95f, 0.65f, 0.2f), 48);
            masa.transform.localScale = new Vector3(0.55f, 0.36f, 1f);
            SpriteRenderer pliegue = Provisional.Crear(g.transform, "Pliegue", Provisional.Cuadrado, new Color(0.75f, 0.45f, 0.12f), 49);
            pliegue.transform.localScale = new Vector3(0.4f, 0.04f, 1f);
        }
        return g;
    }

    /// <summary>Estudiante provisional: cabeza, cuerpo y un brazo.</summary>
    private GameObject CrearEstudiante(Vector2 centroVentana)
    {
        GameObject g = new GameObject("EstudianteVentana");
        g.transform.position = new Vector3(centroVentana.x, centroVentana.y - 0.1f, 0f);
        SpriteRenderer cuerpo = Provisional.Crear(g.transform, "Cuerpo", Provisional.Cuadrado, new Color(0.25f, 0.45f, 0.8f), 12);
        cuerpo.transform.localScale = new Vector3(0.42f, 0.3f, 1f);
        cuerpo.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        SpriteRenderer cabeza = Provisional.Crear(g.transform, "Cabeza", Provisional.Circulo, new Color(0.95f, 0.78f, 0.62f), 13);
        cabeza.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        cabeza.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
        SpriteRenderer brazo = Provisional.Crear(g.transform, "Brazo", Provisional.Cuadrado, new Color(0.95f, 0.78f, 0.62f), 14);
        brazo.transform.localPosition = new Vector3(0.25f, 0.0f, 0f);
        brazo.transform.localScale = new Vector3(0.1f, 0.26f, 1f);
        return g;
    }
}
