using System;
using System.Collections;
using UnityEngine;

namespace RalftDemoledor.Game
{
    /// <summary>
    /// Prueba automatica del ejecutable. Cuando la aplicacion se inicia con
    /// el argumento -autoprueba recorre el menu, el nivel, el menu de pausa
    /// y la pantalla final escribiendo el resultado en el registro del
    /// jugador, lo que permite comprobar la navegacion sin intervencion
    /// manual:
    /// DemoledorUnifranz.exe -autoprueba
    /// </summary>
    public static class Autoprueba
    {
        private static bool? activa;
        private static int visitasDelNivel;

        /// <summary>Indica si la aplicacion se inicio con el argumento -autoprueba.</summary>
        public static bool Activa
        {
            get
            {
                if (!activa.HasValue)
                {
                    activa = false;
                    string[] argumentos = Environment.GetCommandLineArgs();

                    for (int i = 0; i < argumentos.Length; i++)
                    {
                        if (argumentos[i] == "-autoprueba")
                        {
                            activa = true;
                        }
                    }
                }

                return activa.Value;
            }
        }

        /// <summary>Cuenta cuantas veces se ha cargado el nivel en la prueba.</summary>
        public static int SiguienteVisita()
        {
            visitasDelNivel++;
            return visitasDelNivel;
        }

        /// <summary>Ejecuta una accion despues de unos segundos si la prueba esta activa.</summary>
        public static void Programar(MonoBehaviour anfitrion, float segundos, Action accion)
        {
            if (!Activa || anfitrion == null)
            {
                return;
            }

            anfitrion.StartCoroutine(Ejecutar(segundos, accion));
        }

        private static IEnumerator Ejecutar(float segundos, Action accion)
        {
            yield return new WaitForSeconds(segundos);
            accion();
        }
    }
}
