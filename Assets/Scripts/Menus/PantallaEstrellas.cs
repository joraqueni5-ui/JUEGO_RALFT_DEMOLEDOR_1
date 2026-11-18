using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Pantalla de resultado al superar un piso del modo cenital: 1 a 3 estrellas y cuáles condiciones se
    /// cumplieron. Funciona con el juego congelado (tiempo sin escala) y se construye por código, igual que
    /// TransicionPiso. Se cierra con CONTINUAR, ENTER o ESPACIO.
    /// </summary>
    public class PantallaEstrellas : MonoBehaviour
    {
        private const float Px = FabricaUIArcade.Px;
        private const float EsperaAntesDeAceptarTecla = 0.6f;

        private PanelAnimado panel;
        private Text textoPuntos, textoRecord, textoArea;
        private Image[] estrellas;
        private Image[] marcas;
        private Text[] condiciones;
        private float horaApertura;

        public bool Abierta { get; private set; }
        public event Action Cerrada;

        public static PantallaEstrellas Crear(Transform padre)
        {
            GameObject objeto = new GameObject("PantallaEstrellas");
            objeto.transform.SetParent(padre, false);
            PantallaEstrellas p = objeto.AddComponent<PantallaEstrellas>();
            FabricaUIArcade.AsegurarEventSystem();
            p.Construir();
            return p;
        }

        /// <summary>Muestra el resultado: 'cumplidas' y 'descripciones' tienen una entrada por condición.</summary>
        public void Mostrar(int puntos, bool[] cumplidas, string[] descripciones, bool nuevoRecord, string nombreArea = "")
        {
            if (textoArea != null) textoArea.text = nombreArea;
            int total = 0;
            for (int i = 0; i < 3; i++)
            {
                bool ok = cumplidas != null && i < cumplidas.Length && cumplidas[i];
                if (ok) total++;
                Provisional.PonerEstrella(estrellas[i], ok, new Color(1f, 0.82f, 0.15f, 1f), new Color(0.22f, 0.25f, 0.33f, 0.95f));
                marcas[i].sprite = ok ? Provisional.Visto : Provisional.Circulo;
                marcas[i].color = ok ? new Color(0.35f, 0.9f, 0.45f, 1f) : new Color(0.5f, 0.52f, 0.6f, 0.8f);
                marcas[i].rectTransform.sizeDelta = ok ? new Vector2(44f, 44f) : new Vector2(20f, 20f);
                condiciones[i].text = descripciones != null && i < descripciones.Length ? descripciones[i] : "";
                condiciones[i].color = ok ? PaletaArcade.Crema : new Color(0.7f, 0.72f, 0.8f, 0.9f);
            }
            textoPuntos.text = "PUNTOS  " + puntos + "      ESTRELLAS  " + total + "/3";
            textoRecord.gameObject.SetActive(nuevoRecord);

            Abierta = true;
            horaApertura = Time.unscaledTime;
            panel.Mostrar();

            // Al superar el nivel suena el jingle de victoria (una vez) y cada estrella lograda con su sonido.
            GestorAudio.Jingle(Snd.JingleVictoria);
            StartCoroutine(SonidosDeEstrellas(total));
        }

        private System.Collections.IEnumerator SonidosDeEstrellas(int cuantas)
        {
            string[] nombres = { Snd.Estrella1, Snd.Estrella2, Snd.Estrella3 };
            for (int i = 0; i < cuantas && i < 3; i++)
            {
                yield return new WaitForSecondsRealtime(0.45f);
                GestorAudio.Reproducir(nombres[i]);
            }
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

        private void Construir()
        {
            KitSpritesArcade kit = KitSpritesArcade.Actual();

            Canvas canvas = FabricaUIArcade.CrearCanvas("CanvasPantallaEstrellas", 325);
            canvas.transform.SetParent(transform, false);

            RectTransform caja;
            panel = FabricaUIArcade.CrearVentana(canvas.transform, "PanelEstrellas", "¡NIVEL SUPERADO!",
                new Vector2(1100f, 860f), kit, out caja, null, kit.iconoArriba);

            // Tres estrellas grandes, la del medio más alta.
            estrellas = new Image[3];
            float[] xs = { -190f, 0f, 190f };
            float[] ys = { 215f, 245f, 215f };
            float[] lados = { 150f, 180f, 150f };
            for (int i = 0; i < 3; i++)
            {
                Image e = FabricaUIArcade.CrearImagen(caja, "Estrella" + (i + 1), Provisional.Estrella, Color.white);
                e.preserveAspect = true;
                FabricaUIArcade.Colocar(e.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(xs[i], ys[i]), new Vector2(lados[i], lados[i]));
                estrellas[i] = e;
            }

            textoArea = FabricaUIArcade.CrearTexto(caja, "Area", "", 34, PaletaArcade.Crema, TextAnchor.MiddleCenter, Px * 0.75f, 0f);
            FabricaUIArcade.Colocar(textoArea.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 112f), new Vector2(1000f, 44f));

            textoPuntos = FabricaUIArcade.CrearTexto(caja, "Puntos", "", 38, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, Px, Px);
            FabricaUIArcade.Colocar(textoPuntos.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 62f), new Vector2(1000f, 56f));

            textoRecord = FabricaUIArcade.CrearTexto(caja, "Record", "¡NUEVO RÉCORD!", 30, PaletaArcade.Verde, TextAnchor.MiddleCenter, Px * 0.75f, 0f);
            FabricaUIArcade.Colocar(textoRecord.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(600f, 36f));
            textoRecord.gameObject.SetActive(false);

            marcas = new Image[3];
            condiciones = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                float y = -52f - i * 90f;
                marcas[i] = FabricaUIArcade.CrearImagen(caja, "Marca" + (i + 1), Provisional.Circulo, Color.white);
                FabricaUIArcade.Colocar(marcas[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-470f, y), new Vector2(44f, 44f));

                Text t = FabricaUIArcade.CrearTexto(caja, "Condicion" + (i + 1), "", 28, PaletaArcade.Crema, TextAnchor.MiddleLeft, Px * 0.5f, 0f, true);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                FabricaUIArcade.Colocar(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(40f, y), new Vector2(880f, 80f));
                condiciones[i] = t;
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
