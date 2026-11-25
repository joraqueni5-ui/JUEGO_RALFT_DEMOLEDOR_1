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
    private Vector3 escalaDibujo = Vector3.one;

    /// <summary>Ya llegó a la terraza del techo (después de completar la última sección).</summary>
    public bool EnTerraza { get; private set; }

    private float Ritmo
    {
        get
        {
            // Modo de pisos: la dificultad viene de la definición del piso.
            EdificioPisos pisos = edificio as EdificioPisos;
            if (pisos != null) return pisos.RitmoDemoledor;
            float porDefecto = 1f + aumentoPorSeccion * Mathf.Max(0, seccion);
            return edificio != null ? edificio.RitmoDeSeccion(seccion, porDefecto) : porDefecto;
        }
    }

    private void Awake()
    {
        dibujo = GetComponentInChildren<SpriteRenderer>();
        animador = new AnimadorSprites(dibujo);
        if (dibujo != null) escalaDibujo = dibujo.transform.localScale;

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
        RestaurarEscala();
        animador.MostrarCuadroFijo(caminarDerecha, 0);
        if (dibujo != null) dibujo.flipX = false;
    }

    private bool conservarPosicion;

    /// <summary>Se detiene un momento (sin terminar la partida): por ejemplo durante el terremoto.</summary>
    public void Pausar()
    {
        StopAllCoroutines();
        RestaurarEscala();
        if (animador != null) animador.MostrarCuadroFijo(caminarDerecha, 0);
        if (dibujo != null) dibujo.flipX = false;
    }

    /// <summary>Sigue atacando desde donde estaba (después de Pausar).</summary>
    public void Reanudar()
    {
        StopAllCoroutines();
        RestaurarEscala();
        activo = true;
        conservarPosicion = true;
        StartCoroutine(RutinaDemoler());
    }

    /// <summary>Golpea el techo con los dos puños durante 'segundos' (animación simple: el golpe en bucle y pequeños saltos).</summary>
    public IEnumerator GolpeDoble(float segundos)
    {
        Vector3 baseP = transform.position;
        if (dibujo != null) dibujo.flipX = animacionGolpeMiraDerecha != true;
        animador.Reproducir(golpe, cuadrosPorSegundoGolpe * 1.4f, true, true);

        float t = 0f;
        while (t < segundos)
        {
            t += Time.deltaTime;
            transform.position = baseP + Vector3.up * Mathf.Abs(Mathf.Sin(t * 10f)) * 0.12f;
            yield return null;
        }
        transform.position = baseP;
        animador.MostrarCuadroFijo(caminarDerecha, 0);
        if (dibujo != null) dibujo.flipX = false;
    }

    private Coroutine aplastando;

    /// <summary>
    /// Un golpe contra el suelo (el techo, visto desde el muñeco) con los dos puños: animación de golpe, aplastamiento rápido del sprite,
    /// temblor corto de cámara, polvo a sus pies y el sonido del golpe. Úsalo después de Pausar().
    /// </summary>
    public void GolpeAlTecho(float aplastamiento = 0.2f)
    {
        if (dibujo != null) dibujo.flipX = animacionGolpeMiraDerecha != true;
        animador.Reproducir(golpe, cuadrosPorSegundoGolpe * 1.4f, false, true);
        CamaraSeguimiento.Temblar(0.08f, 0.12f);
        EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), transform.position + Vector3.up * 0.05f, 399, 1f, 12f);
        GestorAudio.Reproducir(Snd.GolpeObjeto);
        if (aplastando != null) StopCoroutine(aplastando);
        aplastando = StartCoroutine(Aplastar(aplastamiento));
    }

    private IEnumerator Aplastar(float cantidad)
    {
        if (dibujo == null) yield break;
        Vector3 b = escalaDibujo;
        float t = 0f;
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / 0.18f);
            dibujo.transform.localScale = new Vector3(b.x * (1f + cantidad * 0.6f * k), b.y * (1f - cantidad * k), b.z);
            yield return null;
        }
        dibujo.transform.localScale = b;
        aplastando = null;
    }

    private void RestaurarEscala()
    {
        aplastando = null;
        if (dibujo != null) dibujo.transform.localScale = escalaDibujo;
    }

    /// <summary>Deja el sprite y la animación en reposo (termina la serie de golpes).</summary>
    public void TerminarGolpes()
    {
        if (aplastando != null) { StopCoroutine(aplastando); aplastando = null; }
        if (dibujo != null) { dibujo.transform.localScale = escalaDibujo; dibujo.flipX = false; }
        animador.MostrarCuadroFijo(caminarDerecha, 0);
    }

    /// <summary>Vuelve a empezar desde la primera sección.</summary>
    public void Reiniciar()
    {
        StopAllCoroutines();
        RestaurarEscala();
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

        // Después de una pausa (terremoto, ulti, transformación) sigue donde estaba y conserva la sección
        // en la que está parado: si mientras tanto se completó la sección, el bucle lo ve y sube a la nueva.
        if (conservarPosicion) conservarPosicion = false;
        else
        {
            seccion = Mathf.Min(edificio.SeccionActual, edificio.secciones - 1);
            columnaActual = 0;
            transform.position = new Vector3(edificio.CentroColumna(columnaActual), edificio.AlturaCornisa(seccion), transform.position.z);
        }
        animador.MostrarCuadroFijo(caminarDerecha, 0);

        while (activo)
        {
            // Modo de pisos: al completar el último piso no hay terraza, el demoledor se detiene.
            if (edificio is EdificioPisos && edificio.Terminado)
            {
                animador.MostrarCuadroFijo(caminarDerecha, 0);
                yield break;
            }

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

            // Si algo lo dejó fuera de su cornisa (una pausa a mitad del salto, una escena), vuelve a ella.
            if (Mathf.Abs(transform.position.y - edificio.AlturaCornisa(seccion)) > 0.25f)
            {
                yield return Subir(seccion);
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
        GestorAudio.Reproducir(Snd.Punetazo);

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

        EdificioPisos pisos = edificio as EdificioPisos;
        int cantidad = pisos != null ? pisos.EscombrosPorGolpe : edificio.EscombrosDeSeccion(seccion, escombrosPorGolpe);
        for (int i = 0; i < Mathf.Max(1, cantidad); i++)
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
        EnTerraza = true;

        // Se queda en la terraza golpeando el piso, enojado.
        while (activo)
        {
            yield return Golpe(false);
            yield return new WaitForSeconds(0.5f);
        }
    }
}
