using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Más detalle en los escenarios: reparte por el piso cosas de una universidad en uso (hojas sueltas, libros abiertos,
/// mochilas, café derramado, lápices, una bola de papel, hojas de planta) y, según el piso, algo de obra (conos,
/// cinta de precaución, letrero de piso mojado, escombritos y grietas). No estorban: van en el suelo, debajo de los
/// personajes, y no tapan los objetos reparables. El reparto es siempre el mismo en cada piso.
/// InstaladorExtras lo agrega a cada piso cenital.
/// </summary>
public class DetallesEscenario : MonoBehaviour
{
    [Tooltip("Fracción de baldosas libres que reciben un detalle.")]
    [Range(0f, 0.3f)] public float densidad = 0.1f;
    public int maximo = 80;
    [Tooltip("Fracción de detalles que son propios del piso (ollas en gastronomía, teclados en informática, tizas en las aulas...).")]
    [Range(0f, 1f)] public float propios = 0.45f;

    private static readonly string[] Comunes = { "papeles", "papeles", "bola_papel", "libro", "mochila", "cafe", "lapices", "hoja_planta" };
    private static readonly string[] Obra = { "cono", "cinta_obra", "piso_mojado", "escombritos", "grieta_piso", "escombritos" };

    /// <summary>Lo que deja cada carrera en el suelo, por piso.</summary>
    private static string[] DelPiso(int numero)
    {
        switch (numero)
        {
            case 0: return new[] { "maceta", "folleto", "folleto", "paraguas", "termo" };
            case 1: return new[] { "olla", "cucharon", "botiquin", "venda", "tabla_picar" };
            case 2: return new[] { "teclado", "mouse_pc", "cable", "cable", "laptop", "disco" };
            case 3: case 4: case 5: return new[] { "tiza", "borrador", "examen", "examen", "calculadora", "regla", "audifonos" };
            default: return new[] { "pila_libros", "hoja_seca", "hoja_seca", "maceta", "lentes" };
        }
    }

    // Con volumen: se ordenan con los personajes (no van planos en el suelo) y no se giran.
    private static bool ConVolumen(string n)
    {
        return n == "cono" || n == "piso_mojado" || n == "mochila" || n == "maceta" || n == "olla" || n == "botiquin" ||
               n == "pila_libros" || n == "laptop";
    }

    private System.Collections.IEnumerator Start()
    {
        // Espera a que el piso arme su mapa (celdas y objetos).
        yield return null;
        yield return null;
        PisoCenital piso = GetComponent<PisoCenital>();
        if (piso == null) piso = PisoCenital.Actual;
        if (piso == null || piso.Mapa == null) yield break;
        MapaCenital mapa = piso.Mapa;
        List<Vector2> celdas = mapa.CentrosTransitables;
        if (celdas == null || celdas.Count == 0) yield break;

        int numero = piso.Definicion != null ? piso.Definicion.numero : 0;
        Random.State antes = Random.state;
        Random.InitState(1234 + numero * 97 + gameObject.scene.name.Length);

        GameObject raiz = new GameObject("DetallesEscenario");
        raiz.transform.SetParent(mapa.transform, false);
        int cuantos = Mathf.Min(maximo, Mathf.RoundToInt(celdas.Count * densidad));
        float obra = 0.15f + numero * 0.05f;   // los pisos de arriba están más "en obra"
        string[] delPiso = DelPiso(numero);
        int puestos = 0;
        for (int intento = 0; intento < cuantos * 4 && puestos < cuantos; intento++)
        {
            Vector2 c = celdas[Random.Range(0, celdas.Count)];
            if (CercaDeObjeto(mapa, c)) continue;
            float azar = Random.value;
            string nombre = azar < obra ? Obra[Random.Range(0, Obra.Length)]
                          : azar < obra + (1f - obra) * propios ? delPiso[Random.Range(0, delPiso.Length)]
                          : Comunes[Random.Range(0, Comunes.Length)];
            Sprite s = ArteExtra.Uno("Detalles/" + nombre);
            if (s == null) continue;
            SpriteRenderer sr = Provisional.Crear(raiz.transform, nombre, s, Color.white, -185);
            sr.transform.position = new Vector3(c.x + Random.Range(-0.25f, 0.25f), c.y + Random.Range(-0.25f, 0.25f), 0f);
            bool gira = !ConVolumen(nombre);
            sr.transform.rotation = Quaternion.Euler(0f, 0f, gira ? Random.Range(0, 4) * 90f + Random.Range(-15f, 15f) : 0f);
            float e = Random.Range(0.75f, 1f);
            sr.transform.localScale = new Vector3(Random.value < 0.5f ? -e : e, e, 1f);
            // Lo que tiene volumen (conos, letreros, mochilas) se ordena con los personajes; lo plano va al suelo.
            if (ConVolumen(nombre)) sr.sortingOrder = Provisional.OrdenY(c.y - 0.3f);
            puestos++;
        }
        Random.state = antes;
    }

    private static bool CercaDeObjeto(MapaCenital mapa, Vector2 c)
    {
        foreach (ObjetoReparable o in mapa.objetos)
            if (o != null && Vector2.Distance(c, o.PuntoMasCercano(c)) < 0.9f) return true;
        if (mapa.spawnJugador != null && Vector2.Distance(c, mapa.spawnJugador.position) < 1.2f) return true;
        return false;
    }
}
