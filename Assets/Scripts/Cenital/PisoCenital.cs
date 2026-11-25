using System.Collections.Generic;
using UnityEngine;
using DemoledorUnifranz.Game;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Núcleo del piso visto desde arriba. Hereda de EdificioAlto para que GestorJuego (HUD, vidas,
/// pausa, fin de partida) lo use sin cambios: expone los mismos eventos y cifras.
///
/// Reglas: el demoledor tiene una barra de energía que baja con cada reparación y cada bloqueo del jugador.
/// Se pierde si el destrozo pasa del límite o el jugador se queda sin vidas.
/// El mapa (Mapa_PisoN) se toma del prefab de la DefinicionPiso, o del que ya esté en la escena.
/// </summary>
[DefaultExecutionOrder(-500)]
public class PisoCenital : EdificioAlto
{
    [Tooltip("Piso que se juega si 'Pisos' está vacío.")]
    public DefinicionPiso definicion;
    [Tooltip("Los pisos de esta escena (planta baja, 1, 2 y 3). Se juega el que marca GameSession.PisoActual; su mapa se instancia al empezar.")]
    public DefinicionPiso[] pisos = new DefinicionPiso[0];
    [Tooltip("Mapa en la escena. Si está vacío se busca en los hijos o se crea desde el prefab de la definición.")]
    public MapaCenital mapa;
    public CamaraPisoCenital camara;

    [Header("Depuración")]
    [Tooltip("Muestra en cada objeto el cuadrito de estado (verde = intacto, amarillo = dañado, rojo = roto).")]
    public bool mostrarMarcasEstado = false;

    private bool finalizado;

    public static PisoCenital Actual { get { return Instancia as PisoCenital; } }

    public DefinicionPiso Definicion { get { return definicion; } }
    public MapaCenital Mapa { get { return mapa; } }
    public override bool Terminado { get { return finalizado; } }
    public override int VentanasPorSeccion { get { return mapa != null ? mapa.objetos.Count : 0; } }
    public float LimiteDerrumbe { get { return definicion != null ? definicion.limiteDerrumbe : 0.7f; } }
    public float RitmoDemoledor { get { return (definicion != null ? definicion.ritmoDemoledor : 1f) * Dificultad.Ritmo * ModoSupervivencia.FactorRitmo; } }

    protected override void Awake()
    {
        secciones = 1;
        pisosPorSeccion = 1;
        columnas = 1;

        ElegirPiso();

        if (mapa == null) mapa = GetComponentInChildren<MapaCenital>();
        if (mapa == null && definicion != null && definicion.prefabMapa != null)
        {
            GameObject m = Instantiate(definicion.prefabMapa, transform);
            m.name = definicion.prefabMapa.name;
            mapa = m.GetComponent<MapaCenital>();
        }
        if (mapa == null)
        {
            Debug.LogError("[PisoCenital] No hay mapa: asigna 'Prefab Mapa' en la DefinicionPiso.");
            base.Awake();
            return;
        }

        base.Awake();   // Instancia = this, y avisa 'Cambio'
    }

    /// <summary>Con una lista de pisos, escoge el de GameSession.PisoActual y cambia el mapa de la escena por el suyo.</summary>
    private void ElegirPiso()
    {
        // SUPERVIVENCIA: el escenario nuevo (EL GRAN HALL) reemplaza al mapa de la escena.
        if (GameSession.Supervivencia)
        {
            DefinicionPiso sv = Resources.Load<DefinicionPiso>(ModoSupervivencia.RutaDefinicion);
            if (sv != null && sv.prefabMapa != null)
            {
                NivelEscena nivelSv = FindAnyObjectByType<NivelEscena>();
                MapaCenital viejo = nivelSv != null && nivelSv.mapa != null ? nivelSv.mapa : GetComponentInChildren<MapaCenital>();
                if (viejo == null && nivelSv != null) viejo = nivelSv.GetComponentInChildren<MapaCenital>();
                if (viejo != null) { viejo.gameObject.SetActive(false); Destroy(viejo.gameObject); }
                definicion = sv;
                GameObject mapaSv = Instantiate(sv.prefabMapa, transform);
                mapaSv.name = sv.prefabMapa.name;
                mapa = mapaSv.GetComponent<MapaCenital>();
                if (nivelSv != null) { nivelSv.definicion = sv; nivelSv.mapa = mapa; }
                return;
            }
            Debug.LogWarning("[Supervivencia] Falta el escenario (Resources/" + ModoSupervivencia.RutaDefinicion + "): se juega el mapa de la escena.");
        }

        // Escena de un nivel: su NivelEscena indica la definición y trae el mapa ya armado.
        NivelEscena nivel = FindAnyObjectByType<NivelEscena>();
        if (nivel != null && nivel.definicion != null)
        {
            definicion = nivel.definicion;
            mapa = nivel.mapa != null ? nivel.mapa : nivel.GetComponentInChildren<MapaCenital>();
            if (mapa != null) return;
            if (definicion.prefabMapa != null)
            {
                GameObject delPrefab = Instantiate(definicion.prefabMapa, transform);
                delPrefab.name = definicion.prefabMapa.name;
                mapa = delPrefab.GetComponent<MapaCenital>();
            }
            return;
        }

        if (pisos == null || pisos.Length == 0) return;

        DefinicionPiso elegido = null;
        foreach (DefinicionPiso d in pisos) if (d != null && d.numero == GameSession.PisoActual) { elegido = d; break; }
        if (elegido == null)
        {
            foreach (DefinicionPiso d in pisos) if (d != null) { elegido = d; break; }
            Debug.LogWarning("[PisoCenital] El piso " + GameSession.PisoActual + " no está en la lista: se juega '" + (elegido != null ? elegido.name : "ninguno") + "'.");
        }
        if (elegido == null) return;
        definicion = elegido;

        // Cambia el mapa de la escena por el del piso elegido (si no es ya ese).
        if (mapa == null) mapa = GetComponentInChildren<MapaCenital>();
        if (elegido.prefabMapa == null) return;
        if (mapa != null && mapa.name == elegido.prefabMapa.name) return;
        if (mapa != null)
        {
            mapa.gameObject.SetActive(false);
            DestroyImmediate(mapa.gameObject);
            mapa = null;
        }
        GameObject m = Instantiate(elegido.prefabMapa, transform);
        m.name = elegido.prefabMapa.name;
        mapa = m.GetComponent<MapaCenital>();
    }

    /// <summary>El piso que sigue a este (número + 1) si existe y se puede jugar; null si es el último.</summary>
    public DefinicionPiso Siguiente
    {
        get
        {
            if (definicion == null || pisos == null) return null;
            foreach (DefinicionPiso d in pisos) if (d != null && d.numero == definicion.numero + 1 && d.disponible) return d;
            return null;
        }
    }

    /// <summary>Reparaciones y bloqueos hechos en este piso (los usa el tutorial).</summary>
    public int ReparacionesTotales { get; private set; }
    public int BloqueosTotales { get; private set; }

    // El mapa se lee en Start (no en Awake): así los colliders ya tienen sus límites físicos.
    // Este script corre antes que los demás (orden -500), así que el jugador, el demoledor y
    // GestorJuego encuentran el mapa listo cuando empiezan.
    private void Start()
    {
        if (mapa == null) return;

        energiaPct = definicion != null ? definicion.energiaInicial : 100f;
        mapa.Preparar(definicion);
        CrearPuntosInformacion();
        foreach (ObjetoReparable o in mapa.objetos) o.CambioEstado += AlCambiarObjeto;
        if (camara == null) camara = FindAnyObjectByType<CamaraPisoCenital>();
        if (camara != null) camara.Encuadrar(mapa.Limites);
        if (RedJuego.EsInvitadoEnPartida)
        {
            // Invitado en multijugador: el piso solo dibuja; las reglas (poderes, tutorial, energía) son del anfitrión.
            AvisarCambio();
            return;
        }
        if (FindAnyObjectByType<IndicadoresFueraDePantalla>() == null) new GameObject("Indicadores").AddComponent<IndicadoresFueraDePantalla>();
        if (definicion != null && definicion.poderes != null && definicion.poderes.Length > 0 && GetComponent<GestorPoderes>() == null)
            gameObject.AddComponent<GestorPoderes>();
        if (definicion != null && definicion.pasosTutorial != null && definicion.pasosTutorial.Length > 0 && GetComponent<TutorialPiso>() == null)
            gameObject.AddComponent<TutorialPiso>();
        AvisarCambio();
    }

    // ------------------------------------------------------------ multijugador (invitado)
    /// <summary>El anfitrión manda cómo va el piso: energía, rendición, ascensor, fin y los "?" leídos.</summary>
    public void FijarEstadoRed(float nuevaEnergiaPct, bool rendido, bool salio, bool terminado, bool ventanalRoto, List<int> leidas)
    {
        bool cambio = Mathf.Abs(nuevaEnergiaPct - energiaPct) > 0.01f || rendido != Rendido || terminado != finalizado;
        energiaPct = nuevaEnergiaPct;
        Rendido = rendido;
        finalizado = terminado;
        GestorAudio.SilencioLocal = true;   // estos cambios ya sonaron en el anfitrión
        try
        {
            if (salio && !DemoledorSalio)
            {
                DemoledorSalio = true;
                if (mapa != null && mapa.ascensor != null) mapa.ascensor.Abrir(definicion);
                if (mapa != null && mapa.ascensor != null) GuiaSalida.Mostrar(mapa.ascensor.transform.position);
                else if (mapa != null && mapa.puntoSalto != null) GuiaSalida.Mostrar(mapa.puntoSalto.position);
                cambio = true;
            }
            else if (!salio && DemoledorSalio)
            {
                DemoledorSalio = false;
                if (mapa != null && mapa.ascensor != null) mapa.ascensor.Cerrar(definicion);
                cambio = true;
            }
            if (mapa != null && mapa.ventanal != null && mapa.ventanal.Roto != ventanalRoto)
            {
                if (ventanalRoto) mapa.ventanal.Romper(); else mapa.ventanal.Restaurar();
            }
        }
        finally { GestorAudio.SilencioLocal = false; }
        if (leidas != null && leidas.Count != infoLeidas.Count)
        {
            infoLeidas.Clear();
            foreach (int i in leidas) infoLeidas.Add(i);
            cambio = true;
        }
        if (cambio) AvisarCambio();
    }

    /// <summary>Los "?" leídos (para el multijugador).</summary>
    public IEnumerable<int> InfoLeidas { get { return infoLeidas; } }

    /// <summary>Energía en porcentaje (0 a 100), para el multijugador.</summary>
    public float EnergiaPorcentaje { get { return energiaPct; } }

    // Con un mínimo imposible, la comprobación de victoria por golpes de EdificioAlto nunca se cumple:
    // el piso se supera por la energía del demoledor.
    protected override int GolpesMinimos(int seccion)
    {
        return int.MaxValue;
    }

    /// <summary>Energía del demoledor, de 0 a 1.</summary>
    public float Energia { get { return Mathf.Clamp01(energiaPct / 100f); } }
    /// <summary>La energía llegó a 0.</summary>
    public bool EnergiaAgotada { get { return energiaPct <= 0f; } }

    private float energiaPct = 100f;

    private void BajarEnergia(float porcentaje)
    {
        if (EnergiaAgotada || porcentaje <= 0f || finalizado) return;
        energiaPct = Mathf.Max(0f, energiaPct - porcentaje * Dificultad.Cansancio * ModoSupervivencia.FactorCansancio);
        if (EnergiaAgotada)
        {
            // En supervivencia no se rinden: la oleada queda vencida y llega la siguiente.
            if (ModoSupervivencia.Activo) ModoSupervivencia.OleadaVencida();
            else IniciarRendicion();
        }
        AvisarCambio();
    }

    /// <summary>Supervivencia: la energía de los villanos vuelve a 100 % con cada oleada nueva.</summary>
    public void RecargarEnergia()
    {
        energiaPct = 100f;
        AvisarCambio();
    }

    // ------------------------------------------------------------ rendición y ascensor
    /// <summary>La energía llegó a 0: el demoledor se rinde (ya no ataca) y se va por el ventanal.</summary>
    public bool Rendido { get; private set; }
    /// <summary>El demoledor ya salió del piso y el ascensor está abierto.</summary>
    public bool DemoledorSalio { get; private set; }
    /// <summary>Destrozo (0 a 1) en el momento de la rendición.</summary>
    public float DestrozoAlRendirse { get; private set; }

    private void IniciarRendicion()
    {
        if (Rendido) return;
        Rendido = true;
        DestrozoAlRendirse = DestrozoSeccionActual;
        EscombroCenital.EliminarTodos();
    }

    /// <summary>El demoledor terminó de salir: se abre el ascensor.</summary>
    public void DemoledorSeFue()
    {
        if (DemoledorSalio) return;
        DemoledorSalio = true;

        // Sin ascensor (el último piso): aparece la marca "¡SÍGUELO!" en el punto de salto y el jugador DEBE caminar hasta ella.
        if (mapa != null && mapa.ascensor == null)
        {
            if (mapa.puntoSalto != null)
            {
                MarcaSigueloCenital.Crear(mapa.puntoSalto.position, this);
                GuiaSalida.Mostrar(mapa.puntoSalto.position);
                GestorJuego gestorSalto = FindAnyObjectByType<GestorJuego>();
                if (gestorSalto != null) gestorSalto.Aviso("¡SÍGUELO!", 3.5f);
                AvisarCambio();
                return;
            }
            Debug.LogWarning("[PisoCenital] El piso no tiene ascensor ni punto de salto: se supera al irse el demoledor.");
            JugadorLlegoAlAscensor();
            return;
        }
        if (mapa != null && mapa.ascensor != null) mapa.ascensor.Abrir(definicion);
        if (mapa != null && mapa.ascensor != null) GuiaSalida.Mostrar(mapa.ascensor.transform.position);
        string mensaje = definicion != null ? definicion.mensajeAscensor : "¡El ascensor está abierto!";
        GestorJuego gestor = FindAnyObjectByType<GestorJuego>();
        if (gestor != null) gestor.Aviso(mensaje, 3.5f);
        AvisarCambio();
    }

    /// <summary>El jugador entró al ascensor abierto: se supera el piso.</summary>
    public void JugadorLlegoAlAscensor()
    {
        if (finalizado || !DemoledorSalio) return;
        finalizado = true;
        AvisarCambio();
        AvisarSalvado();
    }

    // ------------------------------------------------------------ puntos de información y estrellas
    private readonly HashSet<int> infoLeidas = new HashSet<int>();

    public int InfoTotal { get { return definicion != null && definicion.puntosInfo != null ? definicion.puntosInfo.Length : 0; } }
    public int InfoLeidasCuenta { get { return infoLeidas.Count; } }
    public bool InfoLeida(int indice) { return infoLeidas.Contains(indice); }

    /// <summary>Marca el punto como leído y da los puntos la primera vez. Devuelve true si queda leído.</summary>
    public bool RegistrarInfoLeida(int indice)
    {
        if (indice < 0) return false;
        if (infoLeidas.Add(indice))
        {
            GestorAudio.Reproducir(Snd.MenuConfirmar, 0.6f);   // sonido suave de interfaz
            Progreso.Sumar("info");
            int puntos = definicion != null ? definicion.puntosPorInfo : 20;
            GestorJuego gestor = FindAnyObjectByType<GestorJuego>();
            if (gestor != null) gestor.SumarPuntos(puntos, "+" + puntos);
            JugadorCenital jugador = FindAnyObjectByType<JugadorCenital>();
            if (jugador != null) TextoFlotante.Mostrar(jugador.Centro + Vector2.up * 1.3f, "+" + puntos, new Color(1f, 0.85f, 0.3f));
            AvisarCambio();
        }
        return true;
    }

    /// <summary>
    /// Condiciones de las 3 estrellas: vencer al demoledor, destrozo menor al máximo al rendirse y haber
    /// leído todos los puntos "?".
    /// </summary>
    public bool[] CondicionesEstrellas()
    {
        float maximo = definicion != null ? definicion.destrozoMaximoEstrella : 0.3f;
        bool todos = InfoTotal > 0 && infoLeidas.Count >= InfoTotal;
        return new[] { Rendido, Rendido && DestrozoAlRendirse < maximo, todos };
    }

    public int Estrellas
    {
        get
        {
            int n = 0;
            foreach (bool b in CondicionesEstrellas()) if (b) n++;
            return n;
        }
    }

    private void CrearPuntosInformacion()
    {
        if (mapa == null || definicion == null || definicion.puntosInfo == null || definicion.puntosInfo.Length == 0) return;

        // Los puntos que el mapa de la escena ya trae (PuntoInformacion 1, 2...) se conservan en su lugar y solo
        // reciben su texto; los demás (por ejemplo el punto único de un mapa viejo) se reemplazan por los de la definición.
        PuntoInformacion[] previos = mapa.GetComponentsInChildren<PuntoInformacion>(true);
        bool[] usado = new bool[previos.Length];
        for (int i = 0; i < definicion.puntosInfo.Length; i++)
        {
            PuntoInfoPiso ficha = definicion.puntosInfo[i];
            if (ficha == null) { Debug.LogError("[DatoÚtil] Piso " + definicion.numero + ": la ficha " + (i + 1) + " de puntosInfo está vacía."); continue; }
            if (string.IsNullOrWhiteSpace(ficha.texto))
                Debug.LogError("[DatoÚtil] Piso " + definicion.numero + ": el \"?\" " + (i + 1) + " en " + ficha.posicion + " no tiene dato (área \"" + ficha.area + "\"). Agrégalo en DefinicionPiso.puntosInfo.");

            string nombre = "PuntoInformacion " + (i + 1);
            PuntoInformacion punto = null;
            for (int k = 0; k < previos.Length; k++)
                if (!usado[k] && previos[k] != null && previos[k].name == nombre) { punto = previos[k]; usado[k] = true; break; }

            if (punto == null)
            {
                GameObject g = new GameObject(nombre);
                g.transform.SetParent(mapa.transform, false);
                g.transform.position = new Vector3(ficha.posicion.x, ficha.posicion.y, 0f);
                punto = g.AddComponent<PuntoInformacion>();
            }
            punto.gameObject.SetActive(true);
            punto.Configurar(i, ficha);
        }
        for (int k = 0; k < previos.Length; k++)
        {
            if (usado[k] || previos[k] == null) continue;
            previos[k].gameObject.SetActive(false);
            Destroy(previos[k].gameObject);
        }
    }

    /// <summary>El jugador bloqueó un golpe del demoledor.</summary>
    public void RegistrarBloqueo()
    {
        BloqueosTotales++;
        Progreso.Sumar("bloqueos");
        BajarEnergia(definicion != null ? definicion.energiaPorBloqueo : 12f);
    }

    private void AlCambiarObjeto(ObjetoReparable o)
    {
        AvisarCambio();
    }

    // ------------------------------------------------------------ consultas
    public override int DanadasEnSeccion(int seccion)
    {
        if (mapa == null) return 0;
        int n = 0;
        foreach (ObjetoReparable o in mapa.objetos) if (o != null && o.Estado != EstadoObjeto.Intacto) n++;
        return n;
    }

    /// <summary>Objetos en el estado Roto (los dañados no cuentan).</summary>
    public int ObjetosRotos
    {
        get
        {
            if (mapa == null) return 0;
            int n = 0;
            foreach (ObjetoReparable o in mapa.objetos) if (o != null && o.Estado == EstadoObjeto.Roto) n++;
            return n;
        }
    }

    /// <summary>Destrozo (0 a 1): un objeto roto cuenta entero y uno dañado la mitad.</summary>
    public override float DestrozoSeccionActual
    {
        get
        {
            if (Terminado || mapa == null || mapa.objetos.Count == 0) return 0f;
            float suma = 0f;
            foreach (ObjetoReparable o in mapa.objetos)
            {
                if (o == null) continue;
                if (o.Estado == EstadoObjeto.Roto) suma += 1f;
                else if (o.Estado == EstadoObjeto.Danado) suma += 0.5f;
            }
            return suma / mapa.objetos.Count;
        }
    }

    // ------------------------------------------------------------ acciones
    /// <summary>El jugador reparó un objeto: suma puntos en GestorJuego.</summary>
    public void RegistrarReparacion()
    {
        ReparacionesTotales++;
        Progreso.Sumar("reparaciones");
        AvisarReparacion();
        BajarEnergia(definicion != null ? definicion.energiaPorReparacion : 4f);
    }

#if UNITY_EDITOR
    // Solo en el editor, para probar rápido el final de cada nivel: F9 deja la energía del demoledor en 0 y F8 en 21 %.
    private void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null || finalizado || Rendido) return;
        if (teclado[Key.F9].wasPressedThisFrame) BajarEnergia(energiaPct);
        else if (teclado[Key.F8].wasPressedThisFrame) { energiaPct = 21f; AvisarCambio(); }
    }
#endif

    public override void Reiniciar()
    {
        MarcaSigueloCenital.Quitar();
        finalizado = false;
        Rendido = false;
        ReparacionesTotales = 0;
        BloqueosTotales = 0;
        DemoledorSalio = false;
        DestrozoAlRendirse = 0f;
        infoLeidas.Clear();
        PanelDatoUtil.Ocultar();
        if (mapa != null && mapa.ascensor != null) mapa.ascensor.Cerrar(definicion);
        energiaPct = definicion != null ? definicion.energiaInicial : 100f;
        if (mapa != null)
        {
            foreach (ObjetoReparable o in mapa.objetos) if (o != null) o.Reiniciar();
        }
        EscombroCenital.EliminarTodos();
        PoderRecogible.EliminarTodos();
        GestorPoderes poderes = GetComponent<GestorPoderes>();
        if (poderes != null) poderes.Reiniciar();
        base.Reiniciar();
    }

    protected override void Avanzar()
    {
        // Único piso de esta prueba: completarlo salva el edificio.
        finalizado = true;
        AvisarCambio();
        AvisarSalvado();
    }
}
