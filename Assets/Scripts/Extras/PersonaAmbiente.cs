using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Una persona del piso (estudiante o personal) para la escena de llegada: camina tranquila entre celdas al azar y,
/// cuando llega el demoledor, grita y corre hacia la salida, desvaneciéndose. La crea IntroNivel.
/// </summary>
public class PersonaAmbiente : MonoBehaviour
{
    private PisoCenital piso;
    private SpriteRenderer dibujo, sombra;
    private Sprite[] abajo, arriba, derecha, abajoSusto, derechaSusto;
    private List<Vector2> ruta;
    private int indice;
    private Vector2 direccion = Vector2.down;
    private float velocidad, proximaDecision, reloj, inicioHuida = -1f, alfa = 1f;
    private bool huyendo;
    private const float Altura = 1.05f;

    public static PersonaAmbiente Crear(Personajes.Ficha f, Vector2 posicion, PisoCenital piso)
    {
        Sprite[] ab = Personajes.Serie(f, "abajo"), ar = Personajes.Serie(f, "arriba"), de = Personajes.Serie(f, "derecha");
        if (ab == null || ar == null || de == null) return null;
        GameObject g = new GameObject("Persona " + f.nombre);
        g.transform.position = new Vector3(posicion.x, posicion.y, 0f);
        PersonaAmbiente p = g.AddComponent<PersonaAmbiente>();
        p.piso = piso;
        p.abajo = ab; p.arriba = ar; p.derecha = de;
        p.abajoSusto = ArteExtra.Serie("Personajes/" + f.id + "/abajo_susto_", 4);
        p.derechaSusto = ArteExtra.Serie("Personajes/" + f.id + "/derecha_susto_", 4);
        p.velocidad = Random.Range(1.1f, 1.7f);
        p.sombra = Provisional.Crear(g.transform, "Sombra", Provisional.Circulo, new Color(0f, 0f, 0f, 0.28f), 0);
        p.sombra.transform.localScale = new Vector3(0.55f, 0.2f, 1f);
        p.dibujo = Provisional.Crear(g.transform, "Dibujo", ab[0], Color.white, 0);
        p.proximaDecision = Time.time + Random.Range(0f, 2f);
        p.reloj = Random.value;
        return p;
    }

    /// <summary>Grita y sale corriendo hacia 'escape', alejándose del demoledor que está en 'desde'.</summary>
    public void Huir(Vector2 desde, Vector2 escape)
    {
        if (huyendo) return;
        huyendo = true;
        inicioHuida = Time.time + Random.Range(0f, 0.35f);   // no reaccionan todos a la vez
        velocidad = Random.Range(3.6f, 4.6f);
        ruta = BuscadorRutas.Buscar(piso.Mapa, transform.position, escape);
        if (ruta == null)
        {
            Vector2 lejos = (Vector2)transform.position + ((Vector2)transform.position - desde).normalized * 6f;
            ruta = new List<Vector2> { lejos };
        }
        indice = 0;
        if (Random.value < 0.45f)
        {
            string[] gritos = { "¡AAAH!", "¡CORRAN!", "¡EL DEMOLEDOR!", "¡AUXILIO!", "¡SALGAN!", "¡AY NO!", "¡MI TESIS!", "¡SÁLVESE QUIEN PUEDA!" };
            Burbuja.Decir(transform, gritos[Random.Range(0, gritos.Length)], Burbuja.Voz.Ninguna, Altura + 0.2f);
        }
    }

    private void Update()
    {
        if (piso == null || piso.Mapa == null) { Destroy(gameObject); return; }

        bool moviendo = false;
        if (huyendo)
        {
            if (Time.time >= inicioHuida)
            {
                moviendo = Seguir(velocidad, false);
                // Se va: desaparece al llegar a la salida o, como máximo, a los 5 s.
                if (!moviendo || Time.time - inicioHuida > 5f) alfa -= Time.deltaTime * 2.5f;
                if (alfa <= 0f) { Destroy(gameObject); return; }
            }
        }
        else
        {
            if (ruta == null || indice >= ruta.Count)
            {
                if (Time.time >= proximaDecision) ElegirPaseo();
            }
            else moviendo = Seguir(velocidad, true);
        }
        Dibujar(moviendo);
    }

    private void ElegirPaseo()
    {
        List<Vector2> celdas = piso.Mapa.CentrosTransitables;
        proximaDecision = Time.time + Random.Range(1f, 3.5f);
        for (int i = 0; i < 6; i++)
        {
            Vector2 c = celdas[Random.Range(0, celdas.Count)];
            if (Vector2.Distance(c, transform.position) > 6f) continue;
            List<Vector2> r = BuscadorRutas.Buscar(piso.Mapa, transform.position, c);
            if (r == null) continue;
            ruta = r;
            indice = 0;
            return;
        }
    }

    /// <summary>Avanza por la ruta. Devuelve true mientras se mueve.</summary>
    private bool Seguir(float rapidez, bool pausar)
    {
        if (ruta == null || indice >= ruta.Count) return false;
        Vector2 d = ruta[indice] - (Vector2)transform.position;
        float paso = rapidez * Time.deltaTime;
        if (d.magnitude <= paso)
        {
            transform.position = new Vector3(ruta[indice].x, ruta[indice].y, 0f);
            indice++;
            if (indice >= ruta.Count && pausar) proximaDecision = Time.time + Random.Range(1f, 4f);
        }
        else
        {
            direccion = d.normalized;
            transform.position += (Vector3)(direccion * paso);
        }
        return true;
    }

    private void Dibujar(bool moviendo)
    {
        bool lado = Mathf.Abs(direccion.x) > Mathf.Abs(direccion.y);
        Sprite[] serie = lado ? derecha : (direccion.y > 0f ? arriba : abajo);
        // Asustados: cara de susto (boca abierta y sudor) mientras huyen.
        if (huyendo && lado && derechaSusto != null) serie = derechaSusto;
        else if (huyendo && !lado && direccion.y <= 0f && abajoSusto != null) serie = abajoSusto;
        reloj += Time.deltaTime * (huyendo ? 12f : 7f);
        dibujo.sprite = moviendo ? serie[(int)reloj % serie.Length] : serie[0];
        dibujo.flipX = Mathf.Abs(direccion.x) > Mathf.Abs(direccion.y) && direccion.x < 0f;

        Bounds b = dibujo.sprite.bounds;
        float e = b.size.y > 0.001f ? Altura / b.size.y : 1f;
        dibujo.transform.localScale = new Vector3(e, e, 1f);
        float signo = dibujo.flipX ? -1f : 1f;
        dibujo.transform.localPosition = new Vector3(-b.center.x * e * signo, -b.min.y * e, 0f);
        int orden = Provisional.OrdenY(transform.position.y);
        dibujo.sortingOrder = orden;
        sombra.sortingOrder = orden - 2;
        Color c = huyendo ? new Color(1f, 0.92f, 0.85f, alfa) : new Color(1f, 1f, 1f, alfa);
        dibujo.color = c;
        sombra.color = new Color(0f, 0f, 0f, 0.28f * alfa);
    }
}
