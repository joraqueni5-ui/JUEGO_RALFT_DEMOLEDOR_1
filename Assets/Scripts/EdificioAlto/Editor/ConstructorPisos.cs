using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Crea los 6 assets DefinicionPiso en Assets/Data/Pisos (menú Tools → Demoledor Unifranz).
/// Trae valores provisionales de nombre, tinte y dificultad. Si un asset ya existe NO se
/// toca, así que se puede ejecutar varias veces sin perder lo que ya se editó.
/// No modifica ninguna escena.
/// </summary>
public static class ConstructorPisos
{
    private const string Carpeta = "Assets/Data/Pisos";

    private struct Datos
    {
        public string archivo, nombre, carrera;
        public string[] objetos;
        public Color tinte;
        public float ritmo;
        public int golpes;
        public float limite;
        public int escombros;
    }

    private static readonly Datos[] Pisos =
    {
        new Datos { archivo = "Piso1_Gastronomia", nombre = "Gastronomía", carrera = "Gastronomía",
            objetos = new[] { "Horno", "Refrigerador", "Estufa", "Mesa de trabajo" },
            tinte = new Color(1.00f, 0.82f, 0.62f), ritmo = 1.0f, golpes = 8,  limite = 0.70f, escombros = 1 },
        new Datos { archivo = "Piso2_Informatica", nombre = "Informática", carrera = "Informática",
            objetos = new[] { "Monitor", "Teclado", "Servidor", "Impresora" },
            tinte = new Color(0.66f, 0.86f, 1.00f), ritmo = 1.1f, golpes = 10, limite = 0.70f, escombros = 1 },
        new Datos { archivo = "Piso3_PorDefinir", nombre = "Por definir", carrera = "",
            objetos = new[] { "Objeto 1", "Objeto 2", "Objeto 3", "Objeto 4" },
            tinte = new Color(0.74f, 1.00f, 0.72f), ritmo = 1.2f, golpes = 12, limite = 0.68f, escombros = 1 },
        new Datos { archivo = "Piso4_PorDefinir", nombre = "Por definir", carrera = "",
            objetos = new[] { "Objeto 1", "Objeto 2", "Objeto 3", "Objeto 4" },
            tinte = new Color(1.00f, 0.74f, 0.86f), ritmo = 1.3f, golpes = 14, limite = 0.66f, escombros = 2 },
        new Datos { archivo = "Piso5_PorDefinir", nombre = "Por definir", carrera = "",
            objetos = new[] { "Objeto 1", "Objeto 2", "Objeto 3", "Objeto 4" },
            tinte = new Color(0.86f, 0.76f, 1.00f), ritmo = 1.4f, golpes = 16, limite = 0.64f, escombros = 2 },
        new Datos { archivo = "Piso6_PorDefinir", nombre = "Por definir", carrera = "",
            objetos = new[] { "Objeto 1", "Objeto 2", "Objeto 3", "Objeto 4" },
            tinte = new Color(1.00f, 0.94f, 0.62f), ritmo = 1.5f, golpes = 18, limite = 0.62f, escombros = 2 },
    };

    [MenuItem("Tools/Demoledor Unifranz/Crear definiciones de piso", false, 21)]
    public static void Crear()
    {
        AsegurarCarpeta();

        int creados = 0, existentes = 0;
        for (int i = 0; i < Pisos.Length; i++)
        {
            Datos d = Pisos[i];
            string ruta = Carpeta + "/" + d.archivo + ".asset";
            if (AssetDatabase.LoadAssetAtPath<DefinicionPiso>(ruta) != null)
            {
                existentes++;
                continue;
            }

            DefinicionPiso piso = ScriptableObject.CreateInstance<DefinicionPiso>();
            piso.numero = i + 1;
            piso.nombre = d.nombre;
            piso.carrera = d.carrera;
            piso.tinte = d.tinte;
            piso.ritmoDemoledor = d.ritmo;
            piso.golpesMinimos = d.golpes;
            piso.limiteDerrumbe = d.limite;
            piso.escombrosPorGolpe = d.escombros;

            piso.objetos = new ObjetoPiso[4];
            for (int o = 0; o < 4; o++)
                piso.objetos[o] = new ObjetoPiso { nombre = d.objetos[o], sprites = new Sprite[3] };

            piso.fichaTitulo = "Piso " + (i + 1) + " - " + d.nombre;
            piso.fichaDescripcion = "Ficha por completar.";
            piso.fichaDatoUtil = "Dato por completar.";

            AssetDatabase.CreateAsset(piso, ruta);
            creados++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefinicionPiso>(Carpeta + "/" + Pisos[0].archivo + ".asset");

        EditorUtility.DisplayDialog("Definiciones de piso",
            "Listo.\n\n• Creados: " + creados + "\n• Ya existían (sin tocar): " + existentes +
            "\n\nEstán en " + Carpeta + ".\nNo se modificó ninguna escena.", "OK");
    }

    private static void AsegurarCarpeta()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets/Data", "Pisos");
    }
}
