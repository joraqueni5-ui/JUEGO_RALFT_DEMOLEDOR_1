using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Escombro que suelta el demoledor al golpear la cornisa. Cae recto por su columna
/// y va cruzando ventanas y repisas de la sección actual. En cada una puede impactar
/// (según su probabilidad): la daña, se achica y le baja la probabilidad al siguiente
/// impacto. Después del último impacto posible se deshace. Si le cae al muñeco,
/// le quita una vida.
/// </summary>
public class EscombroCayendo : MonoBehaviour
{
    private static readonly List<EscombroCayendo> activos = new List<EscombroCayendo>();

    /// <summary>Ventana o repisa que el escombro va a cruzar, a una altura dada.</summary>
    private struct Obstaculo
    {
        public float altura;
        public VentanaEdificio ventana;
        public RepisaEdificio repisa;
    }

    private SpriteRenderer dibujo;
    private Sprite sprite;
    private EdificioAlto edificio;
    private Muneco muneco;
    private readonly List<Obstaculo> camino = new List<Obstaculo>();
    private float[] probabilidades;
    private int siguiente;
    private int impactos;
    private float velocidad;
    private float aceleracion = 4f;
    private float giro;

    // Caja del muñeco (desde sus pies) para saber si le cae encima.
    private const float MedioAnchoMuneco = 0.24f;
    private const float AltoMuneco = 0.85f;
    private const float RadioEscombro = 0.1f;
    // Cuánto se achica en cada impacto.
    private const float Encogimiento = 0.72f;

    /// <summary>
    /// Crea un escombro que cae por la columna indicada. 'probabilidades' tiene la
    /// probabilidad de cada impacto (por ejemplo 0.5, 0.35, 0.2: hasta 3 impactos).
    /// </summary>
    public static EscombroCayendo Crear(Sprite sprite, Vector3 posicion, int columna, float velocidadInicial,
        float[] probabilidades, int orden)
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
        e.giro = Random.Range(-260f, 260f);
        e.probabilidades = probabilidades != null && probabilidades.Length > 0 ? probabilidades : new float[] { 0.5f };
        e.edificio = EdificioAlto.Instancia;
        e.muneco = Object.FindFirstObjectByType<Muneco>();
        e.ArmarCamino(columna);
        return e;
    }

    /// <summary>Borra todos los escombros que estén cayendo (al reiniciar la partida).</summary>
    public static void EliminarTodos()
    {
        for (int i = activos.Count - 1; i >= 0; i--)
        {
            if (activos[i] != null) Destroy(activos[i].gameObject);
        }
        activos.Clear();
    }

    private void OnEnable() { activos.Add(this); }

    private void OnDisable() { activos.Remove(this); }

    /// <summary>Ventanas y repisas de la columna en la sección actual, de arriba hacia abajo.</summary>
    private void ArmarCamino(int columna)
    {
        camino.Clear();
        siguiente = 0;
        if (edificio == null || edificio.Terminado) return;

        int primero = edificio.SeccionActual * edificio.pisosPorSeccion;
        float y = transform.position.y;
        for (int p = primero + edificio.pisosPorSeccion - 1; p >= primero; p--)
        {
            VentanaEdificio v = edificio.VentanaEn(p, columna);
            if (v != null && v.Centro.y < y) camino.Add(new Obstaculo { altura = v.Centro.y, ventana = v });

            RepisaEdificio r = edificio.RepisaEn(p, columna);
            if (r != null && r.Superficie < y) camino.Add(new Obstaculo { altura = r.Superficie, repisa = r });
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
            float radio = RadioEscombro * transform.localScale.x;
            if (Mathf.Abs(p.x - pies.x) < MedioAnchoMuneco + radio &&
                p.y > pies.y - radio && p.y < pies.y + AltoMuneco + radio)
            {
                muneco.RecibirGolpe();
                Romperse(3);
                return;
            }
        }

        // Ventanas y repisas que acaba de cruzar.
        while (siguiente < camino.Count && p.y <= camino[siguiente].altura)
        {
            Obstaculo o = camino[siguiente];
            siguiente++;
            if (IntentarImpacto(o)) return;   // ya se deshizo
        }

        // Llegó a la calle.
        if (p.y <= 0.05f) Romperse(3);
    }

    /// <summary>Tira el dado de impacto. Devuelve true si el escombro se deshizo.</summary>
    private bool IntentarImpacto(Obstaculo o)
    {
        if (edificio == null || impactos >= probabilidades.Length) return false;

        // Solo se dañan cosas de la sección donde está el demoledor.
        int seccion = o.ventana != null ? o.ventana.seccion : (o.repisa != null ? o.repisa.seccion : -1);
        if (seccion != edificio.SeccionActual) return false;

        bool sePuedeDanar = o.ventana != null
            ? o.ventana.Estado != EstadoVentana.Destruida
            : (o.repisa != null && o.repisa.Estado != EstadoRepisa.Destruida);
        if (!sePuedeDanar) return false;

        if (Random.value >= probabilidades[impactos]) return false;

        if (o.ventana != null) o.ventana.Danar();
        else o.repisa.Danar();

        impactos++;
        Escombro.Crear(sprite, transform.position, dibujo != null ? dibujo.sortingOrder : 6);

        if (impactos >= probabilidades.Length)
        {
            Romperse(2);
            return true;
        }

        transform.localScale *= Encogimiento;
        return false;
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
