using System.Collections;
using System.IO;
using UnityEngine;
using DemoledorUnifranz.UI.Menus;

/// <summary>
/// Multijugador: las escenas del final de cada piso se ven en los DOS equipos. Antes el invitado se quedaba mirando el
/// piso vacío mientras el anfitrión veía el ascensor, las estrellas, la ficha y el viaje. Ahora el anfitrión avisa cada
/// paso y el invitado reproduce lo mismo en su pantalla:
///   Puertas   → los estudiantes entran al ascensor y se cierran las puertas
///   Estrellas → la pantalla de estrellas del piso (con los mismos datos)
///   Ficha     → la ficha del piso siguiente (o la del nivel final)
///   Viaje     → el viaje en ascensor (pantalla de carga) hasta que el anfitrión carga el piso nuevo
///   Historia / PantallaFinal → el final del juego ("¡por fin lo vencimos!" y la pantalla de la U salvada)
/// Si el anfitrión avanza antes, lo que el invitado tenga abierto se cierra solo.
/// </summary>
public static class SecuenciaRed
{
    private const byte Puertas = 2, Estrellas = 3, Ficha = 4, Viaje = 5, Historia = 6, Final = 7;

    // ------------------------------------------------------------ anfitrión
    private static bool Anfitrion { get { return RedJuego.EsAnfitrionEnPartida; } }

    public static void EnviarPuertas()
    {
        if (Anfitrion) RedJuego.Enviar(MsgRed.Evento, w => { w.Write(EvRed.Secuencia); w.Write(Puertas); });
    }

    public static void EnviarEstrellas(int puntos, bool[] cumplidas, string[] textos, bool record, string area)
    {
        if (!Anfitrion) return;
        RedJuego.Enviar(MsgRed.Evento, w =>
        {
            w.Write(EvRed.Secuencia); w.Write(Estrellas);
            w.Write(puntos);
            for (int i = 0; i < 3; i++) w.Write(cumplidas != null && i < cumplidas.Length && cumplidas[i]);
            for (int i = 0; i < 3; i++) w.Write(textos != null && i < textos.Length && textos[i] != null ? textos[i] : "");
            w.Write(record);
            w.Write(area ?? "");
        });
    }

    public static void EnviarFicha(bool nivelFinal)
    {
        if (Anfitrion) RedJuego.Enviar(MsgRed.Evento, w => { w.Write(EvRed.Secuencia); w.Write(Ficha); w.Write(nivelFinal); });
    }

    public static void EnviarViaje(int desde, int hasta, string area)
    {
        if (Anfitrion) RedJuego.Enviar(MsgRed.Evento, w => { w.Write(EvRed.Secuencia); w.Write(Viaje); w.Write(desde); w.Write(hasta); w.Write(area ?? ""); });
    }

    public static void EnviarHistoriaFinal()
    {
        if (Anfitrion) RedJuego.Enviar(MsgRed.Evento, w => { w.Write(EvRed.Secuencia); w.Write(Historia); });
    }

    public static void EnviarPantallaFinal(int puntos, string[] nombresPisos)
    {
        if (!Anfitrion) return;
        RedJuego.Enviar(MsgRed.Evento, w =>
        {
            w.Write(EvRed.Secuencia); w.Write(Final);
            w.Write(puntos);
            int n = nombresPisos != null ? nombresPisos.Length : 0;
            w.Write(n);
            for (int i = 0; i < n; i++) w.Write(nombresPisos[i] ?? "");
        });
    }

    /// <summary>La ficha del nivel final (la misma que arma GestorJuego en el anfitrión).</summary>
    public static DefinicionPiso FichaNivelFinal()
    {
        DefinicionPiso ficha = ScriptableObject.CreateInstance<DefinicionPiso>();
        ficha.numero = 7;
        ficha.nombre = "Fachada de Unifranz";
        ficha.nombreArea = DefinicionPiso.NombreNivelFinal;
        ficha.fichaTitulo = "¡El demoledor escapó a la fachada! - NIVEL FINAL";
        ficha.fichaDescripcion = "Sube por la fachada reparando las ventanas antes de que se derrumbe, y cuidado con las palomas.";
        ficha.fichaDatoUtil = "Es el nivel más difícil: tienes 3 vidas.";
        return ficha;
    }

    // ------------------------------------------------------------ invitado
    private class Corredor : MonoBehaviour { }
    private static Corredor corredor;
    private static PantallaEstrellas pantallaEstrellas;
    private static TransicionPiso pantallaFicha;

    private static Corredor Runner()
    {
        if (corredor == null) corredor = new GameObject("[SecuenciaRed]").AddComponent<Corredor>();
        return corredor;
    }

    /// <summary>Cierra lo que el invitado tenga abierto (el anfitrión ya pasó al paso siguiente).</summary>
    private static void CerrarAbiertas()
    {
        if (pantallaEstrellas != null) { if (pantallaEstrellas.Abierta) pantallaEstrellas.Cerrar(); Object.Destroy(pantallaEstrellas.gameObject); }
        if (pantallaFicha != null) { if (pantallaFicha.Abierta) pantallaFicha.Cerrar(); Object.Destroy(pantallaFicha.gameObject); }
        pantallaEstrellas = null;
        pantallaFicha = null;
    }

    public static void Aplicar(BinaryReader r)
    {
        byte paso = r.ReadByte();
        switch (paso)
        {
            case Puertas:
            {
                PisoCenital p = PisoCenital.Actual;
                if (p != null) Runner().StartCoroutine(ViajeAscensor.PuertasRemotas(p));
                break;
            }
            case Estrellas:
            {
                int puntos = r.ReadInt32();
                bool[] cumplidas = new bool[3];
                for (int i = 0; i < 3; i++) cumplidas[i] = r.ReadBoolean();
                string[] textos = new string[3];
                for (int i = 0; i < 3; i++) textos[i] = r.ReadString();
                bool record = r.ReadBoolean();
                string area = r.ReadString();
                CerrarAbiertas();
                pantallaEstrellas = PantallaEstrellas.Crear(Runner().transform);
                pantallaEstrellas.Mostrar(puntos, cumplidas, textos, record, area);
                break;
            }
            case Ficha:
            {
                bool final = r.ReadBoolean();
                CerrarAbiertas();
                PisoCenital p = PisoCenital.Actual;
                DefinicionPiso def = final ? FichaNivelFinal() : p != null ? p.Siguiente : null;
                if (def == null) break;
                pantallaFicha = TransicionPiso.Crear(Runner().transform);
                if (final) pantallaFicha.Mostrar(def, "¡NIVEL FINAL!", "NIVEL FINAL");
                else pantallaFicha.Mostrar(def);
                break;
            }
            case Viaje:
            {
                int desde = r.ReadInt32(), hasta = r.ReadInt32();
                string area = r.ReadString();
                CerrarAbiertas();
                Runner().StartCoroutine(ViajeAscensor.Viaje(desde, hasta, area));
                break;
            }
            case Historia:
                CerrarAbiertas();
                Runner().StartCoroutine(HistoriaJuego.ReproducirFinal());
                break;
            case Final:
            {
                int puntos = r.ReadInt32();
                int n = r.ReadInt32();
                string[] nombres = new string[Mathf.Clamp(n, 0, 64)];
                for (int i = 0; i < n; i++) { string s = r.ReadString(); if (i < nombres.Length) nombres[i] = s; }
                CerrarAbiertas();
                PantallaFinal pf = PantallaFinal.Crear(Runner().transform, nombres, true);
                pf.Mostrar(puntos);
                break;
            }
        }
    }
}
