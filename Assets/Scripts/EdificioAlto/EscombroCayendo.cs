using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Escombro que suelta el demoledor al golpear la cornisa. Cae recto por su columna:
/// al crearse se decide al azar si romperá alguna ventana de la sección (cada ventana
/// que cruza tiene una probabilidad); si la rompe desaparece ahí, si no sigue cayendo.
/// Si en su caída toca al muñeco, le quita una vida.
/// </summary>
public class EscombroCayendo : MonoBehaviour
{
    private static readonly List<EscombroCayendo> activos = new List<EscombroCayendo>();

    private SpriteRenderer dibujo;
    private Sprite sprite;
    private VentanaEdificio objetivo;
    private EdificioAlto edificio;
    private Muneco muneco;
    private float velocidad;
    private float aceleracion;
    private float giro;
    private bool contado;

    // Caja del muñeco (desde sus pies) para saber si le cae encima.
    private const float MedioAnchoMuneco = 0.24f;
    private const float AltoMuneco = 0.85f;
    private const float RadioEscombro = 0.1f;

    /// <summary>
    /// Crea un escombro en 'posicion' que cae por la columna indicada de la sección actual.
    /// </summary>
    public static EscombroCayendo Crear(Sprite sprite, Vector3 posicion, int columna, float velocidadInicial,
        float probabilidadRomper, int orden)
    {
        GameObject objeto = new GameObject("EscombroCayendo");
        objeto.transform.position = posicion;
        objeto.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        SpriteRenderer sr = objeto.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = orden;

        EscombroCayendo e = objeto.AddComponent<EscombroCayendo>();
        e.dibujo = sr;
        e.sprite = sprite;
        e.velocidad = velocidadInicial;
        e.aceleracion = 4f;
        e.giro = Random.Range(-260f, 260f);
        e.edificio = EdificioAlto.Instancia;
        e.muneco = Object.FindFirstObjectByType<Muneco>();
        e.ElegirObjetivo(columna, probabilidadRomper);

        if (e.edificio != null)
        {
            e.edificio.EscombrosEnElAire++;
            e.contado = true;
        }
        return e;
    }

    /// <summary>Borra todos los escombros que estén cayendo (al reiniciar la partida).</summary>
    public static void EliminarTodos()
    {
        for (int i = activos.Count - 1; i >= 0; i--)
        {
            if (activos[i] != null)
            {
                activos[i].contado = false;
                Destroy(activos[i].gameObject);
            }
        }
        activos.Clear();
    }

    private void OnEnable() { activos.Add(this); }

    private void OnDisable() { activos.Remove(this); }

    private void OnDestroy()
    {
        if (contado && edificio != null) edificio.EscombrosEnElAire = Mathf.Max(0, edificio.EscombrosEnElAire - 1);
        contado = false;
    }

    private void ElegirObjetivo(int columna, float probabilidad)
    {
        objetivo = null;
        if (edificio == null || edificio.Terminado) return;

        List<VentanaEdificio> camino = edificio.ColumnaDeSeccion(edificio.SeccionActual, columna);
        foreach (VentanaEdificio v in camino)
        {
            if (v == null || v.Estado == EstadoVentana.Destruida) continue;
            if (v.Centro.y > transform.position.y) continue;
            if (Random.value < probabilidad)
            {
                objetivo = v;
                return;
            }
        }
    }

    private void Update()
    {
        velocidad += aceleracion * Time.deltaTime;
        Vector3 p = transform.position;
        p.y -= velocidad * Time.deltaTime;
        transform.position = p;
        transform.Rotate(0f, 0f, giro * Time.deltaTime);

        // ¿Le cayó al muñeco?
        if (muneco != null && muneco.isActiveAndEnabled && muneco.PuedeRecibirGolpe)
        {
            Vector2 pies = muneco.transform.position;
            if (Mathf.Abs(p.x - pies.x) < MedioAnchoMuneco + RadioEscombro &&
                p.y > pies.y - RadioEscombro && p.y < pies.y + AltoMuneco + RadioEscombro)
            {
                muneco.RecibirGolpe();
                Romperse(3);
                return;
            }
        }

        // ¿Llegó a la ventana que va a romper?
        if (objetivo != null && p.y <= objetivo.Centro.y)
        {
            objetivo.Danar();
            Romperse(2);
            return;
        }

        // Llegó a la calle.
        if (p.y <= 0.05f)
        {
            Romperse(3);
        }
    }

    private void Romperse(int trozos)
    {
        for (int i = 0; i < trozos; i++)
        {
            Escombro.Crear(sprite, transform.position, dibujo != null ? dibujo.sortingOrder : 6);
        }
        Destroy(gameObject);
    }
}
