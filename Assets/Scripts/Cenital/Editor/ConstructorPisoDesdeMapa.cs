using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Arma cualquier piso cenital a partir del mapa de texto de su DefinicionPiso: Tilemaps de suelo y paredes,
/// puertas, entrada del demoledor, ascensor, reparables, decoración, puntos "?", puntos de aparición de poderes,
/// NPC, inicio del jugador y carteles. Guarda el resultado en el prefab Mapa_PisoN (si ya existe lo reconstruye
/// conservando el componente raíz, así las escenas que lo usan siguen funcionando) y, al final, comprueba con el
/// A* que todo sea alcanzable, avisando en la consola de lo que no lo sea.
///
/// Leyenda común (cualquier piso): # pared, / puerta, V entrada del demoledor, A ascensor, J inicio del jugador,
/// ? punto de información, N NPC, P y Q puntos de aparición de poderes. Todo lo demás se define en la leyenda
/// de la DefinicionPiso. Letras iguales y contiguas con 'unirContiguas' forman un solo objeto.
/// </summary>
public static class ConstructorPisoDesdeMapa
{
    private const string CarpetaArte = "Assets/Arte/PisoCenital";
    private const string CarpetaPrefabs = "Assets/Prefabs/Mapas";


    public static string RutaPrefabDe(DefinicionPiso def)
    {
        if (def.prefabMapa != null)
        {
            string existente = AssetDatabase.GetAssetPath(def.prefabMapa);
            if (!string.IsNullOrEmpty(existente)) return existente;
        }
        return CarpetaPrefabs + "/Mapa_Piso" + def.numero + ".prefab";
    }

    // ------------------------------------------------------------ interpretación de la leyenda
    private static bool TipoDe(DefinicionPiso def, char c, out TipoCelda tipo)
    {
        EntradaLeyenda e = def.LeyendaDe(c);
        if (e != null) { tipo = e.tipo; return true; }
        switch (c)
        {
            case '#': tipo = TipoCelda.Pared; return true;
            case '/': tipo = TipoCelda.Puerta; return true;
            case 'V': tipo = TipoCelda.Entrada; return true;
            case 'A': tipo = TipoCelda.Ascensor; return true;
            case 'J': tipo = TipoCelda.Jugador; return true;
            case '?': tipo = TipoCelda.PuntoInfo; return true;
            case 'N': tipo = TipoCelda.Npc; return true;
            case 'P': tipo = TipoCelda.PoderP; return true;
            case 'Q': tipo = TipoCelda.PoderQ; return true;
            case '!': tipo = TipoCelda.PuntoSalto; return true;
        }
        tipo = TipoCelda.Decoracion;
        return false;
    }

    private static bool EsMuro(TipoCelda t)
    {
        return t == TipoCelda.Pared || t == TipoCelda.ParedDecorada || t == TipoCelda.Entrada || t == TipoCelda.Ascensor;
    }

    // ------------------------------------------------------------ construcción
    public static bool Construir(DefinicionPiso def)
    {
        if (Application.isPlaying) { Debug.LogError("[Mapa] Sal del modo Play antes de construir el piso."); return false; }

        Analisis analisis = Analizar(def);
        if (analisis == null) return false;
        MapaTexto mt = analisis.mt;
        TipoCelda[,] tipos = analisis.tipos;
        bool[,] hay = analisis.hay;
        char[,] zona = analisis.zona;
        List<KeyValuePair<EntradaLeyenda, MapaTexto.Grupo>> reparables = analisis.reparables;
        Dictionary<char, int> indiceDecoracion = analisis.indiceDecoracion;

        // 4) Prefab.
        string ruta = RutaPrefabDe(def);
        bool existia = AssetDatabase.LoadAssetAtPath<GameObject>(ruta) != null;
        GameObject raiz = existia ? PrefabUtility.LoadPrefabContents(ruta) : new GameObject("Mapa_Piso" + def.numero);
        try
        {
            ArmarMapa(def, mt, tipos, hay, zona, reparables, indiceDecoracion, raiz);
            PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        }
        finally
        {
            if (existia) PrefabUtility.UnloadPrefabContents(raiz);
            else Object.DestroyImmediate(raiz);
        }

        def.prefabMapa = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        EditorUtility.SetDirty(def);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Validar(def, ruta);
        return true;
    }

    /// <summary>Resultado de leer el mapa de texto de una definición.</summary>
    private class Analisis
    {
        public MapaTexto mt;
        public TipoCelda[,] tipos;
        public bool[,] hay;
        public char[,] zona;
        public List<KeyValuePair<EntradaLeyenda, MapaTexto.Grupo>> reparables;
        public Dictionary<char, int> indiceDecoracion;
    }

    /// <summary>
    /// Lee el mapa de texto: comprueba la leyenda, calcula la zona de suelo de cada celda y deja en la definición la lista
    /// de reparables, de decoración y el tamaño del mapa. Devuelve null si el mapa no se puede leer.
    /// </summary>
    private static Analisis Analizar(DefinicionPiso def)
    {
        MapaTexto mt = MapaTexto.Leer(def.mapa);
        if (mt.Ancho == 0 || mt.Alto == 0) { Debug.LogError("[Mapa] La definición '" + def.name + "' no tiene mapa de texto."); return null; }

        // 1) Cada carácter del mapa debe tener significado.
        TipoCelda[,] tipos = new TipoCelda[mt.Ancho, mt.Alto];
        bool[,] hay = new bool[mt.Ancho, mt.Alto];
        int errores = 0;
        HashSet<char> avisados = new HashSet<char>();
        for (int x = 0; x < mt.Ancho; x++)
        {
            for (int y = 0; y < mt.Alto; y++)
            {
                char c = mt[x, y];
                if (c == ' ') continue;
                TipoCelda t;
                if (!TipoDe(def, c, out t))
                {
                    if (avisados.Add(c)) { Debug.LogError("[Mapa] El carácter '" + c + "' no está en la leyenda de '" + def.name + "'."); errores++; }
                    continue;
                }
                tipos[x, y] = t;
                hay[x, y] = true;
            }
        }
        if (errores > 0) return null;

        ConstructorPisoCenital.AsegurarCarpeta("Assets", "Arte");
        ConstructorPisoCenital.AsegurarCarpeta("Assets/Arte", "PisoCenital");
        ConstructorPisoCenital.AsegurarCarpeta("Assets", "Prefabs");
        ConstructorPisoCenital.AsegurarCarpeta("Assets/Prefabs", "Mapas");

        // 2) Zona de suelo de cada celda (el carácter de suelo del que hereda).
        char[,] zona = CalcularZonas(def, mt, tipos, hay);

        // 3) Listas de objetos y tipos de decoración.
        List<EntradaLeyenda> tiposDecoracion = new List<EntradaLeyenda>();
        Dictionary<char, int> indiceDecoracion = new Dictionary<char, int>();
        if (def.leyenda != null)
        {
            foreach (EntradaLeyenda e in def.leyenda)
            {
                if (e == null || (e.tipo != TipoCelda.Decoracion && e.tipo != TipoCelda.ParedDecorada)) continue;
                indiceDecoracion[e.Letra] = tiposDecoracion.Count;
                tiposDecoracion.Add(e);
            }
        }

        List<KeyValuePair<EntradaLeyenda, MapaTexto.Grupo>> reparables = new List<KeyValuePair<EntradaLeyenda, MapaTexto.Grupo>>();
        if (def.leyenda != null)
        {
            foreach (EntradaLeyenda e in def.leyenda)
                if (e != null && e.tipo == TipoCelda.Reparable)
                    foreach (MapaTexto.Grupo g in mt.Grupos(e.Letra, e.unirContiguas)) reparables.Add(new KeyValuePair<EntradaLeyenda, MapaTexto.Grupo>(e, g));
        }
        reparables.Sort((a, b) =>
        {
            int ta = a.Value.y + a.Value.h - 1, tb = b.Value.y + b.Value.h - 1;
            return ta != tb ? tb.CompareTo(ta) : a.Value.x.CompareTo(b.Value.x);
        });

        def.objetosCenital = new ObjetoCenital[reparables.Count];
        for (int i = 0; i < reparables.Count; i++)
        {
            EntradaLeyenda e = reparables[i].Key;
            MapaTexto.Grupo g = reparables[i].Value;
            def.objetosCenital[i] = new ObjetoCenital
            {
                nombre = e.nombre, laboratorio = Mathf.Clamp(e.zona, 0, 1), tamanoBaldosas = new Vector2Int(g.w, g.h),
                sprites = TresSprites(e, g.w, g.h),
                colorProvisional = e.colorProvisional == Color.gray ? new Color(0f, 0f, 0f, 0f) : e.colorProvisional,
            };
        }

        def.decoracion = new FichaDecoracion[tiposDecoracion.Count];
        for (int i = 0; i < tiposDecoracion.Count; i++)
        {
            EntradaLeyenda e = tiposDecoracion[i];
            def.decoracion[i] = new FichaDecoracion
            {
                nombre = e.nombre, tamanoBaldosas = new Vector2Int(1, 1),
                sprite = e.sprites != null && e.sprites.Length > 0 ? e.sprites[0] : null,
                colorProvisional = e.colorProvisional,
                colision = e.colision && e.tipo == TipoCelda.Decoracion ? ColisionDecoracion.Simple : ColisionDecoracion.Ninguna,
                sombra = e.sombra, ordenarPorAltura = e.ordenarPorAltura, provisionalCircular = e.provisionalCircular,
                escalaProvisional = e.escalaProvisional, ajustarAFranja = e.ajustarAFranja,
                spriteIzquierda = e.spriteIzquierda, spriteCentro = e.spriteCentro, spriteDerecha = e.spriteDerecha,
                spritesPorTamano = e.spritesPorTamano != null ? (Sprite[])e.spritesPorTamano.Clone() : new Sprite[0],
            };
        }
        def.tamanoMapa = new Vector2Int(mt.Ancho, mt.Alto);

        return new Analisis { mt = mt, tipos = tipos, hay = hay, zona = zona, reparables = reparables, indiceDecoracion = indiceDecoracion };
    }

    // ------------------------------------------------------------ zonas de suelo
    private static char[,] CalcularZonas(DefinicionPiso def, MapaTexto mt, TipoCelda[,] tipos, bool[,] hay)
    {
        char[,] zona = new char[mt.Ancho, mt.Alto];
        Queue<Vector2Int> cola = new Queue<Vector2Int>();
        char primera = '\0';
        for (int y = mt.Alto - 1; y >= 0; y--)
        {
            for (int x = 0; x < mt.Ancho; x++)
            {
                if (!hay[x, y] || tipos[x, y] != TipoCelda.Suelo) continue;
                zona[x, y] = mt[x, y];
                if (primera == '\0') primera = mt[x, y];
                cola.Enqueue(new Vector2Int(x, y));
            }
        }

        Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        while (cola.Count > 0)
        {
            Vector2Int c = cola.Dequeue();
            foreach (Vector2Int d in dirs)
            {
                int nx = c.x + d.x, ny = c.y + d.y;
                if (nx < 0 || ny < 0 || nx >= mt.Ancho || ny >= mt.Alto || !hay[nx, ny] || zona[nx, ny] != '\0') continue;
                if (EsMuro(tipos[nx, ny]) || tipos[nx, ny] == TipoCelda.Puerta) continue;
                zona[nx, ny] = zona[c.x, c.y];
                cola.Enqueue(new Vector2Int(nx, ny));
            }
        }

        // Las puertas toman el suelo de un lado.
        for (int x = 0; x < mt.Ancho; x++)
        {
            for (int y = 0; y < mt.Alto; y++)
            {
                if (!hay[x, y] || tipos[x, y] != TipoCelda.Puerta) continue;
                foreach (Vector2Int d in dirs)
                {
                    int nx = x + d.x, ny = y + d.y;
                    if (nx < 0 || ny < 0 || nx >= mt.Ancho || ny >= mt.Alto) continue;
                    if (zona[nx, ny] != '\0') { zona[x, y] = zona[nx, ny]; break; }
                }
            }
        }

        for (int x = 0; x < mt.Ancho; x++)
        {
            for (int y = 0; y < mt.Alto; y++)
            {
                if (!hay[x, y] || EsMuro(tipos[x, y]) || zona[x, y] != '\0') continue;
                if (primera == '\0') { Debug.LogError("[Mapa] El mapa no tiene ningún carácter de suelo en su leyenda."); return zona; }
                zona[x, y] = primera;
                Debug.LogWarning("[Mapa] La celda (" + x + ", " + y + ") no está conectada a ningún suelo: usa '" + primera + "'.");
            }
        }
        return zona;
    }

    // ------------------------------------------------------------ prefab
    private static void ArmarMapa(DefinicionPiso def, MapaTexto mt, TipoCelda[,] tipos, bool[,] hay, char[,] zona,
        List<KeyValuePair<EntradaLeyenda, MapaTexto.Grupo>> reparables, Dictionary<char, int> indiceDecoracion, GameObject raiz)
    {
        for (int i = raiz.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(raiz.transform.GetChild(i).gameObject);
        raiz.transform.position = Vector3.zero;

        MapaCenital mapa = raiz.GetComponent<MapaCenital>();
        if (mapa == null) mapa = raiz.AddComponent<MapaCenital>();
        mapa.spawnJugador = null; mapa.spawnDemoledor = null; mapa.entradaDemoledor = null; mapa.ventanal = null; mapa.ascensor = null;

        // Tilemaps.
        GameObject objGrid = new GameObject("Grid");
        objGrid.transform.SetParent(raiz.transform, false);
        Grid grid = objGrid.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        GameObject objSuelo = new GameObject("Suelo");
        objSuelo.transform.SetParent(objGrid.transform, false);
        Tilemap suelo = objSuelo.AddComponent<Tilemap>();
        objSuelo.AddComponent<TilemapRenderer>().sortingOrder = -200;

        GameObject objParedes = new GameObject("Paredes");
        objParedes.transform.SetParent(objGrid.transform, false);
        Tilemap paredes = objParedes.AddComponent<Tilemap>();
        objParedes.AddComponent<TilemapRenderer>().sortingOrder = -150;
        Rigidbody2D rb = objParedes.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        TilemapCollider2D tc = objParedes.AddComponent<TilemapCollider2D>();
        tc.compositeOperation = Collider2D.CompositeOperation.Merge;
        objParedes.AddComponent<CompositeCollider2D>();

        // Baldosas: la pared es transparente (solo da la colisión); DibujoParedes pinta la franja delgada.
        Texture2D transparente = ConstructorPisoCenital.Lienzo(32, 32, new Color(0f, 0f, 0f, 0f));
        transparente.Apply();
        Tile tilePared = ConstructorPisoCenital.CrearBaldosa("pared_colision", transparente, true);
        Dictionary<char, Tile[]> tilesDeZona = new Dictionary<char, Tile[]>();

        List<Vector2Int> puertas = new List<Vector2Int>();
        for (int x = 0; x < mt.Ancho; x++)
        {
            for (int y = 0; y < mt.Alto; y++)
            {
                if (!hay[x, y]) continue;
                Vector3Int celda = new Vector3Int(x, y, 0);
                TipoCelda t = tipos[x, y];
                if (t == TipoCelda.Pared || t == TipoCelda.ParedDecorada) { paredes.SetTile(celda, tilePared); continue; }
                if (t == TipoCelda.Entrada || t == TipoCelda.Ascensor) continue;

                char z = zona[x, y];
                Tile[] tiles;
                if (!tilesDeZona.TryGetValue(z, out tiles)) { tiles = TilesDeZona(def, z); tilesDeZona[z] = tiles; }
                int h = (int)(((uint)(x * 73856093) ^ (uint)(y * 19349663)) & 0x7fffffff) % 100;
                bool variante = t != TipoCelda.Puerta && tiles.Length > 1 && h >= 55;
                suelo.SetTile(celda, variante ? tiles[1 + (h - 55) % (tiles.Length - 1)] : tiles[0]);
                if (t == TipoCelda.Puerta) puertas.Add(new Vector2Int(x, y));
            }
        }
        mapa.puertas = puertas.ToArray();
        mapa.barandas = CalcularBarandas(def, mt, tipos, hay, zona).ToArray();

        // Reparables.
        Transform contObjetos = Hijo(raiz.transform, "Objetos");
        for (int i = 0; i < reparables.Count; i++)
        {
            EntradaLeyenda e = reparables[i].Key;
            MapaTexto.Grupo g = reparables[i].Value;
            GameObject o = new GameObject(e.nombre);
            o.transform.SetParent(contObjetos, false);
            o.transform.position = new Vector3(g.x + g.w * 0.5f, g.y + g.h * 0.5f, 0f);
            BoxCollider2D col = o.AddComponent<BoxCollider2D>();
            col.size = new Vector2(g.w, g.h);
            ObjetoReparable obj = o.AddComponent<ObjetoReparable>();
            obj.indice = i;
            obj.laboratorio = Mathf.Clamp(e.zona, 0, 1);
        }

        // Decoración (del mapa y por coordenadas).
        Transform contDeco = Hijo(raiz.transform, "Decoracion");
        int piezas = 0;
        foreach (KeyValuePair<char, int> par in indiceDecoracion)
        {
            EntradaLeyenda e = def.LeyendaDe(par.Key);
            foreach (MapaTexto.Grupo g in mt.Grupos(par.Key, e.unirContiguas))
            {
                CrearPieza(contDeco, e, par.Value, g.x, g.y, g.w, g.h, "", e.tipo == TipoCelda.ParedDecorada, false);
                piezas++;
            }
        }
        if (def.piezasExtra != null)
        {
            foreach (PiezaExtra x in def.piezasExtra)
            {
                if (x == null || string.IsNullOrEmpty(x.caracter)) continue;
                int indice;
                if (!indiceDecoracion.TryGetValue(x.caracter[0], out indice))
                {
                    Debug.LogWarning("[Mapa] La pieza extra '" + x.caracter + "' no es una decoración de la leyenda: se omite.");
                    continue;
                }
                CrearPieza(contDeco, def.LeyendaDe(x.caracter[0]), indice, x.posicion.x, x.posicion.y, Mathf.Max(1, x.tamano.x), Mathf.Max(1, x.tamano.y),
                    x.texto, x.enPared, x.encima);
                piezas++;
            }
        }

        // Entrada del demoledor, ascensor, inicio, poderes y NPC.
        CrearEntrada(def, mt, tipos, mapa, raiz.transform);
        CrearAscensor(def, mt, tipos, mapa, raiz.transform);

        List<int[]> inicios = CeldasDe(mt, tipos, TipoCelda.Jugador);
        if (inicios.Count > 0)
        {
            GameObject sj = new GameObject("SpawnJugador");
            sj.transform.SetParent(raiz.transform, false);
            sj.transform.position = new Vector3(inicios[0][0] + 0.5f, inicios[0][1] + 0.5f, 0f);
            mapa.spawnJugador = sj.transform;
            if (inicios.Count > 1) Debug.LogWarning("[Mapa] Hay " + inicios.Count + " celdas de inicio del jugador (J): se usa la primera.");
        }
        else Debug.LogWarning("[Mapa] No hay inicio del jugador (J).");

        // Punto de salto del demoledor ('!'): solo un marcador de editor.
        List<int[]> saltos = CeldasDe(mt, tipos, TipoCelda.PuntoSalto);
        if (saltos.Count > 0)
        {
            int sx = saltos[0][0], sy = saltos[0][1];
            Vector2Int fueraSalto = DireccionHaciaFuera(mt, tipos, sx, sy);
            GameObject ps = new GameObject("PuntoSalto");
            ps.transform.SetParent(raiz.transform, false);
            ps.transform.position = new Vector3(sx + 0.5f, sy + 0.5f, 0f);
            PuntoSaltoDemoledor marca = ps.AddComponent<PuntoSaltoDemoledor>();
            marca.direccion = new Vector2(fueraSalto.x, fueraSalto.y);
            mapa.puntoSalto = ps.transform;
            mapa.direccionSalto = marca.direccion;
            if (saltos.Count > 1) Debug.LogWarning("[Mapa] Hay " + saltos.Count + " puntos de salto (!): se usa el primero.");
        }

        mapa.puntosPoderP = Centros(CeldasDe(mt, tipos, TipoCelda.PoderP));
        mapa.puntosPoderQ = Centros(CeldasDe(mt, tipos, TipoCelda.PoderQ));

        // Puntos "?": de izquierda a derecha. Cada uno conserva el dato de la ficha anterior más cercana (el del área donde está);
        // si hay más "?" que datos, se usan en orden los datos de reserva del piso. Un "?" sin dato es un error.
        List<int[]> info = CeldasDe(mt, tipos, TipoCelda.PuntoInfo);
        info.Sort((a, b) => a[0] != b[0] ? a[0].CompareTo(b[0]) : b[1].CompareTo(a[1]));
        PuntoInfoPiso[] previos = def.puntosInfo ?? new PuntoInfoPiso[0];
        bool[] usado = new bool[previos.Length];
        PuntoInfoPiso[] nuevos = new PuntoInfoPiso[info.Count];
        int siguienteReserva = 0;
        for (int i = 0; i < info.Count; i++)
        {
            Vector2 pos = new Vector2(info[i][0] + 0.5f, info[i][1] + 0.5f);
            int mejor = -1;
            float distMejor = float.MaxValue;
            for (int k = 0; k < previos.Length; k++)
            {
                if (usado[k] || previos[k] == null || string.IsNullOrWhiteSpace(previos[k].texto)) continue;
                float d = Vector2.Distance(previos[k].posicion, pos);
                if (d < distMejor) { distMejor = d; mejor = k; }
            }
            PuntoInfoPiso origen = null;
            if (mejor >= 0) { usado[mejor] = true; origen = previos[mejor]; }
            else if (def.datosReserva != null)
            {
                while (siguienteReserva < def.datosReserva.Length && (def.datosReserva[siguienteReserva] == null || string.IsNullOrWhiteSpace(def.datosReserva[siguienteReserva].texto))) siguienteReserva++;
                if (siguienteReserva < def.datosReserva.Length) origen = def.datosReserva[siguienteReserva++];
            }
            if (origen == null)
                Debug.LogError("[DatoÚtil] Piso " + def.numero + ": el \"?\" en " + pos + " no tiene dato. Agrega uno en DefinicionPiso.puntosInfo o en datosReserva.");
            nuevos[i] = new PuntoInfoPiso
            {
                nombre = origen != null ? origen.nombre : "Punto " + (i + 1),
                texto = origen != null ? origen.texto : "",
                area = origen != null ? origen.area : "",
                consejo = origen != null ? origen.consejo : "",
                posicion = pos,
            };
        }
        def.puntosInfo = nuevos;

        Transform contNpc = Hijo(raiz.transform, "Estudiantes");
        foreach (int[] c in CeldasDe(mt, tipos, TipoCelda.Npc))
        {
            EntradaLeyenda e = def.LeyendaDe(mt[c[0], c[1]]);
            GameObject g = new GameObject(e != null ? e.nombre : "NPC");
            g.transform.SetParent(contNpc, false);
            g.transform.position = new Vector3(c[0] + 0.5f, c[1] + 0.5f, 0f);
            EstudianteNPC npc = g.AddComponent<EstudianteNPC>();
            if (e != null)
            {
                npc.laboratorio = Mathf.Clamp(e.zona, 0, 1);
                npc.esChef = e.esChef;
                if (e.sprites != null && e.sprites.Length > 0) npc.cuadrosPropios = (Sprite[])e.sprites.Clone();
            }
        }

        Debug.Log("[Mapa] '" + def.name + "': " + mt.Ancho + " x " + mt.Alto + " baldosas, " + reparables.Count + " reparables, " + piezas +
                  " piezas de decoración, " + puertas.Count + " puertas, " + info.Count + " puntos '?'.");
    }

    /// <summary>Los tres sprites del reparable (intacto, dañado, roto); los que falten quedan vacíos y se generan al jugar.</summary>
    private static Sprite[] TresSprites(EntradaLeyenda e, int ancho, int alto)
    {
        Sprite[] r = new Sprite[3];
        if (e.sprites != null) for (int i = 0; i < 3 && i < e.sprites.Length; i++) r[i] = e.sprites[i];

        // Un arte hecho justo a la medida del objeto del mapa (por ejemplo 2 x 1 en vez de 3 x 1) reemplaza al intacto.
        if (e.spritesPorTamano != null)
        {
            foreach (Sprite v in e.spritesPorTamano)
            {
                if (v == null) continue;
                if (Mathf.RoundToInt(v.rect.width / v.pixelsPerUnit) == ancho && Mathf.RoundToInt(v.rect.height / v.pixelsPerUnit) == alto) { r[0] = v; break; }
            }
        }
        return r;
    }

    /// <summary>Dirección en la que hay una pared del borde del mapa junto a la celda (hacia fuera del edificio).</summary>
    private static Vector2Int DireccionHaciaFuera(MapaTexto mt, TipoCelda[,] tipos, int x, int y)
    {
        Vector2Int[] dirs = { Vector2Int.right, Vector2Int.down, Vector2Int.up, Vector2Int.left };
        foreach (Vector2Int d in dirs)
        {
            int nx = x + d.x, ny = y + d.y;
            if (mt[nx, ny] != ' ' && tipos[nx, ny] == TipoCelda.Pared && mt[nx + d.x, ny + d.y] == ' ') return d;
        }
        return Vector2Int.right;
    }

    /// <summary>
    /// Paredes del borde exterior que se dibujan como baranda: las que dan al vacío y tienen al lado un suelo
    /// marcado como 'barandaExterior' (una terraza).
    /// </summary>
    private static List<Vector2Int> CalcularBarandas(DefinicionPiso def, MapaTexto mt, TipoCelda[,] tipos, bool[,] hay, char[,] zona)
    {
        List<Vector2Int> lista = new List<Vector2Int>();
        Vector2Int[] ortogonales = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        Vector2Int[] diagonales = { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) };
        System.Func<int, int, bool> terraza = (x, y) =>
        {
            if (x < 0 || y < 0 || x >= mt.Ancho || y >= mt.Alto || !hay[x, y] || EsMuro(tipos[x, y])) return false;
            EntradaLeyenda e = def.LeyendaDe(zona[x, y]);
            return e != null && e.barandaExterior;
        };

        for (int x = 0; x < mt.Ancho; x++)
        {
            for (int y = 0; y < mt.Alto; y++)
            {
                if (!hay[x, y] || tipos[x, y] != TipoCelda.Pared) continue;
                int fuera = 0;
                bool junto = false;
                foreach (Vector2Int d in ortogonales)
                {
                    if (mt[x + d.x, y + d.y] == ' ') fuera++;
                    else if (terraza(x + d.x, y + d.y)) junto = true;
                }
                if (fuera == 0) continue;
                if (!junto && fuera >= 2) foreach (Vector2Int d in diagonales) if (terraza(x + d.x, y + d.y)) junto = true;
                if (junto) lista.Add(new Vector2Int(x, y));
            }
        }
        return lista;
    }

    private static Transform Hijo(Transform padre, string nombre)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        return g.transform;
    }

    private static void CrearPieza(Transform padre, EntradaLeyenda e, int indice, int x, int y, int w, int h, string texto, bool enPared, bool encima)
    {
        GameObject go = new GameObject(e.nombre);
        go.transform.SetParent(padre, false);
        go.transform.position = new Vector3(x + w * 0.5f + e.desplazamiento.x, y + h * 0.5f + e.desplazamiento.y, 0f);
        if (e.colision && e.tipo == TipoCelda.Decoracion && !enPared)
        {
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w, h);
        }
        DecoracionCenital d = go.AddComponent<DecoracionCenital>();
        d.tipo = indice;
        d.tamano = new Vector2Int(w, h);
        d.texto = texto;
        d.enPared = enPared;
        d.encima = encima;
    }

    private static List<int[]> CeldasDe(MapaTexto mt, TipoCelda[,] tipos, TipoCelda tipo)
    {
        List<int[]> lista = new List<int[]>();
        for (int y = mt.Alto - 1; y >= 0; y--)
            for (int x = 0; x < mt.Ancho; x++)
                if (mt[x, y] != ' ' && tipos[x, y] == tipo) lista.Add(new[] { x, y });
        return lista;
    }

    private static Vector2[] Centros(List<int[]> celdas)
    {
        Vector2[] r = new Vector2[celdas.Count];
        for (int i = 0; i < r.Length; i++) r[i] = new Vector2(celdas[i][0] + 0.5f, celdas[i][1] + 0.5f);
        return r;
    }

    // ------------------------------------------------------------ entrada del demoledor y ascensor
    private static MapaTexto.Grupo? PrimerGrupo(DefinicionPiso def, MapaTexto mt, TipoCelda[,] tipos, TipoCelda tipo, string que, bool avisarSiFalta = true)
    {
        List<MapaTexto.Grupo> todos = new List<MapaTexto.Grupo>();
        HashSet<char> letras = new HashSet<char>();
        for (int y = mt.Alto - 1; y >= 0; y--)
            for (int x = 0; x < mt.Ancho; x++)
                if (mt[x, y] != ' ' && tipos[x, y] == tipo && letras.Add(mt[x, y])) todos.AddRange(mt.Grupos(mt[x, y], true));
        if (todos.Count == 0) { if (avisarSiFalta) Debug.LogWarning("[Mapa] No hay " + que + " en el mapa."); return null; }
        if (todos.Count > 1) Debug.LogWarning("[Mapa] Hay " + todos.Count + " grupos de " + que + ": se usa el primero.");
        return todos[0];
    }

    private static void CrearEntrada(DefinicionPiso def, MapaTexto mt, TipoCelda[,] tipos, MapaCenital mapa, Transform raiz)
    {
        MapaTexto.Grupo? gn = PrimerGrupo(def, mt, tipos, TipoCelda.Entrada, "entrada del demoledor (V)");
        if (gn == null) return;
        MapaTexto.Grupo g = gn.Value;
        EntradaLeyenda e = def.LeyendaDe(g.c);

        // Hacia fuera: el lado del grupo que da al vacío o al borde del mapa.
        Vector2Int fuera = Vector2Int.up;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (Vector2Int d in dirs)
        {
            bool todoVacio = true;
            for (int i = 0; i < (d.x != 0 ? g.h : g.w); i++)
            {
                int cx = d.x > 0 ? g.x + g.w : d.x < 0 ? g.x - 1 : g.x + i;
                int cy = d.y > 0 ? g.y + g.h : d.y < 0 ? g.y - 1 : g.y + i;
                if (mt[cx, cy] != ' ') { todoVacio = false; break; }
            }
            if (todoVacio) { fuera = d; break; }
        }

        GameObject o = new GameObject(e != null && !string.IsNullOrEmpty(e.nombre) ? e.nombre : "Entrada");
        o.transform.SetParent(raiz, false);
        Vector3 centro = new Vector3(g.x + g.w * 0.5f, g.y + g.h * 0.5f, 0f);
        o.transform.position = centro;
        o.AddComponent<BoxCollider2D>().size = new Vector2(g.w, g.h);
        VentanalCenital v = o.AddComponent<VentanalCenital>();
        v.rompible = e != null && e.rompible;
        if (e != null && e.sprites != null && e.sprites.Length > 0 && e.sprites[0] != null)
        {
            // El arte se gira o se repite para cubrir el grupo (una puerta vertical, dos ventanas o escaleras seguidas).
            bool girar = fuera.x != 0 && !v.rompible;
            Sprite roto = e.sprites.Length > 1 && e.sprites[1] != null ? e.sprites[1] : e.sprites[0];
            v.spriteEntero = SpritesDerivados.Ajustar(e.sprites[0], g.w, g.h, girar);
            v.spriteRoto = SpritesDerivados.Ajustar(roto, g.w, g.h, girar);
        }
        else if (!v.rompible)
        {
            Sprite escaleras = ConstructorPisoCenital.CrearSpritePublico("escaleras_piso" + def.numero, TexturaEscaleras(g.w * 32, g.h * 32, fuera), 32);
            v.spriteEntero = escaleras;
            v.spriteRoto = escaleras;
        }
        mapa.ventanal = v;

        GameObject spawn = new GameObject("SpawnDemoledor");
        spawn.transform.SetParent(raiz, false);
        spawn.transform.position = centro + new Vector3(fuera.x, fuera.y, 0f) * 1.3f;
        mapa.spawnDemoledor = spawn.transform;

        GameObject entrada = new GameObject("EntradaDemoledor");
        entrada.transform.SetParent(raiz, false);
        entrada.transform.position = centro - new Vector3(fuera.x, fuera.y, 0f) * 1.9f;
        mapa.entradaDemoledor = entrada.transform;
    }

    private static void CrearAscensor(DefinicionPiso def, MapaTexto mt, TipoCelda[,] tipos, MapaCenital mapa, Transform raiz)
    {
        // Un piso con punto de salto ('!') no tiene ascensor: el demoledor se va saltando.
        bool haySalto = CeldasDe(mt, tipos, TipoCelda.PuntoSalto).Count > 0;
        MapaTexto.Grupo? gn = PrimerGrupo(def, mt, tipos, TipoCelda.Ascensor, "ascensor (A)", !haySalto);
        if (gn == null) return;
        MapaTexto.Grupo g = gn.Value;
        EntradaLeyenda e = def.LeyendaDe(g.c);

        GameObject o = new GameObject("Ascensor");
        o.transform.SetParent(raiz, false);
        o.transform.position = new Vector3(g.x + g.w * 0.5f, g.y + g.h * 0.5f, 0f);
        o.AddComponent<BoxCollider2D>().size = new Vector2(g.w, g.h);
        SpriteRenderer sr = o.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -140;
        sr.sprite = e != null && e.sprites != null && e.sprites.Length > 0 && e.sprites[0] != null
            ? SpritesDerivados.Ajustar(e.sprites[0], g.w, g.h, false)
            : ConstructorPisoCenital.CrearSpritePublico("ascensor_piso" + def.numero, TexturaAscensor(g.w * 32, g.h * 32), 32);
        mapa.ascensor = o.AddComponent<AscensorCenital>();

        // El ascensor abierto sigue la orientación del cerrado (un ascensor vertical gira el arte un cuarto de vuelta).
        if (def.ascensorAbierto != null) def.ascensorAbierto = SpritesDerivados.Ajustar(def.ascensorAbierto, g.w, g.h, false);
    }

    // ------------------------------------------------------------ texturas provisionales
    /// <summary>Escalones vistos desde arriba: bandas claras y oscuras que se alejan hacia 'fuera'.</summary>
    private static Texture2D TexturaEscaleras(int ancho, int alto, Vector2Int fuera)
    {
        Texture2D t = ConstructorPisoCenital.Lienzo(ancho, alto, Color.gray);
        Color claro = new Color(0.78f, 0.74f, 0.68f), oscuro = new Color(0.66f, 0.62f, 0.56f), linea = new Color(0.42f, 0.39f, 0.35f);
        const int escalones = 6;
        for (int y = 0; y < alto; y++)
        {
            for (int x = 0; x < ancho; x++)
            {
                float u = fuera.y > 0 ? y / (float)(alto - 1) : fuera.y < 0 ? 1f - y / (float)(alto - 1)
                        : fuera.x > 0 ? x / (float)(ancho - 1) : 1f - x / (float)(ancho - 1);
                float k = Mathf.Min(u, 0.999f) * escalones;
                int paso = (int)k;
                Color c = paso % 2 == 0 ? claro : oscuro;
                if (k - paso < 0.12f) c = linea;
                bool lateral = fuera.y != 0 ? (x < 2 || x >= ancho - 2) : (y < 2 || y >= alto - 2);
                if (lateral) c = new Color(0.30f, 0.28f, 0.26f);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }

    /// <summary>Puertas del ascensor cerradas: cuerpo gris, marco oscuro y la junta de las dos hojas.</summary>
    private static Texture2D TexturaAscensor(int ancho, int alto)
    {
        Texture2D t = ConstructorPisoCenital.Lienzo(ancho, alto, new Color(0.45f, 0.47f, 0.52f));
        Color marco = new Color(0.2f, 0.21f, 0.25f);
        for (int x = 0; x < ancho; x++) { t.SetPixel(x, 0, marco); t.SetPixel(x, alto - 1, marco); }
        for (int y = 0; y < alto; y++) { t.SetPixel(0, y, marco); t.SetPixel(ancho - 1, y, marco); }
        if (ancho >= alto)
            for (int y = 0; y < alto; y++) { t.SetPixel(ancho / 2 - 1, y, marco); t.SetPixel(ancho / 2, y, marco); }
        else
            for (int x = 0; x < ancho; x++) { t.SetPixel(x, alto / 2 - 1, marco); t.SetPixel(x, alto / 2, marco); }
        t.Apply();
        return t;
    }

    // ------------------------------------------------------------ baldosas de suelo
    /// <summary>Baldosa base y variantes de una zona (la base primero). Con sprite propio no hay variantes.</summary>
    private static Tile[] TilesDeZona(DefinicionPiso def, char z)
    {
        EntradaLeyenda e = def.LeyendaDe(z);
        if (e == null) return new[] { ConstructorPisoCenital.CrearBaldosa("suelo_sin_definir", ConstructorPisoCenital.Tablero(Color.gray, Color.black), false) };

        string nombre = string.IsNullOrEmpty(e.nombre) ? "suelo_" + (int)z : e.nombre;
        if (e.sprites != null && e.sprites.Length > 0 && e.sprites[0] != null)
        {
            string ruta = CarpetaArte + "/" + nombre + "_tile.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(ruta);
            if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, ruta); }
            tile.sprite = e.sprites[0];
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return new[] { tile };
        }

        Color a = e.colorProvisional, b = e.colorSecundario;
        switch (e.estilo)
        {
            case EstiloSuelo.Tablero:
                return new[]
                {
                    ConstructorPisoCenital.CrearBaldosa(nombre, ConstructorPisoCenital.Tablero(a, b), false),
                    ConstructorPisoCenital.CrearBaldosa(nombre + "_v1", ConstructorPisoCenital.Tablero(b, a), false),
                    ConstructorPisoCenital.CrearBaldosa(nombre + "_v2", Salpicar(ConstructorPisoCenital.Tablero(a, b), Oscurecer(b, 0.8f), 14, 11), false),
                    ConstructorPisoCenital.CrearBaldosa(nombre + "_v3", Grieta(ConstructorPisoCenital.Tablero(a, b), Oscurecer(b, 0.7f), 5), false),
                };
            case EstiloSuelo.Borde:
                return new[]
                {
                    ConstructorPisoCenital.CrearBaldosa(nombre, ConstructorPisoCenital.Borde(a, b), false),
                    ConstructorPisoCenital.CrearBaldosa(nombre + "_v1", Salpicar(ConstructorPisoCenital.Borde(a, b), Oscurecer(b, 0.9f), 9, 3), false),
                    ConstructorPisoCenital.CrearBaldosa(nombre + "_v2", Grieta(ConstructorPisoCenital.Borde(a, b), Oscurecer(b, 0.8f), 8), false),
                };
            case EstiloSuelo.Pasto:
                return new[]
                {
                    ConstructorPisoCenital.CrearBaldosa(nombre, Pasto(a, b, 3), false),
                    ConstructorPisoCenital.CrearBaldosa(nombre + "_v1", Pasto(a, b, 11), false),
                    ConstructorPisoCenital.CrearBaldosa(nombre + "_v2", Pasto(a, b, 23), false),
                };
            default:
                return new[] { ConstructorPisoCenital.CrearBaldosa(nombre, ConstructorPisoCenital.Lienzo(32, 32, a), false) };
        }
    }

    /// <summary>Césped provisional: fondo verde con briznas más claras y más oscuras repartidas al azar.</summary>
    private static Texture2D Pasto(Color baseVerde, Color brizna, int semilla)
    {
        Texture2D t = ConstructorPisoCenital.Lienzo(32, 32, baseVerde);
        System.Random azar = new System.Random(semilla);
        Color oscuro = Oscurecer(baseVerde, 0.82f);
        for (int i = 0; i < 38; i++)
        {
            int x = azar.Next(1, 31), y = azar.Next(1, 29);
            Color c = i % 3 == 0 ? oscuro : brizna;
            t.SetPixel(x, y, c);
            t.SetPixel(x, y + 1, c);   // brizna de 2 px de alto
        }
        t.Apply();
        return t;
    }

    private static Color Oscurecer(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, 1f); }

    private static Texture2D Salpicar(Texture2D t, Color color, int cantidad, int semilla)
    {
        System.Random azar = new System.Random(semilla);
        for (int i = 0; i < cantidad; i++) t.SetPixel(azar.Next(2, 30), azar.Next(2, 30), color);
        t.Apply();
        return t;
    }

    private static Texture2D Grieta(Texture2D t, Color color, int semilla)
    {
        System.Random azar = new System.Random(semilla);
        int x = azar.Next(8, 24), y = 4;
        for (; y < 28; y++)
        {
            t.SetPixel(x, y, color);
            if (azar.Next(0, 3) == 0) x = Mathf.Clamp(x + azar.Next(-1, 2), 2, 29);
        }
        t.Apply();
        return t;
    }

    // ------------------------------------------------------------ verificación con el A*
    private static void Validar(DefinicionPiso def, string ruta)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (prefab == null) return;
        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            MapaCenital mapa = temp.GetComponent<MapaCenital>();
            mapa.Preparar(def);
            ValidarMapa(def, mapa);
        }
        finally
        {
            Object.DestroyImmediate(temp);
        }
    }

    /// <summary>
    /// Comprueba con el A* que desde la entrada del demoledor se llega a cada reparable, punto "?", punto de poderes,
    /// inicio del jugador y ascensor (o punto de salto). El mapa debe estar preparado (Preparar). Devuelve los problemas.
    /// </summary>
    public static int ValidarMapa(DefinicionPiso def, MapaCenital mapa)
    {
        Vector2 desde = mapa.entradaDemoledor != null ? (Vector2)mapa.entradaDemoledor.position : mapa.Limites.center;
        int problemas = 0;

        // Las paredes deben tener collider de baldosa entera.
        foreach (UnityEngine.Tilemaps.Tilemap tm in mapa.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>())
        {
            CompositeCollider2D cc = tm.GetComponent<CompositeCollider2D>();
            if (cc == null) continue;
            if (cc.pointCount == 0) { problemas++; Debug.LogError("[Mapa] Las paredes de '" + def.name + "' no tienen collider."); }
            else Debug.Log("[Mapa] Paredes de '" + def.name + "': collider con " + cc.pathCount + " contornos.");
        }

        foreach (ObjetoReparable o in mapa.objetos)
        {
            bool llega = false;
            foreach (Vector2 celda in mapa.CentrosTransitables)
            {
                if (Vector2.Distance(celda, o.PuntoMasCercano(celda)) > 0.8f) continue;
                if (BuscadorRutas.Buscar(mapa, desde, celda) != null) { llega = true; break; }
            }
            if (!llega) { problemas++; Debug.LogWarning("[Mapa] El reparable '" + o.name + "' en " + o.Centro + " no es alcanzable."); }
        }

        foreach (PuntoInfoPiso p in def.puntosInfo)
            problemas += Comprobar(mapa, desde, p.posicion, "El punto '?' '" + p.nombre + "'");
        foreach (Vector2 p in mapa.puntosPoderP) problemas += Comprobar(mapa, desde, p, "El punto de poderes P");
        foreach (Vector2 p in mapa.puntosPoderQ) problemas += Comprobar(mapa, desde, p, "El punto de poderes Q");
        if (mapa.spawnJugador != null) problemas += Comprobar(mapa, desde, mapa.spawnJugador.position, "El inicio del jugador");

        if (mapa.ascensor != null)
        {
            Collider2D col = mapa.ascensor.GetComponent<Collider2D>();
            bool llega = false;
            foreach (Vector2 celda in mapa.CentrosTransitables)
            {
                if (Vector2.Distance(celda, col.ClosestPoint(celda)) > 1.0f) continue;
                if (BuscadorRutas.Buscar(mapa, desde, celda) != null) { llega = true; break; }
            }
            if (!llega) { problemas++; Debug.LogWarning("[Mapa] El jugador no puede llegar al ascensor."); }
        }
        else if (mapa.puntoSalto != null) problemas += Comprobar(mapa, desde, mapa.puntoSalto.position, "El punto de salto del demoledor (!)");
        else { problemas++; Debug.LogWarning("[Mapa] El piso no tiene ascensor ni punto de salto."); }

        if (mapa.ventanal == null) { problemas++; Debug.LogWarning("[Mapa] El piso no tiene entrada del demoledor."); }

        Debug.Log("[Mapa] Verificación de '" + def.name + "': " + mapa.CentrosTransitables.Count + " celdas transitables, " + mapa.objetos.Count +
                  " reparables, " + (problemas == 0 ? "todo es alcanzable." : problemas + " problema(s): revisa los avisos."));
        return problemas;
    }

    /// <summary>
    /// Arma el mapa del piso como un objeto "Mapa" en la escena abierta (sin prefab), con todo ya dibujado y guardado:
    /// suelos, paredes, puertas, entrada del demoledor, ascensor, reparables, decoración, carteles, puntos "?" y puntos
    /// de poderes. Comprueba con el A* que todo sea alcanzable. Devuelve el mapa, o null si el mapa de texto no se pudo leer.
    /// </summary>
    public static MapaCenital ConstruirEnEscena(DefinicionPiso def)
    {
        if (Application.isPlaying) { Debug.LogError("[Mapa] Sal del modo Play antes de construir el piso."); return null; }

        Analisis a = Analizar(def);
        if (a == null) return null;

        GameObject raiz = new GameObject("Mapa");
        ArmarMapa(def, a.mt, a.tipos, a.hay, a.zona, a.reparables, a.indiceDecoracion, raiz);
        EditorUtility.SetDirty(def);
        AssetDatabase.SaveAssets();

        MapaCenital mapa = raiz.GetComponent<MapaCenital>();
        GuardadoMapaEscena.Guardar(def, mapa);
        ValidarMapa(def, mapa);
        return mapa;
    }

    private static int Comprobar(MapaCenital mapa, Vector2 desde, Vector2 punto, string que)
    {
        Vector2Int celda = mapa.CeldaDe(punto);
        if (!mapa.EsTransitable(celda))
        {
            Debug.LogWarning("[Mapa] " + que + " en " + punto + " está sobre una celda bloqueada.");
            return 1;
        }
        if (BuscadorRutas.Buscar(mapa, desde, punto) == null)
        {
            Debug.LogWarning("[Mapa] " + que + " en " + punto + " no es alcanzable.");
            return 1;
        }
        return 0;
    }
}
