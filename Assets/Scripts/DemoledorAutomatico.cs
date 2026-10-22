using System.Collections;
using UnityEngine;

/// <summary>
/// El demoledor (el "Ralph"). NO se controla: él solo recorre el edificio
/// piso por piso y rompe los bloques de uno en uno, de arriba hacia abajo.
/// El jugador solo puede reparar lo que él va rompiendo.
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
    [Tooltip("Unidades por segundo al caminar sobre la fachada.")]
    public float velocidad = 1.6f;
    [Tooltip("Desde que empieza el golpe hasta que rompe el bloque.")]
    public float momentoDeImpacto = 0.16f;
    [Tooltip("Pausa después de cada golpe (para que se note el bloque a bloque).")]
    public float pausaTrasGolpe = 0.18f;
    [Tooltip("Pausa al terminar un piso antes de bajar/suibir.")]
    public float esperaEntrePisos = 0.35f;
    [Tooltip("Lo que tarda en pasar de un piso al siguiente.")]
    public float tiempoCambioPiso = 0.4f;
    [Tooltip("A qué lado del bloque se para para pegarle.")]
    public float separacionAlGolpear = 0.38f;

    private AnimadorSprites animador;
    private SpriteRenderer dibujo;
    private Edificio edificio;
    private bool mirandoDerecha = true;
    private bool activo = true;
    private float duracionGolpe = 0.43f;

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
        edificio = Edificio.Instancia;
        StartCoroutine(RutinaDemoler());
    }

    /// <summary>Lo usa el gestor cuando la partida termina.</summary>
    public void Detener()
    {
        activo = false;
        StopAllCoroutines();
    }

    /// <summary>Vuelve a empezar desde el último piso.</summary>
    public void Reiniciar()
    {
        StopAllCoroutines();
        activo = true;
        edificio = Edificio.Instancia;
        StartCoroutine(RutinaDemoler());
    }

    // ------------------------------------------------------------ rutina
    private IEnumerator RutinaDemoler()
    {
        while (edificio == null) edificio = Edificio.Instancia;

        // Empieza arriba, en la primera columna.
        int fila = edificio.Filas - 1;
        int sentidoHorizontal = 1;
        int sentidoVertical = -1;

        ColocarEnPiso(fila, sentidoHorizontal);

        while (activo)
        {
            yield return BarrerPiso(fila, sentidoHorizontal);

            int siguiente = fila + sentidoVertical;
            if (siguiente < 0)
            {
                sentidoVertical = 1;
                siguiente = Mathf.Min(1, edificio.Filas - 1);
            }
            else if (siguiente > edificio.Filas - 1)
            {
                sentidoVertical = -1;
                siguiente = Mathf.Max(0, edificio.Filas - 2);
            }

            if (siguiente != fila)
            {
                yield return CambiarPiso(siguiente);
                fila = siguiente;
                sentidoHorizontal = -sentidoHorizontal;
            }

            yield return new WaitForSeconds(esperaEntrePisos);
        }
    }

    private IEnumerator BarrerPiso(int fila, int sentido)
    {
        int columnas = edificio.Columnas;

        for (int paso = 0; paso < columnas && activo; paso++)
        {
            int columna = sentido > 0 ? paso : columnas - 1 - paso;
            BloqueEdificio bloque = edificio.BloqueEn(fila, columna);
            if (bloque == null) continue;              // celda vacía (por ejemplo el cielo sobre el techo)

            float destinoX = bloque.Area.center.x - sentido * separacionAlGolpear;
            yield return CaminarHasta(destinoX, sentido);

            while (activo && bloque != null && bloque.Estado != EstadoBloque.Destruido)
            {
                yield return Golpe(bloque);
            }
        }
    }

    private IEnumerator CaminarHasta(float destinoX, int sentido)
    {
        mirandoDerecha = sentido > 0;

        while (activo && Mathf.Abs(transform.position.x - destinoX) > 0.02f)
        {
            Vector3 posicion = transform.position;
            posicion.x = Mathf.MoveTowards(posicion.x, destinoX, velocidad * Time.deltaTime);
            transform.position = posicion;

            animador.Reproducir(mirandoDerecha ? caminarDerecha : caminarIzquierda, cuadrosCaminar, true);
            yield return null;
        }

        animador.MostrarCuadroFijo(mirandoDerecha ? caminarDerecha : caminarIzquierda, 0);
    }

    private IEnumerator Golpe(BloqueEdificio bloque)
    {
        if (dibujo != null) dibujo.flipX = animacionGolpeMiraDerecha != mirandoDerecha;
        animador.Reproducir(golpe, cuadrosPorSegundoGolpe, false, true);

        yield return new WaitForSeconds(momentoDeImpacto);

        if (activo && bloque != null) bloque.RecibirGolpe(1);

        float esperaGolpe = Mathf.Max(0.05f, duracionGolpe - momentoDeImpacto) + pausaTrasGolpe;
        yield return new WaitForSeconds(esperaGolpe);

        if (dibujo != null) dibujo.flipX = false;
    }

    private IEnumerator CambiarPiso(int filaDestino)
    {
        Vector3 inicio = transform.position;
        float destinoY = AlturaDePiso(filaDestino);
        float t = 0f;

        animador.Reproducir(salto, 8f, true);

        while (activo && t < tiempoCambioPiso)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, tiempoCambioPiso));
            Vector3 posicion = inicio;
            posicion.y = Mathf.Lerp(inicio.y, destinoY, k * k);
            transform.position = posicion;
            yield return null;
        }

        Vector3 final = transform.position;
        final.y = destinoY;
        transform.position = final;
    }

    // ------------------------------------------------------------ posición
    /// <summary>Altura de los pies para romper los bloques de esa fila.</summary>
    private float AlturaDePiso(int fila)
    {
        float baseFila = edificio.AlturaPiso(fila);
        // Se para "del piso de abajo" para que el bloque le quede a la altura del pecho,
        // pero nunca por debajo del suelo.
        return Mathf.Max(0f, baseFila - 0.5f);
    }

    private void ColocarEnPiso(int fila, int sentido)
    {
        float x = edificio.CentroColumna(sentido > 0 ? 0 : edificio.Columnas - 1) - sentido * separacionAlGolpear;
        transform.position = new Vector3(x, AlturaDePiso(fila), transform.position.z);
    }
}
