using UnityEngine;

/// <summary>
/// Personajes del juego: estudiantes de distintas carreras (los que se pueden elegir) y personal de la universidad
/// (docentes, decana, seguridad, mantenimiento) que solo aparece en los escenarios, entre la gente del piso. Todos usan los mismos movimientos
/// del muñeco original (los genera Tools/generar_personajes.py en Resources/Extras/Personajes) y cada uno tiene una
/// pequeña ventaja propia. La elección se guarda en el dispositivo y la aplica JugadorCenital al empezar cada piso.
/// </summary>
public static class Personajes
{
    public class Ficha
    {
        public string id, nombre, carrera, ventaja;
        /// <summary>Multiplica la altura de dibujo (el gorro de chef hace más alto el sprite).</summary>
        public float escalaAltura = 1f;
        /// <summary>Timbre del balbuceo de su voz en los globos.</summary>
        public Burbuja.Voz Voz = Burbuja.Voz.Hombre;
        /// <summary>Estudiante o personal de la universidad (plantel).</summary>
        public bool plantel;
    }

    public static readonly Ficha[] Todos =
    {
        new Ficha { id = "sistemas", nombre = "DIEGO", carrera = "ING. DE SISTEMAS", ventaja = "MARTILLA 15% MÁS RÁPIDO" },
        new Ficha { id = "medicina", nombre = "VALERIA", carrera = "MEDICINA", ventaja = "EMPIEZA CON CASCO", Voz = Burbuja.Voz.Mujer },
        new Ficha { id = "administracion", nombre = "CAMILA", carrera = "ADMINISTRACIÓN", ventaja = "PODERES DURAN 30% MÁS", Voz = Burbuja.Voz.Mujer },
        new Ficha { id = "gastronomia", nombre = "LUCÍA", carrera = "GASTRONOMÍA", ventaja = "MÁS POTENCIADORES", escalaAltura = 1.12f, Voz = Burbuja.Voz.Mujer },
        new Ficha { id = "derecho", nombre = "MATEO", carrera = "DERECHO", ventaja = "SE RECUPERA RÁPIDO DEL ATURDIMIENTO" },
        new Ficha { id = "arquitectura", nombre = "SOFÍA", carrera = "ARQUITECTURA", ventaja = "REPARA DESDE MÁS LEJOS", Voz = Burbuja.Voz.Mujer },
        new Ficha { id = "odontologia", nombre = "ANA", carrera = "ODONTOLOGÍA", ventaja = "CORRE 12% MÁS RÁPIDO", Voz = Burbuja.Voz.Mujer },
        new Ficha { id = "comunicacion", nombre = "BRUNO", carrera = "COMUNICACIÓN", ventaja = "LOS PODERES APARECEN MÁS SEGUIDO" },
        // Plantel de la universidad: no se elige, aparece caminando por los pisos y escapa cuando llega el demoledor.
        new Ficha { id = "docente", nombre = "PROF. RAMIRO", carrera = "DOCENTE DE FÍSICA", Voz = Burbuja.Voz.Mayor, plantel = true },
        new Ficha { id = "decana", nombre = "DRA. ELENA", carrera = "DECANA", Voz = Burbuja.Voz.Mujer, plantel = true },
        new Ficha { id = "seguridad", nombre = "DON CARLOS", carrera = "SEGURIDAD", Voz = Burbuja.Voz.Mayor, plantel = true },
        new Ficha { id = "mantenimiento", nombre = "DOÑA ROSA", carrera = "MANTENIMIENTO", Voz = Burbuja.Voz.Mujer, plantel = true },
    };

    private const string Clave = "personaje_elegido";

    private static Ficha[] elegibles;

    /// <summary>Los que se pueden elegir para jugar: solo estudiantes (el plantel aparece en los escenarios).</summary>
    public static Ficha[] Elegibles
    {
        get
        {
            if (elegibles == null) elegibles = System.Array.FindAll(Todos, f => !f.plantel);
            return elegibles;
        }
    }

    public static int Indice
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt(Clave, 0), 0, Elegibles.Length - 1); }
        set { PlayerPrefs.SetInt(Clave, Mathf.Clamp(value, 0, Elegibles.Length - 1)); PlayerPrefs.Save(); }
    }

    public static Ficha Actual { get { return Elegibles[Indice]; } }

    /// <summary>Cuadros de una animación ("abajo", "arriba", "derecha", "martillo") o null si faltan.</summary>
    public static Sprite[] Serie(Ficha f, string animacion)
    {
        return f == null ? null : ArteExtra.Serie("Personajes/" + f.id + "/" + animacion + "_", 4);
    }

    /// <summary>Animación fluida de 8 cuadros (los 4 originales y uno intermedio entre cada par), o null si no está.</summary>
    public static Sprite[] Serie8(Ficha f, string animacion)
    {
        return f == null ? null : ArteExtra.Serie("Personajes/" + f.id + "/" + animacion + "8_", 8);
    }

    /// <summary>La de 8 cuadros si existe; si no, la de 4.</summary>
    public static Sprite[] SerieFluida(Ficha f, string animacion)
    {
        Sprite[] s = Serie8(f, animacion);
        return s ?? Serie(f, animacion);
    }

    /// <summary>Cuántas veces más cuadros que la animación original (4): para mantener la misma velocidad.</summary>
    public static float FactorCuadros(Sprite[] serie)
    {
        return serie != null && serie.Length >= 8 ? serie.Length / 4f : 1f;
    }

    /// <summary>Animaciones completas del personaje (8 cuadros si están), o null si falta algún cuadro.</summary>
    public static AnimacionesCenital Animaciones(Ficha f, Sprite[] especial)
    {
        Sprite[] ab = SerieFluida(f, "abajo"), ar = SerieFluida(f, "arriba"), de = SerieFluida(f, "derecha"), ma = SerieFluida(f, "martillo");
        if (ab == null || ar == null || de == null || ma == null) return null;
        return new AnimacionesCenital
        {
            caminarAbajo = ab, caminarArriba = ar, caminarDerecha = de,
            golpeAbajo = ma, golpeArriba = ma, golpeDerecha = ma,
            especial = especial
        };
    }

    /// <summary>Multiplica el intervalo de los potenciadores extra (Lucía, de Gastronomía, consigue más).</summary>
    public static float FactorIntervaloPotenciadores
    {
        get { return Actual.id == "gastronomia" ? 0.7f : (Actual.id == "comunicacion" ? 0.8f : 1f); }
    }

    /// <summary>Alguien al azar para la gente del piso: estudiantes y plantel (un poco más de estudiantes).</summary>
    public static Ficha AlAzar()
    {
        Ficha f = Todos[Random.Range(0, Todos.Length)];
        if (f.plantel && Random.value < 0.35f) f = Todos[Random.Range(0, Todos.Length)];
        return f;
    }

    /// <summary>Un estudiante al azar (para el que repara en el menú).</summary>
    public static Ficha EstudianteAlAzar() { return Elegibles[Random.Range(0, Elegibles.Length)]; }
}
