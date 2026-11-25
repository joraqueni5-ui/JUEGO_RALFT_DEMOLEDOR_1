using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Pantalla "ELIGE TU PERSONAJE": aparece al pulsar JUGAR o al elegir un nivel, antes de cargar el piso.
    /// Seis estudiantes (carreras distintas, mujeres y hombres) caminando en su tarjeta, con nombre, carrera y ventaja.
    /// Flechas o mouse para elegir, ENTER / clic para confirmar, ESC para volver. Se crea por código con el tema del menú.
    /// </summary>
    public class SelectorPersonaje : MonoBehaviour
    {
        private static SelectorPersonaje abierto;

        private Action alConfirmar;
        private int indice;
        private Image[] tarjetas, retratos, marcos;
        private Text textoVentaja;
        private Sprite[][] animaciones;
        private float reloj, inicio;
        private bool cerrado;

        public static bool Abierto { get { return abierto != null; } }

        // ------------------------------------------------------------ multijugador
        /// <summary>Multijugador: devuelve true si ese personaje ya lo eligió el compañero (no se puede repetir).</summary>
        public static Func<int, bool> Ocupado;
        /// <summary>Texto de estado abajo ("ESPERANDO A TU COMPAÑERO...", "TU COMPAÑERO ELIGIÓ A ...").</summary>
        public static string Estado = "";

        private bool multi, esperando;
        private Action<int> alElegirMulti;
        private Action alCancelarMulti;
        private Text textoEstado, textoBoton;
        private Text[] etiquetasOcupado;
        private float finError;

        /// <summary>
        /// Selector del multijugador: al confirmar llama 'alElegir(indice)' y se queda abierto esperando al compañero.
        /// No deja elegir un personaje ocupado. La red lo cierra con CerrarAbierto o lo libera con Desbloquear.
        /// </summary>
        public static void MostrarMulti(Action<int> alElegir, Action alCancelar)
        {
            if (abierto != null) return;
            Canvas canvas = FabricaUIArcade.CrearCanvas("SelectorPersonaje", 900);
            FabricaUIArcade.AsegurarEventSystem();
            abierto = canvas.gameObject.AddComponent<SelectorPersonaje>();
            abierto.multi = true;
            abierto.alElegirMulti = alElegir;
            abierto.alCancelarMulti = alCancelar;
            abierto.Construir();
        }

        /// <summary>Cierra el selector abierto (por ejemplo cuando empieza la partida en red).</summary>
        public static void CerrarAbierto()
        {
            if (abierto != null) { abierto.cerrado = true; abierto.Cerrar(); }
            Ocupado = null;
            Estado = "";
        }

        /// <summary>El anfitrión rechazó la elección (los dos tomaron el mismo a la vez): se puede elegir otra vez.</summary>
        public static void Desbloquear(string motivo)
        {
            if (abierto == null) return;
            abierto.esperando = false;
            abierto.finError = Time.unscaledTime + 2.5f;
            Estado = motivo;
            abierto.Refrescar();
        }

        /// <summary>Muestra el selector; 'continuar' se llama al confirmar (con el personaje ya guardado).</summary>
        public static void Mostrar(Action continuar)
        {
            if (abierto != null) return;
            Canvas canvas = FabricaUIArcade.CrearCanvas("SelectorPersonaje", 900);
            FabricaUIArcade.AsegurarEventSystem();
            abierto = canvas.gameObject.AddComponent<SelectorPersonaje>();
            abierto.alConfirmar = continuar;
            abierto.Construir();
        }

        private void Construir()
        {
            KitSpritesArcade kit = KitSpritesArcade.Actual();
            Transform raiz = transform;
            indice = Personajes.Indice;
            inicio = Time.unscaledTime;

            Image velo = FabricaUIArcade.CrearImagen(raiz, "Velo", null, new Color(0.02f, 0.03f, 0.08f, 0.88f), true);
            FabricaUIArcade.Estirar(velo.rectTransform);

            Text titulo = FabricaUIArcade.CrearTexto(raiz, "Titulo", "ELIGE TU PERSONAJE", 64, PaletaArcade.Amarillo);
            FabricaUIArcade.Centrar(titulo.rectTransform, new Vector2(0f, 470f), new Vector2(1600f, 90f));
            Text sub = FabricaUIArcade.CrearTexto(raiz, "Subtitulo", "ESTUDIANTES DE LA U CONTRA EL DEMOLEDOR", 26, PaletaArcade.Crema, TextAnchor.MiddleCenter, 3f, 0f, true);
            FabricaUIArcade.Centrar(sub.rectTransform, new Vector2(0f, 410f), new Vector2(1600f, 40f));

            int n = Personajes.Elegibles.Length;
            tarjetas = new Image[n];
            retratos = new Image[n];
            marcos = new Image[n];
            animaciones = new Sprite[n][];
            const float ancho = 300f, alto = 320f, separacion = 30f;
            int porFila = Mathf.CeilToInt(n / 2f);
            float x0 = -(porFila - 1) * (ancho + separacion) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                Personajes.Ficha f = Personajes.Elegibles[i];
                int k = i;
                Image tarjeta = FabricaUIArcade.CrearImagen(raiz, "Tarjeta_" + f.id, kit.panel, Color.white, true);
                int fila = i / porFila, col = i % porFila;
                FabricaUIArcade.Centrar(tarjeta.rectTransform, new Vector2(x0 + col * (ancho + separacion), 180f - fila * (alto + 22f)), new Vector2(ancho, alto));
                Button b = tarjeta.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => Clic(k));
                AgregarEntrar(tarjeta.gameObject, k);
                tarjetas[i] = tarjeta;

                Image marco = FabricaUIArcade.CrearImagen(tarjeta.transform, "Marco", kit.marcoSeleccion, PaletaArcade.Amarillo);
                FabricaUIArcade.Estirar(marco.rectTransform);
                marco.rectTransform.offsetMin = new Vector2(-10f, -10f);
                marco.rectTransform.offsetMax = new Vector2(10f, 10f);
                marcos[i] = marco;

                animaciones[i] = Personajes.SerieFluida(f, "abajo");
                Image retrato = FabricaUIArcade.CrearImagen(tarjeta.transform, "Retrato", animaciones[i] != null ? animaciones[i][0] : null, Color.white);
                retrato.preserveAspect = true;
                FabricaUIArcade.Centrar(retrato.rectTransform, new Vector2(0f, 45f), new Vector2(130f, 190f));
                retratos[i] = retrato;

                Text nombre = FabricaUIArcade.CrearTexto(tarjeta.transform, "Nombre", f.nombre, 28, PaletaArcade.Crema);
                FabricaUIArcade.Centrar(nombre.rectTransform, new Vector2(0f, -78f), new Vector2(ancho - 20f, 36f));
                Text carrera = FabricaUIArcade.CrearTexto(tarjeta.transform, "Carrera", f.carrera, 18, f.plantel ? PaletaArcade.Naranja : PaletaArcade.Amarillo, TextAnchor.MiddleCenter, 2f, 0f, true);
                FabricaUIArcade.Centrar(carrera.rectTransform, new Vector2(0f, -118f), new Vector2(ancho - 16f, 44f));
                carrera.horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            textoVentaja = FabricaUIArcade.CrearTexto(raiz, "Ventaja", "", 32, PaletaArcade.Crema, TextAnchor.MiddleCenter, 3f, 3f);
            FabricaUIArcade.Centrar(textoVentaja.rectTransform, new Vector2(0f, -360f), new Vector2(1700f, 50f));

            if (multi)
            {
                titulo.text = "MULTIJUGADOR: ELIGE TU PERSONAJE";
                sub.text = "NO PUEDEN SER EL MISMO: CADA UNO UNA CARRERA DISTINTA";
                textoEstado = FabricaUIArcade.CrearTexto(raiz, "Estado", "", 28, PaletaArcade.Amarillo, TextAnchor.MiddleCenter, 3f, 0f, true);
                FabricaUIArcade.Centrar(textoEstado.rectTransform, new Vector2(0f, -305f), new Vector2(1700f, 44f));
                etiquetasOcupado = new Text[n];
                for (int i = 0; i < n; i++)
                {
                    Text t = FabricaUIArcade.CrearTexto(tarjetas[i].transform, "Ocupado", "TU COMPAÑERO", 26, PaletaArcade.Naranja, TextAnchor.MiddleCenter, 3f, 0f, true);
                    FabricaUIArcade.Centrar(t.rectTransform, new Vector2(0f, 140f), new Vector2(ancho, 36f));
                    t.enabled = false;
                    etiquetasOcupado[i] = t;
                }
            }

            Button jugar = FabricaUIArcade.CrearBoton(raiz, "BotonJugar", "¡A JUGAR!", kit.botonAmarillo, new Vector2(460f, 100f), 40, kit.iconoJugar);
            FabricaUIArcade.Centrar((RectTransform)jugar.transform, new Vector2(170f, -460f), new Vector2(460f, 100f));
            jugar.onClick.AddListener(Confirmar);
            textoBoton = jugar.GetComponentInChildren<Text>();
            if (multi && textoBoton != null) textoBoton.text = "¡LISTO!";
            Button volver = FabricaUIArcade.CrearBoton(raiz, "BotonVolver", "VOLVER", kit.botonRojo, new Vector2(320f, 100f), 36, kit.iconoCasa);
            FabricaUIArcade.Centrar((RectTransform)volver.transform, new Vector2(-260f, -460f), new Vector2(320f, 100f));
            volver.onClick.AddListener(Cancelar);
            jugar.navigation = new Navigation { mode = Navigation.Mode.None };
            volver.navigation = new Navigation { mode = Navigation.Mode.None };
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(jugar.gameObject);

            Refrescar();
        }

        private void AgregarEntrar(GameObject g, int i)
        {
            EventTrigger t = g.AddComponent<EventTrigger>();
            EventTrigger.Entry e = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            e.callback.AddListener(_ => Elegir(i, false));
            t.triggers.Add(e);
        }

        private void Clic(int i)
        {
            if (i == indice) Confirmar();
            else Elegir(i, true);
        }

        private void Elegir(int i, bool sonido)
        {
            if (esperando) return;
            int n = Personajes.Elegibles.Length;
            i = (i % n + n) % n;
            if (i == indice) return;
            indice = i;
            if (sonido) GestorAudio.Reproducir(Snd.MenuMover);
            Refrescar();
        }

        private void Refrescar()
        {
            for (int i = 0; i < tarjetas.Length; i++)
            {
                bool sel = i == indice;
                marcos[i].enabled = sel;
                tarjetas[i].color = sel ? Color.white : new Color(0.65f, 0.68f, 0.78f, 1f);
                retratos[i].color = sel ? Color.white : new Color(0.75f, 0.75f, 0.8f, 1f);
            }
            Personajes.Ficha f = Personajes.Elegibles[indice];
            textoVentaja.text = f.nombre + "  ·  VENTAJA:  " + f.ventaja;
            if (!multi) return;
            for (int i = 0; i < tarjetas.Length; i++)
            {
                bool ocupado = Ocupado != null && Ocupado(i);
                etiquetasOcupado[i].enabled = ocupado;
                if (ocupado)
                {
                    tarjetas[i].color = new Color(0.35f, 0.3f, 0.32f, 1f);
                    retratos[i].color = new Color(0.4f, 0.4f, 0.45f, 0.8f);
                }
                if (esperando && i == indice) marcos[i].color = PaletaArcade.Naranja;
                else marcos[i].color = PaletaArcade.Amarillo;
            }
            if (textoBoton != null) textoBoton.text = esperando ? "ESPERANDO..." : "¡LISTO!";
            if (textoEstado != null)
            {
                string e = Estado;
                if (string.IsNullOrEmpty(e)) e = esperando ? "ESPERANDO A TU COMPAÑERO..." : "ELIGE Y PULSA ¡LISTO!";
                textoEstado.text = e;
                textoEstado.color = Time.unscaledTime < finError ? new Color(1f, 0.4f, 0.35f) : PaletaArcade.Amarillo;
            }
        }

        private void Confirmar()
        {
            if (cerrado || Time.unscaledTime - inicio < 0.2f) return;   // evita que el mismo ENTER que abrió el selector lo cierre
            if (multi)
            {
                if (esperando) return;
                if (Ocupado != null && Ocupado(indice))
                {
                    GestorAudio.Reproducir(Snd.MenuVolver);
                    Estado = "¡ESE YA LO ELIGIÓ TU COMPAÑERO! ELIGE OTRO";
                    finError = Time.unscaledTime + 2.5f;
                    Refrescar();
                    return;
                }
                esperando = true;
                Estado = "";
                Personajes.Indice = indice;
                GestorAudio.Reproducir(Snd.MenuConfirmar);
                Refrescar();
                if (alElegirMulti != null) alElegirMulti(indice);
                return;
            }
            cerrado = true;
            Personajes.Indice = indice;
            GestorAudio.Reproducir(Snd.MenuConfirmar);
            Action seguir = alConfirmar;
            Cerrar();
            if (seguir != null) seguir();
        }

        private void Cancelar()
        {
            if (cerrado) return;
            cerrado = true;
            GestorAudio.Reproducir(Snd.MenuVolver);
            Action cancelar = alCancelarMulti;
            Cerrar();
            if (multi) { Ocupado = null; Estado = ""; }
            if (cancelar != null) cancelar();
        }

        private void Cerrar()
        {
            if (abierto == this) abierto = null;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (abierto == this) abierto = null;
        }

        private void Update()
        {
            reloj += Time.unscaledDeltaTime;
            if (multi) Refrescar();   // la elección del compañero llega por la red en cualquier momento
            // Animación de caminar en las tarjetas (la elegida camina más rápido y rebota).
            for (int i = 0; i < retratos.Length; i++)
            {
                Sprite[] a = animaciones[i];
                if (a == null) continue;
                float cps = (i == indice ? 8f : 3f) * Personajes.FactorCuadros(a);
                retratos[i].sprite = a[(int)(reloj * cps) % a.Length];
                float salto = i == indice ? Mathf.Abs(Mathf.Sin(reloj * 8f)) * 8f : 0f;
                retratos[i].rectTransform.anchoredPosition = new Vector2(0f, 45f + salto);
            }

            Keyboard k = Keyboard.current;
            if (k == null) return;
            if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) Elegir(indice - 1, true);
            if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) Elegir(indice + 1, true);
            int salto6 = Mathf.CeilToInt(Personajes.Elegibles.Length / 2f);
            if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) Elegir(indice - salto6, true);
            if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) Elegir(indice + salto6, true);
            if (k.escapeKey.wasPressedThisFrame) Cancelar();
            else if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) Confirmar();
        }
    }
}
