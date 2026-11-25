using System.Collections.Generic;
using UnityEngine;

/// <summary>Los tres mini villanos que acompañan al demoledor.</summary>
public enum TipoMini
{
    /// <summary>Aprendiz de demoledor: camina hasta un objeto, se prepara y lo golpea con su martillito. Te empuja un poco.</summary>
    Aprendiz,
    /// <summary>Termita robot: rápida, se pega a un objeto y lo va royendo. No hace daño al jugador.</summary>
    Termita,
    /// <summary>Mini topadora: lenta y resistente, aplasta objetos y te atropella (quita vida). Puede soltar un potenciador.</summary>
    Topadora
}

/// <summary>
/// Un mini villano del piso cenital. Lo crea GestorMiniVillanos. Busca objetos reparables con el A* del mapa y los daña
/// (cuenta como destrozo, así que sube la furia del demoledor). El jugador lo ahuyenta a martillazos (ENTER cerca),
/// el reloj de hielo lo congela y las versiones "élite" (casco rojo, termita tóxica, topadora blindada) aguantan más.
/// </summary>
public class MiniVillano : MonoBehaviour
{
    private enum Estado { Apareciendo, Decidiendo, Caminando, Atacando, Recuperando, Huyendo, Brincando }

    private static readonly List<MiniVillano> activos = new List<MiniVillano>();
    public static IReadOnlyList<MiniVillano> Activos { get { return activos; } }

    public TipoMini Tipo { get; private set; }
    public bool Elite { get; private set; }
    public bool Congelado { get { return Time.time < finCongelado; } }
    /// <summary>Centro del cuerpo (la posición del objeto son los pies).</summary>
    public Vector2 Centro { get { return (Vector2)transform.position + new Vector2(0f, 0.3f); } }

    private PisoCenital piso;
    private JugadorCenital jugador;
    private GestorJuego gestor;
    private SpriteRenderer dibujo, sombra, hielo;
    private Sprite[] caminarFrente, caminarLado, ataque;

    private Estado estado;
    private int vida, vidaMax;
    private float velocidad, preparacion, pausaTrasGolpe, alturaDibujo;
    private float finEstado, finCongelado, finDestello, proximoContacto, proximoChequeo, inicioAparicion;
    private ObjetoReparable objetivo;
    private List<Vector2> ruta;
    private int indiceRuta;
    private Vector2 direccion = Vector2.down, posChequeo, empuje;
    private float finEmpuje;
    private int atascos;
    private float reloj;
    // Brinquitos: saltan por encima de los muebles a otra zona (si se traban o de sorpresa) y cambian de idea a mitad de camino.
    private Vector2 brincoDesde, brincoHasta;
    private float inicioBrinco, durBrinco, alturaBrinco, proximoBrinco, proximoCambioIdea;
    private string claveMotor;
    private static int contadorMotores;

    // ------------------------------------------------------------ creación
    public static MiniVillano Crear(TipoMini tipo, bool elite, Vector2 posicion)
    {
        GameObject g = new GameObject("Mini " + tipo + (elite ? " (élite)" : ""));
        g.transform.position = new Vector3(posicion.x, posicion.y, 0f);
        MiniVillano m = g.AddComponent<MiniVillano>();
        m.Configurar(tipo, elite);
        activos.Add(m);
        return m;
    }

    private void Configurar(TipoMini tipo, bool elite)
    {
        Tipo = tipo;
        Elite = elite;
        piso = PisoCenital.Actual;
        jugador = JugadorCenital.MasCercano(transform.position);
        gestor = FindAnyObjectByType<GestorJuego>();
        float ritmo = piso != null ? Mathf.Clamp(piso.RitmoDemoledor, 0.7f, 1.6f) : 1f;

        string carpeta;
        switch (tipo)
        {
            case TipoMini.Termita:
                carpeta = elite ? "Mini/termita_toxica/" : "Mini/termita_cobre/";
                caminarLado = caminarFrente = ArteExtra.Lista(carpeta, "camina_0", "camina_1");
                ataque = ArteExtra.Lista(carpeta, "muerde_0", "muerde_1");
                vidaMax = 1;
                velocidad = (elite ? 3.1f : 2.5f) * ritmo;
                preparacion = elite ? 1.6f : 2.2f;      // cada cuánto roe un estado
                pausaTrasGolpe = 0.2f;
                alturaDibujo = 0.75f;
                break;
            case TipoMini.Topadora:
                carpeta = elite ? "Mini/topadora_blindada/" : "Mini/topadora_amarilla/";
                caminarLado = caminarFrente = ArteExtra.Lista(carpeta, "avanza_0", "avanza_1");
                ataque = ArteExtra.Lista(carpeta, "empuja_0", "empuja_1");
                vidaMax = elite ? 3 : 2;
                velocidad = (elite ? 0.95f : 1.15f) * ritmo;
                preparacion = 1.1f;
                pausaTrasGolpe = 0.9f;
                alturaDibujo = 0.85f;
                claveMotor = "topadora_" + (++contadorMotores);
                break;
            default:
                carpeta = elite ? "Mini/aprendiz_rojo/" : "Mini/aprendiz_naranja/";
                caminarFrente = ArteExtra.Lista(carpeta, "frente_0", "frente_1", "frente_2", "frente_3");
                caminarLado = ArteExtra.Lista(carpeta, "lado_0", "lado_1", "lado_2", "lado_3");
                ataque = ArteExtra.Lista(carpeta, "golpe_0", "golpe_1");
                vidaMax = elite ? 2 : 1;
                velocidad = (elite ? 2.0f : 1.65f) * ritmo;
                preparacion = elite ? 0.7f : 0.95f;
                pausaTrasGolpe = 0.6f;
                alturaDibujo = 0.9f;
                break;
        }
        vida = vidaMax;

        sombra = Provisional.Crear(transform, "Sombra", Provisional.Circulo, new Color(0f, 0f, 0f, 0.3f), 0);
        sombra.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        sombra.transform.localScale = new Vector3(tipo == TipoMini.Topadora ? 0.9f : 0.6f, 0.22f, 1f);

        dibujo = Provisional.Crear(transform, "Dibujo", caminarFrente != null ? caminarFrente[0] : Provisional.Cuadrado, Color.white, 0);
        if (caminarFrente == null) dibujo.color = tipo == TipoMini.Termita ? new Color(0.7f, 0.45f, 0.2f) : new Color(1f, 0.6f, 0.1f);

        hielo = Provisional.Crear(transform, "Hielo", ArteExtra.Uno("Efectos/hielo_0"), Color.white, 0);
        hielo.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        hielo.enabled = false;

        estado = Estado.Apareciendo;
        inicioAparicion = Time.time;
        finEstado = Time.time + 0.45f;
        proximoContacto = Time.time + 1f;
        reloj = Random.value;
        EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), transform.position, Provisional.OrdenY(transform.position.y) + 2, 0.9f, 12f);
        if (Random.value < 0.35f) Burbuja.Decir(transform, Frases.Una(Frases.Mini), Burbuja.Voz.Mini, alturaDibujo + 0.2f);
    }

    private void OnDestroy()
    {
        activos.Remove(this);
        if (objetivo != null) objetivo.MarcarAmenaza(false);
        if (claveMotor != null) GestorAudio.Bucle(claveMotor, SndExtra.TopadoraMotor, false);
    }

    // ------------------------------------------------------------ acciones desde fuera
    /// <summary>Martillazo del jugador. Devuelve true si lo venció.</summary>
    public bool RecibirMartillazo(Vector2 desde)
    {
        if (estado == Estado.Huyendo) return false;
        vida--;
        finDestello = Time.time + 0.12f;
        GestorAudio.Reproducir(SndExtra.MiniGolpe, 0.9f, 0.8f);
        EfectoSprite.Reproducir(ArteNiveles.De(a => a.golpeEstallido), Centro, Provisional.OrdenY(transform.position.y) + 5, 0.7f, 16f);
        Vector2 dir = (Vector2)transform.position - desde;
        if (dir.sqrMagnitude < 0.001f) dir = Random.insideUnitCircle;
        empuje = dir.normalized * (Tipo == TipoMini.Topadora ? 1.5f : 4f);
        finEmpuje = Time.time + 0.15f;
        if (vida <= 0) { Vencer(true); return true; }
        if (estado == Estado.Atacando) { LiberarObjetivo(); estado = Estado.Recuperando; finEstado = Time.time + 0.4f; }
        return false;
    }

    public void Congelar(float segundos)
    {
        if (estado == Estado.Huyendo) return;
        finCongelado = Mathf.Max(finCongelado, Time.time + segundos);
        if (estado == Estado.Atacando) { LiberarObjetivo(); estado = Estado.Recuperando; finEstado = Time.time + 0.2f; }
    }

    /// <summary>Desaparece con una nube. Con 'conPuntos' suma puntos y puede soltar un potenciador.</summary>
    public void Vencer(bool conPuntos)
    {
        if (estado == Estado.Huyendo) return;
        estado = Estado.Huyendo;
        LiberarObjetivo();
        EfectoSprite.Reproducir(ArteExtra.Serie("Efectos/humo_poof_", 4), Centro, Provisional.OrdenY(transform.position.y) + 6, 1f, 14f);
        GestorAudio.Reproducir(SndExtra.MiniVencido);
        if (conPuntos)
        {
            int puntos = Tipo == TipoMini.Topadora ? 60 : (Tipo == TipoMini.Aprendiz ? 30 : 20);
            if (Elite) puntos += 20;
            if (gestor != null) gestor.SumarPuntos(puntos, null);
            ComentariosJugador.Evento(ComentariosJugador.Tipo.MiniVencido, JugadorCenital.MasCercano(Centro));
            IraEstudiante.MiniVencido(Centro);
            Progreso.Sumar("minis");
            TextoFlotante.Mostrar(Centro + Vector2.up * 0.9f, "+" + puntos, new Color(1f, 0.9f, 0.35f), 1.1f);
            // La topadora (y a veces el aprendiz élite) suelta un potenciador al caer.
            float prob = Tipo == TipoMini.Topadora ? 0.45f : (Elite ? 0.2f : 0.06f);
            if (Random.value < prob) GestorPotenciadoresExtra.SoltarEn(transform.position);
        }
        Destroy(gameObject);
    }

    // ------------------------------------------------------------ ciclo
    private void Update()
    {
        if (piso == null || piso.Mapa == null) { Destroy(gameObject); return; }
        if (gestor != null && gestor.PartidaTerminada) { Detener(); return; }

        if (JugadorCenital.Todos.Count > 1) jugador = JugadorCenital.MasCercano(Centro);
        bool frio = Congelado;
        if (hielo.enabled != frio) hielo.enabled = frio;
        JugadorCenital oyente = JugadorCenital.Local;
        if (claveMotor != null && oyente != null)
        {
            float d = Vector2.Distance(Centro, oyente.Centro);
            float cerca = Mathf.Clamp01(1f - d / 7f);
            GestorAudio.Bucle(claveMotor, SndExtra.TopadoraMotor, cerca > 0.02f && !frio, cerca);
        }

        if (Time.time < finEmpuje) Mover(empuje * Time.deltaTime);

        if (estado == Estado.Brincando) UpdateBrinco();
        else if (!frio)
        {
            switch (estado)
            {
                case Estado.Apareciendo: if (Time.time >= finEstado) { estado = Estado.Decidiendo; finEstado = 0f; } break;
                case Estado.Decidiendo: if (Time.time >= finEstado) Decidir(); break;
                case Estado.Caminando: UpdateCaminando(); break;
                case Estado.Atacando: UpdateAtacando(); break;
                case Estado.Recuperando: if (Time.time >= finEstado) { estado = Estado.Decidiendo; finEstado = 0f; } break;
            }
            ContactoConJugador();
        }
        Dibujar(frio);
    }

    private void Detener()
    {
        LiberarObjetivo();
        if (claveMotor != null) GestorAudio.Bucle(claveMotor, SndExtra.TopadoraMotor, false);
        Dibujar(Congelado);
    }

    // ------------------------------------------------------------ elegir objetivo y caminar
    private void Decidir()
    {
        // De sorpresa: un brinquito a otra parte antes de elegir.
        if (Time.time >= proximoBrinco && Random.value < 0.12f && IniciarBrinco(2.5f, 6f)) return;
        objetivo = ElegirObjetivo();
        if (objetivo == null) { finEstado = Time.time + 1f; return; }

        Vector2 destino;
        if (!CeldaJunto(objetivo, out destino)) { objetivo = null; finEstado = Time.time + 0.6f; return; }
        ruta = BuscadorRutas.Buscar(piso.Mapa, transform.position, destino);
        if (ruta == null)
        {
            // Encerrado: salta por encima de lo que lo traba.
            objetivo = null;
            if (!IniciarBrinco(2f, 7f)) finEstado = Time.time + 0.6f;
            return;
        }
        indiceRuta = 0;
        atascos = 0;
        posChequeo = transform.position;
        proximoChequeo = Time.time + 0.6f;
        estado = Estado.Caminando;
    }

    /// <summary>Un objeto no roto, preferiblemente cercano y que no esté atacando otro mini.</summary>
    private ObjetoReparable ElegirObjetivo()
    {
        ObjetoReparable mejor = null;
        float mejorPuntaje = float.MaxValue;
        foreach (ObjetoReparable o in piso.Mapa.objetos)
        {
            if (o == null || o.Estado == EstadoObjeto.Roto) continue;
            float puntaje = Vector2.Distance(transform.position, o.Centro) + Random.Range(0f, 6f);
            if (Random.value < 0.15f) puntaje -= 8f;   // a veces va a uno lejano, sin lógica
            foreach (MiniVillano m in activos) if (m != null && m != this && m.objetivo == o) puntaje += 6f;
            if (o.Estado == EstadoObjeto.Danado) puntaje += Tipo == TipoMini.Termita ? -1f : 1f;
            if (puntaje < mejorPuntaje) { mejorPuntaje = puntaje; mejor = o; }
        }
        return mejor;
    }

    private bool CeldaJunto(ObjetoReparable o, out Vector2 celda)
    {
        celda = Vector2.zero;
        float mejor = float.MaxValue;
        bool hay = false;
        foreach (Vector2 c in piso.Mapa.CentrosTransitables)
        {
            if (Vector2.Distance(c, o.PuntoMasCercano(c)) > 0.8f) continue;
            float d = (c - (Vector2)transform.position).sqrMagnitude;
            if (d < mejor) { mejor = d; celda = c; hay = true; }
        }
        return hay;
    }

    private void UpdateCaminando()
    {
        if (objetivo == null || objetivo.Estado == EstadoObjeto.Roto) { estado = Estado.Decidiendo; finEstado = 0f; return; }
        if (ruta == null || indiceRuta >= ruta.Count) { LlegoAlObjetivo(); return; }

        Vector2 d = ruta[indiceRuta] - (Vector2)transform.position;
        float paso = velocidad * Time.deltaTime;
        if (d.magnitude <= paso + 0.02f) { Mover(d); indiceRuta++; }
        else { direccion = d.normalized; Mover(direccion * paso); }

        if (Time.time >= proximoChequeo)
        {
            if (Vector2.Distance(transform.position, posChequeo) < 0.05f && ++atascos >= 2)
            {
                if (!(Random.value < 0.6f && IniciarBrinco(1.5f, 5f))) { estado = Estado.Decidiendo; finEstado = 0.3f + Time.time; }
            }
            else if (Time.time >= proximoCambioIdea && Random.value < 0.06f)
            {
                // Cambia de idea a mitad de camino.
                proximoCambioIdea = Time.time + Random.Range(3f, 7f);
                objetivo = null;
                estado = Estado.Decidiendo;
                finEstado = Time.time + 0.15f;
            }
            posChequeo = transform.position;
            proximoChequeo = Time.time + 0.6f;
        }
    }

    private void LlegoAlObjetivo()
    {
        if (objetivo == null || Vector2.Distance(Centro, objetivo.PuntoMasCercano(Centro)) > 1.3f) { estado = Estado.Decidiendo; finEstado = 0f; return; }
        Vector2 hacia = objetivo.PuntoMasCercano(Centro) - Centro;
        if (hacia.sqrMagnitude > 0.0001f) direccion = hacia.normalized;
        estado = Estado.Atacando;
        finEstado = Time.time + preparacion;
        objetivo.MarcarAmenaza(true);
    }

    private void UpdateAtacando()
    {
        if (objetivo == null || objetivo.Estado == EstadoObjeto.Roto) { LiberarObjetivo(); estado = Estado.Decidiendo; finEstado = 0f; return; }
        if (Tipo == TipoMini.Termita && Time.time > finEstado - 0.5f && (int)(Time.time * 6f) % 3 == 0)
            GestorAudio.Reproducir(SndExtra.MiniMordida, 0.5f);
        if (Time.time < finEstado) return;

        if (objetivo.Danar())
        {
            piso.RegistrarGolpe();
            GestorAudio.Reproducir(Tipo == TipoMini.Termita ? SndExtra.MiniMordida : SndExtra.MiniGolpe);
            EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), objetivo.PuntoMasCercano(Centro), Provisional.OrdenY(objetivo.Area.min.y) + 4, 0.6f, 12f);
            if (Tipo == TipoMini.Topadora) CamaraPisoCenital.Temblar(0.03f, 0.1f);
        }
        if (Tipo == TipoMini.Termita && objetivo.Estado != EstadoObjeto.Roto)
        {
            finEstado = Time.time + preparacion;   // la termita sigue royendo el mismo objeto
            return;
        }
        LiberarObjetivo();
        estado = Estado.Recuperando;
        finEstado = Time.time + pausaTrasGolpe;
    }

    private void LiberarObjetivo()
    {
        if (objetivo == null) return;
        // Solo quita la marca si nadie más (ni el demoledor) lo está amenazando.
        bool otro = false;
        foreach (MiniVillano m in activos) if (m != null && m != this && m.objetivo == objetivo && m.estado == Estado.Atacando) otro = true;
        foreach (DemoledorCenital dem in DemoledorCenital.Todos)
            if (dem != null && dem.ObjetivoAmenazado == objetivo) otro = true;
        if (!otro) objetivo.MarcarAmenaza(false);
        objetivo = null;
    }


    // ------------------------------------------------------------ brinquito
    /// <summary>Salta (por encima de muebles) a una celda libre a 'min'-'max' baldosas. False si no hay dónde.</summary>
    private bool IniciarBrinco(float min, float max)
    {
        List<Vector2> celdas = piso.Mapa.CentrosTransitables;
        if (celdas == null || celdas.Count == 0) return false;
        for (int intento = 0; intento < 24; intento++)
        {
            Vector2 c = celdas[Random.Range(0, celdas.Count)];
            float d = Vector2.Distance(c, transform.position);
            if (d < min || d > max) continue;
            LiberarObjetivo();
            brincoDesde = transform.position;
            brincoHasta = c;
            durBrinco = Mathf.Clamp(0.4f + d * 0.06f, 0.45f, 0.8f);
            alturaBrinco = Mathf.Clamp(0.5f + d * 0.12f, 0.6f, 1.3f);
            inicioBrinco = Time.time;
            Vector2 hacia = c - brincoDesde;
            if (hacia.sqrMagnitude > 0.001f) direccion = hacia.normalized;
            estado = Estado.Brincando;
            proximoBrinco = Time.time + Random.Range(6f, 12f);
            GestorAudio.Reproducir(SndExtra.MiniGolpe, 0.45f, 1.5f);
            return true;
        }
        return false;
    }

    private void UpdateBrinco()
    {
        float k = Mathf.Clamp01((Time.time - inicioBrinco) / Mathf.Max(0.05f, durBrinco));
        Vector2 p = Vector2.Lerp(brincoDesde, brincoHasta, k);
        transform.position = new Vector3(p.x, p.y, 0f);
        if (k < 1f) return;
        sombra.transform.localScale = new Vector3(Tipo == TipoMini.Topadora ? 0.9f : 0.6f, 0.22f, 1f);
        EfectoSprite.Reproducir(ArteNiveles.De(a => a.polvoImpacto), p, Provisional.OrdenY(p.y) - 1, 0.6f, 14f);
        if (Tipo == TipoMini.Topadora) CamaraPisoCenital.Temblar(0.04f, 0.12f);
        estado = Estado.Decidiendo;
        finEstado = Time.time + 0.15f;
    }

    // ------------------------------------------------------------ jugador
    private void ContactoConJugador()
    {
        if (jugador == null || !jugador.enabled || jugador.EnElAire || Time.time < proximoContacto || estado == Estado.Apareciendo) return;
        float radio = Tipo == TipoMini.Topadora ? 0.75f : 0.55f;
        if (Vector2.Distance(Centro, jugador.Centro) > radio) return;

        switch (Tipo)
        {
            case TipoMini.Topadora:
                // Atropello: te saca de su camino y quita una vida.
                if (jugador.RecibirEmpujon(Centro, 2f, 0.4f, true)) GestorAudio.Reproducir(SndExtra.GolpeFuerteJugador, 0.8f);
                CamaraPisoCenital.Temblar(0.06f, 0.15f);
                proximoContacto = Time.time + 2.5f;
                break;
            case TipoMini.Aprendiz:
                jugador.RecibirEmpujon(Centro, 1.2f, 0.2f, Elite);   // el élite sí quita vida
                GestorAudio.Reproducir(SndExtra.MiniGolpe, 0.7f, 1.2f);
                proximoContacto = Time.time + 1.6f;
                break;
            default:
                jugador.RecibirEmpujon(Centro, 0.7f, 0f, false);
                proximoContacto = Time.time + 1.2f;
                break;
        }
    }

    // ------------------------------------------------------------ movimiento y dibujo
    /// <summary>Mueve sin salir de las celdas transitables (así nunca atraviesa muros ni sale del mapa).</summary>
    private void Mover(Vector2 delta)
    {
        Vector2 nueva = (Vector2)transform.position + delta;
        MapaCenital mapa = piso.Mapa;
        if (!mapa.EsTransitable(mapa.CeldaDe(nueva))) return;
        Rect lim = mapa.Limites;
        nueva.x = Mathf.Clamp(nueva.x, lim.xMin + 0.3f, lim.xMax - 0.3f);
        nueva.y = Mathf.Clamp(nueva.y, lim.yMin + 0.3f, lim.yMax - 0.3f);
        transform.position = new Vector3(nueva.x, nueva.y, 0f);
    }

    private void Dibujar(bool frio)
    {
        Sprite[] serie;
        float cps = 8f;
        bool atacando = estado == Estado.Atacando;
        bool lateral = Mathf.Abs(direccion.x) > Mathf.Abs(direccion.y);
        if (atacando && ataque != null)
        {
            serie = ataque;
            // Aprendiz y topadora: primer cuadro mientras se preparan, segundo justo al golpear. La termita mastica en bucle.
            if (Tipo != TipoMini.Termita) cps = 0f;
        }
        else serie = lateral || Tipo == TipoMini.Termita ? caminarLado : caminarFrente;

        if (!frio) reloj += Time.deltaTime;
        if (serie != null && serie.Length > 0)
        {
            int i;
            if (cps <= 0f) i = Time.time > finEstado - 0.15f ? serie.Length - 1 : 0;
            else if (estado == Estado.Caminando || atacando) i = (int)(reloj * cps) % serie.Length;
            else i = 0;
            dibujo.sprite = serie[i];
        }

        // Orientación: la termita (vista desde arriba) gira hacia donde va; los demás se voltean a izquierda/derecha.
        float escala = 1f;
        if (estado == Estado.Apareciendo) escala = Mathf.Clamp01((Time.time - inicioAparicion) / 0.3f);
        if (Tipo == TipoMini.Termita)
        {
            float ang = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg - 90f;
            dibujo.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            dibujo.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            dibujo.flipX = false;
        }
        else
        {
            dibujo.transform.localRotation = Quaternion.identity;
            dibujo.transform.localPosition = Vector3.zero;
            dibujo.flipX = direccion.x < -0.1f;
        }
        float tamano = 1f;
        if (dibujo.sprite != null && dibujo.sprite != Provisional.Cuadrado && dibujo.sprite.bounds.size.y > 0.001f)
            tamano = alturaDibujo / Mathf.Max(dibujo.sprite.bounds.size.y, Tipo == TipoMini.Termita ? dibujo.sprite.bounds.size.x : 0f);
        else if (dibujo.sprite == Provisional.Cuadrado) tamano = 0.5f;
        dibujo.transform.localScale = new Vector3(tamano * escala, tamano * escala, 1f);
        if (estado == Estado.Brincando)
        {
            float kb = Mathf.Clamp01((Time.time - inicioBrinco) / Mathf.Max(0.05f, durBrinco));
            dibujo.transform.localPosition += new Vector3(0f, Mathf.Sin(kb * Mathf.PI) * alturaBrinco, 0f);
            dibujo.transform.localScale = new Vector3(tamano * 0.92f, tamano * 1.1f, 1f);
            sombra.transform.localScale = new Vector3((Tipo == TipoMini.Topadora ? 0.9f : 0.6f) * (1f - 0.4f * Mathf.Sin(kb * Mathf.PI)), 0.22f, 1f);
        }

        int orden = Provisional.OrdenY(transform.position.y);
        dibujo.sortingOrder = orden;
        sombra.sortingOrder = orden - 2;
        hielo.sortingOrder = orden + 1;

        Color c = Color.white;
        if (caminarFrente == null) c = Tipo == TipoMini.Termita ? new Color(0.7f, 0.45f, 0.2f) : new Color(1f, 0.6f, 0.1f);
        if (atacando && Tipo != TipoMini.Termita && (int)(Time.time * 12f) % 2 == 0) c = new Color(1f, 0.55f, 0.45f);
        if (frio) c = new Color(0.55f, 0.85f, 1f);
        if (Time.time < finDestello) c = new Color(1f, 1f, 1f, 0.4f);
        dibujo.color = c;
    }
}
