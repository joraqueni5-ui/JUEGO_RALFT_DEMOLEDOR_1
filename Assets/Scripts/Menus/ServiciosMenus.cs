using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Objeto persistente (DontDestroyOnLoad) que da servicio a todos los menús:
    /// - Sonidos de la interfaz, sintetizados por código (no hay audios en el proyecto).
    /// - Fundido a negro al cambiar de escena.
    /// - Un AudioListener de respaldo para las escenas de menú que no tienen uno.
    /// Se crea solo la primera vez que se necesita.
    /// </summary>
    public class ServiciosMenus : MonoBehaviour
    {
        private static ServiciosMenus instancia;

        public static ServiciosMenus Instancia
        {
            get
            {
                if (instancia == null)
                {
                    GameObject objeto = new GameObject("[ServiciosMenus]");
                    DontDestroyOnLoad(objeto);
                    instancia = objeto.AddComponent<ServiciosMenus>();
                }
                return instancia;
            }
        }

        public static bool Existe { get { return instancia != null; } }

        private AudioSource fuente;
        private AudioListener oyente;
        private Image cortina;
        private bool cargando;

        private AudioClip sonidoHover;
        private AudioClip sonidoClic;
        private AudioClip sonidoTic;
        private AudioClip sonidoPanel;
        private float ultimoTic;

        public bool Cargando { get { return cargando; } }

        private void Awake()
        {
            if (instancia != null && instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            instancia = this;

            fuente = gameObject.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.spatialBlend = 0f;
            fuente.ignoreListenerPause = true;

            oyente = gameObject.AddComponent<AudioListener>();
            oyente.enabled = false;

            sonidoHover = CrearTono("ui_hover", 0.05f, 660f, 900f, 0.18f, false);
            sonidoClic = CrearTono("ui_clic", 0.12f, 540f, 240f, 0.20f, true);
            sonidoTic = CrearTono("ui_tic", 0.03f, 1200f, 1100f, 0.12f, false);
            sonidoPanel = CrearTono("ui_panel", 0.14f, 300f, 620f, 0.16f, true);

            CrearCortina();
            RevisarOyente();
            SceneManager.sceneLoaded += AlCargarEscena;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            if (instancia == this) instancia = null;
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            RevisarOyente();
        }

        // ------------------------------------------------------------ sonidos
        public void Hover() { Reproducir(sonidoHover); }
        public void Clic() { Reproducir(sonidoClic); }
        public void Panel() { Reproducir(sonidoPanel); }

        /// <summary>Tic corto (al mover el slider), limitado para que no se amontone.</summary>
        public void Tic()
        {
            if (Time.unscaledTime - ultimoTic < 0.06f) return;
            ultimoTic = Time.unscaledTime;
            Reproducir(sonidoTic);
        }

        private void Reproducir(AudioClip clip)
        {
            if (clip == null || fuente == null) return;
            fuente.PlayOneShot(clip);
        }

        private static AudioClip CrearTono(string nombre, float duracion, float frecuenciaInicial, float frecuenciaFinal,
            float volumen, bool cuadrada)
        {
            const int Muestreo = 44100;
            int largo = Mathf.Max(1, Mathf.RoundToInt(Muestreo * duracion));
            float[] datos = new float[largo];
            float fase = 0f;

            for (int i = 0; i < largo; i++)
            {
                float t = i / (float)largo;
                float frecuencia = Mathf.Lerp(frecuenciaInicial, frecuenciaFinal, t);
                fase += 2f * Mathf.PI * frecuencia / Muestreo;
                float onda = Mathf.Sin(fase);
                if (cuadrada) onda = Mathf.Sign(onda) * 0.55f + onda * 0.25f;
                float ataque = Mathf.Clamp01(i / (Muestreo * 0.004f));
                float caida = (1f - t) * (1f - t);
                datos[i] = onda * ataque * caida * volumen;
            }

            AudioClip clip = AudioClip.Create(nombre, largo, 1, Muestreo, false);
            clip.SetData(datos, 0);
            return clip;
        }

        /// <summary>
        /// Enciende el AudioListener propio solo si la escena no tiene otro
        /// (MenuPrincipal y FinJuego no traen uno en su cámara).
        /// </summary>
        public void RevisarOyente()
        {
            if (oyente == null) return;

            bool hayOtro = false;
            AudioListener[] oyentes = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            for (int i = 0; i < oyentes.Length; i++)
            {
                if (oyentes[i] != oyente && oyentes[i].enabled && oyentes[i].gameObject.activeInHierarchy)
                {
                    hayOtro = true;
                    break;
                }
            }
            oyente.enabled = !hayOtro;
        }

        // ------------------------------------------------------------ cambio de escena
        private void CrearCortina()
        {
            GameObject lienzo = new GameObject("Cortina", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            lienzo.transform.SetParent(transform, false);
            Canvas canvas = lienzo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            GameObject imagen = new GameObject("Negro", typeof(RectTransform), typeof(Image));
            imagen.transform.SetParent(lienzo.transform, false);
            RectTransform rt = (RectTransform)imagen.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            cortina = imagen.GetComponent<Image>();
            cortina.color = new Color(PaletaArcade.AzulNoche.r, PaletaArcade.AzulNoche.g, PaletaArcade.AzulNoche.b, 0f);
            cortina.raycastTarget = false;
        }

        /// <summary>Funde a oscuro, carga la escena y vuelve a mostrar.</summary>
        public void CargarEscena(string nombre)
        {
            if (cargando) return;
            StartCoroutine(RutinaCarga(nombre));
        }

        private IEnumerator RutinaCarga(string nombre)
        {
            cargando = true;
            cortina.raycastTarget = true;
            yield return Fundir(0f, 1f, 0.25f);

            Time.timeScale = 1f;
            SceneManager.LoadScene(nombre);
            yield return null;

            yield return Fundir(1f, 0f, 0.3f);
            cortina.raycastTarget = false;
            cargando = false;
        }

        private IEnumerator Fundir(float desde, float hasta, float duracion)
        {
            float t = 0f;
            Color c = cortina.color;
            while (t < duracion)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t / duracion));
                cortina.color = c;
                yield return null;
            }
            c.a = hasta;
            cortina.color = c;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEstatico()
        {
            instancia = null;
        }
    }
}
