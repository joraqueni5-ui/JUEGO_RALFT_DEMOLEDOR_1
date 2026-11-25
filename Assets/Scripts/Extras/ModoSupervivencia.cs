using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DemoledorUnifranz.Game;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// Modo SUPERVIVENCIA: en EL GRAN HALL (escenario propio con auditorio, laboratorio de computación, biblioteca, cafetería,
/// cocina y recepción) llegan oleadas de villanos cada vez más fuertes. No hay ascensor: hay que aguantar.
///   - Bajar la energía de los villanos a 0 vence la oleada (bono de puntos y unos segundos de respiro para reparar).
///   - Cada oleada trae otros villanos: el clásico, LA DEMOLEDORA (torbellino), EL INGENIERO (escombros de a tres) y el
///     ROBO-DEMOLEDOR (láser). Desde la 6 llegan de a tres y cada oleada son más rápidos y aguantan más.
///   - Se pierde si el edificio llega al 100 % o te quedas sin vidas. Se guardan la mejor oleada y el mejor tiempo.
/// InstaladorExtras lo agrega cuando GameSession.Supervivencia está activo.
/// </summary>
public class ModoSupervivencia : MonoBehaviour
{
    public const string RutaDefinicion = "Supervivencia/DefinicionSupervivencia";
    private const string ClaveOleada = "superv.mejorOleada", ClaveTiempo = "superv.mejorTiempo";

    private static ModoSupervivencia inst;

    public static bool Activo { get { return inst != null && GameSession.Supervivencia; } }
    public static int Oleada { get { return inst != null ? inst.oleada : 0; } }
    /// <summary>Cada oleada los villanos son un poco más rápidos...</summary>
    public static float FactorRitmo { get { return Activo ? Mathf.Min(1.5f, 1f + 0.05f * (inst.oleada - 1)) : 1f; } }
    /// <summary>...se cansan menos con cada reparación...</summary>
    public static float FactorCansancio { get { return Activo ? Mathf.Max(0.55f, 1f - 0.05f * (inst.oleada - 1)) : 1f; } }
    /// <summary>...y llaman más mini villanos.</summary>
    public static float FactorMinis { get { return Activo ? Mathf.Max(0.5f, 1f - 0.06f * (inst.oleada - 1)) : 1f; } }

    public static int MejorOleada { get { return PlayerPrefs.GetInt(ClaveOleada, 0); } }
    public static float MejorTiempo { get { return PlayerPrefs.GetFloat(ClaveTiempo, 0f); } }

    private int oleada;
    private bool enDescanso;
    private float inicio, finDescanso, proximoGuardado;
    private PisoCenital piso;
    private GestorJuego gestor;
    private Canvas lienzo;
    private Text textoHud, textoRecord;

    private void Awake() { inst = this; }
    private void OnDestroy()
    {
        if (inst == this) inst = null;
        if (lienzo != null) Destroy(lienzo.gameObject);
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return null;
        piso = PisoCenital.Actual;
        gestor = FindAnyObjectByType<GestorJuego>();
        inicio = Time.time;
        CrearHud();
        GestorAudio.ForzarMusica("musica_supervivencia");
        if (RedJuego.EsInvitadoEnPartida) yield break;   // en el invitado las oleadas llegan por el espejo
        AvisoUlti.Mostrar("¡SUPERVIVENCIA! AGUANTA TODO LO QUE PUEDAS", 3f, false, new Color(1f, 0.85f, 0.3f));
        yield return new WaitForSeconds(0.5f);
        EmpezarOleada(1);
    }

    // ------------------------------------------------------------ oleadas
    private static DemoledorCenital.TipoDemoledor[] Composicion(int n)
    {
        var C = DemoledorCenital.TipoDemoledor.Clasico;
        var I = DemoledorCenital.TipoDemoledor.Ingeniero;
        var D = DemoledorCenital.TipoDemoledor.Demoledora;
        var R = DemoledorCenital.TipoDemoledor.Robot;
        switch (n)
        {
            case 1: return new[] { C };
            case 2: return new[] { D };
            case 3: return new[] { C, I };
            case 4: return new[] { R };
            case 5: return new[] { D, R };
            case 6: return new[] { C, D, I };
        }
        var todos = new[] { C, I, D, R };
        int cuantos = n >= 9 ? 3 : Random.Range(2, 4);
        var r = new DemoledorCenital.TipoDemoledor[cuantos];
        for (int i = 0; i < cuantos; i++) r[i] = todos[Random.Range(0, todos.Length)];
        return r;
    }

    private void EmpezarOleada(int n)
    {
        oleada = n;
        enDescanso = false;
        List<DemoledorCenital.TipoDemoledor> tipos = new List<DemoledorCenital.TipoDemoledor>(Composicion(n));
        if (RedJuego.EnPartida && tipos.Count < 2) tipos.Add(DemoledorCenital.TipoDemoledor.Clasico);   // con dos jugadores, al menos dos villanos

        // Los villanos que ya están (el de la escena y, en multijugador, el segundo) y copias para los que falten.
        List<DemoledorCenital> villanos = new List<DemoledorCenital>();
        foreach (DemoledorCenital d in DemoledorCenital.Todos) if (d != null) villanos.Add(d);
        villanos.Sort((a, b) => a.numero.CompareTo(b.numero));
        if (villanos.Count == 0) { Debug.LogWarning("[Supervivencia] No hay demoledor en la escena."); return; }
        DemoledorCenital molde = villanos[0];
        while (villanos.Count < tipos.Count)
        {
            DemoledorCenital copia = Instantiate(molde, molde.transform.parent);
            copia.name = "Villano" + villanos.Count;
            copia.animaciones = molde.AnimacionesOriginales;
            villanos.Add(copia);
        }
        for (int i = villanos.Count - 1; i >= tipos.Count; i--)
        {
            if (i == 0) continue;
            Destroy(villanos[i].gameObject);
            villanos.RemoveAt(i);
        }

        List<string> nombres = new List<string>();
        for (int i = 0; i < villanos.Count; i++)
        {
            DemoledorCenital d = villanos[i];
            d.numero = i;
            d.gameObject.SetActive(true);
            d.AplicarTipo(tipos[i]);
            d.esperaInicial = n == 1 ? Mathf.Max(d.esperaInicial, 4f) : 2.5f;
            d.Volver(i * 1.6f);
            nombres.Add(d.Nombre);
        }
        if (piso != null) piso.RecargarEnergia();
        GestorAudio.Reproducir(SndExtra.Oleada);
        AvisoUlti.Mostrar("OLEADA " + n + "\n" + string.Join(" + ", nombres.ToArray()), 3.2f, n >= 5, n >= 5 ? new Color(1f, 0.45f, 0.35f) : new Color(1f, 0.85f, 0.35f));
        GuardarRecords();
    }

    /// <summary>La energía de los villanos llegó a 0: la oleada queda vencida.</summary>
    public static void OleadaVencida()
    {
        if (inst == null || inst.enDescanso || RedJuego.EsInvitadoEnPartida) return;
        inst.StartCoroutine(inst.Descanso());
    }

    private IEnumerator Descanso()
    {
        enDescanso = true;
        foreach (DemoledorCenital d in new List<DemoledorCenital>(DemoledorCenital.Todos)) if (d != null && d.isActiveAndEnabled) d.Desaparecer();
        int bono = 250 * oleada;
        if (gestor != null) gestor.SumarPuntos(bono, "+" + bono);
        AvisoUlti.Mostrar("¡OLEADA " + oleada + " SUPERADA!  +" + bono, 2.6f, false, new Color(0.5f, 1f, 0.55f));
        GestorAudio.Reproducir(Snd.JingleVictoria);
        CelebracionPiso.ConfetiDelCielo(30);
        CelebracionPiso.Fuego();
        PlayerPrefs.SetInt("prog.oleada_max", Mathf.Max(PlayerPrefs.GetInt("prog.oleada_max", 0), oleada));
        Progreso.RevisarLogros();
        GuardarRecords();
        finDescanso = Time.time + 7f;
        while (Time.time < finDescanso)
        {
            if (gestor != null && gestor.PartidaTerminada) yield break;
            yield return null;
        }
        GestorAudio.ForzarMusica("musica_supervivencia");
        EmpezarOleada(oleada + 1);
    }

    // ------------------------------------------------------------ marcador
    private void CrearHud()
    {
        lienzo = FabricaUIArcade.CrearCanvas("LienzoSupervivencia", 115);
        textoHud = FabricaUIArcade.CrearTexto(lienzo.transform, "Oleada", "", 34, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, 3f, 3f);
        FabricaUIArcade.Colocar(textoHud.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -175f), new Vector2(1100f, 46f));
        textoRecord = FabricaUIArcade.CrearTexto(lienzo.transform, "Record", "", 22, PaletaArcade.Crema, TextAnchor.MiddleCenter, 2f, 0f);
        FabricaUIArcade.Colocar(textoRecord.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -212f), new Vector2(1100f, 32f));
    }

    private void Update()
    {
        if (textoHud == null) return;
        bool termino = gestor != null && gestor.PartidaTerminada;
        float t = Time.time - inicio;
        string tiempo = Progreso.FormatoTiempo(t);
        if (RedJuego.EsInvitadoEnPartida) textoHud.text = "SUPERVIVENCIA  ·  " + tiempo;
        else if (enDescanso && !termino)
            textoHud.text = "¡RESPIRO! REPARA LO QUE PUEDAS  ·  SIGUIENTE OLEADA EN " + Mathf.CeilToInt(Mathf.Max(0f, finDescanso - Time.time));
        else textoHud.text = "SUPERVIVENCIA  ·  OLEADA " + oleada + "  ·  " + tiempo + "  ·  " + Dificultad.NombreActual;
        textoRecord.text = "RÉCORD: OLEADA " + Mathf.Max(MejorOleada, 0) + "  ·  " + Progreso.FormatoTiempo(MejorTiempo);
        if (!termino && Time.time >= proximoGuardado) { proximoGuardado = Time.time + 2f; GuardarRecords(); }
    }

    private void GuardarRecords()
    {
        if (RedJuego.EsInvitadoEnPartida) return;
        float t = Time.time - inicio;
        if (oleada > MejorOleada) PlayerPrefs.SetInt(ClaveOleada, oleada);
        if (t > MejorTiempo) PlayerPrefs.SetFloat(ClaveTiempo, t);
    }
}
