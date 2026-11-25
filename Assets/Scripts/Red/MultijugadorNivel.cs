using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using DemoledorUnifranz.Game;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// El piso en modo multijugador.
/// ANFITRIÓN: crea al segundo estudiante (lo maneja el invitado por la red) y al SEGUNDO DEMOLEDOR con su cuadrilla,
/// espera a que el invitado cargue el piso y le manda el mundo (EspejoAnfitrion) y el estado del HUD.
/// INVITADO: apaga la simulación local (el anfitrión manda), dibuja el espejo, sigue con la cámara a su personaje,
/// muestra su barra de furia y manda su movimiento, sus martillazos y su K + L.
/// </summary>
public class MultijugadorNivel : MonoBehaviour
{
    private static MultijugadorNivel actual;

    // Estado que llega del anfitrión (lo leen AmbienteNivel y otros en el invitado).
    public static bool FuriaRed, RabiaRed, FrioRed;

    private bool anfitrion;
    private float limiteEspera;
    private bool esperandoInvitado;
    private Canvas aviso;

    // Invitado
    private Transform marcador;
    private IraEstudiante iraEspejo;
    private GestorJuego gestor;
    private float proximoEnvio, proximaLimpieza;
    private int martillazos;
    private bool pedirIra;
    private bool tieneMarcador;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Iniciar()
    {
        RedJuego.AlMensaje -= AlMensajeGlobal;
        RedJuego.AlMensaje += AlMensajeGlobal;
    }

    private static void AlMensajeGlobal(byte tipo, BinaryReader r)
    {
        switch (tipo)
        {
            case MsgRed.Cargar:
                if (RedJuego.EsInvitado) CargarComoInvitado(r);
                break;

            case MsgRed.ListoEscena:
                if (RedJuego.EsAnfitrion && EspejoAnfitrion.Actual != null) EspejoAnfitrion.Actual.invitadoListo = true;
                break;

            case MsgRed.Entrada:
                if (RedJuego.EsAnfitrion) AplicarEntrada(r);
                break;

            case MsgRed.PedirSprite:
                if (RedJuego.EsAnfitrion && EspejoAnfitrion.Actual != null) EspejoAnfitrion.Actual.EnviarPng(r.ReadInt32());
                break;

            case MsgRed.Mundo:
            case MsgRed.SpriteDef:
            case MsgRed.SpritePng:
                if (RedJuego.EsInvitado && EspejoInvitado.Actual != null) EspejoInvitado.Actual.Procesar(tipo, r);
                break;

            case MsgRed.Evento:
                if (RedJuego.EsInvitado) AplicarEvento(r);
                break;
        }
    }

    // ------------------------------------------------------------ carga de escenas (invitado)
    private static void CargarComoInvitado(BinaryReader r)
    {
        string escena = r.ReadString();
        GameSession.PisoActual = r.ReadInt32();
        GameSession.PuntosAcumulados = r.ReadInt32();
        GameSession.VidasAlEmpezarPiso = r.ReadInt32();
        GameSession.Puntaje = r.ReadInt32();
        GameSession.Victoria = r.ReadBoolean();
        GameSession.HayDatosDerrumbe = r.ReadBoolean();
        GameSession.Reparaciones = r.ReadInt32();
        GameSession.PorcentajeDestruido = r.ReadSingle();
        RedJuego.PersonajeAnfitrion = r.ReadInt32();
        RedJuego.PersonajeInvitado = r.ReadInt32();
        if (r.BaseStream.Position < r.BaseStream.Length) Dificultad.Actual = (Dificultad.Nivel)Mathf.Clamp(r.ReadByte(), 0, 2);
        GameSession.Supervivencia = r.BaseStream.Position < r.BaseStream.Length && r.ReadBoolean();
        GameSession.ModoSeleccionado = GameSession.ModoElegido.Pisos;
        if (RedJuego.PersonajeInvitado >= 0 && RedJuego.PersonajeInvitado < Personajes.Elegibles.Length) Personajes.Indice = RedJuego.PersonajeInvitado;
        RedJuego.MarcarPartidaIniciada();
        if (LobbyRed.Abierto) { LobbyRed l = Object.FindAnyObjectByType<LobbyRed>(); if (l != null) Object.Destroy(l.gameObject); }
        if (SceneManager.GetActiveScene().name == escena && PisoCenital.Actual != null)
        {
            // El anfitrión reinició el mismo piso (R): se recarga igual para empezar limpio.
        }
        if (!NavegadorEscenas.Cargar(escena)) SceneManager.LoadScene(escena);
    }

    /// <summary>Se cortó la conexión en medio de la partida.</summary>
    public static void ConexionPerdida()
    {
        string motivo = string.IsNullOrEmpty(RedJuego.UltimoMensaje) ? "Se perdió la conexión." : RedJuego.UltimoMensaje;
        if (actual != null && actual.anfitrion)
        {
            // El anfitrión sigue jugando solo (contra los dos demoledores).
            JugadorCenital otro = JugadorCenital.PorIndice(1);
            if (otro != null) Object.Destroy(otro.gameObject);
            Time.timeScale = 1f;
            GestorJuego g = Object.FindAnyObjectByType<GestorJuego>();
            if (g != null) g.Aviso("TU COMPAÑERO SE DESCONECTÓ: ¡SIGUES SOLO!", 3.5f);
            actual.esperandoInvitado = false;
            if (actual.aviso != null) Destroy(actual.aviso.gameObject);
            return;
        }
        if (actual != null || SceneManager.GetActiveScene().name != Escenas.MenuPrincipal)
        {
            Time.timeScale = 1f;
            AvisoRed.Mostrar(motivo, 3f);
            if (!NavegadorEscenas.Cargar(Escenas.MenuPrincipal)) SceneManager.LoadScene(Escenas.MenuPrincipal);
        }
    }

    // ------------------------------------------------------------ preparar el piso
    /// <summary>Anfitrión: segundo estudiante (remoto) y segundo demoledor. Se llama al cargar el piso, antes de los Start.</summary>
    public static void PrepararAnfitrion(PisoCenital piso)
    {
        GameObject g = new GameObject("[Multijugador]");
        SceneManager.MoveGameObjectToScene(g, piso.gameObject.scene);
        actual = g.AddComponent<MultijugadorNivel>();
        actual.anfitrion = true;
        g.AddComponent<EspejoAnfitrion>();

        JugadorCenital j0 = Object.FindAnyObjectByType<JugadorCenital>();
        if (j0 != null)
        {
            j0.indiceRed = 0;
            j0.controlRemoto = false;
            j0.personaje = FichaDe(RedJuego.PersonajeAnfitrion);
            GameObject copia = Object.Instantiate(j0.gameObject, j0.transform.position + Vector3.right, j0.transform.rotation, j0.transform.parent);
            copia.name = "Jugador2 (invitado)";
            JugadorCenital j1 = copia.GetComponent<JugadorCenital>();
            j1.indiceRed = 1;
            j1.controlRemoto = true;
            j1.personaje = FichaDe(RedJuego.PersonajeInvitado);
            // Sin restos del original (componentes que agregan los extras se agregan después a cada uno).
            foreach (IraEstudiante i in copia.GetComponents<IraEstudiante>()) Object.DestroyImmediate(i);
            foreach (ComentariosJugador c in copia.GetComponents<ComentariosJugador>()) Object.DestroyImmediate(c);
        }

        DemoledorCenital d0 = Object.FindAnyObjectByType<DemoledorCenital>();
        if (d0 != null)
        {
            d0.numero = 0;
            GameObject copia = Object.Instantiate(d0.gameObject, d0.transform.position + new Vector3(1.6f, 0f, 0f), d0.transform.rotation, d0.transform.parent);
            copia.name = "Demoledor2";
            DemoledorCenital d1 = copia.GetComponent<DemoledorCenital>();
            d1.numero = 1;
            d1.desplazamientoEntrada = new Vector2(1.6f, 0f);
            d1.retrasoEntrada = 3f;
        }
    }

    private static Personajes.Ficha FichaDe(int indice)
    {
        Personajes.Ficha[] e = Personajes.Elegibles;
        if (indice >= 0 && indice < e.Length) return e[indice];
        return Personajes.Actual;
    }

    /// <summary>Invitado: apaga lo que simula el anfitrión y prepara el espejo. Se llama al cargar el piso.</summary>
    public static void PrepararInvitado(PisoCenital piso)
    {
        GameObject g = new GameObject("[Multijugador]");
        SceneManager.MoveGameObjectToScene(g, piso.gameObject.scene);
        actual = g.AddComponent<MultijugadorNivel>();
        actual.anfitrion = false;

        GameObject espejo = new GameObject("[EspejoRed]");
        SceneManager.MoveGameObjectToScene(espejo, piso.gameObject.scene);
        espejo.AddComponent<EspejoInvitado>();

        foreach (JugadorCenital j in Object.FindObjectsByType<JugadorCenital>(FindObjectsInactive.Exclude)) j.gameObject.SetActive(false);
        foreach (DemoledorCenital d in Object.FindObjectsByType<DemoledorCenital>(FindObjectsInactive.Exclude)) d.gameObject.SetActive(false);
        foreach (EstudianteNPC n in Object.FindObjectsByType<EstudianteNPC>(FindObjectsInactive.Exclude)) n.gameObject.SetActive(false);

        actual.marcador = new GameObject("MarcadorInvitado").transform;
        actual.marcador.SetParent(g.transform, false);
        actual.marcador.position = piso.Mapa != null && piso.Mapa.spawnJugador != null ? piso.Mapa.spawnJugador.position : Vector3.zero;
        CamaraPisoCenital.ObjetivoExterno = actual.marcador;

        // Barra de furia propia (la furia la calcula el anfitrión).
        GameObject ira = new GameObject("FuriaInvitado");
        SceneManager.MoveGameObjectToScene(ira, piso.gameObject.scene);
        actual.iraEspejo = ira.AddComponent<IraEstudiante>();
        actual.iraEspejo.espejo = true;
        actual.iraEspejo.fichaEspejo = FichaDe(RedJuego.PersonajeInvitado);

        RedJuego.Enviar(MsgRed.ListoEscena, w => w.Write(piso.gameObject.scene.name));
    }

    private void Start()
    {
        gestor = FindAnyObjectByType<GestorJuego>();
        if (anfitrion)
        {
            StartCoroutine(ColocarSegundoJugador());
            if (!(EspejoAnfitrion.Actual != null && EspejoAnfitrion.Actual.invitadoListo)) StartCoroutine(EsperarInvitado());
        }
        else OcultarLocales();
    }

    private void OnDestroy()
    {
        if (actual == this) actual = null;
        if (!anfitrion && CamaraPisoCenital.ObjetivoExterno == marcador) CamaraPisoCenital.ObjetivoExterno = null;
        FuriaRed = RabiaRed = FrioRed = false;
        GestorAudio.Tension("red", false);
    }

    /// <summary>El segundo estudiante empieza en una baldosa libre junto a la salida del primero.</summary>
    private IEnumerator ColocarSegundoJugador()
    {
        yield return null;
        PisoCenital piso = PisoCenital.Actual;
        JugadorCenital j0 = JugadorCenital.PorIndice(0), j1 = JugadorCenital.PorIndice(1);
        if (piso == null || piso.Mapa == null || j0 == null || j1 == null) yield break;
        Vector2 base0 = j0.transform.position;
        Vector2 mejor = base0 + Vector2.right;
        float md = float.MaxValue;
        foreach (Vector2 c in piso.Mapa.CentrosTransitables)
        {
            float d = Vector2.Distance(c, base0);
            if (d < 0.8f || d > 3f) continue;
            float puntaje = Mathf.Abs(d - 1.2f) + Mathf.Abs(c.y - base0.y) * 0.3f;
            if (puntaje < md) { md = puntaje; mejor = c; }
        }
        j1.Reaparecer(new Vector3(mejor.x, mejor.y, j0.transform.position.z));
    }

    /// <summary>El anfitrión espera (con el tiempo detenido) a que el invitado termine de cargar el piso.</summary>
    private IEnumerator EsperarInvitado()
    {
        yield return null;
        esperandoInvitado = true;
        limiteEspera = Time.unscaledTime + 25f;
        aviso = AvisoRed.CrearCartel("ESPERANDO A QUE TU COMPAÑERO CARGUE EL PISO...");
        while (esperandoInvitado && RedJuego.EsAnfitrionEnPartida && Time.unscaledTime < limiteEspera &&
               !(EspejoAnfitrion.Actual != null && EspejoAnfitrion.Actual.invitadoListo))
        {
            Time.timeScale = 0f;
            yield return null;
        }
        esperandoInvitado = false;
        if (aviso != null) Destroy(aviso.gameObject);
        if (gestor == null || !gestor.EstaEnPausa) Time.timeScale = 1f;
    }

    // ------------------------------------------------------------ HUD (anfitrión escribe, invitado lee)
    private static GestorJuego gestorHud;

    public static void EscribirHud(BinaryWriter w)
    {
        if (gestorHud == null) gestorHud = Object.FindAnyObjectByType<GestorJuego>();
        GestorJuego g = gestorHud;
        w.Write(g != null ? g.Puntos : 0);
        w.Write(g != null ? g.Vidas : 0);
        w.Write(g != null ? g.MensajeVisible ?? "" : "");
        JugadorCenital inv = JugadorCenital.PorIndice(1);
        w.Write(inv != null ? inv.TextoPoderes() ?? "" : "");

        PisoCenital p = PisoCenital.Actual;
        w.Write(p != null ? p.EnergiaPorcentaje : 100f);
        bool furia = false, rabia = false, frio = false;
        foreach (DemoledorCenital d in DemoledorCenital.Todos)
        {
            if (d == null || !d.isActiveAndEnabled || !d.EnPiso) continue;
            furia |= d.EnFuria; rabia |= d.EnRabia; frio |= d.Congelado;
        }
        int flags = 0;
        if (p != null && p.Rendido) flags |= 1;
        if (p != null && p.DemoledorSalio) flags |= 2;
        if (p != null && p.Terminado) flags |= 4;
        if (p != null && p.Mapa != null && p.Mapa.ventanal != null && p.Mapa.ventanal.Roto) flags |= 8;
        if (furia) flags |= 16;
        if (rabia) flags |= 32;
        if (frio) flags |= 64;
        if (GestorAudio.HayTension) flags |= 128;
        w.Write((byte)flags);
        w.Write(GestorAudio.AmenazaActual);

        List<ObjetoReparable> objetos = p != null && p.Mapa != null ? p.Mapa.objetos : null;
        int n = objetos != null ? objetos.Count : 0;
        w.Write((ushort)n);
        for (int i = 0; i < n; i++)
        {
            ObjetoReparable o = objetos[i];
            byte b = o != null ? (byte)((int)o.Estado | (o.Amenazado ? 4 : 0)) : (byte)0;
            w.Write(b);
        }
        List<int> leidas = new List<int>();
        if (p != null) leidas.AddRange(p.InfoLeidas);
        w.Write((byte)Mathf.Min(255, leidas.Count));
        for (int i = 0; i < leidas.Count && i < 255; i++) w.Write((byte)Mathf.Clamp(leidas[i], 0, 255));

        w.Write(inv != null);
        if (inv != null) { w.Write(inv.transform.position.x); w.Write(inv.transform.position.y); }
        IraEstudiante ira = inv != null ? inv.ira : null;
        w.Write(ira != null ? ira.Carga : 0f);
        w.Write(ira != null && ira.EstaActiva);
        w.Write(ira != null && ira.Transformando);
    }

    private static readonly List<int> leidasTmp = new List<int>();

    public static void LeerHud(BinaryReader r)
    {
        int puntos = r.ReadInt32();
        int vidas = r.ReadInt32();
        string mensaje = r.ReadString();
        string poderes = r.ReadString();
        float energia = r.ReadSingle();
        int flags = r.ReadByte();
        float amenaza = r.ReadSingle();
        int n = r.ReadUInt16();
        PisoCenital p = PisoCenital.Actual;
        List<ObjetoReparable> objetos = p != null && p.Mapa != null ? p.Mapa.objetos : null;
        for (int i = 0; i < n; i++)
        {
            byte b = r.ReadByte();
            if (objetos == null || i >= objetos.Count || objetos[i] == null) continue;
            objetos[i].FijarEstadoRed((EstadoObjeto)(b & 3), (b & 4) != 0);
        }
        int nl = r.ReadByte();
        leidasTmp.Clear();
        for (int i = 0; i < nl; i++) leidasTmp.Add(r.ReadByte());
        bool hayInv = r.ReadBoolean();
        Vector2 posInv = Vector2.zero;
        if (hayInv) posInv = new Vector2(r.ReadSingle(), r.ReadSingle());
        float carga = r.ReadSingle();
        bool iraActiva = r.ReadBoolean();
        bool iraTransformando = r.ReadBoolean();

        FuriaRed = (flags & 16) != 0;
        RabiaRed = (flags & 32) != 0;
        FrioRed = (flags & 64) != 0;
        GestorAudio.Tension("red", (flags & 128) != 0);
        if (amenaza > 0f) GestorAudio.Amenaza(amenaza);

        if (p != null) p.FijarEstadoRed(energia, (flags & 1) != 0, (flags & 2) != 0, (flags & 4) != 0, (flags & 8) != 0, leidasTmp);

        if (actual == null) return;
        if (actual.gestor == null) actual.gestor = Object.FindAnyObjectByType<GestorJuego>();
        if (actual.gestor != null) actual.gestor.FijarEspejo(puntos, vidas, mensaje, poderes);
        if (actual.iraEspejo != null) actual.iraEspejo.FijarEspejo(carga, iraActiva, iraTransformando);
        if (hayInv && actual.marcador != null)
        {
            actual.objetivoMarcador = new Vector3(posInv.x, posInv.y, 0f);
            if (!actual.tieneMarcador) { actual.marcador.position = actual.objetivoMarcador; actual.tieneMarcador = true; }
        }
    }

    private Vector3 objetivoMarcador;

    // ------------------------------------------------------------ eventos (invitado)
    private static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    private static void AplicarEvento(BinaryReader r)
    {
        byte ev = r.ReadByte();
        switch (ev)
        {
            case EvRed.Sonido:
            {
                string nombre = r.ReadString();
                float vol = r.ReadSingle(), tono = r.ReadSingle();
                GestorAudio.Reproducir(nombre, vol, tono);
                break;
            }
            case EvRed.Clip:
            {
                string clave = r.ReadString(), nombre = r.ReadString();
                float vol = r.ReadSingle();
                AudioClip c;
                if (!clips.TryGetValue(nombre, out c))
                {
                    c = null;
                    foreach (AudioClip x in Resources.FindObjectsOfTypeAll<AudioClip>()) if (x != null && x.name == nombre) { c = x; break; }
                    clips[nombre] = c;
                }
                if (c != null) GestorAudio.ReproducirClip(clave, c, vol);
                break;
            }
            case EvRed.Bucle:
            {
                string clave = r.ReadString(), nombre = r.ReadString();
                bool activo = r.ReadBoolean();
                float vol = r.ReadSingle();
                GestorAudio.Bucle("red_" + clave, nombre, activo, vol);
                break;
            }
            case EvRed.Temblor:
                CamaraPisoCenital.Temblar(r.ReadSingle(), r.ReadSingle());
                break;
            case EvRed.AvisoUlti:
            {
                string m = r.ReadString();
                float s = r.ReadSingle();
                bool bordes = r.ReadBoolean();
                Color c = new Color(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                AvisoUlti.Mostrar(m, s, bordes, c);
                break;
            }
            case EvRed.OcultarUlti:
                AvisoUlti.Ocultar();
                break;
            case EvRed.DatoUtil:
            {
                int i = r.ReadInt32();
                bool primera = r.ReadBoolean();
                PuntoInformacion.MostrarRemoto(i, primera);
                break;
            }
            case EvRed.Secuencia:
                SecuenciaRed.Aplicar(r);   // ascensor, estrellas, ficha, viaje y final: lo mismo que ve el anfitrión
                break;
        }
    }

    // ------------------------------------------------------------ entrada del invitado
    /// <summary>El invitado pulsó K + L (o tocó su barra llena).</summary>
    public static void PedirIra() { if (actual != null) actual.pedirIra = true; }

    private static void AplicarEntrada(BinaryReader r)
    {
        Vector2 mover = new Vector2(r.ReadSingle(), r.ReadSingle());
        int golpes = r.ReadByte();
        bool ira = r.ReadBoolean();
        JugadorCenital j = JugadorCenital.PorIndice(1);
        if (j == null) return;
        j.FijarEntradaRemota(mover, golpes);
        if (ira && j.ira != null) j.ira.ActivarRemoto();
    }

    private void Update()
    {
        if (anfitrion || !RedJuego.EsInvitadoEnPartida) return;

        // El marcador (lo que sigue la cámara) se mueve suave hacia el personaje.
        if (marcador != null && tieneMarcador)
            marcador.position = Vector3.Lerp(marcador.position, objetivoMarcador, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));

        bool pausa = gestor != null && (gestor.EstaEnPausa || gestor.PartidaTerminada);
        Keyboard k = Keyboard.current;
        if (!pausa)
        {
            bool enter = k != null && (k[Key.Enter].wasPressedThisFrame || k[Key.NumpadEnter].wasPressedThisFrame);
            if (ControlesTactiles.RepararPulsado()) enter = true;
            if (enter) martillazos++;
        }

        if (Time.unscaledTime >= proximoEnvio)
        {
            proximoEnvio = Time.unscaledTime + 1f / 30f;
            Vector2 mover = Vector2.zero;
            if (!pausa)
            {
                float x = 0f, y = 0f;
                if (k != null)
                {
                    if (k[Key.A].isPressed || k[Key.LeftArrow].isPressed) x -= 1f;
                    if (k[Key.D].isPressed || k[Key.RightArrow].isPressed) x += 1f;
                    if (k[Key.W].isPressed || k[Key.UpArrow].isPressed) y += 1f;
                    if (k[Key.S].isPressed || k[Key.DownArrow].isPressed) y -= 1f;
                }
                if (ControlesTactiles.Izquierda) x -= 1f;
                if (ControlesTactiles.Derecha) x += 1f;
                if (ControlesTactiles.Arriba) y += 1f;
                if (ControlesTactiles.Abajo) y -= 1f;
                mover = new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
                if (mover.sqrMagnitude > 1f) mover.Normalize();
            }
            int golpes = Mathf.Min(martillazos, 3);
            bool ira = pedirIra;
            martillazos = 0;
            pedirIra = false;
            RedJuego.Enviar(MsgRed.Entrada, w => { w.Write(mover.x); w.Write(mover.y); w.Write((byte)golpes); w.Write(ira); });
        }

        if (Time.unscaledTime >= proximaLimpieza)
        {
            proximaLimpieza = Time.unscaledTime + 1f;
            OcultarLocales();
        }
    }

    /// <summary>
    /// Invitado: lo que se dibuja fuera del mapa lo manda el anfitrión, así que se ocultan las copias locales que hayan
    /// quedado (para no ver doble). El mapa, el ambiente y el espejo se quedan.
    /// </summary>
    private void OcultarLocales()
    {
        PisoCenital p = PisoCenital.Actual;
        Transform mapa = p != null && p.Mapa != null ? p.Mapa.transform : null;
        Transform espejo = EspejoInvitado.Actual != null ? EspejoInvitado.Actual.Raiz : null;
        foreach (SpriteRenderer sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude))
        {
            if (!sr.enabled) continue;
            if (Local(sr.transform, mapa, espejo)) continue;
            sr.enabled = false;
        }
        foreach (TextMesh tm in FindObjectsByType<TextMesh>(FindObjectsInactive.Exclude))
        {
            MeshRenderer mr = tm.GetComponent<MeshRenderer>();
            if (mr == null || !mr.enabled) continue;
            if (Local(tm.transform, mapa, espejo)) continue;
            mr.enabled = false;
        }
    }

    private static bool Local(Transform t, Transform mapa, Transform espejo)
    {
        if (espejo != null && t.IsChildOf(espejo)) return true;
        if (mapa != null && t.IsChildOf(mapa)) return true;
        if (t.GetComponentInParent<AmbienteNivel>(true) != null) return true;
        if (t.GetComponentInParent<Camera>(true) != null) return true;
        if (t.GetComponentInParent<Canvas>(true) != null) return true;
        return false;
    }
}

/// <summary>Carteles simples de la red (esperando, desconexión), con el tema del juego.</summary>
public static class AvisoRed
{
    private static string pendiente;
    private static float segundosPendiente;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Iniciar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    /// <summary>Muestra un mensaje unos segundos (si se está cambiando de escena, aparece en la siguiente).</summary>
    public static void Mostrar(string texto, float segundos)
    {
        pendiente = texto;
        segundosPendiente = segundos;
    }

    private static void AlCargar(Scene s, LoadSceneMode m)
    {
        if (string.IsNullOrEmpty(pendiente)) return;
        Canvas c = CrearCartel(pendiente);
        Object.Destroy(c.gameObject, Mathf.Max(1.5f, segundosPendiente));
        pendiente = null;
    }

    public static Canvas CrearCartel(string texto)
    {
        Canvas canvas = FabricaUIArcade.CrearCanvas("AvisoRed", 950);
        UnityEngine.UI.Image velo = FabricaUIArcade.CrearImagen(canvas.transform, "Franja", null, new Color(0.02f, 0.03f, 0.08f, 0.85f));
        FabricaUIArcade.Centrar(velo.rectTransform, new Vector2(0f, 0f), new Vector2(2400f, 170f));
        UnityEngine.UI.Text t = FabricaUIArcade.CrearTexto(canvas.transform, "Texto", texto, 40, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, 4f, 0f);
        FabricaUIArcade.Centrar(t.rectTransform, Vector2.zero, new Vector2(1800f, 150f));
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        return canvas;
    }
}
