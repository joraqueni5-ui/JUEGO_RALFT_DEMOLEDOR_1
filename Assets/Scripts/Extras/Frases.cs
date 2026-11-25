using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lo que dicen los personajes en sus globos. Humor de ocurrencias que rompe la cuarta pared, sin groserías.
/// Cada lista se reparte "en bolsa": no se repite una frase hasta haber dicho todas las de esa lista.
/// Para cambiar o agregar frases, solo hay que editar estas listas.
/// </summary>
public static class Frases
{
    public static readonly string[] Inicio =
    {
        "¡Vamos a resolver!", "Otro día, otro edificio que salvar.", "Hoy no se cae nada. Bueno... casi nada.",
        "¿Alguien pidió una reparación express?", "Respira. Tienes un martillo. Todo va a salir bien.",
        "Modo héroe: activado. Modo tarea: postergado.", "Si me ves sudar, es parte del personaje.",
        "Este piso no se repara solo. Créeme, ya lo intenté.", "Que alguien avise al jefe de mantenimiento... ah, soy yo.",
    };

    public static readonly string[] Reparar =
    {
        "¡Como nuevo!", "Arreglado. No pregunten cómo.", "Cinta, fe y un martillo.", "Y esto, niños, es ingeniería.",
        "¡Listo! Ni el demoledor lo nota.", "Reparado con amor... y mucha prisa.", "¡Pum! Arte contemporáneo.",
        "Garantía de por vida. Vida corta, eso sí.", "Mis papás estarían orgullosos. Creo.", "Otro más para el portafolio.",
        "¿Viste eso, cámara? ¡Viste eso!", "Si esto fuera un examen, ya aprobé.",
    };

    public static readonly string[] Bloqueo =
    {
        "¡Hoy no, grandote!", "¡Bloqueado! Toma eso.", "Llegué justo a tiempo, como en las películas.",
        "¡Ese pupitre está bajo mi protección!", "Ni lo intentes, ya lo arreglé.", "¡Reflejos de estudiante en semana de parciales!",
    };

    public static readonly string[] Golpeado =
    {
        "¡Auch! Eso va a dejar marca.", "Okey, eso dolió en la autoestima.", "¡Oye! Sin empujar, que hay fila.",
        "Nota mental: no abrazar al demoledor.", "Estoy bien. Totalmente bien. Solo veo estrellitas.",
        "¿Eso cuenta como clase de educación física?", "¡Mi peinado no!",
    };

    public static readonly string[] Poder =
    {
        "¡Poder! Me siento invencible... por unos segundos.", "¡Esto sí es una beca!", "Gracias, guionista.",
        "¿Esto es legal? Bueno, ya lo agarré.", "¡Power up! Siempre quise decir eso.",
    };

    public static readonly string[] LlegaDemoledor =
    {
        "Ahí viene... con su cara de lunes.", "Ese señor necesita un abrazo. O una terapia.",
        "Tranquilos, tengo un plan. Más o menos.", "¿Por qué siempre rompe lo que acabo de arreglar?",
    };

    public static readonly string[] MiniVencido =
    {
        "¡Fuera de aquí, mini-yo!", "Uno menos. Quedan... muchos.", "Ese era el practicante, ¿no?", "¡Despedido!",
    };

    public static readonly string[] Ocioso =
    {
        "¿Hola? ¿Sigues ahí? El edificio no se arregla mirándolo.", "Podría tomarme un café... pero el demoledor no descansa.",
        "Si me quedo quieto, ¿cuenta como estrategia?", "Pensando en la vida. Y en el ascensor.",
        "Psst... el botón ENTER sirve para reparar. Pasa la voz.", "Esto es incómodo. Tú mirándome, yo mirándote.",
    };

    public static readonly string[] Demoledor =
    {
        "¡HORA DE DEMOLER!", "¡Este edificio es mío!", "¡Yo no rompo, remodelo!", "¡Sal de mi camino, estudiante!",
        "¿Reparar? ¡Qué palabra tan fea!", "¡Mi martillo es mi mejor amigo!",
    };

    public static readonly string[] DemoledorBloqueado =
    {
        "¡Ay, mi mano!", "¡Eso no estaba en el plan!", "¡Tramposo!", "¡Grrr!",
    };

    public static readonly string[] Mini =
    {
        "¡A romper!", "¡Sí, jefe!", "¡Je je je!", "¡Pupitre a la vista!", "¡Por el jefe!",
    };

    // ------------------------------------------------------------ bolsas sin repetición
    private static readonly Dictionary<string[], List<int>> bolsas = new Dictionary<string[], List<int>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar() { bolsas.Clear(); }

    /// <summary>Una frase de la lista, sin repetir hasta agotarlas.</summary>
    public static string Una(string[] lista)
    {
        if (lista == null || lista.Length == 0) return "";
        List<int> bolsa;
        if (!bolsas.TryGetValue(lista, out bolsa) || bolsa.Count == 0)
        {
            bolsa = new List<int>();
            for (int i = 0; i < lista.Length; i++) bolsa.Add(i);
            bolsas[lista] = bolsa;
        }
        int k = Random.Range(0, bolsa.Count);
        int indice = bolsa[k];
        bolsa.RemoveAt(k);
        return lista[indice];
    }
}
