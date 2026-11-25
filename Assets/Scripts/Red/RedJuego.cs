using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using DemoledorUnifranz.Game;
using DemoledorUnifranz.UI.Menus;

public enum RolRed { Ninguno, Anfitrion, Invitado }

/// <summary>Tipos de mensaje de la red (el primer byte de cada paquete).</summary>
public static class MsgRed
{
    public const byte Hola = 1, Bienvenida = 2, Elegir = 3, Seleccion = 4, Cargar = 5, ListoEscena = 6, Mundo = 7,
        SpriteDef = 8, PedirSprite = 9, SpritePng = 10, Entrada = 11, Evento = 12, Adios = 14, Ping = 15, Rechazo = 16,
        IniciarCuenta = 17;
    // Internos (no viajan por la red): los genera el propio equipo en el hilo principal.
    public const byte Conectado = 200, Desconectado = 201;
}

/// <summary>Subtipos de MsgRed.Evento.</summary>
public static class EvRed
{
    public const byte Sonido = 1, Temblor = 2, AvisoUlti = 3, OcultarUlti = 4, DatoUtil = 5, Bucle = 6, Clip = 7, Secuencia = 8;
}

/// <summary>
/// MULTIJUGADOR POR WI-FI (o el hotspot de un celular). Sin paquetes externos: TCP para el juego y UDP para encontrar
/// partidas en la red. Un equipo es el ANFITRIÓN (simula todo: demoledores, mini villanos, objetos, puntos) y el otro el
/// INVITADO (manda su movimiento y sus martillazos y dibuja lo que le manda el anfitrión).
/// Este objeto vive entre escenas mientras haya una sesión. La interfaz está en LobbyRed y el juego en EspejoRed.
/// </summary>
public class RedJuego : MonoBehaviour
{
    public const int PuertoJuego = 47811;
    public const int PuertoDescubrir = 47812;
    /// <summary>El anfitrión escucha aquí las preguntas "¿hay partida?" del invitado y le contesta directo a él
    /// (sirve aunque el celular o el punto de acceso Wi-Fi bloqueen la difusión, que es lo que pasa en Android).</summary>
    public const int PuertoSondeo = 47813;
    private const string Firma = "REPARALOFRANZ1";

    private static RedJuego inst;

    // ------------------------------------------------------------ estado público
    public static RolRed Rol { get; private set; }
    public static bool Conectado { get { return inst != null && inst.conectado; } }
    public static bool EsAnfitrion { get { return Rol == RolRed.Anfitrion; } }
    public static bool EsInvitado { get { return Rol == RolRed.Invitado; } }
    /// <summary>Hay una sesión de dos jugadores en curso (conectados y la partida empezó).</summary>
    public static bool EnPartida { get { return Rol != RolRed.Ninguno && Conectado && partidaIniciada; } }
    public static bool EsInvitadoEnPartida { get { return EsInvitado && EnPartida; } }
    public static bool EsAnfitrionEnPartida { get { return EsAnfitrion && EnPartida; } }

    /// <summary>Índices en Personajes.Elegibles (-1 = todavía no eligió).</summary>
    public static int PersonajeAnfitrion = -1, PersonajeInvitado = -1;
    public static int PisoElegido;
    /// <summary>El anfitrión eligió SUPERVIVENCIA (se juega en la escena de la planta baja con el escenario EL GRAN HALL).</summary>
    public static bool SupervivenciaElegida;
    private static bool partidaIniciada;

    /// <summary>Último problema de conexión (para mostrarlo en el menú).</summary>
    public static string UltimoMensaje = "";

    /// <summary>Llega un mensaje (en el hilo principal). El lector está colocado después del tipo.</summary>
    public static event Action<byte, BinaryReader> AlMensaje;

    // ------------------------------------------------------------ interno
    private TcpListener oyente;
    private TcpClient cliente;
    private NetworkStream flujo;
    private Thread hiloAceptar, hiloLeer, hiloBuscar, hiloSondeo, hiloPreguntar;
    private UdpClient udpAnuncio, udpBuscar, udpSondeo;
    private readonly ConcurrentQueue<byte[]> entrantes = new ConcurrentQueue<byte[]>();
    private readonly object candado = new object();
    private volatile bool conectado, cerrando;
    private float proximoAnuncio, proximoPing, ultimoRecibido;
    private string nombrePartida = "PARTIDA";
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject bloqueoMulticast;
#endif

    /// <summary>Partidas encontradas en la red: IP → (nombre, piso, última vez vista).</summary>
    public static readonly Dictionary<string, PartidaEncontrada> Encontradas = new Dictionary<string, PartidaEncontrada>();
    public class PartidaEncontrada { public string ip, nombre; public int piso; public float vista; }
    private readonly ConcurrentQueue<PartidaEncontrada> encontradasHilo = new ConcurrentQueue<PartidaEncontrada>();

    private static RedJuego Asegurar()
    {
        if (inst != null) return inst;
        GameObject g = new GameObject("[RedJuego]");
        DontDestroyOnLoad(g);
        inst = g.AddComponent<RedJuego>();
        return inst;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstatico()
    {
        inst = null;
        Rol = RolRed.Ninguno;
        partidaIniciada = false;
        PersonajeAnfitrion = PersonajeInvitado = -1;
        AlMensaje = null;
        Encontradas.Clear();
    }

    // ------------------------------------------------------------ anfitrión
    /// <summary>Abre la partida: espera a un invitado y la anuncia en la red Wi-Fi.</summary>
    public static bool CrearPartida(int piso)
    {
        RedJuego r = Asegurar();
        r.Cerrar(false);
        UltimoMensaje = "";
        Rol = RolRed.Anfitrion;
        PisoElegido = piso;
        PersonajeAnfitrion = PersonajeInvitado = -1;
        partidaIniciada = false;
        r.nombrePartida = (SystemInfo.deviceName ?? "PARTIDA").ToUpperInvariant();
        try
        {
            r.oyente = new TcpListener(IPAddress.Any, PuertoJuego);
            r.oyente.Start(1);
        }
        catch (Exception e)
        {
            UltimoMensaje = "No se pudo abrir la partida (puerto " + PuertoJuego + " ocupado?): " + e.Message;
            Debug.LogWarning("[Red] " + UltimoMensaje);
            Rol = RolRed.Ninguno;
            return false;
        }
        r.cerrando = false;
        r.hiloAceptar = new Thread(r.Aceptar) { IsBackground = true, Name = "RedAceptar" };
        r.hiloAceptar.Start();
        try
        {
            r.udpAnuncio = new UdpClient();
            r.udpAnuncio.EnableBroadcast = true;
        }
        catch (Exception e) { Debug.LogWarning("[Red] Sin anuncio UDP: " + e.Message); }
        try
        {
            r.udpSondeo = new UdpClient();
            r.udpSondeo.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            r.udpSondeo.Client.Bind(new IPEndPoint(IPAddress.Any, PuertoSondeo));
            r.udpSondeo.EnableBroadcast = true;
            r.hiloSondeo = new Thread(r.ContestarSondeos) { IsBackground = true, Name = "RedSondeo" };
            r.hiloSondeo.Start();
        }
        catch (Exception e) { Debug.LogWarning("[Red] Sin sondeo UDP: " + e.Message); r.udpSondeo = null; }
        Debug.Log("[Red] Partida creada. IP: " + IpLocal() + " puerto " + PuertoJuego);
        return true;
    }

    /// <summary>Anfitrión: alguien preguntó "¿hay partida?": se le contesta directo con el anuncio.</summary>
    private void ContestarSondeos()
    {
        UdpClient u = udpSondeo;
        while (u != null && udpSondeo == u)
        {
            try
            {
                IPEndPoint desde = new IPEndPoint(IPAddress.Any, 0);
                byte[] datos = u.Receive(ref desde);
                if (conectado || Rol != RolRed.Anfitrion) continue;
                string t = Encoding.UTF8.GetString(datos);
                if (!t.StartsWith(Firma + "|BUSCO")) continue;
                byte[] anuncio = DatosAnuncio();
                u.Send(anuncio, anuncio.Length, new IPEndPoint(desde.Address, PuertoDescubrir));
            }
            catch { if (udpSondeo != u) return; Thread.Sleep(100); }
        }
    }

    private void Aceptar()
    {
        try
        {
            TcpClient c = oyente.AcceptTcpClient();
            if (cerrando) { c.Close(); return; }
            PrepararSocket(c);
            lock (candado) { cliente = c; flujo = c.GetStream(); }
            conectado = true;
            try { oyente.Stop(); } catch { }
            entrantes.Enqueue(new[] { MsgRed.Conectado });
            hiloLeer = new Thread(Leer) { IsBackground = true, Name = "RedLeer" };
            hiloLeer.Start();
        }
        catch (Exception e)
        {
            if (!cerrando) Debug.LogWarning("[Red] Aceptar: " + e.Message);
        }
    }

    // ------------------------------------------------------------ invitado
    /// <summary>Escucha los anuncios de partidas en la red (se ven en Encontradas).</summary>
    public static void Buscar()
    {
        RedJuego r = Asegurar();
        if (r.udpBuscar != null) return;
        r.AdquirirMulticastAndroid();
        try
        {
            r.udpBuscar = new UdpClient();
            r.udpBuscar.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            r.udpBuscar.Client.Bind(new IPEndPoint(IPAddress.Any, PuertoDescubrir));
            r.udpBuscar.EnableBroadcast = true;
            r.hiloBuscar = new Thread(r.EscucharAnuncios) { IsBackground = true, Name = "RedBuscar" };
            r.hiloBuscar.Start();
            r.hiloPreguntar = new Thread(r.Preguntar) { IsBackground = true, Name = "RedPreguntar" };
            r.hiloPreguntar.Start();
        }
        catch (Exception e)
        {
            UltimoMensaje = "No se puede buscar partidas: " + e.Message + ". Escribe la IP del anfitrión.";
            Debug.LogWarning("[Red] " + UltimoMensaje);
            r.udpBuscar = null;
        }
    }

    public static void DejarDeBuscar()
    {
        if (inst == null) return;
        try { if (inst.udpBuscar != null) inst.udpBuscar.Close(); } catch { }
        inst.udpBuscar = null;
        inst.LiberarMulticastAndroid();
    }

    /// <summary>
    /// Invitado: cada 1,5 s pregunta "¿hay partida?" a todas las direcciones de su red Wi-Fi (x.x.x.1 a x.x.x.254) y a las
    /// típicas de los puntos de acceso de celulares. El anfitrión contesta directo: así aparece en la lista aunque la red no
    /// deje pasar la difusión (lo normal cuando un celular comparte internet).
    /// </summary>
    private void Preguntar()
    {
        UdpClient u = udpBuscar;
        byte[] pregunta = Encoding.UTF8.GetBytes(Firma + "|BUSCO");
        string[] puntosDeAcceso = { "192.168.43.1", "192.168.49.1", "192.168.137.1", "172.20.10.1", "192.168.0.1", "192.168.1.1" };
        while (u != null && udpBuscar == u)
        {
            try
            {
                HashSet<string> enviados = new HashSet<string>();
                Action<string> enviar = dir =>
                {
                    if (!enviados.Add(dir)) return;
                    try { u.Send(pregunta, pregunta.Length, new IPEndPoint(IPAddress.Parse(dir), PuertoSondeo)); } catch { }
                };
                enviar("255.255.255.255");
                foreach (string pa in puntosDeAcceso) enviar(pa);
                foreach (IPAddress propia in IpsLocales())
                {
                    byte[] b = propia.GetAddressBytes();
                    string red = b[0] + "." + b[1] + "." + b[2] + ".";
                    enviar(red + "255");
                    for (int i = 1; i <= 254; i++)
                    {
                        if (i == b[3]) continue;
                        enviar(red + i);
                        if (i % 64 == 0) Thread.Sleep(5);   // sin ahogar la red
                    }
                }
            }
            catch { }
            for (int k = 0; k < 15 && udpBuscar == u; k++) Thread.Sleep(100);
        }
    }

    /// <summary>Las IPv4 de este equipo en redes locales (Wi-Fi, punto de acceso, cable). Sin datos móviles ni VPN.</summary>
    private static List<IPAddress> IpsLocales()
    {
        List<IPAddress> l = new List<IPAddress>();
        try
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                string nombre = (ni.Name ?? "").ToLowerInvariant();
                if (nombre.StartsWith("rmnet") || nombre.StartsWith("ccmni") || nombre.StartsWith("pdp") || nombre.StartsWith("radio") ||
                    nombre.StartsWith("tun") || nombre.StartsWith("dummy") || nombre.Contains("vpn")) continue;
                foreach (UnicastIPAddressInformation u in ni.GetIPProperties().UnicastAddresses)
                {
                    if (u.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (!EsPrivada(u.Address)) continue;
                    if (!l.Contains(u.Address)) l.Add(u.Address);
                }
            }
        }
        catch { }
        if (l.Count == 0)
        {
            IPAddress a;
            if (IPAddress.TryParse(IpLocal(), out a) && a.AddressFamily == AddressFamily.InterNetwork && EsPrivada(a)) l.Add(a);
        }
        return l;
    }

    private static bool EsPrivada(IPAddress a)
    {
        byte[] b = a.GetAddressBytes();
        if (b.Length != 4) return false;
        return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168);
    }

    private void EscucharAnuncios()
    {
        UdpClient u = udpBuscar;
        while (u != null && udpBuscar == u)
        {
            try
            {
                IPEndPoint desde = new IPEndPoint(IPAddress.Any, 0);
                byte[] datos = u.Receive(ref desde);
                string t = Encoding.UTF8.GetString(datos);
                string[] p = t.Split('|');
                if (p.Length < 3 || p[0] != Firma) continue;
                int piso; int.TryParse(p[2], out piso);
                encontradasHilo.Enqueue(new PartidaEncontrada { ip = desde.Address.ToString(), nombre = p[1], piso = piso });
            }
            catch { if (udpBuscar != u) return; Thread.Sleep(200); }
        }
    }

    /// <summary>Se conecta al anfitrión (en segundo plano). El resultado llega como MsgRed.Conectado o un UltimoMensaje.</summary>
    public static void Unirse(string ip)
    {
        RedJuego r = Asegurar();
        r.Cerrar(false);
        UltimoMensaje = "";
        Rol = RolRed.Invitado;
        partidaIniciada = false;
        PersonajeAnfitrion = PersonajeInvitado = -1;
        r.cerrando = false;
        string destino = (ip ?? "").Trim();
        Thread t = new Thread(() => r.Conectar(destino)) { IsBackground = true, Name = "RedConectar" };
        t.Start();
    }

    private void Conectar(string ip)
    {
        try
        {
            TcpClient c = new TcpClient();
            IAsyncResult ar = c.BeginConnect(ip, PuertoJuego, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5)))
            {
                c.Close();
                UltimoMensaje = "No respondió " + ip + ". ¿Están en la misma red Wi-Fi?";
                entrantes.Enqueue(new[] { MsgRed.Desconectado });
                return;
            }
            c.EndConnect(ar);
            PrepararSocket(c);
            lock (candado) { cliente = c; flujo = c.GetStream(); }
            conectado = true;
            entrantes.Enqueue(new[] { MsgRed.Conectado });
            hiloLeer = new Thread(Leer) { IsBackground = true, Name = "RedLeer" };
            hiloLeer.Start();
        }
        catch (Exception e)
        {
            UltimoMensaje = "No se pudo conectar a " + ip + ": " + e.Message;
            entrantes.Enqueue(new[] { MsgRed.Desconectado });
        }
    }

    private static void PrepararSocket(TcpClient c)
    {
        c.NoDelay = true;
        c.ReceiveBufferSize = 1 << 18;
        c.SendBufferSize = 1 << 18;
    }

    // ------------------------------------------------------------ lectura (hilo)
    private void Leer()
    {
        byte[] cabecera = new byte[4];
        try
        {
            while (conectado && !cerrando)
            {
                if (!LeerExacto(cabecera, 4)) break;
                int largo = BitConverter.ToInt32(cabecera, 0);
                if (largo <= 0 || largo > 16 * 1024 * 1024) break;
                byte[] datos = new byte[largo];
                if (!LeerExacto(datos, largo)) break;
                entrantes.Enqueue(datos);
            }
        }
        catch (Exception e)
        {
            if (!cerrando) Debug.LogWarning("[Red] Lectura: " + e.Message);
        }
        if (!cerrando)
        {
            if (string.IsNullOrEmpty(UltimoMensaje)) UltimoMensaje = "Se perdió la conexión con tu compañero.";
            entrantes.Enqueue(new[] { MsgRed.Desconectado });
        }
        conectado = false;
    }

    private bool LeerExacto(byte[] b, int n)
    {
        int leidos = 0;
        NetworkStream f = flujo;
        while (leidos < n)
        {
            int r = f.Read(b, leidos, n - leidos);
            if (r <= 0) return false;
            leidos += r;
        }
        return true;
    }

    // ------------------------------------------------------------ envío
    private static readonly MemoryStream bufferEnvio = new MemoryStream(1 << 16);
    private static readonly BinaryWriter escritor = new BinaryWriter(bufferEnvio, Encoding.UTF8);

    /// <summary>Envía un mensaje: 'escribir' llena el contenido después del tipo. Solo desde el hilo principal.</summary>
    public static void Enviar(byte tipo, Action<BinaryWriter> escribir = null)
    {
        if (inst == null || !inst.conectado) return;
        bufferEnvio.SetLength(0);
        escritor.Write(0);        // largo (se completa abajo)
        escritor.Write(tipo);
        if (escribir != null) escribir(escritor);
        escritor.Flush();
        int largo = (int)bufferEnvio.Length - 4;
        byte[] datos = bufferEnvio.GetBuffer();
        byte[] l = BitConverter.GetBytes(largo);
        Buffer.BlockCopy(l, 0, datos, 0, 4);
        try
        {
            lock (inst.candado)
            {
                if (inst.flujo != null) inst.flujo.Write(datos, 0, (int)bufferEnvio.Length);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Red] Envío: " + e.Message);
            UltimoMensaje = "Se perdió la conexión con tu compañero.";
            inst.conectado = false;
            inst.entrantes.Enqueue(new[] { MsgRed.Desconectado });
        }
    }

    // ------------------------------------------------------------ ciclo (hilo principal)
    private void Update()
    {
        // Anuncio de la partida en la red, una vez por segundo, mientras espera al invitado.
        if (Rol == RolRed.Anfitrion && !conectado && udpAnuncio != null && Time.unscaledTime >= proximoAnuncio)
        {
            proximoAnuncio = Time.unscaledTime + 1f;
            Anunciar();
        }

        PartidaEncontrada pe;
        while (encontradasHilo.TryDequeue(out pe))
        {
            pe.vista = Time.unscaledTime;
            Encontradas[pe.ip] = pe;
        }

        byte[] m;
        int vueltas = 0;
        while (vueltas++ < 400 && entrantes.TryDequeue(out m))
        {
            if (m.Length == 0) continue;
            byte tipo = m[0];
            if (tipo != MsgRed.Conectado && tipo != MsgRed.Desconectado) ultimoRecibido = Time.unscaledTime;
            if (tipo == MsgRed.Conectado) { ultimoRecibido = Time.unscaledTime; AlConectar(); }
            if (tipo == MsgRed.Desconectado) { AlDesconectar(); }
            if (tipo == MsgRed.Ping) continue;
            if (tipo == MsgRed.Adios)
            {
                using (BinaryReader rr = new BinaryReader(new MemoryStream(m, 1, m.Length - 1)))
                    UltimoMensaje = rr.BaseStream.Length > 0 ? rr.ReadString() : "Tu compañero salió de la partida.";
                Cerrar(true);
                continue;
            }
            if (AlMensaje != null)
            {
                using (BinaryReader r = new BinaryReader(new MemoryStream(m, 1, m.Length - 1), Encoding.UTF8))
                {
                    try { AlMensaje(tipo, r); }
                    catch (Exception e) { Debug.LogError("[Red] Error procesando mensaje " + tipo + ": " + e); }
                }
            }
        }

        if (conectado)
        {
            if (Time.unscaledTime >= proximoPing)
            {
                proximoPing = Time.unscaledTime + 1f;
                Enviar(MsgRed.Ping);
            }
            if (ultimoRecibido > 0f && Time.unscaledTime - ultimoRecibido > 10f)
            {
                UltimoMensaje = "Tu compañero dejó de responder.";
                Cerrar(true);
            }
        }
    }

    private void AlConectar()
    {
        Debug.Log("[Red] Conectado como " + Rol + ".");
        if (Rol == RolRed.Invitado)
            Enviar(MsgRed.Hola, w => { w.Write(Firma); w.Write((SystemInfo.deviceName ?? "INVITADO").ToUpperInvariant()); });
    }

    private void AlDesconectar()
    {
        Debug.Log("[Red] Desconectado: " + UltimoMensaje);
        Cerrar(true);
    }

    private byte[] DatosAnuncio() { return Encoding.UTF8.GetBytes(Firma + "|" + nombrePartida + "|" + PisoElegido); }

    private void Anunciar()
    {
        byte[] datos = DatosAnuncio();
        foreach (IPAddress destino in DireccionesDifusion())
        {
            try { udpAnuncio.Send(datos, datos.Length, new IPEndPoint(destino, PuertoDescubrir)); }
            catch { }
        }
    }

    /// <summary>Difusión general y la de cada red local (algunas PC con varias tarjetas solo la reciben así).</summary>
    private static List<IPAddress> DireccionesDifusion()
    {
        List<IPAddress> l = new List<IPAddress> { IPAddress.Broadcast };
        try
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                foreach (UnicastIPAddressInformation u in ni.GetIPProperties().UnicastAddresses)
                {
                    if (u.Address.AddressFamily != AddressFamily.InterNetwork || u.IPv4Mask == null) continue;
                    byte[] ip = u.Address.GetAddressBytes(), mascara = u.IPv4Mask.GetAddressBytes();
                    if (mascara.Length != 4) continue;
                    byte[] b = new byte[4];
                    for (int i = 0; i < 4; i++) b[i] = (byte)(ip[i] | ~mascara[i]);
                    IPAddress d = new IPAddress(b);
                    if (!l.Contains(d)) l.Add(d);
                }
            }
        }
        catch { }
        return l;
    }

    /// <summary>La IP de este equipo en la red local (para dictársela al compañero).</summary>
    public static string IpLocal()
    {
        string respaldo = null;
        try
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                IPInterfaceProperties prop = ni.GetIPProperties();
                bool conPuerta = false;
                foreach (GatewayIPAddressInformation g in prop.GatewayAddresses)
                    if (g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any)) conPuerta = true;
                foreach (UnicastIPAddressInformation u in prop.UnicastAddresses)
                {
                    if (u.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    string s = u.Address.ToString();
                    if (s.StartsWith("169.254.")) continue;
                    if (conPuerta) return s;
                    if (respaldo == null) respaldo = s;
                }
            }
        }
        catch { }
        if (respaldo != null) return respaldo;
        try
        {
            foreach (IPAddress a in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (a.AddressFamily == AddressFamily.InterNetwork) return a.ToString();
        }
        catch { }
        return "?";
    }

    // ------------------------------------------------------------ atajos del juego
    /// <summary>Invitado: pide su furia (K + L) al anfitrión.</summary>
    public static void EnviarIra() { MultijugadorNivel.PedirIra(); }

    /// <summary>Anfitrión: el personaje del invitado pisó un "?": el dato se muestra en su pantalla.</summary>
    public static void EnviarDatoUtil(int indice, bool primeraVez)
    {
        Enviar(MsgRed.Evento, w => { w.Write(EvRed.DatoUtil); w.Write(indice); w.Write(primeraVez); });
    }

    // ------------------------------------------------------------ sesión
    /// <summary>Empieza la partida en red (los dos ya eligieron personaje).</summary>
    public static void MarcarPartidaIniciada() { partidaIniciada = true; }

    /// <summary>Termina la sesión. 'avisar': manda un adiós al compañero antes de cerrar.</summary>
    public static void Salir(string motivo = null)
    {
        if (inst == null) return;
        if (inst.conectado) Enviar(MsgRed.Adios, w => w.Write(motivo ?? "Tu compañero salió de la partida."));
        inst.Cerrar(false);
    }

    private void Cerrar(bool avisarJuego)
    {
        bool estabaJugando = partidaIniciada;
        cerrando = true;
        conectado = false;
        try { if (oyente != null) oyente.Stop(); } catch { }
        oyente = null;
        lock (candado)
        {
            try { if (flujo != null) flujo.Close(); } catch { }
            try { if (cliente != null) cliente.Close(); } catch { }
            flujo = null;
            cliente = null;
        }
        try { if (udpAnuncio != null) udpAnuncio.Close(); } catch { }
        udpAnuncio = null;
        try { if (udpSondeo != null) udpSondeo.Close(); } catch { }
        udpSondeo = null;
        DejarDeBuscar();
        Rol = RolRed.Ninguno;
        partidaIniciada = false;
        ultimoRecibido = 0f;
        if (avisarJuego)
        {
            if (estabaJugando) MultijugadorNivel.ConexionPerdida();
            else LobbyRed.AvisarDesconexion();
        }
    }

    private void OnApplicationQuit() { Salir("Tu compañero cerró el juego."); }
    private void OnDestroy() { if (inst == this) { Cerrar(false); inst = null; } }

    // ------------------------------------------------------------ Android: recibir difusiones Wi-Fi
    private void AdquirirMulticastAndroid()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass jugador = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject actividad = jugador.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject wifi = actividad.Call<AndroidJavaObject>("getSystemService", "wifi");
                bloqueoMulticast = wifi.Call<AndroidJavaObject>("createMulticastLock", "reparalofranz");
                bloqueoMulticast.Call("setReferenceCounted", false);
                bloqueoMulticast.Call("acquire");
            }
        }
        catch (Exception e) { Debug.LogWarning("[Red] Sin bloqueo multicast: " + e.Message); }
#endif
    }

    private void LiberarMulticastAndroid()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try { if (bloqueoMulticast != null) bloqueoMulticast.Call("release"); } catch { }
        bloqueoMulticast = null;
#endif
    }

    // ------------------------------------------------------------ escenas
    private void OnEnable() { SceneManager.sceneLoaded += AlCargarEscena; }
    private void OnDisable() { SceneManager.sceneLoaded -= AlCargarEscena; }

    /// <summary>El anfitrión cambió de escena: el invitado lo sigue (con los datos de la partida para la pantalla final).</summary>
    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (!EnPartida) return;
        if (EsAnfitrion)
        {
            if (escena.name == Escenas.MenuPrincipal)
            {
                Salir("La partida terminó.");
                return;
            }
            if (escena.name == Escenas.NivelFinal)
            {
                // El nivel final (la fachada) es de un jugador.
                Salir("¡Superaron los pisos! El nivel final de la fachada es para un solo jugador.");
                return;
            }
            Enviar(MsgRed.Cargar, w =>
            {
                w.Write(escena.name);
                w.Write(GameSession.PisoActual);
                w.Write(GameSession.PuntosAcumulados);
                w.Write(GameSession.VidasAlEmpezarPiso);
                w.Write(GameSession.Puntaje);
                w.Write(GameSession.Victoria);
                w.Write(GameSession.HayDatosDerrumbe);
                w.Write(GameSession.Reparaciones);
                w.Write(GameSession.PorcentajeDestruido);
                w.Write(PersonajeAnfitrion);
                w.Write(PersonajeInvitado);
                w.Write((byte)Dificultad.Actual);         // la dificultad la elige quien crea la partida
                w.Write(GameSession.Supervivencia);
            });
        }
    }
}
