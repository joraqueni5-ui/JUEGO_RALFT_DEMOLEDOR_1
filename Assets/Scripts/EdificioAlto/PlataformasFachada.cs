using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// La fachada se recorre como un juego de plataformas, sin tubos para trepar:
///  - quita las canaletas trepables;
///  - une las repisas de cada piso con tablones de andamio (se camina de punta a punta del piso);
///  - en la planta baja, el toldo, el letrero de la entrada y un tablón hacen de escalón entre la
///    vereda y la primera fila de repisas (antes quedaba demasiado alta para un salto).
/// Todo es de un solo sentido: se atraviesa saltando desde abajo y se baja con abajo.
/// </summary>
public class PlataformasFachada : MonoBehaviour
{
    private readonly List<PlataformaFachada> plataformas = new List<PlataformaFachada>();
    private static readonly Dictionary<int, Sprite> cacheTablon = new Dictionary<int, Sprite>();

    private void Start()
    {
        QuitarTubos();
        FachadaCompuesta fachada = Object.FindAnyObjectByType<FachadaCompuesta>();
        EdificioAlto edificio = EdificioAlto.Instancia;
        if (fachada == null || edificio == null) return;
        ConstruirAndamios(fachada, edificio);
        ConstruirPlantaBaja(fachada);
    }

    private void Update()
    {
        for (int i = 0; i < plataformas.Count; i++)
        {
            PlataformaFachada p = plataformas[i];
            if (p == null || p.vecina == null || p.vecina.colision == null || p.caja == null) continue;
            bool activa = p.vecina.colision.enabled;
            if (p.caja.enabled != activa) p.caja.enabled = activa;
        }
    }

    // ------------------------------------------------------------ tubos
    private static void QuitarTubos()
    {
        foreach (ZonaTrepable z in Object.FindObjectsByType<ZonaTrepable>(FindObjectsInactive.Include))
        {
            if (z == null) continue;
            z.enabled = false;
            z.gameObject.SetActive(false);
        }
        GameObject grupo = GameObject.Find("Trepables");
        if (grupo != null) grupo.SetActive(false);
    }

    // ------------------------------------------------------------ andamios entre columnas
    private void ConstruirAndamios(FachadaCompuesta fachada, EdificioAlto edificio)
    {
        float ppu = fachada.pixelesPorUnidad;
        for (int piso = 0; piso < edificio.Pisos; piso++)
        {
            for (int col = 0; col < edificio.columnas - 1; col++)
            {
                RepisaEdificio a = edificio.RepisaEn(piso, col);
                RepisaEdificio b = edificio.RepisaEn(piso, col + 1);
                if (a == null || b == null) continue;

                Vector3 la = a.transform.localPosition;
                Vector3 lb = b.transform.localPosition;
                float x0 = la.x + a.tamano.x - 1f / ppu;      // se monta un pixel sobre cada repisa
                float x1 = lb.x + 1f / ppu;
                if (x1 - x0 <= 0.01f) continue;

                PlataformaFachada p = Crear(a.transform.parent, string.Format("Andamio_{0}_{1}", piso, col),
                    new Vector2(x0, la.y), x1 - x0, a.tamano.y, ppu, true, a.dibujo != null ? a.dibujo.sortingOrder : 3);
                p.piso = piso;
                p.seccion = a.seccion;
                p.vecina = a;
            }
        }
    }

    // ------------------------------------------------------------ planta baja
    private void ConstruirPlantaBaja(FachadaCompuesta fachada)
    {
        float ppu = fachada.pixelesPorUnidad;
        Transform padre = fachada.transform.Find("Repisas");
        if (padre == null) padre = fachada.transform;

        // Filas del dibujo original (px desde arriba) -> altura local: la planta baja ocupa las últimas 'alturaBase' filas.
        System.Func<int, float> Y = fila => (fachada.alturaBase - (fila - fachada.origenBase)) / ppu;
        float grosor = fachada.grosorRepisa / ppu;

        // Toldo a rayas de la cafetería (izquierda): se pisa el toldo mismo.
        Escalon(padre, "Escalon_Toldo", 3, 149, Y(648), grosor, ppu, false);
        // Letrero "Universidad Franz Tamayo" sobre la entrada.
        Escalon(padre, "Escalon_Letrero", 152, 272, Y(640), grosor, ppu, false);
        // A la derecha no hay nada donde pisar: un tablón de andamio sobre las ventanas azules.
        Escalon(padre, "Escalon_Andamio", 274, 338, Y(646), grosor, ppu, true);
    }

    private void Escalon(Transform padre, string nombre, int x0px, int x1px, float y, float grosor, float ppu, bool visible)
    {
        PlataformaFachada p = Crear(padre, nombre, new Vector2(x0px / ppu, y), (x1px - x0px) / ppu, grosor, ppu, visible, 3);
        p.piso = -1;
        p.seccion = 0;
    }

    private PlataformaFachada Crear(Transform padre, string nombre, Vector2 local, float ancho, float grosor, float ppu, bool visible, int orden)
    {
        GameObject g = new GameObject(nombre);
        g.layer = padre != null ? padre.gameObject.layer : 0;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = new Vector3(local.x, local.y, 0f);

        BoxCollider2D caja = g.AddComponent<BoxCollider2D>();
        caja.size = new Vector2(ancho, grosor);
        caja.offset = new Vector2(ancho * 0.5f, -grosor * 0.5f);
        caja.usedByEffector = true;
        PlatformEffector2D efecto = g.AddComponent<PlatformEffector2D>();
        efecto.useOneWay = true;
        efecto.surfaceArc = 160f;

        if (visible)
        {
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = Tablon(Mathf.Max(4, Mathf.RoundToInt(ancho * ppu)), ppu);
            sr.sortingOrder = orden;
        }

        PlataformaFachada p = g.AddComponent<PlataformaFachada>();
        p.caja = caja;
        plataformas.Add(p);
        return p;
    }

    // ------------------------------------------------------------ dibujo del tablón
    /// <summary>Tablón de andamio en pixel art: madera con vetas, borde oscuro y dos soportes de metal debajo.</summary>
    private static Sprite Tablon(int ancho, float ppu)
    {
        Sprite s;
        if (cacheTablon.TryGetValue(ancho, out s) && s != null) return s;

        const int alto = 10;   // 6 de tablón + 4 de soportes
        Color32 contorno = new Color32(58, 34, 20, 255);
        Color32 brillo = new Color32(240, 192, 120, 255);
        Color32 madera = new Color32(205, 142, 78, 255);
        Color32 vetas = new Color32(170, 108, 56, 255);
        Color32 sombra = new Color32(128, 78, 40, 255);
        Color32 metal = new Color32(120, 128, 140, 255);
        Color32 metalOscuro = new Color32(70, 76, 88, 255);
        Color32 nada = new Color32(0, 0, 0, 0);

        Texture2D t = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[ancho * alto];
        for (int i = 0; i < px.Length; i++) px[i] = nada;

        // Filas de arriba (fila 'alto-1' es la de arriba del todo en la textura).
        for (int x = 0; x < ancho; x++)
        {
            bool borde = x == 0 || x == ancho - 1;
            px[(alto - 1) * ancho + x] = contorno;
            px[(alto - 2) * ancho + x] = borde ? contorno : brillo;
            px[(alto - 3) * ancho + x] = borde ? contorno : (x % 7 == 3 ? vetas : madera);
            px[(alto - 4) * ancho + x] = borde ? contorno : (x % 9 == 5 ? vetas : madera);
            px[(alto - 5) * ancho + x] = borde ? contorno : sombra;
            px[(alto - 6) * ancho + x] = contorno;
        }
        // Clavos cada tanto.
        for (int x = 3; x < ancho - 2; x += 12) px[(alto - 3) * ancho + x] = contorno;

        // Soportes: escuadras de metal cerca de cada punta.
        int[] soportes = ancho >= 14 ? new[] { 3, ancho - 5 } : new[] { ancho / 2 - 1 };
        foreach (int sx in soportes)
        {
            for (int f = 0; f < 4; f++)
            {
                int fila = alto - 7 - f;
                if (fila < 0) break;
                for (int dx = 0; dx < 2; dx++)
                {
                    int x = sx + dx;
                    if (x < 0 || x >= ancho) continue;
                    px[fila * ancho + x] = dx == 0 ? metal : metalOscuro;
                }
            }
        }

        t.SetPixels32(px);
        t.Apply(false, true);
        // Pivote arriba a la izquierda: la parte de arriba del tablón coincide con la superficie pisable.
        s = Sprite.Create(t, new Rect(0, 0, ancho, alto), new Vector2(0f, 1f), ppu, 0, SpriteMeshType.FullRect);
        s.name = "Tablon_" + ancho;
        cacheTablon[ancho] = s;
        return s;
    }

    /// <summary>La plataforma agregada bajo un punto (los pies), o null.</summary>
    public static PlataformaFachada Bajo(Vector2 pies, float distancia, LayerMask capa)
    {
        RaycastHit2D[] golpes = Physics2D.RaycastAll(pies, Vector2.down, distancia, capa);
        for (int i = 0; i < golpes.Length; i++)
        {
            if (golpes[i].collider == null) continue;
            PlataformaFachada p = golpes[i].collider.GetComponent<PlataformaFachada>();
            if (p != null) return p;
        }
        return null;
    }
}
