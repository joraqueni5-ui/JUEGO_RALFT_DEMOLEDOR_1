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
        int creados, existentes, migrados;
        Procesar(out creados, out existentes, out migrados);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefinicionPiso>(Carpeta + "/" + Pisos[0].archivo + ".asset");

        EditorUtility.DisplayDialog("Definiciones de piso",
            "Listo.\n\n• Creados: " + creados + "\n• Migrados (datos cenitales añadidos): " + migrados +
            "\n• Ya completos (sin tocar): " + existentes +
            "\n\nEstán en " + Carpeta + ".\nNo se modificó ninguna escena.", "OK");
    }

    /// <summary>Crea o completa las 6 definiciones sin mostrar diálogos (la usa el constructor del piso cenital).</summary>
    public static void Procesar(out int creados, out int existentes, out int migrados)
    {
        AsegurarCarpeta();

        creados = 0; existentes = 0; migrados = 0;
        for (int i = 0; i < Pisos.Length; i++)
        {
            Datos d = Pisos[i];
            string ruta = Carpeta + "/" + d.archivo + ".asset";
            DefinicionPiso previo = AssetDatabase.LoadAssetAtPath<DefinicionPiso>(ruta);
            if (previo != null)
            {
                // Asset de antes del modo cenital: solo se le añaden los datos nuevos que faltan.
                if (previo.objetosCenital == null || previo.objetosCenital.Length == 0)
                {
                    CompletarCenital(previo, i);
                    EditorUtility.SetDirty(previo);
                    migrados++;
                }
                else existentes++;
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

            CompletarCenital(piso, i);
            AssetDatabase.CreateAsset(piso, ruta);
            creados++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // Objetos del piso 1: nombre, laboratorio (0 = Gastronomía, 1 = Medicina) y tamaño en baldosas.
    private static readonly (string nombre, int lab, int ancho, int alto)[] ObjetosPiso1 =
    {
        ("Horno", 0, 2, 2), ("Cocina a gas", 0, 2, 2), ("Mesón de acero", 0, 4, 2),
        ("Refrigerador", 0, 2, 2), ("Lavaplatos", 0, 2, 1), ("Estante de vajilla", 0, 3, 1),
        ("Camilla 1", 1, 1, 2), ("Camilla 2", 1, 1, 2), ("Maniquí clínico", 1, 1, 1),
        ("Mesa de microscopios", 1, 3, 2), ("Vitrina", 1, 2, 1), ("Lavamanos con monitor", 1, 2, 1),
    };

    /// <summary>Rellena los datos del modo cenital: laboratorios, 12 objetos y ataques por defecto.</summary>
    private static void CompletarCenital(DefinicionPiso piso, int indice)
    {
        piso.tamanoMapa = new Vector2Int(22, 12);

        if (indice == 0)
        {
            piso.nombre = "Gastronomía y Medicina";
            piso.carrera = "Gastronomía y Medicina";
            piso.laboratorios = new[]
            {
                new LaboratorioPiso { nombre = "Lab. de Gastronomía", color = new Color(1.00f, 0.62f, 0.25f) },
                new LaboratorioPiso { nombre = "Lab. de Medicina",    color = new Color(0.30f, 0.80f, 0.85f) },
            };
            piso.objetosCenital = new ObjetoCenital[ObjetosPiso1.Length];
            for (int o = 0; o < ObjetosPiso1.Length; o++)
            {
                var d = ObjetosPiso1[o];
                piso.objetosCenital[o] = new ObjetoCenital
                {
                    nombre = d.nombre, laboratorio = d.lab,
                    tamanoBaldosas = new Vector2Int(d.ancho, d.alto), sprites = new Sprite[3],
                };
            }
        }
        else
        {
            // Pisos por definir: mismo molde (2 laboratorios x 6 objetos) con nombres genéricos.
            piso.laboratorios = new[]
            {
                new LaboratorioPiso { nombre = "Laboratorio A", color = new Color(0.95f, 0.70f, 0.35f) },
                new LaboratorioPiso { nombre = "Laboratorio B", color = new Color(0.40f, 0.75f, 0.90f) },
            };
            piso.objetosCenital = new ObjetoCenital[12];
            for (int o = 0; o < 12; o++)
                piso.objetosCenital[o] = new ObjetoCenital
                {
                    nombre = "Objeto " + (o + 1), laboratorio = o < 6 ? 0 : 1,
                    tamanoBaldosas = new Vector2Int(2, 2), sprites = new Sprite[3],
                };
        }

        // Más difícil en pisos altos: lanza más seguido y más a menudo.
        piso.intervaloLanzamiento = Mathf.Max(4f, 8f - indice * 0.6f);
        piso.proporcionLanzamiento = Mathf.Min(0.6f, 0.3f + indice * 0.05f);
    }

    private static void AsegurarCarpeta()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets/Data", "Pisos");
    }
}
