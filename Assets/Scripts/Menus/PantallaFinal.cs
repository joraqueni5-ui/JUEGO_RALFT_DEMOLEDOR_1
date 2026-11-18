using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DemoledorUnifranz.Game;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Pantalla final al superar el último piso: "¡UNIVERSIDAD SALVADA!" con el puntaje total y las estrellas
    /// (mejor marca) de cada piso. Funciona con el juego congelado y se cierra con CONTINUAR, ENTER o ESPACIO;
    /// después se muestra la pantalla de fin con la tabla de récords.
    /// </summary>
    public class PantallaFinal : MonoBehaviour
    {
        private const float Px = FabricaUIArcade.Px;
        private const float EsperaAntesDeAceptarTecla = 0.6f;

        private PanelAnimado panel;
        private Text textoPuntos;
        private float horaApertura;

        public bool Abierta { get; private set; }
        public event Action Cerrada;

        public static PantallaFinal Crear(Transform padre, string[] nombresPisos, bool conSello = false)
        {
            GameObject objeto = new GameObject("PantallaFinal");
            objeto.transform.SetParent(padre, false);
            PantallaFinal p = objeto.AddComponent<PantallaFinal>();
            FabricaUIArcade.AsegurarEventSystem();
            p.Construir(nombresPisos, conSello);
            return p;
        }

        /// <summary>Muestra el puntaje total; las estrellas salen de GameSession.</summary>
        public void Mostrar(int puntajeTotal)
        {
            textoPuntos.text = "PUNTAJE TOTAL  " + puntajeTotal;
            GestorAudio.Jingle(Snd.JingleVictoria);   // "¡UNIVERSIDAD SALVADA!": una sola vez y la música se detiene
            Abierta = true;
            horaApertura = Time.unscaledTime;
            panel.Mostrar();
        }

        public void Cerrar()
        {
            if (!Abierta) return;
            Abierta = false;
            panel.Ocultar();
            if (Cerrada != null) Cerrada();
        }

        private void Update()
        {
            if (!Abierta || Time.unscaledTime < horaApertura + EsperaAntesDeAceptarTecla) return;

            Keyboard teclado = Keyboard.current;
            if (teclado == null) return;
            if (teclado[Key.Enter].wasPressedThisFrame || teclado[Key.NumpadEnter].wasPressedThisFrame ||
                teclado[Key.Space].wasPressedThisFrame)
            {
                Cerrar();
            }
        }

        private void Construir(string[] nombresPisos, bool conSello)
        {
            KitSpritesArcade kit = KitSpritesArcade.Actual();

            Canvas canvas = FabricaUIArcade.CrearCanvas("CanvasPantallaFinal", 326);
            canvas.transform.SetParent(transform, false);

            RectTransform caja;
            panel = FabricaUIArcade.CrearVentana(canvas.transform, "PanelFinal", "¡UNIVERSIDAD SALVADA!",
                new Vector2(1100f, 860f), kit, out caja, null, kit.iconoArriba);

            textoPuntos = FabricaUIArcade.CrearTexto(caja, "Puntos", "", 46, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, Px, Px);
            FabricaUIArcade.Colocar(textoPuntos.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1000f, 70f));

            // Una fila por piso: nombre y tres estrellas.
            int filas = nombresPisos != null ? nombresPisos.Length : 0;
            float paso = filas > 5 ? 62f : 92f;          // con 7 pisos las filas se compactan para caber en la ventana
            float lado = filas > 5 ? 46f : 64f;
            int letra = filas > 5 ? 26 : 32;
            for (int i = 0; i < filas; i++)
            {
                float y = (filas > 5 ? 175f : 150f) - i * paso;
                Text nombre = FabricaUIArcade.CrearTexto(caja, "Piso" + i, nombresPisos[i], letra, PaletaArcade.Crema, TextAnchor.MiddleLeft, Px * 0.75f, 0f);
                FabricaUIArcade.Colocar(nombre.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-230f, y), new Vector2(620f, 56f));

                int logradas = GameSession.EstrellasDe(i);
                for (int k = 0; k < 3; k++)
                {
                    Image e = FabricaUIArcade.CrearImagen(caja, "Estrella" + i + "_" + k, Provisional.Estrella, Color.white);
                    Provisional.PonerEstrella(e, k < logradas, new Color(1f, 0.82f, 0.15f, 1f), new Color(0.22f, 0.25f, 0.33f, 0.95f));
                    e.preserveAspect = true;
                    FabricaUIArcade.Colocar(e.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(250f + k * (lado + 10f), y), new Vector2(lado, lado));
                }
            }

            // Sello del nivel final: una medalla dorada con la estrella y el texto.
            if (conSello)
            {
                Image medalla = FabricaUIArcade.CrearImagen(caja, "Sello", Provisional.Circulo, new Color(0.95f, 0.75f, 0.15f, 1f));
                FabricaUIArcade.Colocar(medalla.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(390f, 190f), new Vector2(190f, 190f));
                Image aro = FabricaUIArcade.CrearImagen(medalla.transform, "Aro", Provisional.Aro, new Color(0.55f, 0.35f, 0.05f, 1f));
                FabricaUIArcade.Estirar(aro.rectTransform);
                Image estrella = FabricaUIArcade.CrearImagen(medalla.transform, "Estrella", Provisional.Estrella, new Color(1f, 0.95f, 0.6f, 1f));
                FabricaUIArcade.Colocar(estrella.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 28f), new Vector2(70f, 70f));
                Text sello = FabricaUIArcade.CrearTexto(medalla.transform, "Texto", "NIVEL FINAL\nCOMPLETADO", 20, new Color(0.35f, 0.2f, 0.02f, 1f), TextAnchor.MiddleCenter, 0f, 0f);
                FabricaUIArcade.Colocar(sello.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -38f), new Vector2(170f, 56f));
            }

            Button continuar = FabricaUIArcade.CrearBoton(caja, "BotonContinuar", "CONTINUAR", kit.botonAmarillo,
                new Vector2(620f, 108f), 44, kit.iconoJugar, kit);
            FabricaUIArcade.Centrar((RectTransform)continuar.transform, new Vector2(0f, -340f), new Vector2(620f, 108f));
            continuar.onClick.AddListener(Cerrar);
            panel.seleccionInicial = continuar.gameObject;

            panel.OcultarInmediato();
        }
    }
}
