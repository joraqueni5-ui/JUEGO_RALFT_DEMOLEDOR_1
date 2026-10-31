using System.Collections;
using UnityEngine;

/// <summary>
/// El demoledor (el "Ralph"). NO se controla: se para en la cornisa de la sección
/// actual del edificio alto, camina de columna en columna y golpea el piso sin parar.
/// Cada golpe suelta escombros que caen por esa columna y pueden romper ventanas y
/// repisas. Cuando la sección queda reparada sube a la siguiente cornisa; al final
/// se queda en la terraza del techo.
/// </summary>
public class DemoledorAutomatico : MonoBehaviour
{
    [Header("Animaciones")]
    public Sprite[] caminarIzquierda;
    public Sprite[] caminarDerecha;
    public Sprite[] salto;
    public Sprite[] golpe;
    [Tooltip("El dibujo del golpe está hecho mirando a la derecha.")]
    public bool animacionGolpeMiraDerecha = true;
    public float cuadrosCaminar = 8f;
    public float cuadrosPorSegundoGolpe = 14f;

    [Header("Ritmo del demoledor")]
    [Tooltip("Unidades por segundo al caminar sobre la cornisa.")]
    public float velocidad = 1.6f;
    [Tooltip("Desde que empieza el golpe hasta que caen los escombros.")]
    public float momentoDeImpacto = 0.16f;
    [Tooltip("Pausa después de cada golpe.")]
    public float pausaTrasGolpe = 0.18f;
    [Tooltip("Pausa antes de elegir la siguiente columna.")]
    public float esperaEntrePisos = 0.35f;
    [Tooltip("Lo que tarda en subir de una cornisa a la siguiente.")]
    public float tiempoCambioPiso = 0.9f;
    [Tooltip("No se usa con el edificio alto (se conserva por compatibilidad).")]
    public float separacionAlGolpear = 0.38f;

    [Header("Escombros")]
    public Sprite spriteEscombro;
    [Tooltip("Escombros que suelta cada golpe.")]
    public int escombrosPorGolpe = 1;
    [Tooltip("Velocidad con la que empiezan a caer.")]
    public float velocidadEscombro = 2.2f;
    [Tooltip("Probabilidad de cada impacto del escombro en ventanas o repisas (hasta 3 impactos).")]
    public float[] probabilidadesImpacto = { 0.5f, 0.35f, 0.2f };
    public int ordenEscombro = 6;

    [Header("Temblor de pantalla")]
    [Tooltip("Fuerza del temblor en cada golpe al piso (0 = sin temblor).")]
    public float temblorGolpe = 0.05f;
    [Tooltip("Fuerza del temblor al caer en una cornisa nueva.")]
    public float temblorAterrizaje = 0.12f;
    public float duracionTemblor = 0.18f;

    [Header("Dificultad")]
    [Tooltip("Cuánto más rápido va en cada sección nueva (0.3 = 30 % más).")]
    public float aumentoPorSeccion = 0.3f;

    private AnimadorSprites animador;
    private SpriteRenderer dibujo;
    private EdificioAlto edificio;
    private bool mirandoDerecha = true;
    private bool activo = true;
    private float duracionGolpe = 0.43f;
    private int seccion;
    private int columnaActual;

    private float Ritmo { get { return 1f + aumentoPorSeccion * Mathf.Max(0, seccion); } }

    private void Awake()
    {
        dibujo = GetComponentInChildren<SpriteRenderer>();
        animador = new AnimadorSprites(dibujo);

        if (golpe != null && golpe.Length > 0)
        {
            duracionGolpe = golpe.Length / Mathf.Max(1f, cuadrosPorSegundoGolpe);
        }
    }

    private void Start()
    {
        edificio = EdificioAlto.Instancia;
        StartCoroutine(RutinaDemoler());
    }

    private void Update()
    {
        animador.Actualizar(Time.deltaTime);
    }

    /// <summary>Lo usa el gestor cuando la partida termina.</summary>
    public void Detener()
    {
        activo = false;
        StopAllCoroutines();
        animador.MostrarCuadroFijo(caminarDerecha, 0);
        if (dibujo != null) dibujo.flipX = false;
    }

    /// <summary>Vuelve a empezar desde la primera sección.</summary>
    public void Reiniciar()
    {
        StopAllCoroutines();
        activo = true;
        if (dibujo != null) dibujo.flipX = false;
        edificio = EdificioAlto.Instancia;
        StartCoroutine(RutinaDemoler());
    }

    // ------------------------------------------------------------ rutina
    private IEnumerator RutinaDemoler()
    {
        while (edificio == null)
        {
            edificio = EdificioAlto.Instancia;
            yield return null;
        }

        seccion = Mathf.Min(edificio.SeccionActual, edificio.secciones - 1);
        columnaActual = 0;
        transform.position = new Vector3(edificio.CentroColumna(columnaActual), edificio.AlturaCornisa(seccion), transform.position.z);
        animador.MostrarCuadroFijo(caminarDerecha, 0);

        while (activo)
        {
            // ¿Se completó la sección? Sube (o, si era la última, va a la terraza).
            if (edificio.SeccionActual != seccion)
            {
                if (edificio.Terminado)
                {
                    yield return IrALaTerraza();
                    yield break;
                }

                yield return Subir(edificio.SeccionActual);
                continue;
            }

            // No para nunca: elige otra columna, camina y golpea.
            int destino = ElegirColumna();
            yield return CaminarHasta(edificio.CentroColumna(destino));
            columnaActual = destino;

            if (activo && edificio.SeccionActual == seccion)
            {
                yield return Golpe(true);
            }
            yield return new WaitForSeconds(esperaEntrePisos / Ritmo);
        }
    }

    private int ElegirColumna()
    {
        int columnas = Mathf.Max(1, edificio.columnas);
        if (columnas == 1) return 0;

        int destino = Random.Range(0, columnas - 1);
        if (destino >= columnaActual) destino++;   // nunca la misma columna dos veces seguidas
        return destino;
    }

    private IEnumerator CaminarHasta(float destinoX)
    {
        if (dibujo != null) dibujo.flipX = false;
        mirandoDerecha = destinoX >= transform.position.x;
        Sprite[] serie = mirandoDerecha ? caminarDerecha : caminarIzquierda;
        animador.Reproducir(serie, cuadrosCaminar * Ritmo, true);

        while (activo && Mathf.Abs(transform.position.x - destinoX) > 0.02f)
        {
            Vector3 posicion = transform.position;
            posicion.x = Mathf.MoveTowards(posicion.x, destinoX, velocidad * Ritmo * Time.deltaTime);
            transform.position = posicion;
            yield return null;
        }

        animador.MostrarCuadroFijo(serie, 0);
    }

    private IEnumerator Golpe(bool conEscombros)
    {
        if (dibujo != null) dibujo.flipX = animacionGolpeMiraDerecha != mirandoDerecha;
        animador.Reproducir(golpe, cuadrosPorSegundoGolpe, false, true);

        yield return new WaitForSeconds(momentoDeImpacto);

        if (activo && conEscombros && edificio != null && edificio.SeccionActual == seccion)
        {
            edificio.RegistrarGolpe();
            SoltarEscombros();
        }
        if (activo && temblorGolpe > 0f) CamaraSeguimiento.Temblar(temblorGolpe, duracionTemblor);

        float esperaGolpe = Mathf.Max(0.05f, duracionGolpe - momentoDeImpacto) + pausaTrasGolpe / Ritmo;
        yield return new WaitForSeconds(esperaGolpe);

        if (dibujo != null) dibujo.flipX = false;
    }

    private void SoltarEscombros()
    {
        if (spriteEscombro == null || edificio == null) return;

        for (int i = 0; i < Mathf.Max(1, escombrosPorGolpe); i++)
        {
            Vector3 origen = new Vector3(
                edificio.CentroColumna(columnaActual) + Random.Range(-0.15f, 0.15f),
                edificio.AlturaCornisa(seccion) - 0.12f,
                0f);
            EscombroCayendo.Crear(spriteEscombro, origen, columnaActual, velocidadEscombro * Ritmo,
                probabilidadesImpacto, ordenEscombro);
        }
    }

    private IEnumerator Subir(int nuevaSeccion)
    {
        Vector3 inicio = transform.position;
        float destinoY = edificio.AlturaCornisa(nuevaSeccion);
        float t = 0f;

        if (dibujo != null) dibujo.flipX = false;
        animador.Reproducir(salto, 8f, true);

        while (activo && t < tiempoCambioPiso)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, tiempoCambioPiso));
            Vector3 posicion = inicio;
            // Sube con un pequeño arco para que se note el salto.
            posicion.y = Mathf.Lerp(inicio.y, destinoY, k) + Mathf.Sin(k * Mathf.PI) * 0.6f;
            transform.position = posicion;
            yield return null;
        }

        transform.position = new Vector3(inicio.x, destinoY, inicio.z);
        seccion = nuevaSeccion;
        if (temblorAterrizaje > 0f) CamaraSeguimiento.Temblar(temblorAterrizaje, duracionTemblor * 1.6f);
        animador.MostrarCuadroFijo(caminarDerecha, 0);
        yield return new WaitForSeconds(0.4f);
    }

    private IEnumerator IrALaTerraza()
    {
        seccion = edificio.secciones - 1;
        yield return CaminarHasta(edificio.CentroTerraza);

        // Se queda en la terraza golpeando el piso, enojado.
        while (activo)
        {
            yield return Golpe(false);
            yield return new WaitForSeconds(0.5f);
        }
    }
}
