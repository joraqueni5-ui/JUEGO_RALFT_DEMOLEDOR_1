using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DemoledorUnifranz.Game;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// Menú MULTIJUGADOR: CREAR PARTIDA (elige el piso y espera) o UNIRSE (lista de partidas de la red Wi-Fi o la IP a mano).
/// Al conectarse, los dos eligen personaje a la vez (no se puede repetir: el del compañero queda bloqueado) y, cuando
/// los dos están listos, cuenta 3 y empieza el piso en los dos equipos.
/// </summary>
public class LobbyRed : MonoBehaviour
{
    private static LobbyRed abierto;

    private static readonly string[] NombresPisos =
    {
        "PLANTA BAJA", "PISO 1 · GASTRONOMÍA Y ENFERMERÍA", "PISO 2 · INFORMÁTICA", "PISO 3 · AULAS 300",
        "PISO 4 · AULAS 400", "PISO 5 · AULAS 500", "PISO 6 · TERRAZA Y BIBLIOTECA",
    };

    private enum Pantalla { Inicio, ElegirPiso, Esperando, Unirse, Eligiendo, Cuenta }
    private Pantalla pantalla;

    private KitSpritesArcade kit;
    private RectTransform contenido;
    private Text titulo, subtitulo, estado;
    private InputField campoIp;
    private RectTransform listaPartidas;
    private readonly List<GameObject> botonesPartidas = new List<GameObject>();
    private float proximoRefresco, finCuenta;
    private int cuentaMostrada;
    private string nombreCompanero = "";

    public static bool Abierto { get { return abierto != null; } }

    /// <summary>Abre el menú multijugador (desde el menú principal).</summary>
    public static void Mostrar()
    {
        if (abierto != null) return;
        Canvas canvas = FabricaUIArcade.CrearCanvas("LobbyMultijugador", 850);
        FabricaUIArcade.AsegurarEventSystem();
        abierto = canvas.gameObject.AddComponent<LobbyRed>();
        abierto.Construir();
    }

    /// <summary>La conexión se cortó antes de empezar a jugar: vuelve al inicio del lobby con el motivo.</summary>
    public static void AvisarDesconexion()
    {
        SelectorPersonaje.CerrarAbierto();
        if (abierto == null) return;
        abierto.IrA(Pantalla.Inicio);
        abierto.estado.text = string.IsNullOrEmpty(RedJuego.UltimoMensaje) ? "Se cortó la conexión." : RedJuego.UltimoMensaje;
        abierto.estado.color = new Color(1f, 0.45f, 0.4f);
    }

    private void OnEnable() { RedJuego.AlMensaje += AlMensaje; }
    private void OnDisable() { RedJuego.AlMensaje -= AlMensaje; }
    private void OnDestroy() { if (abierto == this) abierto = null; }

    // ------------------------------------------------------------ construcción
    private void Construir()
    {
        kit = KitSpritesArcade.Actual();
        Image velo = FabricaUIArcade.CrearImagen(transform, "Velo", null, new Color(0.02f, 0.03f, 0.08f, 0.92f), true);
        FabricaUIArcade.Estirar(velo.rectTransform);

        titulo = FabricaUIArcade.CrearTexto(transform, "Titulo", "MULTIJUGADOR", 72, PaletaArcade.Amarillo);
        FabricaUIArcade.Centrar(titulo.rectTransform, new Vector2(0f, 440f), new Vector2(1700f, 100f));
        subtitulo = FabricaUIArcade.CrearTexto(transform, "Subtitulo", "", 28, PaletaArcade.Crema, TextAnchor.MiddleCenter, 3f, 0f, true);
        FabricaUIArcade.Centrar(subtitulo.rectTransform, new Vector2(0f, 370f), new Vector2(1700f, 44f));
        estado = FabricaUIArcade.CrearTexto(transform, "Estado", "", 30, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, 3f, 0f, true);
        FabricaUIArcade.Centrar(estado.rectTransform, new Vector2(0f, -400f), new Vector2(1700f, 90f));
        estado.horizontalOverflow = HorizontalWrapMode.Wrap;

        contenido = FabricaUIArcade.CrearObjeto("Contenido", transform);
        FabricaUIArcade.Estirar(contenido);
        IrA(Pantalla.Inicio);
    }

    private void Limpiar()
    {
        for (int i = contenido.childCount - 1; i >= 0; i--) Destroy(contenido.GetChild(i).gameObject);
        botonesPartidas.Clear();
        campoIp = null;
        listaPartidas = null;
    }

    private Button Boton(string texto, Sprite sprite, Vector2 pos, Vector2 tam, UnityEngine.Events.UnityAction accion, int fuente = 38, Sprite icono = null)
    {
        Button b = FabricaUIArcade.CrearBoton(contenido, "Boton_" + texto, texto, sprite, tam, fuente, icono, kit);
        FabricaUIArcade.Centrar((RectTransform)b.transform, pos, tam);
        b.onClick.AddListener(() => { GestorAudio.Reproducir(Snd.MenuConfirmar); accion(); });
        return b;
    }

    private void Seleccionar(Button b)
    {
        if (b != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(b.gameObject);
    }

    private void IrA(Pantalla p)
    {
        pantalla = p;
        Limpiar();
        estado.color = PaletaArcade.Amarillo;
        switch (p)
        {
            case Pantalla.Inicio:
                titulo.text = "MULTIJUGADOR";
                subtitulo.text = "DOS JUGADORES EN LA MISMA RED WI-FI (O EN EL HOTSPOT DE UN CELULAR)";
                estado.text = "Uno crea la partida y el otro se une. ¡Y llegan DOS demoledores!";
                Button crear = Boton("CREAR PARTIDA", kit.botonAmarillo, new Vector2(0f, 150f), new Vector2(640f, 110f), () => IrA(Pantalla.ElegirPiso), 42, kit.iconoJugar);
                Button unirse = Boton("UNIRSE", kit.botonAzul, new Vector2(0f, 10f), new Vector2(640f, 110f), () => { RedJuego.Buscar(); IrA(Pantalla.Unirse); }, 42, kit.iconoArriba);
                Button volver = Boton("VOLVER", kit.botonRojo, new Vector2(0f, -140f), new Vector2(480f, 96f), Cerrar, 36, kit.iconoCasa);
                FabricaUIArcade.NavegacionVertical(crear, unirse, volver);
                Seleccionar(crear);
                break;

            case Pantalla.ElegirPiso:
                titulo.text = "CREAR PARTIDA";
                subtitulo.text = "¿EN QUÉ PISO JUEGAN?";
                estado.text = "";
                List<Selectable> pisos = new List<Selectable>();
                for (int i = 0; i < NombresPisos.Length; i++)
                {
                    int k = i;
                    string escena = Escenas.ParaPiso(i);
                    if (!NavegadorEscenas.Disponible(escena)) continue;
                    Button b = Boton(NombresPisos[i], i % 2 == 0 ? kit.botonAzul : kit.botonMorado, new Vector2(0f, 290f - pisos.Count * 76f),
                        new Vector2(900f, 68f), () => ElegirDificultadY(k, false), 30);
                    pisos.Add(b);
                }
                // SUPERVIVENCIA: el escenario EL GRAN HALL con oleadas de villanos.
                if (NavegadorEscenas.Disponible(Escenas.ParaPiso(0)))
                    pisos.Add(Boton("SUPERVIVENCIA · EL GRAN HALL", kit.botonRojo, new Vector2(0f, 290f - pisos.Count * 76f), new Vector2(900f, 68f),
                        () => ElegirDificultadY(0, true), 30));
                Button atras = Boton("VOLVER", kit.botonRojo, new Vector2(0f, 290f - pisos.Count * 76f - 14f), new Vector2(420f, 72f), () => IrA(Pantalla.Inicio), 30, kit.iconoCasa);
                pisos.Add(atras);
                FabricaUIArcade.NavegacionVertical(pisos.ToArray());
                Seleccionar(pisos[0] as Button);
                break;

            case Pantalla.Esperando:
                titulo.text = "ESPERANDO A TU COMPAÑERO";
                subtitulo.text = NombresPisos[Mathf.Clamp(RedJuego.PisoElegido, 0, NombresPisos.Length - 1)];
                Text ip = FabricaUIArcade.CrearTexto(contenido, "Ip", "TU IP:  " + RedJuego.IpLocal(), 64, PaletaArcade.Crema);
                FabricaUIArcade.Centrar(ip.rectTransform, new Vector2(0f, 120f), new Vector2(1600f, 90f));
                Text ayuda = FabricaUIArcade.CrearTexto(contenido, "Ayuda",
                    "Tu compañero entra a MULTIJUGADOR → UNIRSE. Si no ve la partida, que escriba esta IP.\n" +
                    "En Windows, si aparece el aviso del Firewall, pulsa \"Permitir acceso\".", 26, PaletaArcade.Crema, TextAnchor.MiddleCenter, 3f, 0f, true);
                FabricaUIArcade.Centrar(ayuda.rectTransform, new Vector2(0f, 20f), new Vector2(1600f, 100f));
                ayuda.horizontalOverflow = HorizontalWrapMode.Wrap;
                Button cancelar = Boton("CANCELAR", kit.botonRojo, new Vector2(0f, -170f), new Vector2(460f, 96f), () => { RedJuego.Salir(); IrA(Pantalla.Inicio); }, 36, kit.iconoCasa);
                Seleccionar(cancelar);
                estado.text = "Esperando...";
                break;

            case Pantalla.Unirse:
                titulo.text = "UNIRSE A UNA PARTIDA";
                subtitulo.text = "PARTIDAS EN TU RED WI-FI";
                estado.text = "Buscando partidas...";
                listaPartidas = FabricaUIArcade.CrearObjeto("Lista", contenido);
                FabricaUIArcade.Centrar(listaPartidas, new Vector2(0f, 150f), new Vector2(1000f, 300f));
                CrearCampoIp();
                Button conectar = Boton("CONECTAR", kit.botonAmarillo, new Vector2(300f, -160f), new Vector2(380f, 90f), () => Unirse(campoIp != null ? campoIp.text : ""), 34, kit.iconoJugar);
                Button volver2 = Boton("VOLVER", kit.botonRojo, new Vector2(0f, -280f), new Vector2(420f, 86f), () => { RedJuego.DejarDeBuscar(); RedJuego.Salir(); IrA(Pantalla.Inicio); }, 32, kit.iconoCasa);
                FabricaUIArcade.NavegacionVertical(conectar, volver2);
                proximoRefresco = 0f;
                break;

            case Pantalla.Eligiendo:
                titulo.text = "¡CONECTADOS!";
                subtitulo.text = nombreCompanero;
                estado.text = "";
                break;

            case Pantalla.Cuenta:
                titulo.text = "¡A REPARAR JUNTOS!";
                subtitulo.text = NombresPisos[Mathf.Clamp(RedJuego.PisoElegido, 0, NombresPisos.Length - 1)];
                break;
        }
    }

    private void CrearCampoIp()
    {
        Text etiqueta = FabricaUIArcade.CrearTexto(contenido, "EtiquetaIp", "O ESCRIBE LA IP DEL ANFITRIÓN:", 28, PaletaArcade.Crema, TextAnchor.MiddleLeft, 3f, 0f, true);
        FabricaUIArcade.Centrar(etiqueta.rectTransform, new Vector2(-160f, -90f), new Vector2(900f, 40f));

        Image fondo = FabricaUIArcade.CrearImagen(contenido, "CampoIp", kit.panel, Color.white, true);
        fondo.type = Image.Type.Sliced;
        FabricaUIArcade.Centrar(fondo.rectTransform, new Vector2(-200f, -160f), new Vector2(560f, 86f));
        Text texto = FabricaUIArcade.CrearTexto(fondo.transform, "Texto", "", 40, PaletaArcade.Crema, TextAnchor.MiddleCenter, 2f, 0f, true);
        FabricaUIArcade.Estirar(texto.rectTransform);
        texto.supportRichText = false;
        Text marcador = FabricaUIArcade.CrearTexto(fondo.transform, "Marcador", "192.168.1.10", 40, new Color(1f, 1f, 1f, 0.3f), TextAnchor.MiddleCenter, 0f, 0f, true);
        FabricaUIArcade.Estirar(marcador.rectTransform);
        campoIp = fondo.gameObject.AddComponent<InputField>();
        campoIp.textComponent = texto;
        campoIp.placeholder = marcador;
        campoIp.characterLimit = 15;
        campoIp.contentType = InputField.ContentType.Standard;
        campoIp.characterValidation = InputField.CharacterValidation.None;
        campoIp.text = PlayerPrefs.GetString("red_ultima_ip", "");
        // En el celular: teclado con números y punto, y el cuadro de texto del teclado visible.
        campoIp.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
        campoIp.shouldHideMobileInput = false;
        campoIp.onEndEdit.AddListener(v =>
        {
            Keyboard k = Keyboard.current;
            bool enter = k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame);
            bool listo = campoIp != null && campoIp.touchScreenKeyboard != null && campoIp.touchScreenKeyboard.status == TouchScreenKeyboard.Status.Done;
            if ((enter || listo) && !string.IsNullOrEmpty(v) && v.Trim().Length >= 7) Unirse(v);
        });
    }

    // ------------------------------------------------------------ acciones
    /// <summary>Quien crea la partida elige la dificultad (vale para los dos).</summary>
    private void ElegirDificultadY(int piso, bool supervivencia)
    {
        SelectorDificultad.Mostrar(() =>
        {
            RedJuego.SupervivenciaElegida = supervivencia;
            Crear(piso);
            if (pantalla == Pantalla.Esperando) subtitulo.text += "  ·  " + Dificultad.NombreActual + (supervivencia ? "  ·  SUPERVIVENCIA" : "");
        }, null, "La eligen los dos jugadores: tú la decides");
    }

    private void Crear(int piso)
    {
        if (!RedJuego.CrearPartida(piso))
        {
            IrA(Pantalla.Inicio);
            estado.text = RedJuego.UltimoMensaje;
            estado.color = new Color(1f, 0.45f, 0.4f);
            return;
        }
        IrA(Pantalla.Esperando);
    }

    private void Unirse(string ip)
    {
        ip = (ip ?? "").Trim();
        if (ip.Length < 7) { estado.text = "Escribe una IP como 192.168.1.10"; estado.color = new Color(1f, 0.45f, 0.4f); return; }
        PlayerPrefs.SetString("red_ultima_ip", ip);
        estado.text = "Conectando con " + ip + "...";
        estado.color = PaletaArcade.Amarillo;
        RedJuego.DejarDeBuscar();
        RedJuego.Unirse(ip);
    }

    private void Cerrar()
    {
        RedJuego.DejarDeBuscar();
        if (!RedJuego.EnPartida) RedJuego.Salir();
        SelectorPersonaje.CerrarAbierto();
        Destroy(gameObject);
    }

    // ------------------------------------------------------------ mensajes
    private void AlMensaje(byte tipo, BinaryReader r)
    {
        switch (tipo)
        {
            case MsgRed.Conectado:
                break;

            case MsgRed.Hola:   // anfitrión
            {
                string firma = r.ReadString();
                nombreCompanero = r.ReadString();
                if (!firma.StartsWith("REPARALOFRANZ"))
                {
                    RedJuego.Salir("Versión distinta del juego.");
                    IrA(Pantalla.Inicio);
                    return;
                }
                RedJuego.Enviar(MsgRed.Bienvenida, w => { w.Write((SystemInfo.deviceName ?? "ANFITRION").ToUpperInvariant()); w.Write(RedJuego.PisoElegido); });
                EmpezarSeleccion();
                break;
            }

            case MsgRed.Bienvenida:   // invitado
                nombreCompanero = r.ReadString();
                RedJuego.PisoElegido = r.ReadInt32();
                EmpezarSeleccion();
                break;

            case MsgRed.Elegir:   // anfitrión: el invitado eligió
            {
                int idx = r.ReadInt32();
                if (idx == RedJuego.PersonajeAnfitrion || idx < 0 || idx >= Personajes.Elegibles.Length)
                {
                    RedJuego.Enviar(MsgRed.Rechazo, w => w.Write("¡Ese personaje ya es de tu compañero! Elige otro."));
                    return;
                }
                RedJuego.PersonajeInvitado = idx;
                EnviarSeleccion();
                break;
            }

            case MsgRed.Seleccion:   // invitado
                RedJuego.PersonajeAnfitrion = r.ReadInt32();
                RedJuego.PersonajeInvitado = r.ReadInt32();
                ActualizarEstadoSeleccion();
                break;

            case MsgRed.Rechazo:   // invitado
                RedJuego.PersonajeInvitado = -1;
                SelectorPersonaje.Desbloquear(r.ReadString());
                break;

            case MsgRed.IniciarCuenta:   // invitado
                EmpezarCuenta();
                break;
        }
    }

    private void EmpezarSeleccion()
    {
        GestorAudio.Reproducir(SndExtra.RedConectado);
        RedJuego.DejarDeBuscar();
        IrA(Pantalla.Eligiendo);
        RedJuego.PersonajeAnfitrion = RedJuego.PersonajeInvitado = -1;
        SelectorPersonaje.Ocupado = i => RedJuego.EsAnfitrion ? i == RedJuego.PersonajeInvitado : i == RedJuego.PersonajeAnfitrion;
        SelectorPersonaje.Estado = "";
        SelectorPersonaje.MostrarMulti(AlElegirPropio, () =>
        {
            RedJuego.Salir("Tu compañero salió de la elección de personajes.");
            IrA(Pantalla.Inicio);
        });
    }

    private void AlElegirPropio(int idx)
    {
        if (RedJuego.EsAnfitrion)
        {
            if (idx == RedJuego.PersonajeInvitado) { SelectorPersonaje.Desbloquear("¡Ese ya es de tu compañero! Elige otro."); return; }
            RedJuego.PersonajeAnfitrion = idx;
            EnviarSeleccion();
        }
        else
        {
            RedJuego.Enviar(MsgRed.Elegir, w => w.Write(idx));
        }
    }

    private void EnviarSeleccion()
    {
        RedJuego.Enviar(MsgRed.Seleccion, w => { w.Write(RedJuego.PersonajeAnfitrion); w.Write(RedJuego.PersonajeInvitado); });
        ActualizarEstadoSeleccion();
        if (RedJuego.PersonajeAnfitrion >= 0 && RedJuego.PersonajeInvitado >= 0 && RedJuego.PersonajeAnfitrion != RedJuego.PersonajeInvitado)
        {
            RedJuego.Enviar(MsgRed.IniciarCuenta);
            EmpezarCuenta();
        }
    }

    private void ActualizarEstadoSeleccion()
    {
        int otro = RedJuego.EsAnfitrion ? RedJuego.PersonajeInvitado : RedJuego.PersonajeAnfitrion;
        int propio = RedJuego.EsAnfitrion ? RedJuego.PersonajeAnfitrion : RedJuego.PersonajeInvitado;
        string s = "";
        if (otro >= 0 && otro < Personajes.Elegibles.Length)
            s = "TU COMPAÑERO ELIGIÓ A " + Personajes.Elegibles[otro].nombre.ToUpperInvariant() + " (" + Personajes.Elegibles[otro].carrera + ")";
        else s = "TU COMPAÑERO TODAVÍA ESTÁ ELIGIENDO...";
        if (propio >= 0 && otro < 0) s = "¡LISTO! ESPERANDO A TU COMPAÑERO...";
        SelectorPersonaje.Estado = s;
    }

    private void EmpezarCuenta()
    {
        RedJuego.MarcarPartidaIniciada();
        SelectorPersonaje.CerrarAbierto();
        IrA(Pantalla.Cuenta);
        finCuenta = Time.unscaledTime + 3f;
        cuentaMostrada = -1;
    }

    private void Update()
    {
        if (pantalla == Pantalla.Unirse) RefrescarLista();
        // En el celular el teclado tapa la mitad de abajo: mientras se escribe la IP todo sube y se ve lo que escribes.
        if (contenido != null)
        {
            float alto = Application.isMobilePlatform && campoIp != null && campoIp.isFocused ? 430f : 0f;
            Vector2 pc = contenido.anchoredPosition;
            if (Mathf.Abs(pc.y - alto) > 0.5f)
                contenido.anchoredPosition = new Vector2(pc.x, Mathf.Lerp(pc.y, alto, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime)));
            else if (pc.y != alto) contenido.anchoredPosition = new Vector2(pc.x, alto);
        }
        if (pantalla == Pantalla.Esperando && !string.IsNullOrEmpty(RedJuego.UltimoMensaje) && RedJuego.Rol == RolRed.Ninguno)
        {
            IrA(Pantalla.Inicio);
            estado.text = RedJuego.UltimoMensaje;
        }

        if (pantalla == Pantalla.Cuenta)
        {
            int resto = Mathf.CeilToInt(finCuenta - Time.unscaledTime);
            if (resto != cuentaMostrada)
            {
                cuentaMostrada = resto;
                if (resto > 0)
                {
                    estado.text = "EMPIEZA EN " + resto + "...";
                    GestorAudio.Reproducir(Snd.MenuMover);
                }
            }
            if (resto <= 0 && RedJuego.EsAnfitrion)
            {
                pantalla = Pantalla.Inicio;   // una sola vez
                estado.text = "¡VAMOS!";
                GameSession.SeleccionarNivel(true, RedJuego.PisoElegido);
                GameSession.Supervivencia = RedJuego.SupervivenciaElegida;
                string escena = Escenas.ParaPiso(RedJuego.PisoElegido);
                Destroy(gameObject, 0.5f);
                if (!NavegadorEscenas.Cargar(escena)) UnityEngine.SceneManagement.SceneManager.LoadScene(escena);
            }
            else if (resto <= 0) estado.text = "CARGANDO EL PISO...";
        }

        Keyboard k = Keyboard.current;
        if (k != null && k.escapeKey.wasPressedThisFrame && !SelectorPersonaje.Abierto && !SelectorDificultad.Abierto && pantalla != Pantalla.Cuenta)
        {
            if (pantalla == Pantalla.Inicio) Cerrar();
            else { RedJuego.Salir(); IrA(Pantalla.Inicio); }
        }
    }

    private string firmaListaAnterior = "";

    private void RefrescarLista()
    {
        if (listaPartidas == null || Time.unscaledTime < proximoRefresco) return;
        proximoRefresco = Time.unscaledTime + 0.5f;
        if (RedJuego.Rol == RolRed.Ninguno) RedJuego.Buscar();   // tras un intento fallido sigue buscando
        // Los botones solo se rehacen si cambió la lista (antes se rehacían cada medio segundo y en el celular
        // el toque se perdía porque el botón desaparecía mientras lo apretabas).
        System.Text.StringBuilder firma = new System.Text.StringBuilder();
        foreach (RedJuego.PartidaEncontrada p in RedJuego.Encontradas.Values)
            if (Time.unscaledTime - p.vista <= 6f) firma.Append(p.ip).Append('|').Append(p.nombre).Append('|').Append(p.piso).Append(';');
        string firmaLista = firma.ToString();
        bool igual = firmaLista == firmaListaAnterior && botonesPartidas.Count > 0;
        firmaListaAnterior = firmaLista;
        int n = igual ? botonesPartidas.Count : 0;
        if (!igual)
        {
            foreach (GameObject g in botonesPartidas) if (g != null) Destroy(g);
            botonesPartidas.Clear();
        }
        foreach (RedJuego.PartidaEncontrada p in RedJuego.Encontradas.Values)
        {
            if (igual) break;
            if (Time.unscaledTime - p.vista > 6f) continue;
            string ip = p.ip;
            string texto = p.nombre + "  ·  " + NombresPisos[Mathf.Clamp(p.piso, 0, NombresPisos.Length - 1)];
            Button b = FabricaUIArcade.CrearBoton(listaPartidas, "Partida_" + ip, texto, kit.botonAzul, new Vector2(1000f, 80f), 28, kit.iconoJugar, kit);
            FabricaUIArcade.Centrar((RectTransform)b.transform, new Vector2(0f, 110f - n * 92f), new Vector2(1000f, 80f));
            b.onClick.AddListener(() => Unirse(ip));
            botonesPartidas.Add(b.gameObject);
            if (++n >= 3) break;
        }
        if (RedJuego.Rol == RolRed.Invitado && !RedJuego.Conectado) return;   // conectando: deja el mensaje
        if (!string.IsNullOrEmpty(RedJuego.UltimoMensaje) && RedJuego.Rol == RolRed.Ninguno)
        {
            estado.text = RedJuego.UltimoMensaje;
            estado.color = new Color(1f, 0.45f, 0.4f);
        }
        else estado.text = n == 0 ? "Buscando partidas... (también puedes escribir la IP)" : "Elige la partida de tu compañero.";
    }
}
