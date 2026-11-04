using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DemoledorUnifranz.Game;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Pantalla FinJuego. Lee los datos reales que GestorJuego guarda en GameSession y muestra:
    /// - El final: ¡VICTORIA! (edificio salvado), GAME OVER por derrumbe o por quedarse sin vidas.
    /// - Puntos, reparaciones y destrozo (solo en el derrumbe), contando como en el arcade.
    /// - Una medalla (bronce, plata u oro) según los puntos y una frase divertida.
    /// - La tabla de los 5 mejores récords; si el puntaje entra, pide 3 iniciales.
    /// VOLVER A JUGAR carga la misma escena que JUGAR del menú principal.
    /// </summary>
    public class FinJuegoArcade : MonoBehaviour
    {
        private const string ClaveIniciales = "demoledor.ultimasIniciales";
        private const string Letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        [Header("Medallas (puntos mínimos)")]
        public int puntosBronce = 100;
        public int puntosPlata = 300;
        public int puntosOro = 600;
        public Sprite medallaBronce;
        public Sprite medallaPlata;
        public Sprite medallaOro;

        [Header("Textos")]
        public Text textoTitulo;
        public Text textoSubtitulo;
        public Image imagenMedalla;
        public Text textoMedalla;
        public Text textoFrase;
        public Text valorPuntos;
        public Text valorReparaciones;
        public Text valorDestrozo;
        public GameObject filaDestrozo;

        [Header("Récords")]
        public GameObject tablaRecords;
        public Text[] posiciones;
        public Text[] iniciales;
        public Text[] puntajes;
        public GameObject marcaNuevo;

        [Header("Iniciales")]
        public GameObject panelIniciales;
        public Text[] letras;
        public Button[] botonesArriba;
        public Button[] botonesAbajo;
        public Button botonListo;

        [Header("Botones")]
        public Button botonVolverAJugar;
        public Button botonMenuPrincipal;

        private int puntos;
        private readonly int[] indices = new int[3];
        private int letraActual;
        private bool pidiendoIniciales;
        private int posicionNueva = -1;

        private void Start()
        {
            Time.timeScale = 1f;
            puntos = GameSession.Puntaje;

            MostrarResultado();
            ConectarIniciales();

            // No se muestra un botón que no pueda funcionar.
            if (botonMenuPrincipal != null && !NavegadorEscenas.Disponible(Escenas.MenuPrincipal))
            {
                Debug.LogWarning("[Menus] MenuPrincipal no está en Build Settings: se oculta el botón MENÚ PRINCIPAL.");
                botonMenuPrincipal.gameObject.SetActive(false);
            }

            if (TablaRecords.Califica(puntos)) PedirIniciales();
            else
            {
                MostrarTabla();
                Seleccionar(botonVolverAJugar);
            }
        }

        private void Update()
        {
            if (!pidiendoIniciales) return;
            Keyboard teclado = Keyboard.current;
            if (teclado == null) return;

            // Escribir las iniciales directamente con el teclado.
            for (int i = 0; i < Letras.Length; i++)
            {
                if (!teclado[(Key)((int)Key.A + i)].wasPressedThisFrame) continue;
                indices[letraActual] = i;
                letraActual = Mathf.Min(2, letraActual + 1);
                RefrescarLetras();
                ServiciosMenus.Instancia.Tic();
                return;
            }
            if (teclado.backspaceKey.wasPressedThisFrame)
            {
                letraActual = Mathf.Max(0, letraActual - 1);
                RefrescarLetras();
            }
        }

        // ------------------------------------------------------------ resultado
        private void MostrarResultado()
        {
            bool victoria = GameSession.Victoria;
            bool derrumbe = GameSession.HayDatosDerrumbe;

            if (textoTitulo != null)
            {
                textoTitulo.text = victoria ? "¡VICTORIA!" : "GAME OVER";
                textoTitulo.color = victoria ? PaletaArcade.Amarillo : PaletaArcade.Rojo;
            }
            if (textoSubtitulo != null)
            {
                textoSubtitulo.text = victoria ? "¡EDIFICIO SALVADO!"
                    : derrumbe ? "¡EL EDIFICIO SE CAYÓ!" : "¡TE QUEDASTE SIN VIDAS!";
            }

            if (filaDestrozo != null) filaDestrozo.SetActive(derrumbe);

            // Medalla y frase.
            Sprite medalla = null;
            string nombre;
            string[] frases;
            if (puntos >= puntosOro) { medalla = medallaOro; nombre = "¡MEDALLA DE ORO!"; frases = FrasesOro; }
            else if (puntos >= puntosPlata) { medalla = medallaPlata; nombre = "¡MEDALLA DE PLATA!"; frases = FrasesPlata; }
            else if (puntos >= puntosBronce) { medalla = medallaBronce; nombre = "¡MEDALLA DE BRONCE!"; frases = FrasesBronce; }
            else { nombre = "¡SIGUE INTENTANDO!"; frases = FrasesSinMedalla; }
            if (victoria) frases = FrasesVictoria;

            if (imagenMedalla != null)
            {
                imagenMedalla.gameObject.SetActive(medalla != null);
                imagenMedalla.sprite = medalla;
            }
            if (textoMedalla != null) textoMedalla.text = nombre;
            if (textoFrase != null) textoFrase.text = "\"" + frases[Random.Range(0, frases.Length)] + "\"";

            StartCoroutine(Contar(valorPuntos, puntos, ""));
            StartCoroutine(Contar(valorReparaciones, GameSession.Reparaciones, ""));
            if (derrumbe) StartCoroutine(Contar(valorDestrozo, Mathf.RoundToInt(GameSession.PorcentajeDestruido * 100f), "%"));
        }

        private static readonly string[] FrasesVictoria =
        {
            "¡SALVASTE LA UNIVERSIDAD!",
            "EL DEMOLEDOR SE QUEDÓ SIN TRABAJO",
        };
        private static readonly string[] FrasesOro =
        {
            "EL INGENIERO ESTARÍA ORGULLOSO DE TI",
            "¡EL MEJOR REPARADOR DE UNIFRANZ!",
        };
        private static readonly string[] FrasesPlata =
        {
            "¡CASI LO SALVAS TODO!",
            "EL DEMOLEDOR YA TE TIENE RESPETO",
        };
        private static readonly string[] FrasesBronce =
        {
            "NADA MAL, PERO EL DEMOLEDOR SIGUE FUERTE",
            "¡A PRACTICAR CON EL MARTILLO!",
        };
        private static readonly string[] FrasesSinMedalla =
        {
            "EL DEMOLEDOR GANÓ ESTA VEZ...",
            "¡AGARRA EL MARTILLO Y VUELVE!",
        };

        /// <summary>Cuenta de 0 al valor final, como en las máquinas arcade.</summary>
        private IEnumerator Contar(Text texto, int valor, string sufijo)
        {
            if (texto == null) yield break;
            const float Duracion = 0.9f;
            float t = 0f;
            texto.text = "0" + sufijo;
            yield return new WaitForSecondsRealtime(0.35f);
            while (t < Duracion)
            {
                t += Time.unscaledDeltaTime;
                float p = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / Duracion), 3f);
                int actual = Mathf.RoundToInt(valor * p);
                texto.text = actual + sufijo;
                if (ServiciosMenus.Existe && actual != valor) ServiciosMenus.Instancia.Tic();
                yield return null;
            }
            texto.text = valor + sufijo;
        }

        // ------------------------------------------------------------ iniciales
        private void ConectarIniciales()
        {
            for (int i = 0; i < 3; i++)
            {
                int indice = i;
                if (botonesArriba != null && i < botonesArriba.Length && botonesArriba[i] != null)
                    botonesArriba[i].onClick.AddListener(() => CambiarLetra(indice, -1));
                if (botonesAbajo != null && i < botonesAbajo.Length && botonesAbajo[i] != null)
                    botonesAbajo[i].onClick.AddListener(() => CambiarLetra(indice, +1));
            }
            if (botonListo != null) botonListo.onClick.AddListener(GuardarIniciales);
        }

        private void PedirIniciales()
        {
            pidiendoIniciales = true;
            string ultimas = PlayerPrefs.GetString(ClaveIniciales, "AAA");
            for (int i = 0; i < 3; i++)
            {
                int idx = i < ultimas.Length ? Letras.IndexOf(ultimas[i]) : 0;
                indices[i] = idx < 0 ? 0 : idx;
            }
            letraActual = 0;
            RefrescarLetras();
            if (tablaRecords != null) tablaRecords.SetActive(false);
            if (panelIniciales != null) panelIniciales.SetActive(true);
            Seleccionar(botonListo);
        }

        private void CambiarLetra(int indice, int paso)
        {
            letraActual = indice;
            indices[indice] = (indices[indice] + paso + Letras.Length) % Letras.Length;
            RefrescarLetras();
            ServiciosMenus.Instancia.Tic();
        }

        private void RefrescarLetras()
        {
            if (letras == null) return;
            for (int i = 0; i < letras.Length && i < 3; i++)
            {
                if (letras[i] == null) continue;
                letras[i].text = Letras[indices[i]].ToString();
                letras[i].color = i == letraActual ? PaletaArcade.Amarillo : PaletaArcade.Crema;
            }
        }

        private string InicialesActuales()
        {
            return "" + Letras[indices[0]] + Letras[indices[1]] + Letras[indices[2]];
        }

        /// <summary>Guarda el récord con las iniciales elegidas y muestra la tabla.</summary>
        public void GuardarIniciales()
        {
            if (!pidiendoIniciales) return;
            pidiendoIniciales = false;
            string texto = InicialesActuales();
            PlayerPrefs.SetString(ClaveIniciales, texto);
            posicionNueva = TablaRecords.Agregar(texto, puntos);
            ServiciosMenus.Instancia.Panel();
            if (panelIniciales != null) panelIniciales.SetActive(false);
            MostrarTabla();
            Seleccionar(botonVolverAJugar);
        }

        // ------------------------------------------------------------ tabla
        private void MostrarTabla()
        {
            if (tablaRecords != null) tablaRecords.SetActive(true);
            List<TablaRecords.Entrada> lista = TablaRecords.Cargar();
            for (int i = 0; i < TablaRecords.Maximo; i++)
            {
                bool hay = i < lista.Count;
                bool nuevo = i == posicionNueva;
                Color color = nuevo ? PaletaArcade.Amarillo : PaletaArcade.Crema;
                Poner(posiciones, i, (i + 1) + ".", color);
                Poner(iniciales, i, hay ? lista[i].iniciales : "---", color);
                Poner(puntajes, i, hay ? lista[i].puntos.ToString() : "0", color);
            }
            if (marcaNuevo != null) marcaNuevo.SetActive(posicionNueva >= 0);
        }

        private static void Poner(Text[] textos, int i, string valor, Color color)
        {
            if (textos == null || i >= textos.Length || textos[i] == null) return;
            textos[i].text = valor;
            textos[i].color = color;
        }

        private static void Seleccionar(Button boton)
        {
            if (boton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(boton.gameObject);
        }

        // ------------------------------------------------------------ botones
        public void VolverAJugar()
        {
            GuardarIniciales();   // si dejó las iniciales a medias, el récord no se pierde
            NavegadorEscenas.Cargar(Escenas.Nivel1);
        }

        public void IrAlMenuPrincipal()
        {
            GuardarIniciales();
            NavegadorEscenas.Cargar(Escenas.MenuPrincipal);
        }
    }
}
