using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// El demoledor llama a su cuadrilla: cada cierto tiempo aparecen mini villanos junto a él (silbido y "¡A ROMPER!").
/// Cuantos más pisos subes, más y más variados son (planta baja: aprendices; luego termitas; desde el piso 3, topadoras).
/// Sinergia: lo que rompen sube el destrozo, eso enfurece al demoledor y en furia llama más seguido y a más élites.
/// Se van todos cuando el demoledor se rinde. InstaladorExtras lo agrega solo a cada piso cenital.
/// </summary>
public class GestorMiniVillanos : MonoBehaviour
{
    [Tooltip("Segundos después de que entra el demoledor hasta la primera llamada.")]
    public float esperaInicial = 6f;
    [Tooltip("Segundos entre llamadas en la planta baja y en el último piso (se interpola según el piso).")]
    public Vector2 intervaloPisoBajoYAlto = new Vector2(17f, 8f);
    [Tooltip("En furia las llamadas son más seguidas (multiplica el intervalo).")]
    public float factorFuria = 0.6f;

    private PisoCenital piso;
    private DemoledorCenital demoledor;
    private JugadorCenital jugador;
    private GestorJuego gestor;
    private float proximaLlamada = -1f, proximaOleada = -1f;
    private bool estabaEnPiso;

    [Tooltip("Con la energía del demoledor por debajo de este valor (0 a 1) lanza oleadas de varios ayudantes a la vez.")]
    [Range(0f, 1f)] public float umbralOleada = 0.3f;
    [Tooltip("Segundos entre oleadas.")]
    public float intervaloOleada = 16f;

    /// <summary>En multijugador cada demoledor tiene su propia cuadrilla (InstaladorExtras lo asigna).</summary>
    [System.NonSerialized] public DemoledorCenital demoledorAsignado;
    /// <summary>Cuántos demoledores comparten el piso: los topes de minis se multiplican.</summary>
    private int Cuadrillas { get { return Mathf.Max(1, DemoledorCenital.Todos.Count); } }

    private int Numero { get { return piso != null && piso.Definicion != null ? Mathf.Clamp(piso.Definicion.numero, 0, 6) : 0; } }

    // ------------------------------------------------------------ acceso estático (jugador y potenciadores)
    /// <summary>Congela a todos los mini villanos. Devuelve cuántos congeló.</summary>
    public static int CongelarTodos(float segundos)
    {
        int n = 0;
        foreach (MiniVillano m in new List<MiniVillano>(MiniVillano.Activos))
        {
            if (m == null) continue;
            m.Congelar(segundos);
            n++;
        }
        return n;
    }

    /// <summary>Martillazo del jugador: golpea a los mini villanos que estén a 'radio' de 'centro'.</summary>
    public static int GolpearEnRadio(Vector2 centro, float radio)
    {
        int n = 0;
        foreach (MiniVillano m in new List<MiniVillano>(MiniVillano.Activos))
        {
            if (m == null || Vector2.Distance(centro, m.Centro) > radio) continue;
            m.RecibirMartillazo(centro);
            n++;
        }
        return n;
    }

    /// <summary>La onda de la furia: vence de un golpe a los mini villanos que estén a 'radio' de 'centro'.</summary>
    public static int VencerEnRadio(Vector2 centro, float radio)
    {
        int n = 0;
        foreach (MiniVillano m in new List<MiniVillano>(MiniVillano.Activos))
        {
            if (m == null || Vector2.Distance(centro, m.Centro) > radio) continue;
            m.Vencer(true);
            n++;
        }
        return n;
    }

    public static void EliminarTodos(bool conPuntos)
    {
        foreach (MiniVillano m in new List<MiniVillano>(MiniVillano.Activos)) if (m != null) m.Vencer(conPuntos);
    }

    // ------------------------------------------------------------ ciclo
    private void Start()
    {
        piso = GetComponent<PisoCenital>();
        if (piso == null) piso = PisoCenital.Actual;
        demoledor = demoledorAsignado != null ? demoledorAsignado : FindAnyObjectByType<DemoledorCenital>();
        jugador = JugadorCenital.MasCercano(transform.position);
        gestor = FindAnyObjectByType<GestorJuego>();
    }

    private void OnDestroy()
    {
        foreach (MiniVillano m in new List<MiniVillano>(MiniVillano.Activos)) if (m != null) Destroy(m.gameObject);
    }

    private void Update()
    {
        if (piso == null || piso.Mapa == null || demoledor == null) return;
        if (gestor != null && (gestor.PartidaTerminada || gestor.EstaEnPausa)) return;
        if (JugadorCenital.Todos.Count > 1) jugador = JugadorCenital.MasCercano(demoledor.transform.position);

        // Rendido o piso terminado: la cuadrilla huye.
        if (piso.Rendido || piso.Terminado)
        {
            if (MiniVillano.Activos.Count > 0) EliminarTodos(false);
            return;
        }

        bool enPiso = demoledor.EnPiso;
        if (!enPiso)
        {
            // Antes de que entre (o tras reiniciar el piso): sin minis y el reloj espera
            // (con dos demoledores, solo si ninguno está dentro).
            bool alguno = false;
            foreach (DemoledorCenital d in DemoledorCenital.Todos) if (d != null && d.EnPiso) alguno = true;
            if (estabaEnPiso && !alguno && MiniVillano.Activos.Count > 0) EliminarTodos(false);
            estabaEnPiso = false;
            proximaLlamada = -1f;
            proximaOleada = -1f;
            return;
        }
        if (!estabaEnPiso)
        {
            estabaEnPiso = true;
            proximaLlamada = Time.time + esperaInicial;
        }
        if (demoledor.Congelado) return;

        // Demoledor débil (menos de 30 % de energía): desesperado, despliega varios ayudantes a la vez.
        // En rabia (energía bajo 20 %) las oleadas son continuas y más grandes.
        if ((piso.Energia < umbralOleada || demoledor.EnRabia) && Time.time >= proximaOleada)
        {
            float intervalo = demoledor.EnRabia ? intervaloOleada * 0.55f : intervaloOleada;
            proximaOleada = Time.time + intervalo * Random.Range(0.8f, 1.2f);
            Oleada();
            return;
        }

        if (Time.time < proximaLlamada) return;
        proximaLlamada = Time.time + Intervalo();
        if (MiniVillano.Activos.Count >= Maximo()) return;
        Llamar();
    }

    private void Oleada()
    {
        int cuantos = Random.Range(3, 6) + Numero / 3 + (demoledor.EnRabia ? 1 : 0);
        int tope = (demoledor.EnRabia ? 13 : 10) + (Cuadrillas - 1) * 6;
        cuantos = Mathf.Min(cuantos, tope - MiniVillano.Activos.Count);   // tope de minis a la vez
        if (cuantos <= 0) return;
        Vector2 centro = demoledor.transform.position;
        List<Vector2> usadas = new List<Vector2>();
        int creados = 0;
        for (int i = 0; i < cuantos; i++)
        {
            Vector2 p;
            if (!BuscarCelda(centro, usadas, out p, 3.5f)) break;
            usadas.Add(p);
            // En la oleada salen más élites y cualquier tipo, sin importar el piso.
            TipoMini tipo = (TipoMini)Random.Range(0, 3);
            MiniVillano.Crear(tipo, Random.value < 0.45f, p);
            creados++;
        }
        if (creados == 0) return;
        GestorAudio.Reproducir(SndExtra.MiniLlamada, 1f, 0.8f);
        GestorAudio.Reproducir(SndExtra.RugidoDemoledor, 0.8f, 1.15f);
        CamaraPisoCenital.Temblar(0.1f, 0.35f);
        string[] gritos = { "¡TODOS CONTRA ÉL!", "¡OLEADA DE AYUDANTES!", "¡A DESTRUIR TODO!", "¡CUADRILLA COMPLETA!" };
        TextoFlotante.Mostrar(centro + Vector2.up * 2.8f, gritos[Random.Range(0, gritos.Length)], new Color(1f, 0.35f, 0.25f), 2.4f);
#if UNITY_EDITOR
        Debug.Log("[Extras] Oleada: " + creados + " ayudantes (energía " + (piso.Energia * 100f).ToString("0") + " %).");
#endif
    }

    private float Intervalo()
    {
        float t = Numero / 6f;
        float i = Mathf.Lerp(intervaloPisoBajoYAlto.x, intervaloPisoBajoYAlto.y, t);
        if (demoledor.EnFuria) i *= factorFuria;
        i *= Dificultad.IntervaloMinis * ModoSupervivencia.FactorMinis;
        return i * Random.Range(0.85f, 1.15f);
    }

    private int Maximo()
    {
        return (1 + Numero / 2 + (demoledor.EnFuria ? 1 : 0)) * Cuadrillas;
    }

    private void Llamar()
    {
        int cuantos = 1;
        if (Numero >= 3 && Random.value < 0.4f) cuantos = 2;
        if (demoledor.EnFuria && Random.value < 0.5f) cuantos++;
        cuantos = Mathf.Min(cuantos, Maximo() - MiniVillano.Activos.Count);
        if (cuantos <= 0) return;

        Vector2 centro = demoledor.transform.position;
        List<Vector2> usadas = new List<Vector2>();
        int creados = 0;
        for (int i = 0; i < cuantos; i++)
        {
            Vector2 p;
            if (!BuscarCelda(centro, usadas, out p)) break;
            usadas.Add(p);
            MiniVillano.Crear(ElegirTipo(), Random.value < ProbElite(), p);
            creados++;
        }
        if (creados == 0) return;
#if UNITY_EDITOR
        Debug.Log("[Extras] El demoledor llamó " + creados + " mini villano(s).");
#endif
        GestorAudio.Reproducir(SndExtra.MiniLlamada);
        string[] gritos = { "¡A ROMPER, MUCHACHOS!", "¡CUADRILLA, A MÍ!", "¡TODOS A DEMOLER!", "¡REFUERZOS!" };
        TextoFlotante.Mostrar(centro + Vector2.up * 2.6f, gritos[Random.Range(0, gritos.Length)], new Color(1f, 0.7f, 0.3f), 1.6f);
    }

    private TipoMini ElegirTipo()
    {
        int n = Numero;
        float aprendiz = 3f;
        float termita = n >= 1 ? 2f + n * 0.3f : 0f;
        float topadora = n >= 3 ? 1f + (n - 3) * 0.4f : 0f;
        if (demoledor.EnFuria) topadora *= 1.5f;
        float azar = Random.value * (aprendiz + termita + topadora);
        if (azar < aprendiz) return TipoMini.Aprendiz;
        if (azar < aprendiz + termita) return TipoMini.Termita;
        return TipoMini.Topadora;
    }

    private float ProbElite()
    {
        return Mathf.Clamp01(0.1f + Numero * 0.05f + (demoledor.EnFuria ? 0.25f : 0f) + (demoledor.EnRabia ? 0.2f : 0f));
    }

    /// <summary>Celda libre a 1 a 2,5 baldosas del demoledor, lejos del jugador y de los otros minis.</summary>
    private bool BuscarCelda(Vector2 centro, List<Vector2> usadas, out Vector2 celda, float radioMax = 2.6f)
    {
        celda = Vector2.zero;
        List<Vector2> candidatas = new List<Vector2>();
        foreach (Vector2 c in piso.Mapa.CentrosTransitables)
        {
            float d = Vector2.Distance(c, centro);
            if (d < 1f || d > radioMax) continue;
            if (jugador != null && Vector2.Distance(c, jugador.Centro) < 2.2f) continue;
            bool ocupada = false;
            foreach (Vector2 u in usadas) if (Vector2.Distance(u, c) < 0.9f) ocupada = true;
            foreach (MiniVillano m in MiniVillano.Activos) if (m != null && Vector2.Distance(m.transform.position, c) < 0.9f) ocupada = true;
            if (!ocupada) candidatas.Add(c);
        }
        if (candidatas.Count == 0) return false;
        celda = candidatas[Random.Range(0, candidatas.Count)];
        return true;
    }
}
