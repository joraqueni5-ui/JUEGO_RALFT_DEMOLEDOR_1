using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ulti del demoledor en el nivel final: "¡LLUVIA DE ESCOMBROS!". Se activa una vez cuando su energía baja de 'umbralPorcentaje'
/// (después del terremoto). Mismo aviso que en los pisos (el demoledor golpea el techo, la cámara tiembla y los bordes se tiñen de rojo).
/// Después caen rocas en vertical sobre columnas al azar de la sección actual: cada una marca con su sombra la repisa donde caerá,
/// golpea al muñeco y daña las ventanas igual que los ladrillos normales.
/// </summary>
[RequireComponent(typeof(EdificioAlto))]
public class UltiFachada : MonoBehaviour
{
    public bool activa = true;
    [Tooltip("Energía (%) por debajo de la cual se activa (una sola vez, después del terremoto).")]
    [Range(1f, 50f)] public float umbralPorcentaje = 15f;
    [Min(1)] public int cantidadRocas = 10;
    [Tooltip("Golpes al suelo con los dos puños durante el aviso.")]
    [Min(1)] public int golpesAviso = 4;
    [Tooltip("Segundos entre un golpe al suelo y el siguiente (aviso y lluvia): 4 golpes de aviso = 2 s.")]
    [Min(0.2f)] public float intervaloGolpe = 0.5f;
    [Tooltip("Segundos de lluvia: sigue golpeando cada 'intervaloGolpe' (4 / 0,5 = 8 golpes) y cada golpe suelta la siguiente tanda de rocas.")]
    [Min(1f)] public float duracionLluvia = 4f;
    [Tooltip("Segundos que la sombra avisa antes de que caiga cada roca.")]
    [Min(0.3f)] public float avisoCaida = 1f;
    [Tooltip("Segundos finales en que la sombra se marca con borde rojo.")]
    [Min(0f)] public float segundosBordeRojo = 0.3f;
    [Tooltip("Probabilidad de cada impacto de la roca en ventanas o repisas (como los ladrillos normales).")]
    public float[] probabilidadesImpacto = { 0.5f, 0.35f, 0.2f };
    public int ordenRoca = 6;

    private EdificioAlto edificio;
    private EnergiaDemoledorFachada energia;
    private TerremotoFachada terremoto;
    private DemoledorAutomatico demoledor;
    private GestorJuego gestor;
    private bool usada;
    private Coroutine secuencia;
    private readonly List<GameObject> sombras = new List<GameObject>();

    public bool Usada { get { return usada; } }

    private void Start()
    {
        edificio = GetComponent<EdificioAlto>();
        energia = GetComponent<EnergiaDemoledorFachada>();
        terremoto = GetComponent<TerremotoFachada>();
        demoledor = FindFirstObjectByType<DemoledorAutomatico>();
        gestor = FindFirstObjectByType<GestorJuego>();
    }

    private void Update()
    {
        if (!activa || usada || energia == null || edificio == null || edificio.Terminado) return;
        if (gestor != null && (gestor.PartidaTerminada || gestor.EstaEnPausa)) return;
        if (terremoto != null && (!terremoto.Activado || terremoto.EnCurso)) return;   // la ulti llega después del terremoto
        if (energia.Porcentaje <= umbralPorcentaje) secuencia = StartCoroutine(Secuencia());
    }

    /// <summary>Vuelve a estar disponible (al reiniciar la partida).</summary>
    public void Reiniciar()
    {
        if (secuencia != null) { StopCoroutine(secuencia); secuencia = null; }
        usada = false;
        AvisoUlti.Ocultar();
        foreach (GameObject s in sombras) if (s != null) Destroy(s);
        sombras.Clear();
    }

    private IEnumerator Secuencia()
    {
        usada = true;
        GestorAudio.Reproducir(Snd.AvisoLluvia);

        // 1) Aviso: golpea el suelo con los dos puños 'golpesAviso' veces (uno cada 'intervaloGolpe') y la pantalla se tiñe de rojo.
        if (demoledor != null) demoledor.Pausar();
        AvisoUlti.Mostrar("¡LLUVIA DE ESCOMBROS!", Mathf.Max(1, golpesAviso) * intervaloGolpe, true, new Color(1f, 0.92f, 0.45f));
        for (int i = 0; i < golpesAviso; i++)
        {
            if (demoledor != null) demoledor.GolpeAlTecho();
            yield return Esperar(intervaloGolpe);
        }

        // 2) Lluvia: sigue golpeando cada 'intervaloGolpe' y cada golpe suelta la siguiente tanda de rocas (repartidas parejo).
        int n = Mathf.Max(1, cantidadRocas);
        int golpesLluvia = Mathf.Max(1, Mathf.RoundToInt(duracionLluvia / intervaloGolpe));
        int sueltas = 0;
        for (int g = 1; g <= golpesLluvia; g++)
        {
            if (edificio.Terminado) break;
            if (demoledor != null) demoledor.GolpeAlTecho();
            int hasta = g >= golpesLluvia ? n : n * g / golpesLluvia;
            while (sueltas < hasta) { SoltarRoca(); sueltas++; }
            yield return Esperar(intervaloGolpe);
        }
        if (demoledor != null) demoledor.TerminarGolpes();

        // 3) Termina la lluvia: esperan las últimas rocas y el demoledor sigue atacando.
        yield return Esperar(avisoCaida);
        if (demoledor != null) demoledor.Reanudar();
        secuencia = null;
    }

    private static IEnumerator Esperar(float segundos)
    {
        float t = 0f;
        while (t < segundos) { t += Time.deltaTime; yield return null; }
    }

    private void SoltarRoca()
    {
        int seccion = Mathf.Clamp(edificio.SeccionActual, 0, edificio.secciones - 1);
        int columna = Random.Range(0, Mathf.Max(1, edificio.columnas));
        float x = edificio.CentroColumna(columna) + Random.Range(-0.1f, 0.1f);
        float origenY = edificio.AlturaCornisa(seccion) - 0.12f;

        // La sombra va sobre la primera repisa de la columna (la de arriba de la sección), o sobre la cornisa si no hay.
        int primero = edificio.PrimerPisoDeSeccion(seccion);
        int ultimo = primero + Mathf.Max(1, edificio.PisosDeSeccion(seccion)) - 1;
        RepisaEdificio repisa = edificio.RepisaEn(ultimo, columna);
        float sueloY = repisa != null ? repisa.Superficie : origenY - 1.5f;

        // La roca arranca con la velocidad justa para llegar a la sombra cuando termina su aviso.
        float distancia = Mathf.Max(0.5f, origenY - sueloY);
        float a = EscombroCayendo.AceleracionBase;
        float v0 = Mathf.Max(0.2f, distancia / avisoCaida - 0.5f * a * avisoCaida);

        GestorAudio.Reproducir(Snd.RocaCayendo);
        Sprite[] serie = ElegirRoca();
        EscombroCayendo.Crear(serie != null ? serie[0] : null, new Vector3(x, origenY, 0f), columna, v0, probabilidadesImpacto, ordenRoca, serie);
        CrearSombra(new Vector3(x, sueloY, 0f));
    }

    private static Sprite[] ElegirRoca()
    {
        ArteNiveles arte = ArteNiveles.Actual;
        if (arte == null) return null;
        int c = Random.Range(0, 3);
        return ArteNiveles.Serie(c == 0 ? arte.rocaLluvia1 : c == 1 ? arte.rocaLluvia2 : arte.rocaLluvia3);
    }

    private void CrearSombra(Vector3 posicion)
    {
        ArteNiveles arte = ArteNiveles.Actual;
        Sprite[] cuadros = arte != null ? ArteNiveles.Serie(arte.sombraImpactoGrande) : null;

        GameObject g = new GameObject("SombraRoca");
        g.transform.position = posicion;
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sortingOrder = ordenRoca - 1;
        sr.sprite = cuadros != null ? cuadros[0] : Provisional.Circulo;
        if (cuadros == null) { sr.color = new Color(0f, 0f, 0f, 0.35f); g.transform.localScale = new Vector3(1f, 0.4f, 1f); }
        SombraAviso s = g.AddComponent<SombraAviso>();
        s.Iniciar(sr, cuadros, avisoCaida, segundosBordeRojo);
        sombras.Add(g);
    }
}

/// <summary>Sombra de aviso fija: crece con sus cuadros y los últimos instantes se marca con borde rojo; se destruye al acabar.</summary>
public class SombraAviso : MonoBehaviour
{
    private SpriteRenderer dibujo, borde;
    private Sprite[] cuadros;
    private float duracion, segundosBorde, t;

    public void Iniciar(SpriteRenderer sr, Sprite[] cuadros, float duracion, float segundosBorde)
    {
        dibujo = sr;
        this.cuadros = cuadros;
        this.duracion = Mathf.Max(0.2f, duracion);
        this.segundosBorde = segundosBorde;

        GameObject b = new GameObject("BordeRojo");
        b.transform.SetParent(transform, false);
        borde = b.AddComponent<SpriteRenderer>();
        borde.sortingOrder = sr.sortingOrder - 1;
        borde.color = new Color(1f, 0.1f, 0.08f, 0.95f);
        borde.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        borde.enabled = false;
    }

    private void Update()
    {
        t += Time.deltaTime / duracion;
        float k = Mathf.Clamp01(t);
        if (cuadros != null)
        {
            Sprite f = cuadros[Mathf.Min(cuadros.Length - 1, (int)(k * cuadros.Length))];
            dibujo.sprite = f;
            dibujo.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.55f, 1f, k));
            borde.sprite = f;
        }
        else borde.sprite = dibujo.sprite;
        borde.enabled = duracion * (1f - k) <= segundosBorde;
        if (t >= 1f) Destroy(gameObject);
    }
}
