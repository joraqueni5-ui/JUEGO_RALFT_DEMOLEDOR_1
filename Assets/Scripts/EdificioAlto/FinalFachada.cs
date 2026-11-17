using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Final del nivel final: el demoledor queda quieto en la terraza, aparecen varios estudiantes a su lado, lo
/// levantan y lo lanzan del edificio; cae hasta abajo con un "¡AAAH!" y desaparece en una nube de polvo.
/// Animación simple con figuras provisionales. La lanza GestorJuego cuando se completa la última sección.
/// </summary>
public class FinalFachada : MonoBehaviour
{
    [Header("Estudiantes")]
    public int cantidadEstudiantes = 6;
    public float velocidadEstudiantes = 2.6f;
    [Header("Lanzamiento")]
    public float alturaLevantado = 0.9f;
    public float segundosLevantando = 1.1f;
    [Tooltip("Velocidad horizontal y vertical inicial del lanzamiento (unidades por segundo).")]
    public Vector2 velocidadLanzamiento = new Vector2(2.6f, 4.2f);
    public float gravedad = 14f;
    [Tooltip("Cuánto espera a que el demoledor llegue a la terraza antes de seguir de todos modos.")]
    public float esperaMaximaTerraza = 15f;

    private readonly List<GameObject> estudiantes = new List<GameObject>();

    /// <summary>Corrutina de la escena completa. Termina cuando el demoledor ya cayó y desapareció.</summary>
    public IEnumerator Reproducir(DemoledorAutomatico demoledor, EdificioAlto edificio, CamaraSeguimiento camara)
    {
        if (demoledor == null || edificio == null) yield break;

        // 1) Espera a que llegue a la terraza y lo deja quieto.
        float limite = Time.time + esperaMaximaTerraza;
        while (!demoledor.EnTerraza && Time.time < limite) yield return null;
        demoledor.Detener();

        Transform dem = demoledor.transform;
        float suelo = edificio.AlturaCornisa(edificio.secciones - 1);
        float x0 = edificio.CentroTerraza;
        dem.position = new Vector3(x0, suelo, dem.position.z);
        if (camara != null) { camara.objetivo = dem; camara.demoledor = null; }
        yield return new WaitForSeconds(0.6f);

        // 2) Llegan los estudiantes, de los dos lados, y lo rodean.
        List<float> destinos = new List<float>();
        List<float> salidas = new List<float>();
        for (int i = 0; i < cantidadEstudiantes; i++)
        {
            bool izquierda = i % 2 == 0;
            float paso = 0.38f + (i / 2) * 0.4f;
            destinos.Add(x0 + (izquierda ? -paso : paso));
            salidas.Add(x0 + (izquierda ? -4.2f - i * 0.3f : 4.2f + i * 0.3f));
            estudiantes.Add(CrearEstudiante(new Vector3(salidas[i], suelo, 0f), i));
        }
        bool llegaron = false;
        while (!llegaron)
        {
            llegaron = true;
            for (int i = 0; i < estudiantes.Count; i++)
            {
                Vector3 p = estudiantes[i].transform.position;
                float nuevo = Mathf.MoveTowards(p.x, destinos[i], velocidadEstudiantes * Time.deltaTime);
                estudiantes[i].transform.position = new Vector3(nuevo, p.y, p.z);
                Rebotito(estudiantes[i], Time.time + i);
                if (Mathf.Abs(nuevo - destinos[i]) > 0.01f) llegaron = false;
            }
            yield return null;
        }

        // 3) Lo levantan con los brazos en alto.
        float t = 0f;
        while (t < segundosLevantando)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / segundosLevantando);
            dem.position = new Vector3(x0 + Mathf.Sin(t * 30f) * 0.02f, suelo + alturaLevantado * k, dem.position.z);
            dem.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 14f) * 6f);
            foreach (GameObject e in estudiantes) BrazosArriba(e, k);
            yield return null;
        }
        yield return new WaitForSeconds(0.35f);

        // 4) Lo lanzan: sale en arco hacia la derecha, gira y cae gritando hasta la calle.
        float calle = edificio.transform.position.y + 0.15f;
        Vector2 pos = dem.position;
        Vector2 vel = velocidadLanzamiento;
        TextoFlotante.Mostrar(pos + Vector2.up * 1.4f, "¡AAAH!", new Color(1f, 0.9f, 0.4f), 2.2f);
        GestorAudio.Reproducir(Snd.CaidaFinal);
        float giro = 0f;
        while (pos.y > calle)
        {
            vel.y -= gravedad * Time.deltaTime;
            pos += vel * Time.deltaTime;
            giro += 520f * Time.deltaTime;
            dem.position = new Vector3(pos.x, pos.y, dem.position.z);
            dem.rotation = Quaternion.Euler(0f, 0f, giro);
            foreach (GameObject e in estudiantes) BrazosArriba(e, 1f);
            yield return null;
        }

        // 5) Cae en la calle: nube de polvo y desaparece.
        dem.position = new Vector3(pos.x, calle, dem.position.z);
        SpriteRenderer[] dibujos = dem.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer d in dibujos) d.enabled = false;
        CamaraSeguimiento.Temblar(0.2f, 0.4f);
        yield return Polvo(new Vector3(pos.x, calle + 0.2f, 0f));

        // Los estudiantes celebran un momento.
        float fiesta = 0f;
        while (fiesta < 1.2f)
        {
            fiesta += Time.deltaTime;
            for (int i = 0; i < estudiantes.Count; i++) Rebotito(estudiantes[i], Time.time * 1.6f + i);
            yield return null;
        }
        foreach (GameObject e in estudiantes) if (e != null) Destroy(e);
        estudiantes.Clear();
    }

    private IEnumerator Polvo(Vector3 donde)
    {
        List<SpriteRenderer> nubes = new List<SpriteRenderer>();
        for (int i = 0; i < 6; i++)
        {
            SpriteRenderer s = Provisional.Crear(transform, "Polvo" + i, Provisional.Circulo, new Color(0.85f, 0.8f, 0.7f, 0.8f), 60);
            s.transform.position = donde + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.1f, 0.3f), 0f);
            s.transform.localScale = Vector3.one * 0.3f;
            nubes.Add(s);
        }
        float t = 0f;
        while (t < 0.9f)
        {
            t += Time.deltaTime;
            float k = t / 0.9f;
            foreach (SpriteRenderer s in nubes)
            {
                s.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.1f, k);
                s.color = new Color(0.85f, 0.8f, 0.7f, 0.8f * (1f - k));
                s.transform.position += Vector3.up * 0.4f * Time.deltaTime;
            }
            yield return null;
        }
        foreach (SpriteRenderer s in nubes) Destroy(s.gameObject);
    }

    private GameObject CrearEstudiante(Vector3 pies, int indice)
    {
        Color[] camisas = { new Color(0.25f, 0.45f, 0.8f), new Color(0.8f, 0.3f, 0.3f), new Color(0.3f, 0.7f, 0.4f),
                            new Color(0.9f, 0.7f, 0.2f), new Color(0.6f, 0.4f, 0.8f), new Color(0.3f, 0.7f, 0.75f) };
        GameObject g = new GameObject("EstudianteFinal" + indice);
        g.transform.position = pies;
        int orden = 30;
        SpriteRenderer cuerpo = Provisional.Crear(g.transform, "Cuerpo", Provisional.Cuadrado, camisas[indice % camisas.Length], orden);
        cuerpo.transform.localScale = new Vector3(0.3f, 0.42f, 1f);
        cuerpo.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        SpriteRenderer piernas = Provisional.Crear(g.transform, "Piernas", Provisional.Cuadrado, new Color(0.2f, 0.22f, 0.3f), orden);
        piernas.transform.localScale = new Vector3(0.24f, 0.18f, 1f);
        piernas.transform.localPosition = new Vector3(0f, 0.09f, 0f);
        SpriteRenderer cabeza = Provisional.Crear(g.transform, "Cabeza", Provisional.Circulo, new Color(0.95f, 0.78f, 0.62f), orden + 1);
        cabeza.transform.localScale = new Vector3(0.26f, 0.26f, 1f);
        cabeza.transform.localPosition = new Vector3(0f, 0.62f, 0f);
        SpriteRenderer brazos = Provisional.Crear(g.transform, "Brazos", Provisional.Cuadrado, new Color(0.95f, 0.78f, 0.62f), orden + 1);
        brazos.transform.localScale = new Vector3(0.44f, 0.07f, 1f);
        brazos.transform.localPosition = new Vector3(0f, 0.38f, 0f);
        return g;
    }

    private static void Rebotito(GameObject e, float fase)
    {
        if (e == null) return;
        Transform c = e.transform.Find("Cuerpo");
        if (c != null) c.localScale = new Vector3(0.3f, 0.42f + Mathf.Sin(fase * 12f) * 0.03f, 1f);
    }

    private static void BrazosArriba(GameObject e, float k)
    {
        if (e == null) return;
        Transform b = e.transform.Find("Brazos");
        if (b == null) return;
        b.localPosition = new Vector3(0f, Mathf.Lerp(0.38f, 0.74f, k), 0f);
    }
}
