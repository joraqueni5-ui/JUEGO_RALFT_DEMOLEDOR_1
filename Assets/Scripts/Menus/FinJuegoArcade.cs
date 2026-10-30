using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DemoledorUnifranz.Game;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Pantalla FinJuego: muestra los datos reales de la partida de Universidad
    /// (puntos, reparaciones y % de estructura destruida) que GestorJuego guarda en
    /// GameSession, y el mejor puntaje (PlayerPrefs).
    /// VOLVER A JUGAR carga la misma escena que JUGAR del menú principal.
    /// </summary>
    public class FinJuegoArcade : MonoBehaviour
    {
        private const string ClaveRecord = "demoledor.mejorPuntaje";

        [Header("Textos")]
        public Text textoTitulo;
        public Text textoSubtitulo;
        public Text valorPuntos;
        public Text valorReparaciones;
        public Text valorDestruido;
        public GameObject filaReparaciones;
        public GameObject filaDestruido;
        public Text textoRecord;

        [Header("Botones")]
        public Button botonVolverAJugar;
        public Button botonMenuPrincipal;

        private void Start()
        {
            Time.timeScale = 1f;
            MostrarResultado();

            // No se muestra un botón que no pueda funcionar.
            if (botonMenuPrincipal != null && !NavegadorEscenas.Disponible(Escenas.MenuPrincipal))
            {
                Debug.LogWarning("[Menus] MenuPrincipal no está en Build Settings: se oculta el botón MENÚ PRINCIPAL.");
                botonMenuPrincipal.gameObject.SetActive(false);
            }

            if (botonVolverAJugar != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(botonVolverAJugar.gameObject);
        }

        private void MostrarResultado()
        {
            int puntos = GameSession.Puntaje;
            bool derrumbe = GameSession.HayDatosDerrumbe;

            if (textoTitulo != null)
            {
                bool victoria = !derrumbe && GameSession.Victoria;
                textoTitulo.text = victoria ? "¡VICTORIA!" : "GAME OVER";
                textoTitulo.color = victoria ? PaletaArcade.Hex(0x8CF26E) : PaletaArcade.Rojo;
            }
            if (textoSubtitulo != null)
                textoSubtitulo.text = derrumbe ? "¡EL EDIFICIO SE CAYÓ!" : (GameSession.Victoria ? "¡LO LOGRASTE!" : "FIN DE LA PARTIDA");

            // Las filas de reparaciones y estructura solo tienen sentido en Universidad.
            if (filaReparaciones != null) filaReparaciones.SetActive(derrumbe);
            if (filaDestruido != null) filaDestruido.SetActive(derrumbe);

            // Mejor puntaje.
            int record = PlayerPrefs.GetInt(ClaveRecord, 0);
            bool nuevo = puntos > record;
            if (nuevo)
            {
                record = puntos;
                PlayerPrefs.SetInt(ClaveRecord, record);
                PlayerPrefs.Save();
            }
            if (textoRecord != null)
            {
                textoRecord.text = nuevo && puntos > 0 ? "¡NUEVO RÉCORD!" : "MEJOR PUNTAJE: " + record;
                textoRecord.color = nuevo && puntos > 0 ? PaletaArcade.Amarillo : PaletaArcade.Crema;
            }

            StartCoroutine(Contar(valorPuntos, puntos, ""));
            if (derrumbe)
            {
                StartCoroutine(Contar(valorReparaciones, GameSession.Reparaciones, ""));
                StartCoroutine(Contar(valorDestruido, Mathf.RoundToInt(GameSession.PorcentajeDestruido * 100f), "%"));
            }
        }

        /// <summary>Cuenta de 0 al valor final, como en las máquinas arcade.</summary>
        private IEnumerator Contar(Text texto, int valor, string sufijo)
        {
            if (texto == null) yield break;
            const float Duracion = 0.9f;
            float t = 0f;
            yield return new WaitForSecondsRealtime(0.35f);
            while (t < Duracion)
            {
                t += Time.unscaledDeltaTime;
                float p = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / Duracion), 3f);
                texto.text = Mathf.RoundToInt(valor * p) + sufijo;
                if (ServiciosMenus.Existe && Mathf.RoundToInt(valor * p) != valor) ServiciosMenus.Instancia.Tic();
                yield return null;
            }
            texto.text = valor + sufijo;
        }

        public void VolverAJugar()
        {
            NavegadorEscenas.Cargar(Escenas.Nivel1);
        }

        public void IrAlMenuPrincipal()
        {
            NavegadorEscenas.Cargar(Escenas.MenuPrincipal);
        }
    }
}
