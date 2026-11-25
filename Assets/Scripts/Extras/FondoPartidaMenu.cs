using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Fondo del menú como una mini partida que se juega sola (modo demostración): un aula y un pasillo vistos desde
    /// arriba, el demoledor rompiendo pupitres y escritorios, un estudiante o docente al azar reparándolos con sus
    /// pensamientos en un globo, y de vez en cuando un mini villano que el estudiante ahuyenta. Todo con el arte del juego.
    /// La agrega TemaSerio en lugar del muro del menú. Las piezas están en Resources/Extras/Menu y Extras/Personajes.
    /// </summary>
    public class FondoPartidaMenu : MonoBehaviour
    {
        private const float AnchoBaldosas = 20f, AltoBaldosas = 11.25f;

        private class Objeto
        {
            public RectTransform rt;
            public Image img, grieta;
            public Vector2 pos, tam;     // baldosas (arriba a la izquierda) y tamaño
            public int estado;           // 0 intacto, 1 dañado, 2 roto
        }

        private class Transeunte
        {
            public Actor actor;
            public Sprite[] abajo, arriba, derecha, abajoSusto, derechaSusto;
            public Vector2 destino;
            public float pausa;
            public bool asustado;
        }

        private class Actor
        {
            public RectTransform rt;
            public Image img;
            public Vector2 pos;          // pies, en baldosas
            public float alto, velocidad;
            public bool izquierda;
            public float reloj;
        }

        private RectTransform area, capa;
        private readonly List<Objeto> objetos = new List<Objeto>();
        private Actor demoledor, jugador, mini;
        private readonly List<Transeunte> gente = new List<Transeunte>();
        private Sprite[] demCaminar, demGolpe, jugAbajo, jugArriba, jugDerecha, jugMartillo, miniCaminar, miniGolpe, poof;
        private Sprite grietaChica, grietaGrande;
        private Personajes.Ficha ficha;
        private float temblor;

        // Estados simples de cada actor.
        private Vector2 destDem, destJug, destMini;
        private Objeto objDem, objJug, objMini;
        private float finAccionDem, finAccionJug, finAccionMini, proximoMini, proximaFrase;
        private bool golpeandoDem, martillandoJug, golpeandoMini, impactoDem;

        // Globo del estudiante.
        private RectTransform globo;
        private Text textoGlobo;
        private float finGlobo;

        private float P { get { return area.rect.height / AltoBaldosas; } }

        private void Start()
        {
            area = (RectTransform)transform;
            capa = new GameObject("PartidaDemo", typeof(RectTransform)).GetComponent<RectTransform>();
            capa.SetParent(area, false);
            capa.anchorMin = Vector2.zero;
            capa.anchorMax = Vector2.one;
            capa.offsetMin = capa.offsetMax = Vector2.zero;

            demCaminar = ArteExtra.Serie("Menu/demoledor_caminar_", 6);
            demGolpe = ArteExtra.Serie("Menu/demoledor_golpe_", 6);
            ficha = Personajes.EstudianteAlAzar();
            jugAbajo = Personajes.Serie(ficha, "abajo");
            jugArriba = Personajes.Serie(ficha, "arriba");
            jugDerecha = Personajes.Serie(ficha, "derecha");
            jugMartillo = Personajes.Serie(ficha, "martillo");
            miniCaminar = ArteExtra.Lista("Mini/aprendiz_naranja/", "lado_0", "lado_1", "lado_2", "lado_3");
            miniGolpe = ArteExtra.Lista("Mini/aprendiz_naranja/", "golpe_0", "golpe_1");
            poof = ArteExtra.Serie("Efectos/humo_poof_", 4);
            grietaChica = ArteExtra.Uno("Menu/grieta_chica");
            grietaGrande = ArteExtra.Uno("Menu/grieta_grande");
            if (demCaminar == null || jugDerecha == null) { enabled = false; return; }

            CrearObjeto("Menu/obj_pupitres3", new Vector2(7f, 4f), new Vector2(3f, 1f));
            CrearObjeto("Menu/obj_pupitres2", new Vector2(8f, 7.5f), new Vector2(2f, 1f));
            CrearObjeto("Menu/obj_escritorio", new Vector2(14f, 4f), new Vector2(2f, 1f));
            CrearObjeto("Menu/obj_pupitres3", new Vector2(14f, 8f), new Vector2(3f, 1f));
            CrearObjeto("Menu/obj_lavamanos", new Vector2(18f, 6f), new Vector2(1f, 1f));

            // Gente de la universidad (estudiantes y plantel) que pasa por el pasillo y huye si el demoledor se acerca.
            for (int i = 0; i < 4; i++)
            {
                Personajes.Ficha f = Personajes.AlAzar();
                Transeunte t = new Transeunte
                {
                    abajo = Personajes.Serie(f, "abajo"), arriba = Personajes.Serie(f, "arriba"), derecha = Personajes.Serie(f, "derecha"),
                    abajoSusto = ArteExtra.Serie("Personajes/" + f.id + "/abajo_susto_", 4),
                    derechaSusto = ArteExtra.Serie("Personajes/" + f.id + "/derecha_susto_", 4),
                    destino = PuntoAlAzar(), pausa = Random.Range(0f, 3f)
                };
                if (t.abajo == null || t.derecha == null || t.arriba == null) continue;
                t.actor = CrearActor("Persona", t.abajo[0], PuntoAlAzar(), 1.2f, Random.Range(1f, 1.6f));
                gente.Add(t);
            }

            demoledor = CrearActor("Demoledor", demCaminar[0], new Vector2(16f, 10.6f), 2.1f, 1.7f);
            jugador = CrearActor("Estudiante", jugAbajo[0], new Vector2(10f, 10.2f), 1.3f, 3.2f);

            // Velo para que el menú se lea encima de la partida.
            Image velo = new GameObject("Velo", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            velo.transform.SetParent(capa, false);
            velo.rectTransform.anchorMin = Vector2.zero;
            velo.rectTransform.anchorMax = Vector2.one;
            velo.rectTransform.offsetMin = velo.rectTransform.offsetMax = Vector2.zero;
            velo.color = new Color(0.03f, 0.04f, 0.1f, 0.38f);
            velo.raycastTarget = false;

            CrearGlobo();
            proximoMini = Time.unscaledTime + 8f;
            proximaFrase = Time.unscaledTime + 2f;
            ElegirObjetivoDemoledor();
        }

        // ------------------------------------------------------------ creación
        private void CrearObjeto(string sprite, Vector2 pos, Vector2 tam)
        {
            Objeto o = new Objeto { pos = pos, tam = tam };
            o.img = new GameObject("Objeto", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            o.rt = o.img.rectTransform;
            o.rt.SetParent(capa, false);
            o.rt.anchorMin = o.rt.anchorMax = new Vector2(0f, 1f);
            o.rt.pivot = new Vector2(0f, 1f);
            o.img.sprite = ArteExtra.Uno(sprite);
            o.img.raycastTarget = false;
            o.grieta = new GameObject("Grieta", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            o.grieta.rectTransform.SetParent(o.rt, false);
            o.grieta.rectTransform.anchorMin = Vector2.zero;
            o.grieta.rectTransform.anchorMax = Vector2.one;
            o.grieta.rectTransform.offsetMin = o.grieta.rectTransform.offsetMax = Vector2.zero;
            o.grieta.raycastTarget = false;
            o.grieta.enabled = false;
            objetos.Add(o);
        }

        private Actor CrearActor(string nombre, Sprite s, Vector2 pos, float alto, float velocidad)
        {
            Actor a = new Actor { pos = pos, alto = alto, velocidad = velocidad };
            a.img = new GameObject(nombre, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            a.rt = a.img.rectTransform;
            a.rt.SetParent(capa, false);
            a.rt.anchorMin = a.rt.anchorMax = new Vector2(0f, 1f);
            a.rt.pivot = new Vector2(0.5f, 0f);
            a.img.sprite = s;
            a.img.preserveAspect = true;
            a.img.raycastTarget = false;
            return a;
        }

        private void CrearGlobo()
        {
            Image fondo = new GameObject("Globo", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            globo = fondo.rectTransform;
            globo.SetParent(capa, false);
            globo.anchorMin = globo.anchorMax = new Vector2(0f, 1f);
            globo.pivot = new Vector2(0.5f, 0f);
            fondo.sprite = ArteExtra.Uno("Tema/burbuja");
            fondo.type = Image.Type.Sliced;
            fondo.pixelsPerUnitMultiplier = 1f / 4f;
            fondo.raycastTarget = false;
            textoGlobo = FabricaUIArcade.CrearTexto(globo, "Texto", "", 22, new Color(0.08f, 0.09f, 0.16f), TextAnchor.MiddleCenter, 0f, 0f, true);
            textoGlobo.rectTransform.anchorMin = Vector2.zero;
            textoGlobo.rectTransform.anchorMax = Vector2.one;
            textoGlobo.rectTransform.offsetMin = new Vector2(14f, 10f);
            textoGlobo.rectTransform.offsetMax = new Vector2(-14f, -10f);
            textoGlobo.horizontalOverflow = HorizontalWrapMode.Wrap;
            globo.gameObject.SetActive(false);
        }

        private void Decir(string frase)
        {
            if (string.IsNullOrEmpty(frase)) return;
            textoGlobo.text = frase;
            float ancho = Mathf.Clamp(frase.Length * 13f + 40f, 160f, 420f);
            float alto = frase.Length * 13f > 380f ? 96f : 62f;
            globo.sizeDelta = new Vector2(ancho, alto);
            globo.gameObject.SetActive(true);
            finGlobo = Time.unscaledTime + 2.2f + frase.Length / 16f;
        }

        // ------------------------------------------------------------ ciclo
        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            ActualizarDemoledor(dt);
            ActualizarJugador(dt);
            ActualizarMini(dt);
            ActualizarGente(dt);

            if (Time.unscaledTime >= proximaFrase && !globo.gameObject.activeSelf)
            {
                proximaFrase = Time.unscaledTime + Random.Range(7f, 12f);
                Decir(Frases.Una(Random.value < 0.5f ? Frases.Ocioso : Frases.Inicio));
            }
            if (globo.gameObject.activeSelf && Time.unscaledTime > finGlobo) globo.gameObject.SetActive(false);

            Colocar();
        }

        private bool Caminar(Actor a, Vector2 destino, float dt)
        {
            Vector2 d = destino - a.pos;
            float paso = a.velocidad * dt;
            if (Mathf.Abs(d.x) > 0.05f) a.izquierda = d.x < 0f;
            if (d.magnitude <= paso) { a.pos = destino; return true; }
            a.pos += d.normalized * paso;
            a.reloj += dt;
            return false;
        }

        private static Vector2 Lado(Objeto o, bool derecha)
        {
            float y = o.pos.y + o.tam.y - 0.05f;
            return derecha ? new Vector2(o.pos.x + o.tam.x + 0.7f, y) : new Vector2(o.pos.x - 0.7f, y);
        }

        private static Vector2 PuntoAlAzar()
        {
            return new Vector2(Random.Range(6.5f, 19f), Random.Range(3.8f, 10.8f));
        }

        // ------------------------------------------------------------ demoledor
        private void ElegirObjetivoDemoledor()
        {
            List<Objeto> sanos = objetos.FindAll(o => o.estado < 2);
            if (sanos.Count == 0 || Random.value < 0.25f) { objDem = null; destDem = PuntoAlAzar(); return; }
            objDem = sanos[Random.Range(0, sanos.Count)];
            destDem = Lado(objDem, Random.value < 0.5f);
        }

        private void ActualizarDemoledor(float dt)
        {
            if (golpeandoDem)
            {
                float t = 1f - (finAccionDem - Time.unscaledTime) / 0.6f;
                int c = Mathf.Clamp((int)(t * demGolpe.Length), 0, demGolpe.Length - 1);
                demoledor.img.sprite = demGolpe[c];
                if (!impactoDem && c >= 3)
                {
                    impactoDem = true;
                    if (objDem != null && objDem.estado < 2) { objDem.estado++; temblor = 0.25f; Poof(objDem); }
                }
                if (Time.unscaledTime >= finAccionDem)
                {
                    golpeandoDem = false;
                    finAccionDem = Time.unscaledTime + Random.Range(0.4f, 1.4f);
                    ElegirObjetivoDemoledor();
                }
                return;
            }
            if (Time.unscaledTime < finAccionDem) { demoledor.img.sprite = demCaminar[0]; return; }

            if (Caminar(demoledor, destDem, dt))
            {
                if (objDem != null && objDem.estado < 2)
                {
                    golpeandoDem = true;
                    impactoDem = false;
                    demoledor.izquierda = demoledor.pos.x > objDem.pos.x + objDem.tam.x * 0.5f;
                    finAccionDem = Time.unscaledTime + 0.6f;
                }
                else
                {
                    finAccionDem = Time.unscaledTime + Random.Range(0.3f, 1f);
                    ElegirObjetivoDemoledor();
                }
            }
            demoledor.img.sprite = demCaminar[(int)(demoledor.reloj * 9f) % demCaminar.Length];
        }

        // ------------------------------------------------------------ estudiante
        private void ActualizarJugador(float dt)
        {
            if (martillandoJug)
            {
                float t = 1f - (finAccionJug - Time.unscaledTime) / 0.7f;
                jugador.img.sprite = jugMartillo[(int)(Mathf.Clamp01(t) * jugMartillo.Length * 2f) % jugMartillo.Length];
                if (Time.unscaledTime < finAccionJug) return;
                martillandoJug = false;
                if (objJug == null && mini != null && Vector2.Distance(mini.pos, jugador.pos) < 1.6f)
                {
                    Poof(mini.pos + new Vector2(0f, -0.4f));
                    Destroy(mini.rt.gameObject);
                    mini = null;
                    objMini = null;
                    Decir(Frases.Una(Frases.MiniVencido));
                }
                else if (objJug != null && objJug.estado > 0)
                {
                    objJug.estado--;
                    if (objJug.estado == 0 && Random.value < 0.5f) Decir(Frases.Una(Frases.Reparar));
                }
                objJug = null;
                finAccionJug = Time.unscaledTime + Random.Range(0.2f, 0.6f);
                return;
            }
            if (Time.unscaledTime < finAccionJug) { jugador.img.sprite = jugAbajo[0]; return; }

            // Prioridad: el mini villano que está rompiendo; si no, el objeto dañado más cercano; si no, pasear.
            Vector2 destino;
            if (mini != null && golpeandoMini) { objJug = null; destino = mini.pos + new Vector2(mini.izquierda ? -0.8f : 0.8f, 0f); }
            else
            {
                if (objJug == null || objJug.estado == 0) objJug = DanadoMasCercano();
                if (objJug == null && (destJug == Vector2.zero || Vector2.Distance(jugador.pos, destJug) < 0.1f)) destJug = PuntoAlAzar();
                destino = objJug != null ? Lado(objJug, jugador.pos.x > objJug.pos.x + objJug.tam.x * 0.5f) : destJug;
            }

            bool llego = Caminar(jugador, destino, dt);
            Vector2 dir = destino - jugador.pos;
            Sprite[] serie = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y) ? jugDerecha : (dir.y < 0f ? jugArriba : jugAbajo);
            jugador.img.sprite = serie[(int)(jugador.reloj * 8f) % serie.Length];

            if (llego && (objJug != null || (mini != null && golpeandoMini)))
            {
                martillandoJug = true;
                finAccionJug = Time.unscaledTime + 0.7f;
                if (objJug != null) jugador.izquierda = jugador.pos.x > objJug.pos.x + objJug.tam.x * 0.5f;
            }
            else if (llego) finAccionJug = Time.unscaledTime + Random.Range(0.5f, 1.5f);
        }

        private Objeto DanadoMasCercano()
        {
            Objeto mejor = null;
            float md = float.MaxValue;
            foreach (Objeto o in objetos)
            {
                if (o.estado == 0 || o == objDem) continue;
                float d = Vector2.Distance(jugador.pos, o.pos);
                if (d < md) { md = d; mejor = o; }
            }
            return mejor;
        }

        // ------------------------------------------------------------ mini villano
        private void ActualizarMini(float dt)
        {
            if (mini == null)
            {
                if (Time.unscaledTime < proximoMini || miniCaminar == null) return;
                proximoMini = Time.unscaledTime + Random.Range(12f, 18f);
                mini = CrearActor("Mini", miniCaminar[0], demoledor.pos + new Vector2(1f, 0.2f), 0.85f, 2.1f);
                List<Objeto> sanos = objetos.FindAll(o => o.estado < 2);
                objMini = sanos.Count > 0 ? sanos[Random.Range(0, sanos.Count)] : null;
                destMini = objMini != null ? Lado(objMini, Random.value < 0.5f) : PuntoAlAzar();
                golpeandoMini = false;
                return;
            }
            if (golpeandoMini)
            {
                mini.img.sprite = miniGolpe[((int)(Time.unscaledTime * 4f)) % miniGolpe.Length];
                if (Time.unscaledTime >= finAccionMini)
                {
                    if (objMini != null && objMini.estado < 2) { objMini.estado++; Poof(objMini); }
                    finAccionMini = Time.unscaledTime + 1.6f;
                }
                return;
            }
            if (Caminar(mini, destMini, dt))
            {
                golpeandoMini = objMini != null;
                finAccionMini = Time.unscaledTime + 1.2f;
                if (!golpeandoMini) destMini = PuntoAlAzar();
            }
            mini.img.sprite = miniCaminar[(int)(mini.reloj * 9f) % miniCaminar.Length];
        }

        // ------------------------------------------------------------ gente
        private void ActualizarGente(float dt)
        {
            foreach (Transeunte t in gente)
            {
                Actor a = t.actor;
                float lejos = Vector2.Distance(a.pos, demoledor.pos);
                if (!t.asustado && lejos < 2.6f)
                {
                    // ¡Se acerca el demoledor! Cara de susto y a correr en sentido contrario.
                    t.asustado = true;
                    Vector2 huida = a.pos + (a.pos - demoledor.pos).normalized * 5f;
                    t.destino = new Vector2(Mathf.Clamp(huida.x, 6.5f, 19f), Mathf.Clamp(huida.y, 3.8f, 10.8f));
                    a.velocidad = 3.8f;
                    t.pausa = 0f;
                }
                if (t.pausa > 0f) { t.pausa -= dt; a.img.sprite = t.abajo[0]; continue; }

                Vector2 dir = t.destino - a.pos;
                if (Caminar(a, t.destino, dt))
                {
                    t.asustado = false;
                    a.velocidad = Random.Range(1f, 1.6f);
                    t.destino = PuntoAlAzar();
                    t.pausa = Random.Range(0.8f, 3f);
                }
                bool lado = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);
                Sprite[] serie = lado ? (t.asustado && t.derechaSusto != null ? t.derechaSusto : t.derecha)
                                      : (dir.y < 0f ? t.arriba : (t.asustado && t.abajoSusto != null ? t.abajoSusto : t.abajo));
                a.img.sprite = serie[(int)(a.reloj * (t.asustado ? 12f : 7f)) % serie.Length];
            }
        }

        // ------------------------------------------------------------ efectos y dibujo
        private void Poof(Objeto o) { Poof(o.pos + new Vector2(o.tam.x * 0.5f, o.tam.y * 0.5f)); }

        private void Poof(Vector2 pos)
        {
            if (poof == null) return;
            Image img = new GameObject("Poof", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.rectTransform.SetParent(capa, false);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0f, 1f);
            img.rectTransform.anchoredPosition = new Vector2(pos.x * P, -pos.y * P);
            img.rectTransform.sizeDelta = new Vector2(P * 1.2f, P * 1.2f);
            img.raycastTarget = false;
            img.sprite = poof[0];
            AnimadorImagenUI anim = img.gameObject.AddComponent<AnimadorImagenUI>();
            anim.cuadros = poof;
            anim.cuadrosPorSegundo = 12f;
            Destroy(img.gameObject, 0.4f);
        }

        private void Colocar()
        {
            float p = P;
            Vector2 sacudida = temblor > 0f ? Random.insideUnitCircle * 6f * temblor : Vector2.zero;
            temblor = Mathf.Max(0f, temblor - Time.unscaledDeltaTime);
            capa.anchoredPosition = sacudida;

            // Orden de dibujo: lo que está más abajo en pantalla va encima.
            List<KeyValuePair<float, RectTransform>> orden = new List<KeyValuePair<float, RectTransform>>();
            foreach (Objeto o in objetos)
            {
                o.rt.anchoredPosition = new Vector2(o.pos.x * p, -o.pos.y * p);
                o.rt.sizeDelta = new Vector2(o.tam.x * p, o.tam.y * p);
                o.grieta.enabled = o.estado > 0;
                o.grieta.sprite = o.estado >= 2 ? grietaGrande : grietaChica;
                o.img.color = o.estado >= 2 ? new Color(0.7f, 0.66f, 0.62f) : Color.white;
                orden.Add(new KeyValuePair<float, RectTransform>(o.pos.y + o.tam.y, o.rt));
            }
            List<Actor> actores = new List<Actor> { demoledor, jugador, mini };
            foreach (Transeunte t in gente) actores.Add(t.actor);
            foreach (Actor a in actores)
            {
                if (a == null) continue;
                Sprite s = a.img.sprite;
                float aspecto = s != null && s.rect.height > 0 ? s.rect.width / s.rect.height : 0.5f;
                a.rt.sizeDelta = new Vector2(a.alto * aspecto * p, a.alto * p);
                a.rt.anchoredPosition = new Vector2(a.pos.x * p, -a.pos.y * p);
                a.rt.localScale = new Vector3(a.izquierda ? -1f : 1f, 1f, 1f);
                orden.Add(new KeyValuePair<float, RectTransform>(a.pos.y, a.rt));
            }
            orden.Sort((x, y) => x.Key.CompareTo(y.Key));
            for (int i = 0; i < orden.Count; i++) orden[i].Value.SetSiblingIndex(i);

            // El velo y el globo siempre arriba de todo.
            Transform velo = capa.Find("Velo");
            if (velo != null) velo.SetAsLastSibling();
            if (globo.gameObject.activeSelf)
            {
                globo.anchoredPosition = new Vector2(jugador.pos.x * p, -(jugador.pos.y - jugador.alto - 0.25f) * p);
                globo.SetAsLastSibling();
            }
        }
    }
}
