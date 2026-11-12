using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DemoledorUnifranz.Game;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Ventana "ELIGE UN PISO" del menú principal: muestra el edificio completo de Unifranz
    /// (solo para mirar) dividido en franjas, una por piso. Al pasar el mouse, usar las flechas
    /// o tocar una franja, la ficha de la derecha explica qué hay en ese piso. Un segundo clic,
    /// ENTER o el botón JUGAR cargan el nivel. EDIFICIO COMPLETO juega el modo original.
    ///
    /// Sirve para probar cada nivel sin tocar el Inspector: GameSession recuerda el modo y el
    /// piso elegidos. Los pisos que todavía no están hechos se ven apagados y no se pueden jugar.
    /// Lo construye ConstructorMenuPrincipal.
    /// </summary>
    public class MenuNivelesArcade : MonoBehaviour
    {
        [Header("Edificio (un elemento por piso, el Piso 1 primero)")]
        public Button[] botonesBanda;
        public Image[] bandas;
        public Text[] etiquetas;

        [Header("Datos de cada piso")]
        public string[] titulos;
        public string[] carreras;
        public string[] descripciones;
        public string[] datos;
        [Tooltip("Qué pisos se pueden jugar. Debe coincidir con 'Pisos Activos' de EdificioPisos.")]
        public bool[] disponible;

        [Header("Ficha")]
        public Text textoTitulo;
        public Text textoCarrera;
        public Text textoEstado;
        public Text textoDescripcion;
        public Text textoDatoTitulo;
        public Text textoDato;

        [Header("Botones")]
        public Button botonJugar;
        public Button botonEdificio;

        private int seleccionado = -1;
        private bool conectado;

        private static readonly Color BandaBloqueada = new Color(0.02f, 0.03f, 0.08f, 0.55f);
        private static readonly Color BandaElegida = new Color(1f, 0.76f, 0.1f, 0.38f);
        private static readonly Color BandaElegidaBloqueada = new Color(0.5f, 0.55f, 0.7f, 0.40f);
        private static readonly Color TextoBloqueado = new Color(0.72f, 0.74f, 0.82f, 0.85f);

        private int Total { get { return botonesBanda == null ? 0 : botonesBanda.Length; } }

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

            for (int i = 0; i < Total; i++)
            {
                if (botonesBanda[i] == null) continue;
                int piso = i;   // copia para el cierre
                botonesBanda[i].onClick.AddListener(() => ClicEnPiso(piso));

                BandaPisoUI banda = botonesBanda[i].gameObject.AddComponent<BandaPisoUI>();
                banda.menu = this;
                banda.indice = i;
            }

            if (botonJugar != null) botonJugar.onClick.AddListener(JugarSeleccionado);
            if (botonEdificio != null) botonEdificio.onClick.AddListener(IniciarEdificioCompleto);
        }

        private bool Disponible(int i)
        {
            return disponible != null && i >= 0 && i < disponible.Length && disponible[i];
        }

        /// <summary>Muestra la ficha del piso y resalta su franja en el edificio.</summary>
        public void Seleccionar(int i)
        {
            if (i < 0 || i >= Total) return;
            seleccionado = i;
            bool libre = Disponible(i);

            for (int k = 0; k < Total; k++)
            {
                bool elegido = k == i;
                bool kLibre = Disponible(k);
                if (bandas != null && k < bandas.Length && bandas[k] != null)
                {
                    Color c = kLibre ? new Color(0f, 0f, 0f, 0f) : BandaBloqueada;
                    if (elegido) c = kLibre ? BandaElegida : BandaElegidaBloqueada;
                    bandas[k].color = c;
                }
                if (etiquetas != null && k < etiquetas.Length && etiquetas[k] != null)
                    etiquetas[k].color = elegido ? Color.white : (kLibre ? PaletaArcade.Crema : TextoBloqueado);
            }

            if (textoTitulo != null) textoTitulo.text = Dato(titulos, i);
            string carrera = Dato(carreras, i);
            if (textoCarrera != null) textoCarrera.text = string.IsNullOrEmpty(carrera) ? "" : "CARRERA: " + carrera.ToUpperInvariant();
            if (textoEstado != null)
            {
                textoEstado.text = libre ? "DISPONIBLE" : "PRÓXIMAMENTE";
                textoEstado.color = libre ? PaletaArcade.Verde : TextoBloqueado;
            }
            if (textoDescripcion != null) textoDescripcion.text = Dato(descripciones, i);

            string dato = Dato(datos, i);
            bool hayDato = libre && !string.IsNullOrEmpty(dato);
            if (textoDatoTitulo != null) textoDatoTitulo.gameObject.SetActive(hayDato);
            if (textoDato != null)
            {
                textoDato.gameObject.SetActive(hayDato);
                textoDato.text = dato;
            }

            if (botonJugar != null)
            {
                botonJugar.interactable = libre;
                Text etiqueta = botonJugar.GetComponentInChildren<Text>();
                if (etiqueta != null) etiqueta.text = libre ? "JUGAR PISO " + (i + 1) : "NO DISPONIBLE";
            }
        }

        private static string Dato(string[] lista, int i)
        {
            return lista != null && i >= 0 && i < lista.Length && lista[i] != null ? lista[i] : "";
        }

        // ------------------------------------------------------------ acciones
        /// <summary>Primer clic o toque: muestra la ficha. Con el piso ya elegido (mouse encima, flechas), lo juega.</summary>
        private void ClicEnPiso(int i)
        {
            if (seleccionado == i && Disponible(i)) IniciarPiso(i);
            else Seleccionar(i);
        }

        private void JugarSeleccionado()
        {
            if (seleccionado >= 0 && Disponible(seleccionado)) IniciarPiso(seleccionado);
        }

        private void IniciarEdificioCompleto()
        {
            GameSession.SeleccionarNivel(false, 0);
            NavegadorEscenas.Cargar(Escenas.Nivel1);
        }

        private void IniciarPiso(int piso)
        {
            GameSession.SeleccionarNivel(true, piso);
            string escena = Escenas.ParaPiso(piso);
            // Si la escena cenital todavía no está en Build Settings, se juega el modo lateral.
            if (!NavegadorEscenas.Disponible(escena)) escena = Escenas.Nivel1;
            NavegadorEscenas.Cargar(escena);
        }
    }
}
