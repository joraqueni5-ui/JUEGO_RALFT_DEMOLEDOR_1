using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Arma el escenario del modo SUPERVIVENCIA: "EL GRAN HALL", un mapa nuevo con piezas de varios pisos (auditorio y cafetería
/// de la planta baja, computadoras de Informática, cocina de Gastronomía, biblioteca de la terraza). Crea la definición en
/// Assets/Resources/Supervivencia y su prefab de mapa con ConstructorPisoDesdeMapa. Se arma solo al abrir Unity (si falta)
/// y antes de construir el PC y el APK; también desde el menú REPARALOFRANZ.
/// </summary>
[InitializeOnLoad]
public static class ConstructorSupervivencia
{
    private const string Carpeta = "Assets/Resources/Supervivencia";
    private const string RutaDef = Carpeta + "/DefinicionSupervivencia.asset";
    private const int Numero = 9;

    // Leyenda: lo de la planta baja tal cual, más piezas de otros pisos con otra letra.
    //   v suelo de Informática · L computadoras · Z iMacs · I impresora · c silla
    //   g suelo de Gastronomía · 2 estación de cocina · 3 horno · 4 refrigerador · 5 lavaplatos · q banquito
    //   b suelo de la biblioteca · 6 estante de libros · 7 mesa de grupo · 9 recepción de libros · u pupitre
    public const string Mapa =
        "################################\n" +
        "#H;y;y;y;GEE#vLLLvZZZv#b999bb6b#\n" +
        "#;;y;y;y;;EE#vvvvvvvvv#bbbbbb6b#\n" +
        "#;;;;;;;;;;;#vLLLvLLLv#b77bb77b#\n" +
        "#;y;y;y;y;K;#vvvvvvvvv#bubbbbub#\n" +
        "#;;;;;;;;;;;#vvvIvvvvv#bbbbbbbb#\n" +
        "#####//#########//########//####\n" +
        "V::k:::::::::::T:::::::::::k::pA\n" +
        "V::::::::::::::::J:::::::::::::A\n" +
        "#p::::k::::::::::::::::::k:::::#\n" +
        "####//######//##############//##\n" +
        "#CC...FF.p#3ggggggg4g#XX,YY,,,,#\n" +
        "#.s.MM.s..#3ggggggg4g#,,,,,,,,,#\n" +
        "#.........#gg22gg22gg#,e,e,e,,,#\n" +
        "#.s.MM.s..#gg22gg22gg#,,,RRR,,,#\n" +
        "#P........#gg55gggggq#,,N,,,,Q,#\n" +
        "################################";

    private static readonly string[,] Prestados =
    {
        // piso, letra original, letra nueva
        { "Piso2_Informatica", ".", "v" }, { "Piso2_Informatica", "L", "L" }, { "Piso2_Informatica", "Z", "Z" },
        { "Piso2_Informatica", "I", "I" }, { "Piso2_Informatica", "c", "c" },
        { "Piso1_Gastronomia", ".", "g" }, { "Piso1_Gastronomia", "S", "2" }, { "Piso1_Gastronomia", "H", "3" },
        { "Piso1_Gastronomia", "R", "4" }, { "Piso1_Gastronomia", "L", "5" }, { "Piso1_Gastronomia", "q", "q" },
        { "Piso6_PorDefinir", ".", "b" }, { "Piso6_PorDefinir", "E", "6" }, { "Piso6_PorDefinir", "M", "7" },
        { "Piso6_PorDefinir", "R", "9" }, { "Piso6_PorDefinir", "u", "u" },
    };

    static ConstructorSupervivencia()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Existe()) { try { Construir(); } catch (Exception e) { Debug.LogWarning("[Supervivencia] " + e.Message); } }
        };
    }

    public static bool Existe()
    {
        DefinicionPiso d = AssetDatabase.LoadAssetAtPath<DefinicionPiso>(RutaDef);
        return d != null && d.prefabMapa != null && d.mapa == Mapa;
    }

    /// <summary>Lo llama ConstructorBuilds antes de construir: arma el escenario si falta.</summary>
    public static void Asegurar(System.Text.StringBuilder log)
    {
        if (Existe()) return;
        bool ok = false;
        try { ok = Construir(); } catch (Exception e) { log.AppendLine("Supervivencia: " + e.Message); }
        log.AppendLine(ok ? "Supervivencia: escenario armado" : "Supervivencia: NO se pudo armar el escenario");
    }

    [MenuItem("REPARALOFRANZ/Construir escenario de Supervivencia")]
    public static bool Construir()
    {
        DefinicionPiso baseDef = Cargar("Piso0_PlantaBaja");
        if (baseDef == null) { Debug.LogError("[Supervivencia] Falta Assets/Data/Pisos/Piso0_PlantaBaja.asset"); return false; }

        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets/Resources", "Supervivencia");

        DefinicionPiso def = AssetDatabase.LoadAssetAtPath<DefinicionPiso>(RutaDef);
        bool nueva = def == null;
        if (nueva) def = ScriptableObject.CreateInstance<DefinicionPiso>();
        GameObject prefabPrevio = def.prefabMapa;
        EditorUtility.CopySerialized(baseDef, def);
        def.name = "DefinicionSupervivencia";

        def.numero = Numero;
        def.nombre = "El Gran Hall";
        def.nombreArea = "SUPERVIVENCIA: EL GRAN HALL";
        def.carrera = "";
        def.nombreEscena = "";
        def.escenaSiguiente = "";
        def.mapa = Mapa;
        def.tamanoMapa = new Vector2Int(32, 17);
        def.puntosInfo = new PuntoInfoPiso[0];
        def.datosReserva = new PuntoInfoPiso[0];
        def.pasosTutorial = new PasoTutorial[0];
        def.prefabMapa = prefabPrevio != null && AssetDatabase.GetAssetPath(prefabPrevio).Contains("Mapa_Piso" + Numero) ? prefabPrevio : null;
        def.energiaInicial = 100f;
        def.fichaTitulo = "SUPERVIVENCIA";
        def.fichaDescripcion = "Aguanta oleada tras oleada de villanos.";

        // Leyenda: la de la planta baja más las piezas prestadas.
        List<EntradaLeyenda> leyenda = new List<EntradaLeyenda>();
        foreach (EntradaLeyenda e in baseDef.leyenda) if (e != null) leyenda.Add(Clonar(e, e.caracter));
        for (int i = 0; i < Prestados.GetLength(0); i++)
        {
            DefinicionPiso origen = Cargar(Prestados[i, 0]);
            if (origen == null) { Debug.LogWarning("[Supervivencia] Falta " + Prestados[i, 0]); continue; }
            EntradaLeyenda e = origen.LeyendaDe(Prestados[i, 1][0]);
            if (e == null) { Debug.LogWarning("[Supervivencia] " + Prestados[i, 0] + " no tiene la letra '" + Prestados[i, 1] + "'"); continue; }
            leyenda.RemoveAll(x => x.Letra == Prestados[i, 2][0]);
            leyenda.Add(Clonar(e, Prestados[i, 2]));
        }
        def.leyenda = leyenda.ToArray();

        if (nueva) AssetDatabase.CreateAsset(def, RutaDef);
        EditorUtility.SetDirty(def);
        AssetDatabase.SaveAssets();

        bool ok = ConstructorPisoDesdeMapa.Construir(def);
        EditorUtility.SetDirty(def);
        AssetDatabase.SaveAssets();
        Debug.Log(ok ? "[Supervivencia] Escenario EL GRAN HALL armado." : "[Supervivencia] No se pudo armar el escenario (revisa los errores de [Mapa]).");
        return ok;
    }

    private static DefinicionPiso Cargar(string nombre)
    {
        return AssetDatabase.LoadAssetAtPath<DefinicionPiso>("Assets/Data/Pisos/" + nombre + ".asset");
    }

    private static EntradaLeyenda Clonar(EntradaLeyenda o, string letra)
    {
        EntradaLeyenda c = new EntradaLeyenda();
        foreach (FieldInfo f in typeof(EntradaLeyenda).GetFields(BindingFlags.Public | BindingFlags.Instance)) f.SetValue(c, f.GetValue(o));
        c.caracter = letra;
        return c;
    }
}
