using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// El jugador visto desde arriba: movimiento libre en 8 direcciones (WASD y flechas, o los
/// botones táctiles), sin gravedad, chocando con paredes y objetos. ENTER (o el botón MARTILLO)
/// repara un estado el objeto dañado o roto más cercano. Al recibir daño queda invulnerable 1 s
/// parpadeando. Las 4 direcciones usan los sprites de arriba, abajo y derecha (izquierda = derecha volteada).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class JugadorCenital : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 3.6f;

    [Header("Reparación")]
    [Tooltip("Alcance del martillo (baldosas), medido desde el cuerpo hasta el borde del objeto.")]
    public float radioReparacion = 0.9f;
    public float momentoDeReparacion = 0.15f;
    public float enfriamiento = 0.25f;
    public float cuadrosCaminar = 8f;
    public float cuadrosPorSegundoMartillo = 12f;

    [Header("Daño")]
    public float tiempoInvulnerable = 1f;

    [Header("Choque con el demoledor (distancia, aturdimiento y espera salen de la DefinicionPiso)")]
    [Tooltip("Segundos que dura el empujón del choque.")]
    public float duracionEmpuje = 0.15f;
    [Tooltip("Estrellas que giran sobre la cabeza mientras está aturdido.")]
    public int numeroEstrellas = 3;
    public float radioEstrellas = 0.4f;
    [Tooltip("Grados por segundo.")]
    public float velocidadEstrellas = 420f;
    public float tamanoEstrella = 0.3f;
    [Tooltip("Altura (baldosas) sobre los pies a la que giran las estrellas.")]
    public float alturaEstrellas = 1.45f;

    [Header("Poderes (duración, mensajes y colores salen de DefinicionPiso.poderes)")]
    [Tooltip("Tamaño (baldosas) del aro que rodea al jugador mientras un poder está activo.")]
    public Vector2 tamanoAro = new Vector2(1.1f, 1.6f);
    [Tooltip("Altura (baldosas) del centro del aro sobre los pies.")]
    public float alturaAro = 0.65f;
    [Tooltip("Velocidad de la pulsación del aro.")]
    public float pulsoAro = 6f;

    [Header("Dibujo")]
    [Tooltip("Alto en baldosas al que se escala el sprite.")]
    public float alturaDibujo = 1.25f;
    public AnimacionesCenital animaciones = new AnimacionesCenital();

    public event Action Golpeado;

    private Rigidbody2D cuerpo;
    private SpriteRenderer dibujo;
    private AnimadorSprites animador;
    private Vector2 entrada;
    private Vector2 direccion = Vector2.down;
    private bool martillando;
    private bool reparoHecho;
    private float inicioMartillo, finMartillo, proximoMartillo, finInvulnerable;
    private bool fueraDeCuadros;

    private float finAturdido, finEmpuje, proximoChoque;
    private Vector2 velocidadEmpuje;
    private Transform estrellas;
    private SpriteRenderer[] spritesEstrellas;
    private bool estrellasConArte;

    private DemoledorCenital demoledorRef;
    private float finInvencible, finReparacionTotal;
    private PoderPiso poderInvencible, poderReparacion;
    private SpriteRenderer aroInvencible, aroReparacion;

    /// <summary>Centro del cuerpo (el collider está sobre los pies).</summary>
    public Vector2 Centro { get { return (Vector2)transform.position + new Vector2(0f, 0.3f); } }
    /// <summary>Aturdido por un choque: no se mueve ni repara y no puede recibir daño.</summary>
    public bool Aturdido { get { return Time.time < finAturdido; } }
    /// <summary>Con el poder de invulnerabilidad: no recibe daño ni choques.</summary>
    public bool Invencible { get { return Time.time < finInvencible; } }
    /// <summary>Con el poder de reparación total: cada martillazo deja el objeto intacto.</summary>
    public bool ReparacionTotal { get { return Time.time < finReparacionTotal; } }
    public bool PuedeRecibirGolpe { get { return enabled && Time.time >= finInvulnerable && !Aturdido && !Invencible; } }

    private DefinicionPiso Definicion
    {
        get { PisoCenital p = PisoCenital.Actual; return p != null ? p.Definicion : null; }
    }

    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        cuerpo.gravityScale = 0f;
        cuerpo.freezeRotation = true;
        cuerpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;

        dibujo = GetComponentInChildren<SpriteRenderer>();
        if (dibujo == null) dibujo = Provisional.Crear(transform, "Dibujo", null, Color.white, 0);
        animador = new AnimadorSprites(dibujo);
        CrearEstrellas();
    }

    private void Start()
    {
        // El arte de la definición (si lo hay) reemplaza al de la escena.
        PisoCenital piso = PisoCenital.Actual;
        if (piso != null && piso.Definicion != null) animaciones = AnimacionesCenital.Combinar(animaciones, piso.Definicion.jugador);
        animaciones.QuitarInvalidos();
        if (animaciones.caminarArriba == null) animaciones.caminarArriba = animaciones.caminarAbajo;
        MostrarQuieto();
    }

    private void OnDisable()
    {
        if (cuerpo != null) cuerpo.linearVelocity = Vector2.zero;
        if (dibujo != null) dibujo.enabled = true;
    }

    private void Update()
    {
        LeerEntrada();

        bool aturdido = Aturdido;
        if (aturdido)
        {
            entrada = Vector2.zero;
            martillando = false;
            ControlesTactiles.RepararPulsado();   // se descarta el toque: no se acumula para después
        }

        if (martillando)
        {
            if (!reparoHecho && Time.time >= inicioMartillo + momentoDeReparacion) Reparar();
            if (Time.time >= finMartillo) martillando = false;
        }
        else if (!aturdido)
        {
            Keyboard k = Keyboard.current;
            bool enter = k != null && (k[Key.Enter].wasPressedThisFrame || k[Key.NumpadEnter].wasPressedThisFrame);
            bool tactil = ControlesTactiles.RepararPulsado();   // se lee siempre para consumir el toque
            if ((enter || tactil) && Time.time >= proximoMartillo) Martillar();
        }

        Animar();
        Parpadear();
        AjustarDibujo();
        ActualizarEstrellas(aturdido);
        ActualizarAros();
        SonidosDeMovimiento(aturdido);
    }

    private float proximoPaso;
    private bool pasoAlterno;

    /// <summary>Pasos alternados cada 0,28 s al caminar, sonido de aturdido y sonido suave mientras dura un poder.</summary>
    private void SonidosDeMovimiento(bool aturdido)
    {
        GestorAudio.Bucle("aturdido_jugador", Snd.AturdidoBucle, aturdido);
        GestorAudio.Bucle("poder_jugador", Snd.PoderActivoBucle, Invencible || ReparacionTotal);

        bool camina = !aturdido && !martillando && cuerpo != null && cuerpo.linearVelocity.sqrMagnitude > 0.2f;
        if (camina && Time.time >= proximoPaso)
        {
            GestorAudio.Reproducir(pasoAlterno ? Snd.Paso2 : Snd.Paso1);
            pasoAlterno = !pasoAlterno;
            proximoPaso = Time.time + 0.28f;
        }
    }

    /// <summary>Seguro: si falla un collider, el jugador nunca sale del rectángulo del mapa.</summary>
    private void AcotarAlMapa()
    {
        PisoCenital piso = PisoCenital.Actual;
        if (piso == null || piso.Mapa == null || cuerpo == null) return;
        Rect lim = piso.Mapa.Limites;
        if (lim.width < 1f || lim.height < 1f) return;

        const float margen = 0.4f;
        Vector2 p = cuerpo.position;
        Vector2 c = new Vector2(Mathf.Clamp(p.x, lim.xMin + margen, lim.xMax - margen), Mathf.Clamp(p.y, lim.yMin + margen, lim.yMax - margen));
        if (c != p) cuerpo.position = c;
    }

    private void FixedUpdate()
    {
        AcotarAlMapa();
        if (Time.time < finEmpuje) cuerpo.linearVelocity = velocidadEmpuje;
        else cuerpo.linearVelocity = (martillando || Aturdido) ? Vector2.zero : entrada * velocidad;
    }

    // ------------------------------------------------------------ choque con el demoledor
    private void OnCollisionEnter2D(Collision2D c) { Chocar(c); }
    private void OnCollisionStay2D(Collision2D c) { Chocar(c); }

    /// <summary>
    /// Tocar al demoledor no quita vida: empuja al jugador en dirección contraria y lo aturde.
    /// Después de un choque hay una espera antes de que otro vuelva a aturdirlo.
    /// </summary>
    private void Chocar(Collision2D c)
    {
        if (!enabled || Aturdido || Invencible || Time.time < proximoChoque) return;
        DemoledorCenital demoledor = c.collider.GetComponentInParent<DemoledorCenital>();
        if (demoledor == null) return;

        DefinicionPiso def = Definicion;
        float empuje = def != null ? def.empujeChoque : 1f;
        float aturdimiento = def != null ? def.aturdimientoChoque : 1.5f;
        float espera = def != null ? def.esperaChoque : 3f;

        Vector2 dir = (Vector2)transform.position - (Vector2)demoledor.transform.position;
        if (dir.sqrMagnitude < 0.0001f) dir = -direccion;
        dir.Normalize();

        finAturdido = Time.time + aturdimiento;
        proximoChoque = Time.time + Mathf.Max(espera, aturdimiento);
        finEmpuje = Time.time + Mathf.Max(0.02f, duracionEmpuje);
        velocidadEmpuje = dir * (empuje / Mathf.Max(0.02f, duracionEmpuje));
        martillando = false;
        entrada = Vector2.zero;
    }

    // ------------------------------------------------------------ poderes
    /// <summary>
    /// Aplica el efecto de un poder. Devuelve un mensaje que reemplaza al 'mensajeRecogido' del poder
    /// (por ejemplo cuando el botiquín no sirve porque las vidas están al máximo), o null.
    /// </summary>
    public string AplicarPoder(PoderPiso poder)
    {
        if (poder == null) return null;
        switch (poder.tipo)
        {
            case TipoPoder.Invulnerabilidad:
                poderInvencible = poder;
                finInvencible = Time.time + poder.duracion;
                if (aroInvencible == null) aroInvencible = CrearAro("AroInvencible");
                break;

            case TipoPoder.ReparacionTotal:
                poderReparacion = poder;
                finReparacionTotal = Time.time + poder.duracion;
                if (aroReparacion == null) aroReparacion = CrearAro("AroReparacion");
                break;

            case TipoPoder.VidaExtra:
                GestorJuego gestor = FindFirstObjectByType<GestorJuego>();
                PisoCenital piso = PisoCenital.Actual;
                int maximo = piso != null && piso.Definicion != null ? piso.Definicion.vidasMaximas : 3;
                if (gestor == null || !gestor.GanarVida(poder.valor, maximo)) return "VIDAS AL MÁXIMO";
                break;
        }
        return null;
    }

    /// <summary>Poderes activos con su tiempo restante, para el HUD ("INVENCIBLE 3,2 s").</summary>
    public string TextoPoderes()
    {
        bool inv = Invencible, rep = ReparacionTotal;
        if (!inv && !rep) return "";

        string t = "";
        if (inv) t += (poderInvencible != null ? poderInvencible.etiquetaHud : "INVENCIBLE") + "  " + (finInvencible - Time.time).ToString("0.0") + " s";
        if (inv && rep) t += "     ";
        if (rep) t += (poderReparacion != null ? poderReparacion.etiquetaHud : "REPARACIÓN TOTAL") + "  " + (finReparacionTotal - Time.time).ToString("0.0") + " s";
        return t;
    }

    private SpriteRenderer CrearAro(string nombre)
    {
        SpriteRenderer sr = Provisional.Crear(transform, nombre, Provisional.Aro, Color.white, 0);
        sr.name = nombre;
        sr.gameObject.SetActive(false);
        return sr;
    }

    private void ActualizarAros()
    {
        ActualizarAro(aroInvencible, Invencible, poderInvencible, 0f, ArteNiveles.De(a => a.aroPoderDorado));
        ActualizarAro(aroReparacion, ReparacionTotal, poderReparacion, 0.12f, ArteNiveles.De(a => a.aroPoderVerde));
    }

    private void ActualizarAro(SpriteRenderer aro, bool activo, PoderPiso poder, float extra, Sprite[] arte)
    {
        if (aro == null) return;
        if (aro.gameObject.activeSelf != activo) aro.gameObject.SetActive(activo);
        if (!activo) return;

        // Aro del arte (2 cuadros en bucle): dorado = invulnerable, verde = reparación total.
        if (arte != null && (poder == null || poder.spriteAro == null))
        {
            if (aro.GetComponent<AnimadorSprite>() == null) EfectoSprite.EnBucle(aro, arte, 6f);
            aro.color = Color.white;
            float pulsoArte = 1f + 0.03f * Mathf.Sin(Time.time * pulsoAro);
            aro.transform.localPosition = new Vector3(0f, alturaAro, 0f);
            aro.transform.localScale = new Vector3(pulsoArte, pulsoArte, 1f);
            aro.sortingOrder = Provisional.OrdenY(transform.position.y) + 4;
            return;
        }

        if (poder != null)
        {
            if (poder.spriteAro != null && aro.sprite != poder.spriteAro) aro.sprite = poder.spriteAro;
            Color c = poder.colorAro;
            float pulso = 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(Time.time * pulsoAro));
            aro.color = new Color(c.r, c.g, c.b, pulso);
        }
        float k = 1f + 0.04f * Mathf.Sin(Time.time * pulsoAro);
        aro.transform.localPosition = new Vector3(0f, alturaAro, 0f);
        aro.transform.localScale = new Vector3((tamanoAro.x + extra) * k, (tamanoAro.y + extra) * k, 1f);
        aro.sortingOrder = Provisional.OrdenY(transform.position.y) + 4;
    }

    private void CrearEstrellas()
    {
        estrellas = new GameObject("Estrellas").transform;
        estrellas.SetParent(transform, false);

        // Con arte: una sola animación de 4 cuadros que gira sobre la cabeza.
        Sprite[] arteAturdido = ArteNiveles.De(a => a.estrellasAturdido);
        if (arteAturdido != null)
        {
            SpriteRenderer unica = Provisional.Crear(estrellas, "EstrellasAturdido", arteAturdido[0], Color.white, 0);
            EfectoSprite.EnBucle(unica, arteAturdido, 8f);
            spritesEstrellas = new[] { unica };
            estrellasConArte = true;
            estrellas.gameObject.SetActive(false);
            return;
        }
        spritesEstrellas = new SpriteRenderer[Mathf.Max(1, numeroEstrellas)];
        for (int i = 0; i < spritesEstrellas.Length; i++)
        {
            SpriteRenderer sr = Provisional.Crear(estrellas, "Estrella" + (i + 1), Provisional.Estrella, new Color(1f, 0.9f, 0.2f, 1f), 0);
            sr.transform.localScale = new Vector3(tamanoEstrella, tamanoEstrella, 1f);
            spritesEstrellas[i] = sr;
        }
        estrellas.gameObject.SetActive(false);
    }

    private void ActualizarEstrellas(bool visibles)
    {
        if (estrellas == null) return;
        if (estrellas.gameObject.activeSelf != visibles) estrellas.gameObject.SetActive(visibles);
        if (!visibles) return;

        int n = spritesEstrellas.Length;
        int orden = Provisional.OrdenY(transform.position.y) + 5;
        if (estrellasConArte)
        {
            spritesEstrellas[0].transform.localPosition = new Vector3(0f, alturaEstrellas, 0f);
            spritesEstrellas[0].sortingOrder = orden;
            return;
        }
        for (int i = 0; i < n; i++)
        {
            float ang = (Time.time * velocidadEstrellas + i * 360f / n) * Mathf.Deg2Rad;
            spritesEstrellas[i].transform.localPosition = new Vector3(Mathf.Cos(ang) * radioEstrellas,
                alturaEstrellas + Mathf.Sin(ang) * radioEstrellas * 0.35f, 0f);
            spritesEstrellas[i].transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 200f);
            spritesEstrellas[i].sortingOrder = orden;
        }
    }

    private void LeerEntrada()
    {
        float x = 0f, y = 0f;
        Keyboard k = Keyboard.current;
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

        entrada = new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
        if (entrada.sqrMagnitude > 1f) entrada.Normalize();
        if (entrada.sqrMagnitude > 0.01f && !martillando) direccion = entrada;
    }

    // ------------------------------------------------------------ martillo
    private void Martillar()
    {
        martillando = true;
        reparoHecho = false;
        inicioMartillo = Time.time;
        Sprite[] serie = SerieGolpe();
        float duracion = serie != null && serie.Length > 0 ? serie.Length / Mathf.Max(1f, cuadrosPorSegundoMartillo) : 0.35f;
        finMartillo = inicioMartillo + Mathf.Max(0.3f, duracion);
        proximoMartillo = finMartillo + enfriamiento;
        if (serie != null) animador.Reproducir(serie, cuadrosPorSegundoMartillo, false, true);
    }

    private void Reparar()
    {
        reparoHecho = true;
        GestorAudio.Reproducir(Snd.Martillazo);
        PisoCenital piso = PisoCenital.Actual;
        if (piso == null || piso.Mapa == null) return;

        // Si el demoledor va a golpear un objeto que está al alcance, ese tiene prioridad: así se puede bloquear.
        if (demoledorRef == null) demoledorRef = FindFirstObjectByType<DemoledorCenital>();
        ObjetoReparable amenazado = demoledorRef != null ? demoledorRef.ObjetivoAmenazado : null;

        ObjetoReparable mejor = null;
        float mejorDistancia = radioReparacion;
        if (amenazado != null && Vector2.Distance(Centro, amenazado.PuntoMasCercano(Centro)) <= radioReparacion)
        {
            // Un objeto intacto también se defiende: martillarlo mientras lo amenazan basta para bloquear.
            if (amenazado.Estado == EstadoObjeto.Intacto)
            {
                if (demoledorRef.IntentarBloqueo(amenazado)) ConfirmarBloqueo(piso);
                return;
            }
            mejor = amenazado;
            mejorDistancia = 0f;
        }
        foreach (ObjetoReparable o in piso.Mapa.objetos)
        {
            if (o == null || o.Estado == EstadoObjeto.Intacto) continue;
            float d = Vector2.Distance(Centro, o.PuntoMasCercano(Centro));
            if (d > mejorDistancia) continue;
            mejorDistancia = d;
            mejor = o;
        }
        if (mejor == null) return;
        bool reparo = false;
        EfectoSprite.Reproducir(ArteNiveles.De(a => a.chispasReparar), mejor.Centro, Provisional.OrdenY(mejor.Area.min.y) - 5, 1f, 12f);
        if (ReparacionTotal)
        {
            int pasos = mejor.RepararTodo();
            for (int i = 0; i < pasos; i++) piso.RegistrarReparacion();
            reparo = pasos > 0;
        }
        else if (mejor.Reparar())
        {
            piso.RegistrarReparacion();
            reparo = true;
        }

        if (reparo && mejor.Estado == EstadoObjeto.Intacto) GestorAudio.Reproducir(Snd.ObjetoReparado);

        // Bloqueo: reparó el objeto justo cuando el demoledor iba a golpearlo.
        if (reparo && demoledorRef != null && demoledorRef.IntentarBloqueo(mejor)) ConfirmarBloqueo(piso);
    }

    private void ConfirmarBloqueo(PisoCenital piso)
    {
        piso.RegistrarBloqueo();
        GestorAudio.Reproducir(Snd.BloqueoSalvado);
        int puntos = piso.Definicion != null ? piso.Definicion.puntosBloqueo : 50;
        GestorJuego gestor = FindFirstObjectByType<GestorJuego>();
        if (gestor != null) gestor.RegistrarBloqueo(puntos);
        TextoFlotante.Mostrar(Centro + Vector2.up * 1.3f, "¡SALVADO!", new Color(0.4f, 1f, 0.5f));
    }

    // ------------------------------------------------------------ daño
    /// <summary>Quita una vida (la cuenta GestorJuego). Devuelve false si está invulnerable.</summary>
    public bool RecibirGolpe()
    {
        if (!PuedeRecibirGolpe) return false;
        finInvulnerable = Time.time + tiempoInvulnerable;
        GestorAudio.Reproducir(Snd.RecibirDano);
        if (Golpeado != null) Golpeado();
        return true;
    }

    public void Reaparecer(Vector3 posicion)
    {
        transform.position = posicion;
        if (cuerpo != null)
        {
            cuerpo.position = posicion;
            cuerpo.linearVelocity = Vector2.zero;
        }
        finInvulnerable = 0f;
        finAturdido = 0f;
        finEmpuje = 0f;
        proximoChoque = 0f;
        finInvencible = 0f;
        finReparacionTotal = 0f;
        martillando = false;
        direccion = Vector2.down;
        if (dibujo != null) dibujo.enabled = true;
        MostrarQuieto();
    }

    private void Parpadear()
    {
        if (dibujo == null) return;
        bool invulnerable = Time.time < finInvulnerable;
        dibujo.enabled = !invulnerable || ((int)(Time.time * 12f) % 2 == 0);
    }

    // ------------------------------------------------------------ dibujo
    private Sprite[] SerieCaminar()
    {
        if (Mathf.Abs(direccion.x) > Mathf.Abs(direccion.y)) return animaciones.caminarDerecha;
        return direccion.y > 0f ? animaciones.caminarArriba : animaciones.caminarAbajo;
    }

    private Sprite[] SerieGolpe()
    {
        Sprite[] s;
        if (Mathf.Abs(direccion.x) > Mathf.Abs(direccion.y)) s = animaciones.golpeDerecha;
        else s = direccion.y > 0f ? animaciones.golpeArriba : animaciones.golpeAbajo;
        if (s == null || s.Length == 0) s = animaciones.golpeDerecha;
        return s;
    }

    private void MostrarQuieto()
    {
        Sprite[] s = SerieCaminar();
        if (s != null && s.Length > 0 && animador != null) animador.MostrarCuadroFijo(s, 0);
    }

    private void Animar()
    {
        if (dibujo == null) return;
        if (Mathf.Abs(direccion.x) > Mathf.Abs(direccion.y)) dibujo.flipX = direccion.x < 0f;
        else dibujo.flipX = false;

        if (!martillando)
        {
            Sprite[] serie = SerieCaminar();
            if (serie != null && serie.Length > 0)
            {
                if (entrada.sqrMagnitude > 0.01f) animador.Reproducir(serie, cuadrosCaminar, true, false);
                else animador.MostrarCuadroFijo(serie, 0);
            }
        }
        animador.Actualizar(Time.deltaTime);

        // Sin arte: cuadro azul provisional (se marca el martillazo con otro tono).
        if (dibujo.sprite == null)
        {
            dibujo.sprite = Provisional.Cuadrado;
            fueraDeCuadros = true;
        }
        if (fueraDeCuadros)
        {
            dibujo.color = martillando ? new Color(1f, 0.85f, 0.3f) : new Color(0.3f, 0.55f, 1f);
        }
    }

    /// <summary>Escala el sprite a 'alturaDibujo' baldosas y apoya sus pies en el punto del jugador.</summary>
    private void AjustarDibujo()
    {
        Sprite s = dibujo.sprite;
        if (s == null) return;
        Bounds b = s.bounds;
        if (b.size.y <= 0.0001f) return;

        float escala;
        if (fueraDeCuadros && s == Provisional.Cuadrado) escala = 1f;
        else escala = alturaDibujo / b.size.y;

        Vector3 tam = fueraDeCuadros && s == Provisional.Cuadrado
            ? new Vector3(0.6f, alturaDibujo * 0.8f, 1f) : new Vector3(escala, escala, 1f);
        dibujo.transform.localScale = tam;
        float signo = dibujo.flipX ? -1f : 1f;
        dibujo.transform.localPosition = new Vector3(-b.center.x * tam.x * signo, -b.min.y * tam.y, 0f);
        dibujo.sortingOrder = Provisional.OrdenY(transform.position.y);
    }
}
