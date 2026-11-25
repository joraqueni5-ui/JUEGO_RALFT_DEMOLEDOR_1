using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DemoledorUnifranz.Game;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Ventana "ELIGE UN NIVEL": el edificio de Unifranz (el dibujo edificio_unifranz) con una franja por nivel
    /// que se resalta al pasar el mouse, usar las flechas o tocar, y una lista con los 7 niveles de los pisos y el
    /// NIVEL FINAL (bloqueado con candado hasta superar la terraza). Debajo del edificio va la ficha del nivel:
    /// nombre, estado con estrellas, descripción y dato útil. Un segundo clic, ENTER o el botón JUGAR cargan el nivel.
    ///
    /// Los niveles se numeran de 0 (planta baja) a 6; el 7 es el nivel final. Lo construye ConstructorMenuPrincipal.
    /// </summary>
    public class MenuNivelesArcade : MonoBehaviour
    {
        /// <summary>Niveles de la lista: planta baja, pisos 1 a 6 y el nivel final.</summary>
        public const int Total = 8;
        public const int NivelFinal = 7;

        [Header("Edificio (una franja por piso de 0 a 6)")]
        public Button[] botonesBanda;
        public Image[] bandas;
        [Tooltip("Marco que se enciende alrededor de todo el edificio al elegir el nivel final.")]
        public GameObject marcoFinal;

        [Header("Lista de niveles (8, en el orden de la lista)")]
        public Button[] botonesLista;
        public Text[] etiquetasLista;

        [Header("Datos de cada nivel (8)")]
        public string[] titulos;
        public string[] descripciones;
        public string[] datos;
        [Tooltip("Qué pisos se pueden jugar (el nivel final depende del progreso guardado).")]
        public bool[] disponible;

        [Header("Ficha")]
        public Text textoTitulo;
        public Text textoEstado;
        public Text textoDescripcion;
        public Text textoDatoTitulo;
        public Text textoDato;

        [Header("Botones")]
        public Button botonJugar;

        private int seleccionado = -1;
        private bool conectado;
        private Image[] estrellasFicha;
        private Image[][] estrellasLista;
        private Image candadoLista;

        private static readonly Color BandaElegida = new Color(1f, 0.76f, 0.1f, 0.38f);
        private static readonly Color BandaBloqueada = new Color(0.02f, 0.03f, 0.08f, 0.45f);
        private static readonly Color TextoBloqueado = new Color(0.72f, 0.74f, 0.82f, 0.85f);
        private static readonly Color Oro = new Color(1f, 0.82f, 0.15f, 1f);
        private static readonly Color EstrellaApagada = new Color(0.25f, 0.27f, 0.35f, 0.9f);

        public const string TextoDesbloquear = "Supera la terraza para desbloquear";

        private void Awake()
        {
            Conectar();
        }

        private void OnEnable()
        {
            Conectar();
            Seleccionar(Mathf.Max(0, seleccionado));
        }

        private void Conectar()
        {
            if (conectado) return;
            conectado = true;

            // Franjas del edificio: solo mouse y toque (el teclado usa la lista).
            for (int i = 0; botonesBanda != null && i < botonesBanda.Length; i++)
            {
                if (botonesBanda[i] == null) continue;
                int nivel = i;
                botonesBanda[i].onClick.AddListener(() => ClicEn(nivel));
                botonesBanda[i].navigation = new Navigation { mode = Navigation.Mode.None };
                Enlazar(botonesBanda[i], i);
            }

            // Lista de niveles.
            for (int i = 0; botonesLista != null && i < botonesLista.Length; i++)
            {
                if (botonesLista[i] == null) continue;
                int nivel = i;
                botonesLista[i].onClick.AddListener(() => ClicEn(nivel));
                Enlazar(botonesLista[i], i);
            }

            if (botonJugar != null) botonJugar.onClick.AddListener(JugarSeleccionado);
        }

        private void Enlazar(Button boton, int indice)
        {
            BandaPisoUI banda = boton.gameObject.AddComponent<BandaPisoUI>();
            banda.menu = this;
            banda.indice = indice;
        }

        // ------------------------------------------------------------ estado
        private bool Libre(int i)
        {
            if (i == NivelFinal) return GameSession.NivelFinalDesbloqueado;
            return disponible != null && i >= 0 && i < disponible.Length && disponible[i];
        }

        private static string Dato(string[] lista, int i)
        {
            return lista != null && i >= 0 && i < lista.Length && lista[i] != null ? lista[i] : "";
        }

        /// <summary>Muestra la ficha del nivel y resalta su franja en el edificio (o todo el edificio si es el final).</summary>
        public void Seleccionar(int i)
        {
            if (i < 0 || i >= Total) return;
            seleccionado = i;
            bool libre = Libre(i);

            // Edificio.
            for (int k = 0; bandas != null && k < bandas.Length; k++)
            {
                if (bandas[k] == null) continue;
                Color c = Libre(k) ? new Color(0f, 0f, 0f, 0f) : BandaBloqueada;
                if (k == i) c = BandaElegida;
                bandas[k].color = c;
            }
            if (marcoFinal != null) marcoFinal.SetActive(i == NivelFinal);

            // Lista.
            for (int k = 0; etiquetasLista != null && k < etiquetasLista.Length; k++)
            {
                if (etiquetasLista[k] == null) continue;
                etiquetasLista[k].color = k == i ? Color.white : (Libre(k) ? PaletaArcade.Crema : TextoBloqueado);
            }
            ActualizarLista();

            // Ficha.
            if (textoTitulo != null) textoTitulo.text = Dato(titulos, i);
            if (textoEstado != null)
            {
                textoEstado.text = libre ? "DISPONIBLE" : (i == NivelFinal ? "BLOQUEADO" : "PRÓXIMAMENTE");
                textoEstado.color = libre ? PaletaArcade.Verde : TextoBloqueado;
            }
            if (textoDescripcion != null)
                textoDescripcion.text = i == NivelFinal && !libre ? TextoDesbloquear.ToUpperInvariant() : Dato(descripciones, i);

            string dato = Dato(datos, i);
            bool hayDato = libre && !string.IsNullOrEmpty(dato);
            if (textoDatoTitulo != null) textoDatoTitulo.gameObject.SetActive(hayDato);
            if (textoDato != null)
            {
                textoDato.gameObject.SetActive(hayDato);
                textoDato.text = dato;
            }

            MostrarEstrellasFicha(i, libre && i < NivelFinal);

            if (botonJugar != null)
            {
                botonJugar.interactable = libre;
                Text etiqueta = botonJugar.GetComponentInChildren<Text>();
                if (etiqueta != null) etiqueta.text = libre ? "JUGAR" : (i == NivelFinal ? "BLOQUEADO" : "NO DISPONIBLE");
            }
        }

        // ------------------------------------------------------------ estrellas y candado
        private void MostrarEstrellasFicha(int nivel, bool ver)
        {
            if (textoEstado == null) return;
            if (estrellasFicha == null) estrellasFicha = CrearEstrellas(textoEstado.rectTransform, 38f);

            int logradas = nivel < NivelFinal ? GameSession.EstrellasDe(nivel) : 0;
            float inicio = textoEstado.preferredWidth * 0.5f + 40f;
            for (int k = 0; k < 3; k++)
            {
                estrellasFicha[k].gameObject.SetActive(ver);
                estrellasFicha[k].rectTransform.anchoredPosition = new Vector2(inicio + k * 44f, 0f);
                Provisional.PonerEstrella(estrellasFicha[k], k < logradas, Oro, EstrellaApagada, true);
            }
        }

        private static Image[] CrearEstrellas(RectTransform padre, float lado)
        {
            Image[] r = new Image[3];
            for (int k = 0; k < 3; k++)
            {
                GameObject g = new GameObject("Estrella" + (k + 1), typeof(RectTransform), typeof(Image));
                g.transform.SetParent(padre, false);
                Image im = g.GetComponent<Image>();
                im.sprite = Provisional.Estrella;
                im.raycastTarget = false;
                RectTransform rt = im.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(lado, lado);
                r[k] = im;
            }
            return r;
        }

        /// <summary>Estrellas de cada nivel en la lista (o el candado del nivel final mientras esté bloqueado).</summary>
        private void ActualizarLista()
        {
            if (botonesLista == null) return;
            if (estrellasLista == null) estrellasLista = new Image[Total][];

            for (int i = 0; i < botonesLista.Length && i < Total; i++)
            {
                if (botonesLista[i] == null) continue;
                RectTransform rb = (RectTransform)botonesLista[i].transform;
                if (estrellasLista[i] == null && i < NivelFinal)
                {
                    estrellasLista[i] = CrearEstrellas(rb, 24f);
                    for (int k = 0; k < 3; k++)
                    {
                        RectTransform rt = estrellasLista[i][k].rectTransform;
                        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
                        rt.anchoredPosition = new Vector2(-98f + k * 30f, 2f);
                    }
                }

                if (estrellasLista[i] != null)
                {
                    int logradas = GameSession.EstrellasDe(i);
                    bool ver = Libre(i);
                    for (int k = 0; k < 3; k++)
                    {
                        estrellasLista[i][k].gameObject.SetActive(ver);
                        Provisional.PonerEstrella(estrellasLista[i][k], k < logradas, Oro, EstrellaApagada, false);
                    }
                }
            }

            // Candado en el nivel final, mientras esté bloqueado.
            if (botonesLista.Length > NivelFinal && botonesLista[NivelFinal] != null)
            {
                if (candadoLista == null)
                {
                    RectTransform rb = (RectTransform)botonesLista[NivelFinal].transform;
                    GameObject g = new GameObject("Candado", typeof(RectTransform), typeof(Image));
                    g.transform.SetParent(rb, false);
                    candadoLista = g.GetComponent<Image>();
                    candadoLista.sprite = Provisional.Candado;
                    candadoLista.raycastTarget = false;
                    RectTransform rc = candadoLista.rectTransform;
                    rc.anchorMin = rc.anchorMax = new Vector2(1f, 0.5f);
                    rc.pivot = new Vector2(1f, 0.5f);
                    rc.anchoredPosition = new Vector2(-24f, 2f);
                    rc.sizeDelta = new Vector2(38f, 38f);
                    candadoLista.color = ArteNiveles.Actual != null && ArteNiveles.Actual.candado != null ? Color.white : Oro;
                }
                candadoLista.gameObject.SetActive(!GameSession.NivelFinalDesbloqueado);
            }
        }

        // ------------------------------------------------------------ acciones
        /// <summary>Primer clic o toque: muestra la ficha. Con el nivel ya elegido (mouse encima, flechas), lo juega.</summary>
        private void ClicEn(int i)
        {
            if (seleccionado == i && Libre(i)) Iniciar(i);
            else Seleccionar(i);
        }

        private void JugarSeleccionado()
        {
            if (seleccionado >= 0 && Libre(seleccionado)) Iniciar(seleccionado);
        }

        private void Iniciar(int nivel)
        {
            if (nivel == NivelFinal)
            {
                SelectorDificultad.Mostrar(() =>
                {
                    GameSession.NuevaPartida();
                    GameSession.Supervivencia = false;
                    GameSession.EnNivelFinal = true;
                    NavegadorEscenas.Cargar(Escenas.NivelFinal);
                });
                return;
            }

            // Antes de un piso se elige el personaje (Extras/SelectorPersonaje) y la dificultad.
            SelectorPersonaje.Mostrar(() => SelectorDificultad.Mostrar(() =>
            {
                GameSession.SeleccionarNivel(true, nivel);
                NavegadorEscenas.Cargar(Escenas.ParaPiso(nivel));
            }));
        }
    }
}
