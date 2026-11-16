using UnityEngine;

/// <summary>
/// Arma la fachada del edificio alto a partir de recortes del dibujo original (edificio_alto.png) y coloca las
/// ventanas y repisas exactamente sobre las ventanas dibujadas. Como los recortes salen del mismo dibujo, el
/// edificio puede tener 7 pisos (el nivel final: piso 0 a piso 6) o 9 (el modo original) con solo cambiar los
/// valores de aquí: la cantidad de pisos por sección, las columnas y las medidas del arte.
///
/// Se ejecuta antes que EdificioAlto, que luego lee las ventanas y repisas creadas aquí. También rellena en
/// EdificioAlto las alturas de las cornisas, el centro de las columnas, la terraza y las secciones.
///
/// Orden de la fachada (de arriba abajo): techo, pisos de la última sección, cornisa, pisos de la sección
/// anterior, cornisa, ..., pisos de la primera sección y la planta baja del dibujo.
/// </summary>
[DefaultExecutionOrder(-900)]
[RequireComponent(typeof(EdificioAlto))]
public class FachadaCompuesta : MonoBehaviour
{
    [Header("Arte")]
    [Tooltip("Dibujo original de la fachada (386 x 700 px, 9 pisos de ventanas).")]
    public Sprite fachadaOriginal;
    [Tooltip("Ventanas: 4 tipos x 3 estados (intacta, rota, destruida), en ese orden.")]
    public Sprite[] ventanas = new Sprite[12];
    [Tooltip("Repisas: intacta, agrietada, destruida.")]
    public Sprite[] repisas = new Sprite[3];
    public Sprite escombro;
    [Tooltip("Pixeles del arte por unidad de mundo.")]
    public float pixelesPorUnidad = 50f;

    [Header("Edificio")]
    [Tooltip("Pisos de cada sección, de abajo hacia arriba. 3,2,2 = nivel final (7 pisos); 3,3,3 = modo original (9 pisos).")]
    public int[] pisosEnSeccion = { 3, 2, 2 };
    [Tooltip("Columnas de ventanas por piso (como máximo las de 'Columnas X').")]
    [Range(1, 5)] public int columnas = 5;

    [Header("Medidas del arte original (px) - para afinar a mano")]
    [Tooltip("Alto del techo del dibujo (arriba del primer piso).")]
    public int alturaTecho = 48;
    [Tooltip("Alto de un piso del dibujo (separación vertical entre ventanas).")]
    public int alturaPiso = 58;
    [Tooltip("Alto de la cornisa que separa dos secciones.")]
    public int alturaCornisa = 8;
    [Tooltip("Alto de la planta baja del dibujo (margen inferior bajo el último piso).")]
    public int alturaBase = 114;
    [Tooltip("Filas del dibujo original donde empiezan los recortes: primer piso, piso normal, piso bajo una cornisa, cornisa y planta baja.")]
    public int origenPrimerPiso = 48, origenPiso = 106, origenPisoBajoCornisa = 230, origenCornisa = 222, origenBase = 586;

    [Header("Ventanas y repisas (px del dibujo)")]
    [Tooltip("Esquina izquierda del marco de cada ventana (separación de columnas).")]
    public int[] columnasX = { 22, 84, 156, 218, 280 };
    public int ventanaAncho = 45, ventanaAlto = 37;
    [Tooltip("Distancia desde el inicio de un piso hasta el borde superior de su ventana.")]
    public int ventanaBajoInicioPiso = 11;
    [Tooltip("Corrimiento fino de todas las ventanas (px): x a la derecha, y hacia arriba.")]
    public Vector2 ajusteVentanas = Vector2.zero;
    [Tooltip("Esquina izquierda de cada repisa.")]
    public int[] repisasX = { 25, 87, 159, 221, 283 };
    public int anchoRepisa = 40, grosorRepisa = 6;
    [Tooltip("Distancia desde el borde superior de la ventana hasta la parte de arriba de su repisa.")]
    public int repisaBajoVentana = 42;
    public Vector2 ajusteRepisas = Vector2.zero;

    [Header("Interior de cada piso (de abajo hacia arriba: piso 0, 1, 2...)")]
    [Tooltip("Color que se ve a través del vidrio de cada piso. Provisional hasta que haya arte.")]
    public Color[] interiorPorPiso =
    {
        new Color(0.80f, 0.55f, 0.30f),   // 0 cafetería
        new Color(0.45f, 0.85f, 0.80f),   // 1 laboratorios de cocina y medicina
        new Color(0.30f, 0.50f, 0.95f),   // 2 computadoras
        new Color(0.90f, 0.85f, 0.65f),   // 3 aulas
        new Color(0.85f, 0.88f, 0.75f),   // 4 aulas
        new Color(0.80f, 0.85f, 0.90f),   // 5 aulas
        new Color(0.45f, 0.30f, 0.60f),   // 6 biblioteca
    };
    [Range(0f, 0.8f)] public float opacidadInterior = 0.28f;

    [Header("Terraza y cartel")]
    [Tooltip("Inicio y fin (px) de la terraza verde del techo.")]
    public Vector2Int terrazaX = new Vector2Int(241, 340);
    [Tooltip("Zona del dibujo original con el cartel UNIFRANZ (x0, y0, x1, y1 desde arriba). Se copia a la terraza.")]
    public RectInt cartelOrigen = new RectInt(244, 592, 56, 50);
    public bool cartelEnTerraza = true;
    [Tooltip("Dónde se para el demoledor en el techo (px desde arriba).")]
    public int alturaTechoPx = 41;
    [Tooltip("Fila del dibujo donde empieza la fachada útil (px desde arriba) y su borde derecho.")]
    public Vector2Int fachadaX = new Vector2Int(1, 362);

    [Header("Vereda (suelo firme de la planta baja)")]
    public bool crearVereda = true;
    [Tooltip("Alto (px) de la franja de vereda en la parte de abajo de la fachada: su superficie queda a esta altura.")]
    public int alturaVeredaPx = 12;
    [Tooltip("Cuánto sobresale la vereda (px) a cada lado del edificio.")]
    public int margenVeredaPx = 40;

    [Header("Muñeco y cámara")]
    [Tooltip("Coloca al muñeco sobre la primera fila de repisas, en esta columna.")]
    public bool colocarMuneco = true;
    [Range(0, 4)] public int columnaInicioMuneco = 1;
    [Tooltip("Ajusta los límites de la cámara al alto de este edificio.")]
    public bool ajustarCamara = true;
    [Tooltip("Cuánto se ve por debajo del suelo (la vereda) cuando el muñeco está abajo.")]
    public float margenInferiorCamara = 0.6f;

    private Vector3 inicioMuneco;
    private bool hayInicioMuneco;

    /// <summary>Alto total de la fachada armada, en unidades de mundo.</summary>
    public float AltoTotal { get; private set; }
    /// <summary>Ancho total (px del dibujo) de la fachada.</summary>
    public int AnchoPx { get { return fachadaOriginal != null ? (int)fachadaOriginal.rect.width : 386; } }

    /// <summary>Alto total (px) de una fachada con esos pisos por sección.</summary>
    public int AltoTotalPx()
    {
        int total = 0;
        foreach (int p in pisosEnSeccion) total += Mathf.Max(0, p);
        return alturaTecho + total * alturaPiso + Mathf.Max(0, pisosEnSeccion.Length - 1) * alturaCornisa + alturaBase;
    }

    private int SeccionDePiso(int piso)
    {
        int acumulado = 0;
        for (int s = 0; s < pisosEnSeccion.Length; s++)
        {
            acumulado += Mathf.Max(0, pisosEnSeccion[s]);
            if (piso < acumulado) return s;
        }
        return Mathf.Max(0, pisosEnSeccion.Length - 1);
    }

    private void Awake()
    {
        if (fachadaOriginal == null || pisosEnSeccion == null || pisosEnSeccion.Length == 0)
        {
            Debug.LogError("[Fachada] Falta el dibujo de la fachada o los pisos por sección.");
            return;
        }
        Construir();
    }

    private void Construir()
    {
        // Limpia lo armado antes (por si el objeto trae hijos de otra construcción).
        foreach (string n in new[] { "Fachada", "Ventanas", "Repisas", "Interiores", "Cartel", "Vereda" })
        {
            Transform t = transform.Find(n);
            if (t != null) Destroy(t.gameObject);
        }

        int totalPisos = 0;
        foreach (int p in pisosEnSeccion) totalPisos += Mathf.Max(0, p);
        int secciones = pisosEnSeccion.Length;
        float ppu = pixelesPorUnidad;

        // 1) Diseño de arriba abajo: cada fila guarda su inicio (px desde arriba), su origen en el dibujo y su alto.
        int altoTotal = alturaTecho + totalPisos * alturaPiso + (secciones - 1) * alturaCornisa + alturaBase;
        AltoTotal = altoTotal / ppu;

        Transform fachada = Hijo("Fachada");
        Transform contVentanas = Hijo("Ventanas");
        Transform contRepisas = Hijo("Repisas");
        Transform contInterior = Hijo("Interiores");

        int cursor = 0;
        Recortar(fachada, 0, alturaTecho, cursor, altoTotal);
        cursor += alturaTecho;

        float[] cornisaY = new float[secciones];
        cornisaY[secciones - 1] = (altoTotal - alturaTechoPx) / ppu;

        for (int u = 0; u < totalPisos; u++)
        {
            int piso = totalPisos - 1 - u;                       // 0 = el de abajo
            int seccion = SeccionDePiso(piso);
            bool bajoCornisa = u > 0 && SeccionDePiso(piso + 1) != seccion;

            if (bajoCornisa)
            {
                Recortar(fachada, origenCornisa, alturaCornisa, cursor, altoTotal);
                cornisaY[seccion] = (altoTotal - (cursor + 1)) / ppu;
                cursor += alturaCornisa;
            }

            int origen = u == 0 ? origenPrimerPiso : bajoCornisa ? origenPisoBajoCornisa : origenPiso;
            Recortar(fachada, origen, alturaPiso, cursor, altoTotal);

            int ventanaY = cursor + ventanaBajoInicioPiso;
            CrearPiso(contVentanas, contRepisas, contInterior, piso, seccion, ventanaY, altoTotal);
            cursor += alturaPiso;
        }

        Recortar(fachada, origenBase, alturaBase, cursor, altoTotal);

        // 2) Cartel UNIFRANZ sobre la terraza del techo.
        if (cartelEnTerraza && cartelOrigen.width > 0)
        {
            Rect r = fachadaOriginal.textureRect;
            float yDesdeAbajo = r.y + r.height - (cartelOrigen.y + cartelOrigen.height);
            Sprite cartel = Sprite.Create(fachadaOriginal.texture, new Rect(r.x + cartelOrigen.x, yDesdeAbajo, cartelOrigen.width, cartelOrigen.height),
                new Vector2(0.5f, 0f), ppu, 0, SpriteMeshType.FullRect);
            GameObject g = new GameObject("Cartel");
            g.transform.SetParent(transform, false);
            float centroTerrazaPx = (terrazaX.x + terrazaX.y) * 0.5f;
            g.transform.localPosition = new Vector3(centroTerrazaPx / ppu, (altoTotal - alturaTechoPx + 1) / ppu, 0f);
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = cartel;
            sr.sortingOrder = 2;
        }

        // Vereda: suelo firme a todo lo ancho (se derrumba en el terremoto).
        if (crearVereda)
        {
            GameObject v = new GameObject("Vereda");
            v.transform.SetParent(transform, false);
            v.AddComponent<VeredaFachada>().Construir(GetComponent<EdificioAlto>(), ppu, AnchoPx, alturaVeredaPx, margenVeredaPx);
        }

        // 3) Datos para EdificioAlto (se leen en su Awake, que corre después).
        EdificioAlto edificio = GetComponent<EdificioAlto>();
        edificio.pisosEnSeccion = (int[])pisosEnSeccion.Clone();
        edificio.secciones = secciones;
        edificio.columnas = columnas;
        edificio.fachada = Rect.MinMaxRect(fachadaX.x / ppu, 0f, fachadaX.y / ppu, (altoTotal - alturaTechoPx) / ppu);
        edificio.alturaCornisas = cornisaY;
        edificio.centroColumnas = new float[columnas];
        for (int c = 0; c < columnas; c++) edificio.centroColumnas[c] = (columnasX[c] + ventanaAncho * 0.5f) / ppu;
        edificio.terraza = new Vector2(terrazaX.x / ppu, terrazaX.y / ppu);

        // 4) Muñeco sobre la primera fila de repisas y límites de la cámara.
        if (colocarMuneco && hayInicioMuneco)
        {
            Muneco muneco = FindFirstObjectByType<Muneco>(FindObjectsInactive.Include);
            if (muneco != null)
            {
                Vector3 mundo = transform.TransformPoint(inicioMuneco);
                muneco.transform.position = new Vector3(mundo.x, mundo.y, muneco.transform.position.z);
            }
        }
        if (ajustarCamara)
        {
            CamaraSeguimiento seguir = FindFirstObjectByType<CamaraSeguimiento>();
            Camera cam = seguir != null ? seguir.GetComponent<Camera>() : null;
            if (seguir != null && cam != null)
            {
                float techo = transform.position.y + altoTotal / ppu;
                seguir.yMinima = cam.orthographicSize - margenInferiorCamara;
                seguir.yMaxima = techo + 0.4f - cam.orthographicSize;
            }
        }
    }

    private Transform Hijo(string nombre)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(transform, false);
        return g.transform;
    }

    /// <summary>Coloca un recorte del dibujo original ('alto' filas desde 'origen') con su borde superior en 'inicio'.</summary>
    private void Recortar(Transform padre, int origen, int alto, int inicio, int altoTotal)
    {
        Rect r = fachadaOriginal.textureRect;
        float yDesdeAbajo = r.y + r.height - (origen + alto);
        Sprite s = Sprite.Create(fachadaOriginal.texture, new Rect(r.x, yDesdeAbajo, r.width, alto), Vector2.zero, pixelesPorUnidad, 0, SpriteMeshType.FullRect);

        GameObject g = new GameObject("Recorte_" + origen);
        g.transform.SetParent(padre, false);
        g.transform.localPosition = new Vector3(0f, (altoTotal - (inicio + alto)) / pixelesPorUnidad, 0f);
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = 1;
    }

    private void CrearPiso(Transform contVentanas, Transform contRepisas, Transform contInterior, int piso, int seccion, int ventanaY, int altoTotal)
    {
        float ppu = pixelesPorUnidad;
        Color interior = interiorPorPiso != null && interiorPorPiso.Length > 0 ? interiorPorPiso[Mathf.Min(piso, interiorPorPiso.Length - 1)] : Color.white;

        for (int col = 0; col < columnas; col++)
        {
            int tipo = (piso * 3 + col * 5 + seccion) % 4;
            float vx = (columnasX[col] + ajusteVentanas.x) / ppu;
            float vy = (altoTotal - ventanaY + ajusteVentanas.y) / ppu;

            GameObject obj = new GameObject(string.Format("Ventana_{0}_{1}", piso, col));
            obj.transform.SetParent(contVentanas, false);
            obj.transform.localPosition = new Vector3(vx, vy, 0f);
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = Ventana(tipo, 0);
            sr.sortingOrder = 2;

            VentanaEdificio v = obj.AddComponent<VentanaEdificio>();
            v.piso = piso;
            v.columna = col;
            v.seccion = seccion;
            v.dibujo = sr;
            v.sprites = new Sprite[] { Ventana(tipo, 0), Ventana(tipo, 1), Ventana(tipo, 2) };
            v.spriteTrozos = escombro;
            v.tamanoMarco = new Vector2(ventanaAncho / ppu, ventanaAlto / ppu);

            // Tinte del interior del piso sobre el vidrio.
            SpriteRenderer tinte = Provisional.Crear(contInterior, "Interior_" + piso + "_" + col, Provisional.Cuadrado,
                new Color(interior.r, interior.g, interior.b, opacidadInterior), 3);
            tinte.transform.localPosition = new Vector3(vx + ventanaAncho * 0.5f / ppu, vy - ventanaAlto * 0.5f / ppu, 0f);
            tinte.transform.localScale = new Vector3(ventanaAncho / ppu, ventanaAlto / ppu, 1f);

            // Repisa pisable bajo la ventana.
            float rx = (repisasX[col] + ajusteRepisas.x) / ppu;
            float ry = (altoTotal - (ventanaY + repisaBajoVentana) + ajusteRepisas.y) / ppu;
            GameObject rep = new GameObject(string.Format("Repisa_{0}_{1}", piso, col));
            rep.transform.SetParent(contRepisas, false);
            rep.transform.localPosition = new Vector3(rx, ry, 0f);
            SpriteRenderer rsr = rep.AddComponent<SpriteRenderer>();
            rsr.sprite = repisas != null && repisas.Length > 0 ? repisas[0] : null;
            rsr.sortingOrder = 3;

            BoxCollider2D caja = rep.AddComponent<BoxCollider2D>();
            caja.size = new Vector2(anchoRepisa / ppu, grosorRepisa / ppu);
            caja.offset = new Vector2(anchoRepisa / ppu * 0.5f, -grosorRepisa / ppu * 0.5f);
            caja.usedByEffector = true;
            PlatformEffector2D efecto = rep.AddComponent<PlatformEffector2D>();
            efecto.useOneWay = true;
            efecto.surfaceArc = 160f;

            RepisaEdificio r = rep.AddComponent<RepisaEdificio>();
            r.piso = piso;
            r.columna = col;
            r.seccion = seccion;
            r.dibujo = rsr;
            r.colision = caja;
            r.sprites = new Sprite[] { repisas[0], repisas[1], repisas[2] };
            r.spriteTrozos = escombro;
            r.tamano = new Vector2(anchoRepisa / ppu, grosorRepisa / ppu);

            if (piso == 0 && col == Mathf.Min(columnaInicioMuneco, columnas - 1))
            {
                inicioMuneco = new Vector3(rx + anchoRepisa * 0.5f / ppu, ry + 0.02f, 0f);
                hayInicioMuneco = true;
            }
        }
    }

    private Sprite Ventana(int tipo, int estado)
    {
        int i = tipo * 3 + estado;
        return ventanas != null && i < ventanas.Length ? ventanas[i] : null;
    }
}
