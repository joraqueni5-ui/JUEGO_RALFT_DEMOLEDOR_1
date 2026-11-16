using UnityEngine;

/// <summary>
/// Punto de información "?" del piso. Al pasar encima da puntos la primera vez, muestra su dato en el panel DATO ÚTIL del HUD
/// (PanelDatoUtil) y el "?" cambia a una marca de verificación. No bloquea el paso. Posición, textos, puntos y
/// tiempos salen de la DefinicionPiso (puntosInfo); PisoCenital crea uno por cada ficha.
/// </summary>
public class PuntoInformacion : MonoBehaviour
{
    private int indice;
    private string dato = "";
    private bool configurado;
    private PuntoInfoPiso ficha;

    private JugadorCenital jugador;
    private PisoCenital piso;
    private DefinicionPiso def;
    private Transform icono;
    private SpriteRenderer disco;
    private SpriteRenderer marca;
    private TextMesh signo;
    private Vector3 posIcono;
    private Sprite spritePendiente, spriteLeido;
    private bool leido;
    private bool dentro;

    private static readonly Color Amarillo = new Color(1f, 0.82f, 0.2f, 1f);
    private static readonly Color Verde = new Color(0.3f, 0.85f, 0.4f, 1f);

    private float Radio { get { return def != null ? def.radioInfo : 0.8f; } }

    /// <summary>Asigna la ficha del piso que este punto muestra (antes de que empiece).</summary>
    public void Configurar(int indice, PuntoInfoPiso ficha)
    {
        this.indice = indice;
        this.ficha = ficha;
        dato = ficha != null ? ficha.texto : "";
        configurado = true;
    }

    private void Start()
    {
        piso = PisoCenital.Actual;
        def = piso != null ? piso.Definicion : null;
        if (!configurado)
        {
            dato = def != null && !string.IsNullOrEmpty(def.fichaDatoUtil) ? def.fichaDatoUtil : "";
            indice = -1;
        }
        if (string.IsNullOrEmpty(dato)) dato = "Todavía no hay un dato para este punto.";
        dato = dato.Replace("\n", " ").Trim();

        jugador = FindFirstObjectByType<JugadorCenital>();
        CrearIcono();
        if (piso != null) piso.Cambio += AlCambiarPiso;
    }

    private void OnDestroy()
    {
        if (piso != null) piso.Cambio -= AlCambiarPiso;
    }

    /// <summary>Al reiniciar el piso el punto vuelve a estar sin leer.</summary>
    private void AlCambiarPiso()
    {
        if (leido && piso != null && indice >= 0 && !piso.InfoLeida(indice)) Restablecer();
    }

    private void Restablecer()
    {
        leido = false;
        Pintar();
    }

    private void CrearIcono()
    {
        int orden = Provisional.OrdenY(transform.position.y - 0.5f) + 2;

        // Con arte: el icono es el sprite de pendiente o leído (sin sombra ni disco provisionales).
        ArteNiveles arte = ArteNiveles.Actual;
        spritePendiente = arte != null ? arte.puntoInfoPendiente : null;
        spriteLeido = arte != null ? arte.puntoInfoLeido : null;
        if (spritePendiente != null)
        {
            disco = Provisional.Crear(transform, "Icono", spritePendiente, Color.white, orden);
            icono = disco.transform;
            posIcono = icono.localPosition;
            Pintar();
            return;
        }

        SpriteRenderer sombra = Provisional.Crear(transform, "Sombra", Provisional.Circulo, new Color(0f, 0f, 0f, 0.3f), orden - 2);
        sombra.transform.localPosition = new Vector3(0f, -0.35f, 0f);
        sombra.transform.localScale = new Vector3(0.7f, 0.28f, 1f);

        disco = Provisional.Crear(transform, "Icono", Provisional.Circulo, Amarillo, orden);
        disco.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        icono = disco.transform;
        posIcono = icono.localPosition;

        signo = Provisional.CrearTexto(icono, "Signo", "?", 0.55f, new Color(0.25f, 0.15f, 0.02f, 1f), orden + 1);
        signo.transform.localScale = new Vector3(1f / 0.8f, 1f / 0.8f, 1f);

        marca = Provisional.Crear(icono, "Visto", Provisional.Visto, Color.white, orden + 1);
        marca.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
        Pintar();
    }

    private void Pintar()
    {
        if (spritePendiente != null)
        {
            if (disco != null) { disco.sprite = leido && spriteLeido != null ? spriteLeido : spritePendiente; disco.color = Color.white; }
            return;
        }
        if (disco != null) disco.color = leido ? Verde : Amarillo;
        if (signo != null) signo.gameObject.SetActive(!leido);
        if (marca != null) marca.gameObject.SetActive(leido);
    }

    // Marca visible en el editor (el icono se crea al jugar): círculo amarillo con un "?" de rayas.
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.82f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);
        Gizmos.DrawLine(transform.position + new Vector3(-0.12f, 0.12f, 0f), transform.position + new Vector3(0.12f, 0.12f, 0f));
        Gizmos.DrawLine(transform.position + new Vector3(0.12f, 0.12f, 0f), transform.position + new Vector3(0f, -0.02f, 0f));
        Gizmos.DrawLine(transform.position + new Vector3(0f, -0.02f, 0f), transform.position + new Vector3(0f, -0.14f, 0f));
    }

    private void Update()
    {
        if (icono != null) icono.localPosition = posIcono + new Vector3(0f, Mathf.Sin(Time.time * 3f) * 0.06f, 0f);
        if (jugador == null) return;

        bool encima = Vector2.Distance(jugador.Centro, transform.position) <= Radio;
        if (encima && !dentro)
        {
            // Al entrar: la primera vez da los puntos y marca el "?" como leído; siempre muestra (o completa) el dato en el panel.
            bool primeraVez = false;
            if (!leido && piso != null && piso.RegistrarInfoLeida(indice)) { leido = true; primeraVez = true; }
            else if (!leido && indice < 0) { leido = true; primeraVez = true; }
            Pintar();
            PanelDatoUtil.Mostrar(transform, indice, ficha, dato, primeraVez);
        }
        dentro = encima;
    }
}
