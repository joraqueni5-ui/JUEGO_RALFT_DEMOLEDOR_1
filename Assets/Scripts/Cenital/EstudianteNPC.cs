using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estudiante de ambiente. Camina por las celdas libres de su laboratorio (rutas con el mismo A*
/// del demoledor) y, si el demoledor se acerca, huye hacia el punto más lejano de su zona. No tiene
/// collider: no estorba ni recibe daño. Con arte (DefinicionPiso.estudiante*) usa esos cuadros; si
/// no, una figura provisional de cuerpo y cabeza.
/// </summary>
public class EstudianteNPC : MonoBehaviour
{
    [Tooltip("0 = laboratorio izquierdo (Gastronomía), 1 = derecho (Medicina).")]
    [Range(0, 1)] public int laboratorio;
    [Tooltip("Es el chef: deja los poderes junto a él (DefinicionPiso.poderes con origen JuntoAlChef).")]
    public bool esChef;
    public float velocidad = 1.1f;
    public float velocidadHuida = 2.9f;
    [Tooltip("Distancia (baldosas) a la que el demoledor lo hace huir.")]
    public float radioPeligro = 3.4f;
    [Tooltip("Alto en baldosas al que se escala el sprite.")]
    public float alturaDibujo = 1.1f;
    public float cuadrosPorSegundo = 8f;
    [Tooltip("Cuadros de caminata propios (mirando a la derecha). Si están vacíos se usan los de la DefinicionPiso.")]
    public Sprite[] cuadrosPropios;

    private enum Estado { Quieto, Caminando, Huyendo }

    /// <summary>El chef del piso, o null si no hay.</summary>
    public static EstudianteNPC Chef { get; private set; }

    private MapaCenital mapa;
    private DemoledorCenital demoledor;
    private readonly List<Vector2> zona = new List<Vector2>();
    private List<Vector2> ruta;
    private int indiceRuta;
    private Estado estado = Estado.Quieto;
    private float finEspera;
    private float proximaRevision;

    private SpriteRenderer cuerpo, cabeza, sombra, gorro;
    private AnimadorSprites animador;
    private Sprite[] cuadros;
    private bool mirandoIzquierda;
    private float fase;

    private void Start()
    {
        PisoCenital piso = PisoCenital.Actual;
        if (piso == null || piso.Mapa == null) { enabled = false; return; }
        mapa = piso.Mapa;
        demoledor = FindFirstObjectByType<DemoledorCenital>();
        if (esChef) Chef = this;

        DefinicionPiso def = piso.Definicion;
        if (def != null && !def.mostrarNPC)
        {
            // Oculto: no se dibuja ni camina. El chef conserva su punto para los poderes (JuntoAlChef).
            enabled = false;
            return;
        }
        if (def != null)
        {
            cuadros = laboratorio == 0 ? def.estudianteGastronomia : def.estudianteMedicina;
            if (esChef && def.chef != null && def.chef.Length > 0) cuadros = def.chef;
        }
        if (cuadrosPropios != null && cuadrosPropios.Length > 0) cuadros = cuadrosPropios;
        if (cuadros != null && cuadros.Length == 0) cuadros = null;

        // Zona: la habitación donde empieza (celdas libres conectadas sin cruzar puertas).
        LlenarZona();
        if (zona.Count == 0) { enabled = false; return; }

        transform.position = CeldaMasCercana(transform.position);
        CrearDibujo();
        finEspera = Time.time + Random.Range(0.5f, 2f);
    }

    private void LlenarZona()
    {
        Vector2Int inicio;
        if (!mapa.CeldaTransitableCercana(transform.position, out inicio)) return;

        HashSet<Vector2Int> vistas = new HashSet<Vector2Int> { inicio };
        Queue<Vector2Int> cola = new Queue<Vector2Int>();
        cola.Enqueue(inicio);
        Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        while (cola.Count > 0)
        {
            Vector2Int c = cola.Dequeue();
            zona.Add(mapa.CentroDe(c));
            foreach (Vector2Int d in dirs)
            {
                Vector2Int n = c + d;
                if (vistas.Contains(n) || !mapa.EsTransitable(n) || mapa.EsPuerta(n)) continue;
                vistas.Add(n);
                cola.Enqueue(n);
            }
        }
    }

    private Vector2 CeldaMasCercana(Vector2 p)
    {
        Vector2 mejor = zona[0];
        float d = float.MaxValue;
        foreach (Vector2 c in zona)
        {
            float k = (c - p).sqrMagnitude;
            if (k < d) { d = k; mejor = c; }
        }
        return mejor;
    }

    private void CrearDibujo()
    {
        Color ropa = laboratorio == 0 ? new Color(0.95f, 0.95f, 0.92f) : new Color(0.35f, 0.75f, 0.85f);

        sombra = Provisional.Crear(transform, "Sombra", Provisional.Circulo, new Color(0f, 0f, 0f, 0.28f), 0);
        sombra.transform.localPosition = new Vector3(0f, -0.28f, 0f);
        sombra.transform.localScale = new Vector3(0.6f, 0.26f, 1f);

        if (cuadros != null)
        {
            cuerpo = Provisional.Crear(transform, "Dibujo", cuadros[0], Color.white, 0);
            animador = new AnimadorSprites(cuerpo);
        }
        else
        {
            cuerpo = Provisional.Crear(transform, "Cuerpo", Provisional.Cuadrado, ropa, 0);
            cuerpo.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            cuerpo.transform.localScale = new Vector3(0.42f, 0.62f, 1f);
            cabeza = Provisional.Crear(transform, "Cabeza", Provisional.Circulo, new Color(1f, 0.85f, 0.7f), 0);
            cabeza.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            cabeza.transform.localScale = new Vector3(0.36f, 0.36f, 1f);
            if (esChef)
            {
                // Gorro de chef: una banda y una copa blancas sobre la cabeza.
                gorro = Provisional.Crear(transform, "Gorro", Provisional.Cuadrado, Color.white, 0);
                gorro.transform.localPosition = new Vector3(0f, 0.86f, 0f);
                gorro.transform.localScale = new Vector3(0.34f, 0.3f, 1f);
            }
        }
        ActualizarOrden();
    }

    private void ActualizarOrden()
    {
        int orden = Provisional.OrdenY(transform.position.y - 0.3f);
        if (sombra != null) sombra.sortingOrder = orden - 1;
        if (cuerpo != null) cuerpo.sortingOrder = orden;
        if (cabeza != null) cabeza.sortingOrder = orden + 1;
        if (gorro != null) gorro.sortingOrder = orden + 2;
    }

    private void OnDestroy()
    {
        if (Chef == this) Chef = null;
    }

    private void Update()
    {
        if (mapa == null) return;

        PeligroCerca();
        switch (estado)
        {
            case Estado.Quieto:
                if (Time.time >= finEspera) NuevoPaseo();
                break;
            case Estado.Caminando:
                Avanzar(velocidad);
                break;
            case Estado.Huyendo:
                Avanzar(velocidadHuida);
                break;
        }
        Animar();
        ActualizarOrden();
    }

    // ------------------------------------------------------------ comportamiento
    private bool DemoledorCerca(out Vector2 posDemoledor)
    {
        posDemoledor = Vector2.zero;
        if (demoledor == null) return false;
        posDemoledor = demoledor.transform.position;
        return Vector2.Distance(posDemoledor, transform.position) < radioPeligro;
    }

    private void PeligroCerca()
    {
        Vector2 dem;
        if (!DemoledorCerca(out dem)) return;
        if (estado == Estado.Huyendo && Time.time < proximaRevision) return;
        proximaRevision = Time.time + 0.6f;
        Huir(dem);
    }

    private void Huir(Vector2 dem)
    {
        // Los puntos de su zona más lejanos del demoledor; prueba varios hasta hallar una ruta.
        List<Vector2> lejanos = new List<Vector2>(zona);
        lejanos.Sort((a, b) => (b - dem).sqrMagnitude.CompareTo((a - dem).sqrMagnitude));
        int n = Mathf.Min(10, lejanos.Count);
        for (int intento = 0; intento < 5 && n > 0; intento++)
        {
            Vector2 meta = lejanos[Random.Range(0, n)];
            List<Vector2> r = BuscadorRutas.Buscar(mapa, transform.position, meta);
            if (r == null || r.Count == 0) continue;
            ruta = r;
            indiceRuta = 0;
            estado = Estado.Huyendo;
            return;
        }
    }

    private void NuevoPaseo()
    {
        for (int intento = 0; intento < 6; intento++)
        {
            Vector2 meta = zona[Random.Range(0, zona.Count)];
            if (Vector2.Distance(meta, transform.position) < 2f) continue;
            List<Vector2> r = BuscadorRutas.Buscar(mapa, transform.position, meta);
            if (r == null || r.Count == 0) continue;
            ruta = r;
            indiceRuta = 0;
            estado = Estado.Caminando;
            return;
        }
        finEspera = Time.time + 1.5f;
    }

    private void Avanzar(float rapidez)
    {
        if (ruta == null || indiceRuta >= ruta.Count)
        {
            estado = Estado.Quieto;
            finEspera = Time.time + Random.Range(1.2f, 3.5f);
            return;
        }

        Vector2 objetivo = ruta[indiceRuta];
        Vector2 actual = transform.position;
        Vector2 paso = Vector2.MoveTowards(actual, objetivo, rapidez * Time.deltaTime);
        if (Mathf.Abs(paso.x - actual.x) > 0.001f) mirandoIzquierda = paso.x < actual.x;
        transform.position = new Vector3(paso.x, paso.y, 0f);
        if ((objetivo - paso).sqrMagnitude < 0.0004f) indiceRuta++;
    }

    // ------------------------------------------------------------ dibujo
    private void Animar()
    {
        bool andando = estado != Estado.Quieto;
        fase += Time.deltaTime * (estado == Estado.Huyendo ? 16f : 9f);

        if (cuadros != null)
        {
            if (andando) animador.Reproducir(cuadros, cuadrosPorSegundo * (estado == Estado.Huyendo ? 1.8f : 1f), true, false);
            else animador.MostrarCuadroFijo(cuadros, 0);
            animador.Actualizar(Time.deltaTime);
            cuerpo.flipX = mirandoIzquierda;

            Sprite s = cuerpo.sprite;
            if (s != null && s.bounds.size.y > 0.0001f)
            {
                float e = alturaDibujo / s.bounds.size.y;
                cuerpo.transform.localScale = new Vector3(e, e, 1f);
                float signo = mirandoIzquierda ? -1f : 1f;
                cuerpo.transform.localPosition = new Vector3(-s.bounds.center.x * e * signo, -s.bounds.min.y * e - 0.3f, 0f);
            }
            return;
        }

        // Figura provisional: salta un poco al caminar.
        float salto = andando ? Mathf.Abs(Mathf.Sin(fase)) * 0.07f : 0f;
        cuerpo.transform.localPosition = new Vector3(0f, 0.12f + salto, 0f);
        cabeza.transform.localPosition = new Vector3(0f, 0.6f + salto, 0f);
        if (gorro != null) gorro.transform.localPosition = new Vector3(0f, 0.86f + salto, 0f);
        cuerpo.color = estado == Estado.Huyendo ? Color.Lerp(cuerpo.color, new Color(1f, 0.8f, 0.5f), 0.2f)
                                                 : Color.Lerp(cuerpo.color, laboratorio == 0 ? new Color(0.95f, 0.95f, 0.92f) : new Color(0.35f, 0.75f, 0.85f), 0.2f);
    }
}
