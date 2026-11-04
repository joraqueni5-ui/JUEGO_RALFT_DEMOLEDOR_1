using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Tabla de los 5 mejores puntajes con iniciales de 3 letras, estilo arcade.
    /// Se guarda en el dispositivo con PlayerPrefs (no necesita internet).
    /// </summary>
    public static class TablaRecords
    {
        public const int Maximo = 5;
        private const string Clave = "demoledor.records";
        private const string ClaveAntigua = "demoledor.mejorPuntaje";

        [Serializable]
        public class Entrada
        {
            public string iniciales;
            public int puntos;
        }

        [Serializable]
        private class Lista
        {
            public List<Entrada> entradas = new List<Entrada>();
        }

        /// <summary>Los récords ordenados de mayor a menor (como máximo 5).</summary>
        public static List<Entrada> Cargar()
        {
            Lista lista = null;
            string json = PlayerPrefs.GetString(Clave, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { lista = JsonUtility.FromJson<Lista>(json); }
                catch (Exception) { lista = null; }
            }
            if (lista == null) lista = new Lista();

            // Migra el "mejor puntaje" de la versión anterior de los menús.
            int antiguo = PlayerPrefs.GetInt(ClaveAntigua, 0);
            if (antiguo > 0 && lista.entradas.Count == 0)
            {
                lista.entradas.Add(new Entrada { iniciales = "???", puntos = antiguo });
                Guardar(lista);
            }

            lista.entradas.Sort((a, b) => b.puntos.CompareTo(a.puntos));
            if (lista.entradas.Count > Maximo) lista.entradas.RemoveRange(Maximo, lista.entradas.Count - Maximo);
            return lista.entradas;
        }

        /// <summary>El mejor puntaje guardado (0 si no hay).</summary>
        public static int Mejor
        {
            get
            {
                List<Entrada> e = Cargar();
                return e.Count > 0 ? e[0].puntos : 0;
            }
        }

        /// <summary>True si el puntaje entra en la tabla.</summary>
        public static bool Califica(int puntos)
        {
            if (puntos <= 0) return false;
            List<Entrada> e = Cargar();
            return e.Count < Maximo || puntos > e[e.Count - 1].puntos;
        }

        /// <summary>Agrega el puntaje y devuelve su posición (0 = primero), o -1 si no entra.</summary>
        public static int Agregar(string iniciales, int puntos)
        {
            if (!Califica(puntos)) return -1;
            List<Entrada> e = Cargar();
            Entrada nueva = new Entrada { iniciales = Limpiar(iniciales), puntos = puntos };
            e.Add(nueva);
            e.Sort((a, b) => b.puntos.CompareTo(a.puntos));
            if (e.Count > Maximo) e.RemoveRange(Maximo, e.Count - Maximo);
            Guardar(new Lista { entradas = e });
            return e.IndexOf(nueva);
        }

        private static string Limpiar(string iniciales)
        {
            string s = string.IsNullOrEmpty(iniciales) ? "???" : iniciales.ToUpperInvariant();
            if (s.Length > 3) s = s.Substring(0, 3);
            return s.PadRight(3, '-');
        }

        private static void Guardar(Lista lista)
        {
            PlayerPrefs.SetString(Clave, JsonUtility.ToJson(lista));
            PlayerPrefs.Save();
        }
    }
}
