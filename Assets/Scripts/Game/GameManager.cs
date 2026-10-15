using UnityEngine;
using UnityEngine.SceneManagement;
using DemoledorUnifranz.UI;

namespace DemoledorUnifranz.Game
{
    /// <summary>
    /// Controla las reglas del nivel: cuenta el puntaje, descuenta vidas,
    /// habilita el menu de pausa con la tecla Escape y decide cuando
    /// termina la partida (Victoria o Game Over).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instancia { get; private set; }

        [Header("Referencias de interfaz")]
        public HUDController hud;
        public PauseMenuController menuPausa;

        [Header("Reglas del nivel")]
        public int metaLadrillos = 5;
        public int puntosPorLadrillo = 100;

        private void Awake()
        {
            Instancia = this;

            // Al entrar al nivel el tiempo vuelve a la normalidad,
            // caso contrario la pausa anterior quedaria activa.
            Time.timeScale = 1f;
            GameSession.IniciarPartida(metaLadrillos, SceneManager.GetActiveScene().name);
        }

        private void Start()
        {
            RefrescarHud();

            if (Autoprueba.Activa)
            {
                int visita = Autoprueba.SiguienteVisita();

                if (visita == 1)
                {
                    Debug.Log("[AUTOPRUEBA] nivel cargado: vidas=" + GameSession.Vidas + " ladrillos=" + GameSession.Ladrillos);
                    Autoprueba.Programar(this, 3f, PruebaDelNivel);
                }
                else
                {
                    Debug.Log("[AUTOPRUEBA] nivel reiniciado: vidas=" + GameSession.Vidas + " ladrillos=" + GameSession.Ladrillos + " puntaje=" + GameSession.Puntaje);
                    Autoprueba.Programar(this, 1.5f, TerminarPrueba);
                }
            }
        }

        private void PruebaDelNivel()
        {
            if (menuPausa != null)
            {
                menuPausa.Pausar();
                Debug.Log("[AUTOPRUEBA] menu de pausa abierto, tiempo=" + Time.timeScale);
                menuPausa.Reanudar();
                Debug.Log("[AUTOPRUEBA] menu de pausa cerrado, tiempo=" + Time.timeScale);
            }

            for (int i = 0; i < metaLadrillos; i++)
            {
                RecolectarLadrillo();
            }

            Debug.Log("[AUTOPRUEBA] ladrillos recogidos=" + GameSession.Ladrillos + " puntaje=" + GameSession.Puntaje);
        }

        private void TerminarPrueba()
        {
            Debug.Log("[AUTOPRUEBA] fin de la prueba");
            Application.Quit();
        }

        private void Update()
        {
            // Menu de pausa con Escape.
            if (Input.GetKeyDown(KeyCode.Escape) && menuPausa != null)
            {
                menuPausa.AlternarPausa();
            }
        }

        /// <summary>Llamado por los ladrillos cuando el jugador los recoge.</summary>
        public void RecolectarLadrillo()
        {
            GameSession.Puntaje += puntosPorLadrillo;
            GameSession.Ladrillos++;
            RefrescarHud();

            if (GameSession.Ladrillos >= GameSession.MetaLadrillos)
            {
                TerminarNivel(true);
            }
        }

        /// <summary>Llamado por el jugador al ser alcanzado por un enemigo.</summary>
        public void RecibirDanio()
        {
            GameSession.Vidas--;
            RefrescarHud();

            if (GameSession.Vidas <= 0)
            {
                TerminarNivel(false);
            }
        }

        /// <summary>Cierra el nivel y carga la pantalla de Victoria / Game Over.</summary>
        public void TerminarNivel(bool victoria)
        {
            GameSession.Victoria = victoria;
            Time.timeScale = 1f;
            SceneManager.LoadScene(Escenas.FinJuego);
        }

        /// <summary>Envia los datos actuales al HUD.</summary>
        public void RefrescarHud()
        {
            if (hud == null)
            {
                return;
            }

            hud.Refrescar(
                GameSession.Puntaje,
                GameSession.Vidas,
                GameSession.Ladrillos,
                GameSession.MetaLadrillos);
        }
    }
}
