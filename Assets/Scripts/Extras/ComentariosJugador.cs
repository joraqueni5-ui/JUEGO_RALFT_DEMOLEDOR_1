using UnityEngine;

/// <summary>
/// Los pensamientos del personaje: un globo sobre su cabeza con frases que van variando ("¡Vamos a resolver!",
/// chistes que rompen la cuarta pared...) y su voz. Reacciona a lo que pasa (reparar, bloquear, recibir un golpe,
/// tomar un poder, la llegada del demoledor, vencer a un mini villano o quedarse quieto) sin saturar: cada tipo de
/// comentario tiene su propio tiempo de espera. InstaladorExtras lo agrega al jugador de cada piso.
/// </summary>
public class ComentariosJugador : MonoBehaviour
{
    public enum Tipo { Inicio, Reparar, Bloqueo, Golpeado, Poder, LlegaDemoledor, MiniVencido, Ocioso }

    private static readonly System.Collections.Generic.List<ComentariosJugador> todos = new System.Collections.Generic.List<ComentariosJugador>();

    [Tooltip("Segundos mínimos entre dos comentarios cualesquiera.")]
    public float esperaGeneral = 4.5f;
    [Tooltip("Probabilidad de comentar al reparar (para no hablar en cada martillazo).")]
    [Range(0f, 1f)] public float probabilidadReparar = 0.3f;

    private JugadorCenital jugador;
    private float proximoComentario, ultimoMovimiento, proximoOcioso;
    private Vector3 posAnterior;

    private Burbuja.Voz VozPropia { get { return jugador != null && jugador.Ficha != null ? jugador.Ficha.Voz : Personajes.Actual.Voz; } }

    /// <summary>Avisa de algo que pasó; el personaje decide si lo comenta. Sin jugador: lo pueden comentar todos.</summary>
    public static void Evento(Tipo t, JugadorCenital quien = null)
    {
        for (int i = 0; i < todos.Count; i++)
        {
            ComentariosJugador c = todos[i];
            if (c == null) continue;
            if (quien != null && c.jugador != quien) continue;
            c.Comentar(t);
        }
    }

    private void Awake()
    {
        todos.Add(this);
        jugador = GetComponent<JugadorCenital>();
    }

    private void OnDestroy()
    {
        todos.Remove(this);
    }

    private void Start()
    {
        proximoComentario = Time.time + 1.2f;
        ultimoMovimiento = Time.time;
        proximoOcioso = Time.time + 12f;
        posAnterior = transform.position;
        Invoke(nameof(DecirInicio), 1.4f);
    }

    private void DecirInicio() { Comentar(Tipo.Inicio, true); }

    private void Update()
    {
        if ((transform.position - posAnterior).sqrMagnitude > 0.0004f) ultimoMovimiento = Time.time;
        posAnterior = transform.position;
        if (Time.time - ultimoMovimiento > 9f && Time.time >= proximoOcioso)
        {
            proximoOcioso = Time.time + 15f;
            Comentar(Tipo.Ocioso, true);
        }
    }

    private void Comentar(Tipo t, bool forzar = false)
    {
        if (jugador == null || !jugador.enabled) return;
        bool importante = t == Tipo.Bloqueo || t == Tipo.Golpeado || t == Tipo.LlegaDemoledor;
        if (!forzar && !importante && Time.time < proximoComentario) return;
        if (t == Tipo.Reparar && Random.value > probabilidadReparar) return;
        if ((t == Tipo.Poder || t == Tipo.MiniVencido) && Random.value > 0.6f) return;

        string frase;
        switch (t)
        {
            case Tipo.Inicio: frase = Frases.Una(Frases.Inicio); break;
            case Tipo.Reparar: frase = Frases.Una(Frases.Reparar); break;
            case Tipo.Bloqueo: frase = Frases.Una(Frases.Bloqueo); break;
            case Tipo.Golpeado: frase = Frases.Una(Frases.Golpeado); break;
            case Tipo.Poder: frase = Frases.Una(Frases.Poder); break;
            case Tipo.LlegaDemoledor: frase = Frases.Una(Frases.LlegaDemoledor); break;
            case Tipo.MiniVencido: frase = Frases.Una(Frases.MiniVencido); break;
            default: frase = Frases.Una(Frases.Ocioso); break;
        }
        Burbuja.Decir(transform, frase, VozPropia, jugador.alturaDibujo + 0.35f, true);
        proximoComentario = Time.time + esperaGeneral;
    }
}
