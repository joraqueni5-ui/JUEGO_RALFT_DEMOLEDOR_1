using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta el modo de 6 pisos en la escena Universidad (menú Tools → Demoledor Unifranz).
///
/// - NO borra ni modifica el edificio alto original ("EdificioAlto"): solo lo deja apagado.
/// - Crea "EdificioPisos": copia exacta de la sección 0 del original (3 filas x 5 columnas
///   de objetos reparables y sus repisas) más el componente EdificioPisos con los 6 pisos.
/// - Crea "SelectorModo" (SelectorModoJuego): el interruptor "Modo Pisos". Marcado enciende
///   EdificioPisos; desmarcado, el edificio original.
/// - No mueve al muñeco, al demoledor, la cámara ni el fondo: el modo pisos ajusta la cámara
///   solo al jugar y la geometría es la de la sección 0, donde ya están el muñeco y el demoledor.
///
/// Se puede ejecutar varias veces: rehace "EdificioPisos" y "SelectorModo" desde cero.
/// Requiere haber creado antes las definiciones de piso (Tools → Crear definiciones de piso).
/// </summary>
public static class ConstructorEdificioPisos
{
    private const string RutaEscena = "Assets/Scenes/Universidad.unity";
    private const string CarpetaPisos = "Assets/Data/Pisos";
    private const int FilasPorPiso = 3;
    private const int Columnas = 5;

    [MenuItem("Tools/Demoledor Unifranz/Construir Edificio Pisos (Universidad)", false, 22)]
    public static void Construir()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Edificio pisos", "Sal del modo Play antes de construir el modo pisos.", "OK");
            return;
        }

        // 1) Las 6 definiciones de piso, en orden.
        List<DefinicionPiso> definiciones = CargarPisos();
        if (definiciones.Count < 6)
        {
            EditorUtility.DisplayDialog("Edificio pisos",
                "Faltan definiciones de piso (hay " + definiciones.Count + " de 6).\n\n" +
                "Ejecuta primero:\nTools → Demoledor Unifranz → Crear definiciones de piso", "OK");
            return;
        }

        if (!AbrirUniversidad()) return;

        // 2) El edificio original debe seguir en la escena (aunque esté apagado).
        GameObject original = RaizPorNombre("EdificioAlto");
        EdificioAlto edificioOriginal = original != null ? original.GetComponent<EdificioAlto>() : null;
        Transform origVentanas = original != null ? original.transform.Find("Ventanas") : null;
        Transform origRepisas = original != null ? original.transform.Find("Repisas") : null;
        if (edificioOriginal == null || origVentanas == null || origRepisas == null)
        {
            EditorUtility.DisplayDialog("Edificio pisos",
                "No encuentro el objeto 'EdificioAlto' (con 'Ventanas' y 'Repisas') en la escena Universidad.\n\n" +
                "Es el edificio original: hace falta para copiar su geometría. No se cambió nada.", "OK");
            return;
        }

        // 3) Rehacer lo que este constructor creó antes (nunca el original).
        BorrarRaiz("EdificioPisos");
        BorrarRaiz("SelectorModo");

        // 4) EdificioPisos: misma posición que el original, así la sección 0 coincide.
        GameObject raiz = new GameObject("EdificioPisos");
        Undo.RegisterCreatedObjectUndo(raiz, "Construir edificio pisos");
        raiz.transform.position = original.transform.position;

        Transform contVentanas = CrearHijo(raiz.transform, "Ventanas");
        Transform contRepisas = CrearHijo(raiz.transform, "Repisas");

        int copiadas = 0;
        for (int piso = 0; piso < FilasPorPiso; piso++)
        {
            for (int col = 0; col < Columnas; col++)
            {
                Transform v = origVentanas.Find(string.Format("Ventana_{0}_{1}", piso, col));
                Transform r = origRepisas.Find(string.Format("Repisa_{0}_{1}", piso, col));
                if (v == null || r == null) continue;

                GameObject copiaV = Object.Instantiate(v.gameObject, contVentanas);
                copiaV.name = v.name;
                copiaV.SetActive(true);
                VentanaEdificio ventana = copiaV.GetComponent<VentanaEdificio>();
                ventana.seccion = 0;
                ventana.tipo = (piso * 3 + col * 5) % 4;

                GameObject copiaR = Object.Instantiate(r.gameObject, contRepisas);
                copiaR.name = r.name;
                copiaR.SetActive(true);
                copiaR.GetComponent<RepisaEdificio>().seccion = 0;
                copiadas++;
            }
        }

        if (copiadas != FilasPorPiso * Columnas)
        {
            Object.DestroyImmediate(raiz);
            EditorUtility.DisplayDialog("Edificio pisos",
                "El edificio original no tiene las 15 ventanas y repisas esperadas (copiadas: " + copiadas + ").\n" +
                "No se cambió nada.", "OK");
            return;
        }

        // Imagen de fondo del piso (vacía hasta que haya arte; el modo pisos usa un color provisional).
        GameObject objFondo = new GameObject("FondoPiso");
        objFondo.transform.SetParent(raiz.transform, false);
        SpriteRenderer srFondo = objFondo.AddComponent<SpriteRenderer>();
        srFondo.sortingOrder = -50;
        srFondo.enabled = false;

        // 5) Componente EdificioPisos, con la geometría de la sección 0 del original.
        EdificioPisos edificio = Undo.AddComponent<EdificioPisos>(raiz);
        edificio.columnas = Columnas;
        edificio.pisosPorSeccion = FilasPorPiso;
        edificio.secciones = 1;
        edificio.fachada = edificioOriginal.fachada;
        edificio.alturaCornisas = new float[] { edificioOriginal.alturaCornisas[0] };
        edificio.centroColumnas = (float[])edificioOriginal.centroColumnas.Clone();
        edificio.terraza = edificioOriginal.terraza;
        edificio.golpesMinimosPorSeccion = new int[] { definiciones[0].golpesMinimos };
        edificio.pisos = definiciones.ToArray();
        edificio.pisoInicial = 0;
        edificio.fondoExterior = RaizPorNombre("Fondo");
        edificio.fondoPiso = srFondo;

        // 6) Interruptor de modo.
        GameObject objSelector = new GameObject("SelectorModo");
        Undo.RegisterCreatedObjectUndo(objSelector, "Construir edificio pisos");
        SelectorModoJuego selector = objSelector.AddComponent<SelectorModoJuego>();
        selector.edificioOriginal = original;
        selector.edificioPisos = raiz;
        selector.modoPisos = true;
        selector.Aplicar();   // deja EdificioAlto apagado y EdificioPisos encendido

        EditorUtility.SetDirty(edificio);
        EditorUtility.SetDirty(selector);

        Scene escena = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        bool guardada = EditorSceneManager.SaveScene(escena);
        Selection.activeGameObject = objSelector;

        EditorUtility.DisplayDialog("Edificio pisos",
            "Listo: modo de 6 pisos montado en la escena Universidad.\n\n" +
            "• 'EdificioAlto' (el original) sigue ahí, APAGADO.\n" +
            "• 'EdificioPisos' (15 objetos + 15 repisas + 6 pisos) está ENCENDIDO.\n" +
            "• 'SelectorModo' tiene la casilla 'Modo Pisos': marcada = 6 pisos, desmarcada = edificio original.\n" +
            (guardada ? "• La escena se guardó.\n" : "• Guarda la escena (Ctrl+S).\n") +
            "\nDale Play desde MenuPrincipal → JUGAR.", "OK");
    }

    private static List<DefinicionPiso> CargarPisos()
    {
        List<DefinicionPiso> lista = new List<DefinicionPiso>();
        if (!AssetDatabase.IsValidFolder(CarpetaPisos)) return lista;

        foreach (string guid in AssetDatabase.FindAssets("t:DefinicionPiso", new[] { CarpetaPisos }))
        {
            DefinicionPiso d = AssetDatabase.LoadAssetAtPath<DefinicionPiso>(AssetDatabase.GUIDToAssetPath(guid));
            if (d != null) lista.Add(d);
        }
        lista.Sort((a, b) => a.numero.CompareTo(b.numero));
        return lista;
    }

    private static bool AbrirUniversidad()
    {
        Scene activa = SceneManager.GetActiveScene();
        if (activa.path == RutaEscena) return true;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        return SceneManager.GetActiveScene().path == RutaEscena;
    }

    private static Transform CrearHijo(Transform padre, string nombre)
    {
        GameObject obj = new GameObject(nombre);
        obj.transform.SetParent(padre, false);
        return obj.transform;
    }

    /// <summary>Objeto raíz de la escena por su nombre, aunque esté apagado.</summary>
    private static GameObject RaizPorNombre(string nombre)
    {
        foreach (GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (obj != null && obj.name == nombre) return obj;
        }
        return null;
    }

    private static void BorrarRaiz(string nombre)
    {
        GameObject obj = RaizPorNombre(nombre);
        if (obj != null) Undo.DestroyObjectImmediate(obj);
    }
}
