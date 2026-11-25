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

        // Los estudiantes celebran: saltan, llueve confeti y explotan fuegos artificiales.
        if (camara != null && estudiantes.Count > 0) camara.objetivo = estudiantes[0].transform;
        GestorAudio.Reproducir(Snd.JingleVictoria);
        CelebracionPiso.Confeti(new Vector2(x0, suelo + 0.8f), 40, 3.5f);
        float fiesta = 0f, proximoFuego = 0f;
        while (fiesta < 3.2f)
        {
            fiesta += Time.deltaTime;
            for (int i = 0; i < estudiantes.Count; i++)
            {
                Rebotito(estudiantes[i], Time.time * 1.2f + i * 0.37f, true);
                EstudianteFinal ef = estudiantes[i] != null ? estudiantes[i].GetComponent<EstudianteFinal>() : null;
                if (ef != null) ef.sr.transform.localScale = new Vector3(ef.escala, ef.escala, 1f);
            }
            if (fiesta >= proximoFuego)
            {
                proximoFuego = fiesta + 0.45f;
                CelebracionPiso.Fuego();
                CelebracionPiso.ConfetiDelCielo(8);
            }
            yield return null;
        }
        foreach (GameObject e in estudiantes) if (e != null) Destroy(e);
        estudiantes.Clear();
    }

    private IEnumerator Polvo(Vector3 donde)
    {
        // Nube de polvo con el humo del juego (si no está, círculos).
        Sprite[] humo = ArteExtra.Serie("Mejoras/humo_", 4);
        if (humo != null)
        {
            for (int i = 0; i < 9; i++)
                ParticulaSimple.Crear(humo, donde + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.1f, 0.3f), 0f),
                    new Vector2(Random.Range(-0.8f, 0.8f), Random.Range(0.3f, 0.9f)), Random.Range(0.8f, 1.2f), 60,
                    new Color(0.95f, 0.9f, 0.8f, 0.9f), 0.5f, 1.4f);
            TrozoVolador.Lanzar(donde, 6);
            yield return new WaitForSeconds(0.9f);
            yield break;
        }
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

    /// <summary>Un estudiante de verdad (los personajes del juego: estudiantes y personal de la U).</summary>
    private GameObject CrearEstudiante(Vector3 pies, int indice)
    {
        Personajes.Ficha[] todas = Personajes.Todos;
        Personajes.Ficha f = todas != null && todas.Length > 0 ? todas[indice % todas.Length] : null;
        Sprite[] costado = f != null ? Personajes.SerieFluida(f, "derecha") : null;
        Sprite[] frente = f != null ? Personajes.SerieFluida(f, "abajo") : null;
        if (costado == null || frente == null) return CrearEstudianteProvisional(pies, indice);

        GameObject g = new GameObject("EstudianteFinal" + indice);
        g.transform.position = pies;
        EstudianteFinal e = g.AddComponent<EstudianteFinal>();
        e.sr = Provisional.Crear(g.transform, "Dibujo", costado[0], Color.white, 30);
        e.costado = costado;
        e.frente = frente;
        e.escala = 0.85f / Mathf.Max(0.01f, costado[0].bounds.size.y);
        e.haciaIzquierda = indice % 2 == 1;   // los de la derecha caminan hacia la izquierda
        e.sr.transform.localScale = new Vector3(e.escala, e.escala, 1f);
        e.sr.flipX = e.haciaIzquierda;
        return g;
    }

    private GameObject CrearEstudianteProvisional(Vector3 pies, int indice)
    {
        Color[] camisas = { new Color(0.25f, 0.45f, 0.8f), new Color(0.8f, 0.3f, 0.3f), new Color(0.3f, 0.7f, 0.4f),
                            new Color(0.9f, 0.7f, 0.2f), new Color(0.6f, 0.4f, 0.8f), new Color(0.3f, 0.7f, 0.75f) };
        GameObject g = new GameObject("EstudianteFinal" + indice);
        g.transform.position = pies;
        SpriteRenderer cuerpo = Provisional.Crear(g.transform, "Cuerpo", Provisional.Cuadrado, camisas[indice % camisas.Length], 30);
        cuerpo.transform.localScale = new Vector3(0.3f, 0.42f, 1f);
        cuerpo.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        SpriteRenderer cabeza = Provisional.Crear(g.transform, "Cabeza", Provisional.Circulo, new Color(0.95f, 0.78f, 0.62f), 31);
        cabeza.transform.localScale = new Vector3(0.26f, 0.26f, 1f);
        cabeza.transform.localPosition = new Vector3(0f, 0.62f, 0f);
        return g;
    }

    /// <summary>Camina (cuadros de costado) o, si 'quieto', mira al frente y rebota.</summary>
    private static void Rebotito(GameObject g, float fase, bool quieto = false)
    {
        if (g == null) return;
        EstudianteFinal e = g.GetComponent<EstudianteFinal>();
        if (e == null || e.sr == null) return;
        Sprite[] serie = quieto ? e.frente : e.costado;
        int n = serie.Length;
        e.sr.sprite = serie[Mathf.Abs((int)(fase * (n >= 8 ? 16f : 8f))) % n];
        e.sr.flipX = !quieto && e.haciaIzquierda;
        float salto = quieto ? Mathf.Abs(Mathf.Sin(fase * 7f)) * 0.18f : 0f;
        e.sr.transform.localPosition = new Vector3(0f, salto, 0f);
    }

    /// <summary>Levantando al demoledor: de frente, estirados (k = cuánto lo subieron).</summary>
    private static void BrazosArriba(GameObject g, float k)
    {
        if (g == null) return;
        EstudianteFinal e = g.GetComponent<EstudianteFinal>();
        if (e == null || e.sr == null) return;
        e.sr.sprite = e.frente[0];
        e.sr.flipX = false;
        float estira = 1f + 0.12f * k + 0.03f * Mathf.Sin(Time.time * 20f);
        e.sr.transform.localScale = new Vector3(e.escala / Mathf.Sqrt(estira), e.escala * estira, 1f);
        e.sr.transform.localPosition = Vector3.zero;
    }
}
